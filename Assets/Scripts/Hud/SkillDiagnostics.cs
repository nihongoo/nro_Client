using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Hud
{
    // Temporary diagnostics only. Nested timings are inclusive; total = frame paint + input.
    internal static class SkillDiagnostics
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
            UnityEngine.Debug.Log(text.ToString());
        }
    }
}
