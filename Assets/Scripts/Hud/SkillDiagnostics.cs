using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Hud
{
    // Temporary diagnostics only. Nested timings are inclusive; total = frame paint + input.
    internal static partial class SkillDiagnostics
    {
        internal enum Area { Frame, Panel, Grid, Intrinsic, Potential, Detail, Input, InventoryBody, Other }
        private const int AreaCount = 9;
        private static readonly string[] Names = { "PaintInventoryFrame", "PaintSkillPanel", "grid", "intrinsic", "potential", "PaintSkillDetail", "UpdateSkillPanelInput", "inventoryBody", "other" };
        private static readonly double[] Pending = new double[AreaCount], Sums = new double[AreaCount], Maxima = new double[AreaCount];
        private static readonly int[] CopiesByArea = new int[AreaCount];
        private static readonly Dictionary<string,int> Exceptions = new Dictionary<string,int>();
        private static readonly HashSet<string> ReportedExceptions = new HashSet<string>();
        private static readonly double TickToMs = 1000d / Stopwatch.Frequency;
        private static int tab = -1, frame = -1, frames, copies, fullClears, sourceClears, entries, exceptionCount;
        private static long windowStart, lastKey;
        private static int lastFill, lastBorder, lastAlpha, lastSource, changedFields;
        private static bool havePreviousEntry;
        private static double totalSum, totalMax;
        private static Area currentArea = Area.Other, lastEntryArea = Area.Other;

        internal struct Scope : IDisposable
        {
            private readonly bool active;
            private readonly int scopeTab;
            private readonly Area area, previous;
            private readonly long started;
            internal Scope(int selectedTab, Area measured)
            {
                active = true; scopeTab = selectedTab; area = measured; previous = currentArea;
                currentArea = measured; started = Stopwatch.GetTimestamp();
            }
            public void Dispose()
            {
                if (!active) return;
                long elapsed = Stopwatch.GetTimestamp() - started;
                if (tab == scopeTab) Pending[(int)area] += elapsed * TickToMs;
                currentArea = previous;
            }
        }

        private static int ActiveTab()
        {
            Panel panel = GameCanvas.panel;
            return panel != null && panel.isShow && panel.InventoryFrameActive
                && (panel.currentTabIndex == 1 || panel.currentTabIndex == 2) ? panel.currentTabIndex : -1;
        }
        internal static Scope Measure(Area area)
        {
            Tick();
            return tab < 0 ? default(Scope) : new Scope(tab, area);
        }
        internal static void Tick()
        {
            int selected = ActiveTab();
            long now = Stopwatch.GetTimestamp();
            if (selected != tab) {
                // Start a clean interval on tab changes; never mix the two baselines.
                ResetWindow(); tab = selected; frame = -1; windowStart = now; currentArea = Area.Other; havePreviousEntry = false;
            }
            if (tab < 0 || frame == UnityEngine.Time.frameCount) return;
            if (frame >= 0) {
                CommitDeepFrame();
                double total = Pending[(int)Area.Frame] + Pending[(int)Area.Input];
                totalSum += total; totalMax = System.Math.Max(totalMax, total); frames++;
                for (int i = 0; i < AreaCount; i++) {
                    Sums[i] += Pending[i]; Maxima[i] = System.Math.Max(Maxima[i], Pending[i]); Pending[i] = 0;
                }
            }
            if (now - windowStart >= Stopwatch.Frequency && frames > 0) {
                Emit(now); ResetWindow(); windowStart = now;
            }
            frame = UnityEngine.Time.frameCount;
        }
        private static void ResetWindow()
        {
            ResetDeep();
            Array.Clear(Pending,0,AreaCount); Array.Clear(Sums,0,AreaCount); Array.Clear(Maxima,0,AreaCount);
            Array.Clear(CopiesByArea,0,AreaCount); Exceptions.Clear();
            frames = copies = fullClears = sourceClears = entries = exceptionCount = changedFields = 0;
            totalSum = totalMax = 0;
        }
        internal static void CopyNativeCalled()
        {
            Tick(); if (tab < 0) return;
            copies++; CopiesByArea[(int)currentArea]++;
        }
        internal static void CacheCleared(bool full)
        {
            Tick(); if (tab < 0) return;
            if (full) fullClears++; else sourceClears++;
        }
        internal static void CacheEntry(long key, int fill, int border, byte alpha, int source)
        {
            Tick(); if (tab < 0) return;
            if (havePreviousEntry) {
                if (fill != lastFill) changedFields |= 1;
                if (border != lastBorder) changedFields |= 2;
                if (alpha != lastAlpha) changedFields |= 4;
                if (source != lastSource) changedFields |= 8;
            }
            lastKey = key; lastFill = fill; lastBorder = border; lastAlpha = alpha; lastSource = source;
            havePreviousEntry = true; lastEntryArea = currentArea; entries++;
        }
        internal static void CaughtException(string site, Exception exception)
        {
            Tick(); if (tab < 0) return;
            string identity = exception.GetType().FullName + ": " + exception.Message;
            exceptionCount++;
            Exceptions.TryGetValue(identity,out int count); Exceptions[identity] = count + 1;
            if (ReportedExceptions.Add(identity))
                UnityEngine.Debug.LogWarning("[SKILLDIAG] exception tab=" + TabName + " site=" + site + " " + identity + "\n" + exception.StackTrace);
        }
        private static string TabName => tab == 2 ? "Skills" : "Inventory";
        private static string Number(double value) => value.ToString("F3",CultureInfo.InvariantCulture);
        private static void Timing(StringBuilder text, string name, double sum, double maximum)
        {
            text.Append(' ').Append(name).Append('=').Append(Number(sum / frames)).Append('/').Append(Number(maximum));
        }
        private static void Emit(long now)
        {
            var text = new StringBuilder(640);
            text.Append("[SKILLDIAG] tab=").Append(TabName).Append(" windowMs=").Append(Number((now-windowStart)*TickToMs))
                .Append(" frames=").Append(frames).Append(" ms(avg/max)");
            Timing(text,"total",totalSum,totalMax);
            for (int i = 0; i < AreaCount-1; i++) Timing(text,tab==1&&i==(int)Area.Detail?"InventoryDetail":tab==1&&i==(int)Area.Input?"UpdateInventoryFrameInput":Names[i],Sums[i],Maxima[i]);
            text.Append(" CopyNative=").Append(copies).Append(" copyByArea=");
            for (int i = 0; i < AreaCount; i++) if (CopiesByArea[i] > 0) text.Append(Names[i]).Append(':').Append(CopiesByArea[i]).Append(',');
            text.Append(" cacheClearFull=").Append(fullClears).Append(" cacheClearSource=").Append(sourceClears)
                .Append(" cacheSize=").Append(InventoryFrameSkin.DiagnosticCacheCount).Append(" cacheNew=").Append(entries).Append(" lastEntry=");
            if (entries == 0) text.Append('-');
            else text.Append("key:0x").Append(lastKey.ToString("X16")).Append(",fill:0x").Append(lastFill.ToString("X6"))
                .Append(",area:").Append(Names[(int)lastEntryArea]).Append(",border:0x").Append(lastBorder.ToString("X6")).Append(",alpha:").Append(lastAlpha).Append(",source:").Append(lastSource);
            text.Append(" entryChanges=");
            if (changedFields == 0) text.Append('-');
            if ((changedFields & 1) != 0) text.Append("fill,");
            if ((changedFields & 2) != 0) text.Append("border,");
            if ((changedFields & 4) != 0) text.Append("alpha,");
            if ((changedFields & 8) != 0) text.Append("source,");
            text.Append(" exceptions=").Append(exceptionCount);
            foreach (var item in Exceptions) text.Append(" [").Append(item.Key.Replace('\n',' ').Replace('\r',' ')).Append(" x").Append(item.Value).Append(']');
            EmitDeep(text);
            UnityEngine.Debug.Log(text.ToString());
        }
    }
}

namespace Hud
{
    internal static partial class SkillDiagnostics
    {
        // Timings are inclusive, grouped by coarse area; never add parent and child times.
        internal enum Stage { Sync, HitTest, Pointer, Release, Keys, Wheel, Select, Action, Book, Templates,
            Metrics, IntrinsicLines, Description, AddLines, FontSplit, FontWidth, FontExact, FontHeight,
            HudGetRect, HudResolve, HudVisible, PotentialData, PotentialRect, PotentialFrame, PotentialIcon,
            PotentialText, MoneyFormat, Concat, FrameSync, FrameBackground, FrameHeader, FrameTabs,
            FrameMoney, FrameDetails, PopUp, CompactText, LayoutScroll, LayoutSize, LayoutCell, LayoutPotential, LayoutIntrinsic, LayoutGroups, LayoutDetail, Learning, Platform, Count }
        internal enum Counter { BeginFrame, ViewportChange, GetRect, RectMiss, HitTest, Visible,
            MetricsCall, MetricsRebuild, MobileLayoutReads, Split, FontMeasure, NewList, NewArray, NewLine, NewGUIStyle,
            NewGUIContent, StringConcat, FormatMoney, TemplateCall, BookCall,
            LayoutFrame, LayoutContent, LayoutLineHeight, LayoutRowHeight, LayoutDesignSize, LayoutSize,
            LayoutColumnGap, LayoutRowGap, LayoutGridBottomGap, LayoutPotentialContent, LayoutPotentialHeaderBounds,
            LayoutScrollBounds, LayoutIntrinsicTextWidth, LayoutIntrinsicBounds, LayoutIntrinsicIconBounds,
            LayoutIntrinsicBarBounds, LayoutSkillsGroupBounds, LayoutPotentialGroupTop, LayoutPotentialGroupBounds,
            LayoutPotentialTitle, LayoutActivityTitle, LayoutCombinedTitle, LayoutTitleHeight, LayoutDetailBounds,
            LayoutDetailContent, LayoutControlsTop, LayoutUpperBounds, LayoutCellBounds, LayoutPotentialBounds,
            LayoutKeyBounds, LayoutActionBounds, Count }
        private const int StageCount = (int)Stage.Count, CounterCount = (int)Counter.Count;
        private static readonly double[] DeepPending = new double[AreaCount*StageCount], DeepSums = new double[AreaCount*StageCount], DeepMax = new double[AreaCount*StageCount];
        private static readonly long[] DeepCalls = new long[AreaCount*StageCount], Counts = new long[AreaCount*CounterCount];
        private static readonly Counter[] InputAllocCounters = {Counter.NewList,Counter.NewArray,Counter.Split,Counter.FontMeasure,Counter.StringConcat};
        private static readonly string[] StageNames = Enum.GetNames(typeof(Stage)), CounterNames = Enum.GetNames(typeof(Counter));
        private sealed class RectStat { internal int Calls; internal double Sum, Max; }
        private sealed class SiteStat { internal int Calls, Down, Held, Released, Click; }
        private static readonly Dictionary<string,RectStat> RectStats = new Dictionary<string,RectStat>();
        private static readonly Dictionary<string,Dictionary<int,SiteStat>> Invalidations = new Dictionary<string,Dictionary<int,SiteStat>>();
        internal struct ProbeScope : IDisposable
        {
            private readonly int index, selected;
            private readonly long started;
            private readonly bool active;
            internal ProbeScope(Stage stage) { active=true; selected=tab; index=(int)currentArea*StageCount+(int)stage; started=Stopwatch.GetTimestamp(); DeepCalls[index]++; }
            public void Dispose() { if(active && selected==tab)DeepPending[index]+=(Stopwatch.GetTimestamp()-started)*TickToMs; }
        }
        // No Tick, stack capture, strings or heap allocation in these hot-path operations.
        internal static ProbeScope Probe(Stage stage) => tab<0?default(ProbeScope):new ProbeScope(stage);
        internal static void Count(Counter counter, int amount=1) { if(tab>=0)Counts[(int)currentArea*CounterCount+(int)counter]+=amount; }
        internal static long StartRect() => tab<0?0:Stopwatch.GetTimestamp();
        internal static void RectResolved(string name,long start)
        {
            if(start==0||tab<0)return;
            double ms=(Stopwatch.GetTimestamp()-start)*TickToMs;
            if(!RectStats.TryGetValue(name,out RectStat stat)) { stat=new RectStat();RectStats.Add(name,stat); }
            stat.Calls++;stat.Sum+=ms;stat.Max=System.Math.Max(stat.Max,ms);
        }
        internal static void Invalidated(string file,int line)
        {
            if(tab<0)return;
            if(!Invalidations.TryGetValue(file,out Dictionary<int,SiteStat> sites)) {sites=new Dictionary<int,SiteStat>();Invalidations.Add(file,sites);}
            if(!sites.TryGetValue(line,out SiteStat stat)) {stat=new SiteStat();sites.Add(line,stat);}
            stat.Calls++;
            if(GameCanvas.isPointerJustDown)stat.Down++;
            if(GameCanvas.isPointerDown)stat.Held++;
            if(GameCanvas.isPointerJustRelease)stat.Released++;
            if(GameCanvas.isPointerClick)stat.Click++;
        }
        private static void CommitDeepFrame()
        {
            for(int i=0;i<DeepPending.Length;i++) { DeepSums[i]+=DeepPending[i];DeepMax[i]=System.Math.Max(DeepMax[i],DeepPending[i]);DeepPending[i]=0; }
        }
        private static void ResetDeep()
        {
            Array.Clear(DeepPending,0,DeepPending.Length);Array.Clear(DeepSums,0,DeepSums.Length);Array.Clear(DeepMax,0,DeepMax.Length);
            Array.Clear(DeepCalls,0,DeepCalls.Length);Array.Clear(Counts,0,Counts.Length);
            foreach(var item in RectStats){item.Value.Calls=0;item.Value.Sum=item.Value.Max=0;}
            foreach(var file in Invalidations)foreach(var site in file.Value){site.Value.Calls=site.Value.Down=site.Value.Held=site.Value.Released=site.Value.Click=0;}
        }
        private static void EmitDeep(StringBuilder text)
        {
            text.Append(" deepMs(avg/max;calls/window,inclusive)=");
            int heaviest=-1;
            for(int i=0;i<DeepSums.Length;i++) {
                if(DeepCalls[i]==0 && !(i/StageCount==(int)Area.Input && i%StageCount<=(int)Stage.HudVisible))continue;
                text.Append('[').Append(Names[i/StageCount]).Append('.').Append(StageNames[i%StageCount]).Append(':')
                    .Append(Number(DeepSums[i]/frames)).Append('/').Append(Number(DeepMax[i])).Append(';').Append(DeepCalls[i]).Append(']');
                // Direct input blocks only, not their nested callees.
                int stage=i%StageCount;
                if(i/StageCount==(int)Area.Input && (stage==(int)Stage.Sync||stage==(int)Stage.HitTest||stage==(int)Stage.Pointer||stage==(int)Stage.Release||stage==(int)Stage.Keys)
                    &&(heaviest<0||DeepSums[i]>DeepSums[heaviest]))heaviest=i;
            }
            text.Append(" inputHeaviest=");
            if(heaviest<0)text.Append('-');else text.Append(StageNames[heaviest%StageCount]).Append(':').Append(Number(DeepSums[heaviest]/frames));
            text.Append(" counts/window=");
            for(int i=0;i<Counts.Length;i++)if(Counts[i]!=0)text.Append('[').Append(Names[i/CounterCount]).Append('.').Append(CounterNames[i%CounterCount]).Append(':').Append(Counts[i]).Append(']');
            text.Append(" inputAlloc/window=");
            foreach(Counter counter in InputAllocCounters)
                text.Append(counter).Append(':').Append(Counts[(int)Area.Input*CounterCount+(int)counter]).Append(',');
            text.Append(" Invalidate/window=");int total=0;
            foreach(var file in Invalidations)foreach(var site in file.Value) {
                SiteStat v=site.Value;if(v.Calls==0)continue;total+=v.Calls;
                text.Append('[').Append(file.Key).Append(':').Append(site.Key).Append(" calls:").Append(v.Calls)
                    .Append(" justDown/held/release/click:").Append(v.Down).Append('/').Append(v.Held).Append('/').Append(v.Released).Append('/').Append(v.Click).Append(']');
            }
            text.Append(" total:").Append(total).Append(" rectMiss/window(ms sum/maxCall)=");
            foreach(var item in RectStats)if(item.Value.Calls>0)text.Append('[').Append(item.Key).Append(':').Append(item.Value.Calls).Append(';').Append(Number(item.Value.Sum)).Append('/').Append(Number(item.Value.Max)).Append(']');
        }
    }
}
