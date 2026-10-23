using System;
using System.Collections.Generic;
using Hud;
using UnityEngine;

namespace Hud
{
    internal static class SkillPanelLayout
    {
        internal const string Detail = "skills.detail", Close = "skills.close", Upper = "skills.upper", Left = "skills.left", Intrinsic = "skills.intrinsic";
        internal static string Cell(int i) => "skills.cell." + i;
        internal static string Key(int i) => "skills.key." + i;
        internal static string Potential(int i) => "skills.potential." + i;
        internal static string Action(int i) => "skills.action." + i;
        private static bool Visible => InventoryFrame.Visible && GameCanvas.panel.SkillPanelActive;
        private static bool DetailVisible => Visible && GameCanvas.panel.SkillDetailOpen;
        internal const int Margin=InventoryFrame.Padding, Gap=InventoryFrame.Gap;
        internal const int PotentialGap=1;
        private struct LayoutMetrics
        {
            internal Rect Frame,Content,PotentialContent,PotentialHeaderBounds,ScrollBounds,IntrinsicBounds,IntrinsicIconBounds,
                SkillsGroupBounds,PotentialGroupBounds,DetailBounds,DetailContent,UpperBounds;
            internal float RowHeight,DesignSize,Size,ColumnGap,RowGap,GridBottomGap,IntrinsicTextWidth,PotentialGroupTop,ControlsTop;
            internal int LineHeight,TitleHeight,Width,Height,Zoom;
            internal string PotentialTitle,ActivityTitle;
            internal bool CombinedTitle,DetailOpen,HasBook;
            internal long FrameVersion,Potential,Activity;
            internal mFont TitleFont,BlueFont,GreenFont,FrameFont;
            internal Panel Panel;
        }
        private static LayoutMetrics metrics;
        private static readonly Rect[] cells=new Rect[8],keys=new Rect[8],actions=new Rect[2],potentialRows=new Rect[5];
        private static bool metricsReady;
        private static long checkedFrame=-1,checkedState=-1,metricsVersion,rectVersion,rectMetrics=-1,rectState=-1;
        private static int rectScroll;
        private static void EnsureMetrics()
        {
            Char character=Char.myCharz();Panel panel=GameCanvas.panel;
            long tick=HudLayout.FrameId,state=HudLayout.StateGeneration;
            // Validate mutable state once per game tick/invalidation. Cheap data checks also
            // catch a stat/font/viewport update between input and paint in the same tick.
            if(metricsReady&&checkedFrame==tick&&checkedState==state&&metrics.Width==GameCanvas.w&&metrics.Height==GameCanvas.h
                &&metrics.Zoom==mGraphics.zoomLevel&&ReferenceEquals(metrics.Panel,panel)
                &&metrics.Potential==character.cTiemNang&&metrics.Activity==character.cNangdong
                &&ReferenceEquals(metrics.TitleFont,mFont.tahoma_7b_dark)&&ReferenceEquals(metrics.BlueFont,mFont.tahoma_7b_blue)
                &&ReferenceEquals(metrics.GreenFont,mFont.tahoma_7_green2)&&ReferenceEquals(metrics.FrameFont,mFont.tahoma_7_yellow))return;
            using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.Metrics)) {
                SkillDiagnostics.Count(SkillDiagnostics.Counter.MetricsCall);
                long frameVersion=InventoryFrame.MetricsVersion;
                bool detailOpen=panel!=null&&panel.SkillDetailOpen,hasBook=panel!=null&&panel.SkillHasBook;
                checkedFrame=tick;checkedState=state;
                if(metricsReady&&metrics.FrameVersion==frameVersion&&ReferenceEquals(metrics.Panel,panel)
                    &&metrics.Potential==character.cTiemNang&&metrics.Activity==character.cNangdong
                    &&metrics.DetailOpen==detailOpen&&metrics.HasBook==hasBook
                    &&ReferenceEquals(metrics.TitleFont,mFont.tahoma_7b_dark)&&ReferenceEquals(metrics.BlueFont,mFont.tahoma_7b_blue)
                    &&ReferenceEquals(metrics.GreenFont,mFont.tahoma_7_green2)&&ReferenceEquals(metrics.FrameFont,mFont.tahoma_7_yellow))return;
                SkillDiagnostics.Count(SkillDiagnostics.Counter.MetricsRebuild);
                var next=new LayoutMetrics {Width=GameCanvas.w,Height=GameCanvas.h,Zoom=mGraphics.zoomLevel,Panel=panel,
                    FrameVersion=frameVersion,Potential=character.cTiemNang,Activity=character.cNangdong,DetailOpen=detailOpen,HasBook=hasBook,
                    TitleFont=mFont.tahoma_7b_dark,BlueFont=mFont.tahoma_7b_blue,GreenFont=mFont.tahoma_7_green2,FrameFont=mFont.tahoma_7_yellow};
                Rect root=HudLayout.GetRect(InventoryFrame.Root),money=HudLayout.GetRect(InventoryFrame.Money);
                next.Frame=new Rect(InventoryFrame.FrameInset,73,root.width-2*InventoryFrame.FrameInset,Mathf.Max(0,money.y-Gap-73));
                next.Content=InventoryFrame.Inset(next.Frame);
                next.LineHeight=Mathf.Max(12,Mathf.Max(next.BlueFont.getHeight(),next.GreenFont.getHeight()));
                next.PotentialTitle="Tiềm năng · "+NinjaUtil.getMoneys(next.Potential);
                next.ActivityTitle=mResources.active_point+": "+NinjaUtil.getMoneys(next.Activity);
                next.CombinedTitle=GameCanvas.h<=208||next.TitleFont.getWidth(next.PotentialTitle+" · "+next.ActivityTitle)<=next.Content.width-2*Margin;
                next.TitleHeight=(next.CombinedTitle?1:2)*next.LineHeight+(GameCanvas.h<=208?Gap:2*Gap);
                next.RowGap=Mathf.Max(Gap+3,InventoryFrame.Unit(14));
                next.GridBottomGap=Margin+3;
                next.DesignSize=Mathf.Min(InventoryFrame.Unit(HudLayout.IsMobileLayout?44:60),(next.Content.width-3*Gap)/4);
                next.Size=Mathf.Min(next.DesignSize,Mathf.Max(8,(next.Frame.height-4*Margin-2*Gap-next.RowGap-next.GridBottomGap-2*next.LineHeight-next.TitleHeight)/3));
                next.RowHeight=Mathf.Max(next.Size,2*next.LineHeight+2);
                next.ColumnGap=(next.Content.width-4*next.Size)/5;
                for(int i=0;i<8;i++)cells[i]=new Rect(next.Content.x+next.ColumnGap+i%4*(next.Size+next.ColumnGap),next.Content.y+i/4*(next.Size+next.RowGap),next.Size,next.Size);
                next.IntrinsicTextWidth=next.Content.width-next.Size-Gap-Margin;
                next.IntrinsicBounds=new Rect(next.Content.x,cells[4].yMax+next.GridBottomGap,next.Content.width,next.Size);
                next.IntrinsicIconBounds=new Rect(next.IntrinsicBounds.x,next.IntrinsicBounds.center.y-next.Size/2,next.Size,next.Size);
                next.SkillsGroupBounds=new Rect(next.Frame.x,next.Frame.y,next.Frame.width,Mathf.Max(cells[4].yMax+3+Margin,next.IntrinsicBounds.yMax+Margin)-next.Frame.y);
                next.PotentialGroupTop=next.IntrinsicBounds.yMax+Margin+Gap;
                next.PotentialGroupBounds=new Rect(next.Frame.x,next.PotentialGroupTop,next.Frame.width,Mathf.Max(0,next.Frame.yMax-next.PotentialGroupTop));
                next.PotentialContent=InventoryFrame.Inset(next.PotentialGroupBounds);
                next.PotentialHeaderBounds=new Rect(next.PotentialContent.x,next.PotentialContent.y,next.PotentialContent.width,Mathf.Min(next.TitleHeight,next.PotentialContent.height));
                next.ScrollBounds=new Rect(next.PotentialContent.x,next.PotentialHeaderBounds.yMax,next.PotentialContent.width,Mathf.Max(0,next.PotentialContent.yMax-next.PotentialHeaderBounds.yMax));
                for(int i=0;i<5;i++)potentialRows[i]=new Rect(next.Content.x,next.ScrollBounds.y+i*(next.RowHeight+PotentialGap),next.Content.width,next.RowHeight);
                next.DetailBounds=InventoryFrame.Inset(HudLayout.GetRect(InventoryFrame.Detail),InventoryFrame.FrameInset);
                next.DetailContent=InventoryFrame.Inset(next.DetailBounds);
                next.ControlsTop=next.DetailContent.yMax-100;
                next.UpperBounds=new Rect(next.DetailContent.x,next.DetailContent.y,next.DetailContent.width,Mathf.Max(1,next.ControlsTop-Gap-next.DetailContent.y));
                float keyWidth=(next.DetailContent.width-3*Gap)/4;
                for(int i=0;i<8;i++)keys[i]=new Rect(next.DetailContent.x+i%4*(keyWidth+Gap),next.ControlsTop+16+i/4*28,keyWidth,24);
                float actionWidth=hasBook?(next.DetailContent.width-Gap)/2:next.DetailContent.width;
                for(int i=0;i<2;i++)actions[i]=new Rect(next.DetailContent.x+i*(actionWidth+Gap),next.DetailContent.yMax-28,actionWidth,28);
                metrics=next;metricsReady=true;metricsVersion++;
            }
        }
        // Rect/hit-test invalidation includes scroll; the layout key above deliberately does not.
        private static long RectVersion
        {
            get {
                EnsureMetrics();long state=HudLayout.StateGeneration;int scroll=GameCanvas.panel?.SkillScroll??0;
                if(rectMetrics!=metricsVersion||rectState!=state||rectScroll!=scroll) {
                    rectMetrics=metricsVersion;rectState=state;rectScroll=scroll;rectVersion++;
                }
                return rectVersion;
            }
        }
        internal static Rect Frame {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutFrame);EnsureMetrics();return metrics.Frame;}}
        internal static Rect Content {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutContent);EnsureMetrics();return metrics.Content;}}
        internal static int LineHeight {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutLineHeight);EnsureMetrics();return metrics.LineHeight;}}
        internal static float RowHeight {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutRowHeight);EnsureMetrics();return metrics.RowHeight;}}
        internal static float DesignSize {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutDesignSize);EnsureMetrics();return metrics.DesignSize;}}
        internal static float Size {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutSize);EnsureMetrics();return metrics.Size;}}
        internal static float ColumnGap {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutColumnGap);EnsureMetrics();return metrics.ColumnGap;}}
        internal static float RowGap {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutRowGap);EnsureMetrics();return metrics.RowGap;}}
        internal static float GridBottomGap {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutGridBottomGap);EnsureMetrics();return metrics.GridBottomGap;}}
        internal static Rect PotentialContent {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutPotentialContent);EnsureMetrics();return metrics.PotentialContent;}}
        internal static Rect PotentialHeaderBounds {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutPotentialHeaderBounds);EnsureMetrics();return metrics.PotentialHeaderBounds;}}
        internal static Rect ScrollBounds {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutScrollBounds);EnsureMetrics();return metrics.ScrollBounds;}}
        internal static float IntrinsicTextWidth {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutIntrinsicTextWidth);EnsureMetrics();return metrics.IntrinsicTextWidth;}}
        internal static Rect IntrinsicBounds {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutIntrinsicBounds);EnsureMetrics();return metrics.IntrinsicBounds;}}
        internal static Rect IntrinsicIconBounds {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutIntrinsicIconBounds);EnsureMetrics();return metrics.IntrinsicIconBounds;}}
        internal static Rect SkillsGroupBounds {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutSkillsGroupBounds);EnsureMetrics();return metrics.SkillsGroupBounds;}}
        internal static float PotentialGroupTop {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutPotentialGroupTop);EnsureMetrics();return metrics.PotentialGroupTop;}}
        internal static Rect PotentialGroupBounds {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutPotentialGroupBounds);EnsureMetrics();return metrics.PotentialGroupBounds;}}
        internal static string PotentialTitle {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutPotentialTitle);EnsureMetrics();return metrics.PotentialTitle;}}
        internal static string ActivityTitle {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutActivityTitle);EnsureMetrics();return metrics.ActivityTitle;}}
        internal static bool CombinedTitle {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutCombinedTitle);EnsureMetrics();return metrics.CombinedTitle;}}
        internal static int TitleHeight {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutTitleHeight);EnsureMetrics();return metrics.TitleHeight;}}
        internal static Rect DetailBounds {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutDetailBounds);EnsureMetrics();return metrics.DetailBounds;}}
        internal static Rect DetailContent {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutDetailContent);EnsureMetrics();return metrics.DetailContent;}}
        internal static float ControlsTop {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutControlsTop);EnsureMetrics();return metrics.ControlsTop;}}
        internal static Rect UpperBounds {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutUpperBounds);EnsureMetrics();return metrics.UpperBounds;}}
        internal static Rect IntrinsicBarBounds {get{SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutIntrinsicBarBounds);EnsureMetrics();return metrics.IntrinsicBounds;}}
        internal static Rect CellBounds(int i) {SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutCellBounds);EnsureMetrics();return cells[i];}
        internal static Rect PotentialBounds(int i) {SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutPotentialBounds);EnsureMetrics();Rect row=potentialRows[i];row.y-=GameCanvas.panel?.SkillScroll??0;return row;}
        internal static Rect KeyBounds(int i) {SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutKeyBounds);EnsureMetrics();return keys[i];}
        internal static Rect ActionBounds(int i) {SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutActionBounds);EnsureMetrics();return actions[i];}
        internal static void Register()
        {
            HudLayout.Register(Left,()=>ScrollBounds,HudLayout.OverlayOrder+11,()=>Visible,cacheVersion:()=>RectVersion);
            for(int i=0;i<8;i++) {
                int slot=i;
                HudLayout.Register(Cell(i),()=>InventoryFrame.Intersection(CellBounds(slot),Content),HudLayout.OverlayOrder+12,()=>Visible&&slot<SkillTabState.Templates().Length,cacheVersion:()=>RectVersion);
                HudLayout.Register(Key(i),()=>KeyBounds(slot),HudLayout.OverlayOrder+17,()=>DetailVisible,cacheVersion:()=>RectVersion);
            }
            HudLayout.Register(Intrinsic,()=>InventoryFrame.Intersection(IntrinsicBounds,Content),HudLayout.OverlayOrder+12,()=>Visible,cacheVersion:()=>RectVersion);
            for(int i=0;i<5;i++) { int row=i; HudLayout.Register(Potential(i),()=>InventoryFrame.Intersection(PotentialBounds(row),ScrollBounds),HudLayout.OverlayOrder+12,()=>Visible,cacheVersion:()=>RectVersion); }
            HudLayout.Register(Detail,()=>DetailBounds,HudLayout.OverlayOrder+14,()=>DetailVisible,cacheVersion:()=>RectVersion);
            HudLayout.Register(Upper,()=>UpperBounds,HudLayout.OverlayOrder+15,()=>DetailVisible,cacheVersion:()=>RectVersion);
            HudLayout.Register(Close,()=>new Rect(DetailBounds.xMax-Margin-18,DetailBounds.y+Margin,18,18),HudLayout.OverlayOrder+18,()=>DetailVisible,cacheVersion:()=>RectVersion);
            for(int i=0;i<2;i++) { int button=i; HudLayout.Register(Action(i),()=>ActionBounds(button),HudLayout.OverlayOrder+17,()=>DetailVisible&&(button==0||GameCanvas.panel.SkillHasBook),cacheVersion:()=>RectVersion); }
        }
    }
}

public partial class Panel
{
    private int skillSelectedId=-1, skillTopScroll;
    private bool skillDetailOpen;
    private string skillPressed;
    private int skillDragX,skillDragY,skillDragStart;
    private bool skillDragging;
    private int skillUpperHeight;
    internal bool SkillPanelActive => InventoryFrameActive && currentTabIndex==2;
    internal int SkillScroll => cmy;
    internal bool SkillDetailOpen => SkillPanelActive && skillDetailOpen && SelectedSkillTemplate!=null;
    private SkillTemplate SelectedSkillTemplate {
        get { foreach(SkillTemplate t in SkillTabState.Templates()) if(t.id==skillSelectedId) return t; return null; }
    }
    private Skill CurrentPanelSkill => SelectedSkillTemplate==null ? null : Char.myCharz().getSkill(SelectedSkillTemplate);
    private int CurrentPanelLevel => CurrentPanelSkill?.point??0;
    internal bool SkillHasBook => SelectedSkillTemplate!=null && !SkillTabState.Studying(skillSelectedId)
        && CurrentPanelLevel<SelectedSkillTemplate.maxPoint && SkillTabState.FindBook(skillSelectedId,CurrentPanelLevel+1)>=0;
    private void OpenSkillPanel()
    {
        cp=null; waitToPerform=0; skillTopScroll=0; selected=-1; cmy=cmtoY=0; ITEM_HEIGHT=30;
        SkillTemplate[] templates=SkillTabState.Templates();
        bool found=false;foreach(SkillTemplate t in templates) found|=t.id==skillSelectedId;
        if(!found) skillSelectedId=templates.Length>0 ? templates[0].id : -1;
        skillDetailOpen=skillSelectedId>=0; SkillTabState.Request(); SyncSkillPanel();
    }
    private void SyncSkillPanel()
    {
        using (SkillDiagnostics.Probe(SkillDiagnostics.Stage.Sync)) {
        currentListLength=14;
        float contentHeight=5*SkillPanelLayout.RowHeight+4*SkillPanelLayout.PotentialGap;
        cmyLim=Mathf.Max(0,(int)System.Math.Ceiling(contentHeight-SkillPanelLayout.ScrollBounds.height));
        cmy=cmtoY=Mathf.Clamp(cmy,0,cmyLim);
        }
    }
    private void SelectPanelSkill(int slot)
    {
        using (SkillDiagnostics.Probe(SkillDiagnostics.Stage.Select)) {
        SkillTemplate[] templates=SkillTabState.Templates(); if(slot<0||slot>=templates.Length)return;
        skillSelectedId=templates[slot].id; skillDetailOpen=true; skillTopScroll=0; cp=null;
        HudLayout.Invalidate();
        }
    }
    private bool UpdateSkillPanelInput()
    {
        using (SkillDiagnostics.Measure(SkillDiagnostics.Area.Input)) {
        SyncSkillPanel();
        string hit=HudLayout.HitTest(new Vector2(GameCanvas.px,GameCanvas.py));
        using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.Pointer)) {
        if(GameCanvas.isPointerJustDown) {
            skillPressed=hit;skillDragX=GameCanvas.px;skillDragY=GameCanvas.py;skillDragging=false;
            skillDragStart=hit==SkillPanelLayout.Upper ? skillTopScroll : cmy;
            GameCanvas.isPointerJustDown=false;
        }
        bool left=skillPressed!=null&&(skillPressed==SkillPanelLayout.Left||skillPressed.StartsWith("skills.potential."));
        if(skillPressed==SkillPanelLayout.Intrinsic&&(GameCanvas.isPointerDown||GameCanvas.isPointerJustRelease)
            &&(System.Math.Abs(GameCanvas.py-skillDragY)>4||System.Math.Abs(GameCanvas.px-skillDragX)>4))skillDragging=true;
        bool upper=skillPressed==SkillPanelLayout.Upper;
        if((left||upper)&&(GameCanvas.isPointerDown||GameCanvas.isPointerJustRelease)) {
            int delta=GameCanvas.py-skillDragY;
            if(System.Math.Abs(delta)>4||System.Math.Abs(GameCanvas.px-skillDragX)>4)skillDragging=true;
            if(skillDragging) {
                int previousStatScroll=cmy;
                if(upper)skillTopScroll=Mathf.Clamp(skillDragStart-delta,0,Mathf.Max(0,skillUpperHeight-(int)SkillPanelLayout.UpperBounds.height));
                else cmy=cmtoY=Mathf.Clamp(skillDragStart-delta,0,cmyLim);
                if(previousStatScroll!=cmy)HudLayout.Invalidate();
            }
        }
        }
        using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.Release)) {
        if(GameCanvas.isPointerJustRelease) {
            bool click=GameCanvas.isPointerClick&&!skillDragging&&skillPressed==hit;skillPressed=null;
            if(click&&hit!=null) {
                if(hit==InventoryFrame.Close)CloseInventoryFrame();
                else if(hit.StartsWith("inventory.tab."))SelectInventoryFrameTab(InventoryFrameTabIds[int.Parse(hit.Substring("inventory.tab.".Length))]);
                else if(hit==SkillPanelLayout.Close) {skillDetailOpen=false;HudLayout.Invalidate();}
                else if(hit.StartsWith("skills.cell."))SelectPanelSkill(int.Parse(hit.Substring("skills.cell.".Length)));
                else if(hit.StartsWith("skills.key.")&&SkillTabState.Ready)SkillTabState.Swap(skillSelectedId,int.Parse(hit.Substring("skills.key.".Length)));
                else if(hit==SkillPanelLayout.Intrinsic) {selected=5;doFireSkill();}
                else if(hit.StartsWith("skills.potential.")) {selected=int.Parse(hit.Substring("skills.potential.".Length));doFireSkill();}
                else if(hit==SkillPanelLayout.Action(0))ActivateSkillAction();
                else if(hit==SkillPanelLayout.Action(1)&&SkillHasBook)SkillTabState.GoToTeacher();
            }
            GameCanvas.clearAllPointerEvent();
        }
        }
        using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.Keys)) {
        int previous=Main.isPC?23:4,next=Main.isPC?24:6;
        if(GameCanvas.keyPressed[previous]||GameCanvas.keyPressed[next]) {
            int[] tabs=InventoryFrameTabIds;int at=Array.IndexOf(tabs,currentTabIndex),dir=GameCanvas.keyPressed[next]?1:-1;
            SelectInventoryFrameTab(tabs[(at+dir+tabs.Length)%tabs.Length]);GameCanvas.keyPressed[previous]=GameCanvas.keyPressed[next]=false;
        }
        int up=Main.isPC?21:2,down=Main.isPC?22:8;
        if(GameCanvas.keyPressed[up]||GameCanvas.keyPressed[down]) {
            SkillTemplate[] templates=SkillTabState.Templates();int at=0;
            for(int i=0;i<templates.Length;i++)if(templates[i].id==skillSelectedId)at=i;
            if(templates.Length>0)SelectPanelSkill((at+(GameCanvas.keyPressed[down]?1:-1)+templates.Length)%templates.Length);
            GameCanvas.keyPressed[up]=GameCanvas.keyPressed[down]=false;
        }
        if(GameCanvas.keyPressed[13]) {GameCanvas.keyPressed[13]=false;if(skillDetailOpen)skillDetailOpen=false;else CloseInventoryFrame();HudLayout.Invalidate();}
        }
        return true;
        }
    }
    private bool ScrollSkillPanelMouse(int amount)
    {
        using (SkillDiagnostics.Probe(SkillDiagnostics.Stage.Wheel)) {
        if (!SkillPanelActive) return false;
        if (amount == 0 || GameCanvas.menu.showMenu || GameCanvas.currentDialog != null) return true;
        Vector2 point = new Vector2(GameCanvas.pxMouse, GameCanvas.pyMouse);
        int previousStatScroll=cmy;
        if (SkillDetailOpen && SkillPanelLayout.UpperBounds.Contains(point))
            skillTopScroll = Mathf.Clamp(skillTopScroll - amount * 24, 0, Mathf.Max(0, skillUpperHeight - (int)SkillPanelLayout.UpperBounds.height));
        else if (SkillPanelLayout.ScrollBounds.Contains(point)) cmy = cmtoY = Mathf.Clamp(cmy - amount * 24, 0, cmyLim);
        if(previousStatScroll!=cmy)HudLayout.Invalidate();
        return true;
        }
    }

    private void ActivateSkillAction()
    {
        using (SkillDiagnostics.Probe(SkillDiagnostics.Stage.Action)) {
        SkillTemplate template=SelectedSkillTemplate;if(template==null)return;
        if(CurrentPanelLevel>=template.maxPoint)return;
        if(SkillTabState.Studying(template.id)) {SkillTabState.ConfirmQuick();return;}
        if(SkillTabState.Waiting&&!SkillHasBook)return;
        int book=SkillTabState.FindBook(template.id,CurrentPanelLevel+1);
        if(book>=0)SkillTabState.UseBook(Char.myCharz().arrItemBag[book].template.id);
        else SkillTabState.GoToTeacher();
        }
    }
    private static string SkillFit(mFont font,string text,float width)
    {
        text=text??string.Empty;if(font.getWidth(text)<=width)return text;
        while(text.Length>0&&font.getWidth(text+"...")>width)text=text.Substring(0,text.Length-1);
        return text+"...";
    }
    private static void SkillLabel(mGraphics g,mFont font,string text,Rect bounds,bool center=false)
    {
        InventoryFrame.Clip(g,bounds);
        font.drawString(g,SkillFit(font,text,bounds.width-2),(int)(center?bounds.center.x:bounds.x+1),(int)(bounds.center.y-font.getHeight()/2f),center?mFont.CENTER:mFont.LEFT);
    }
    private void PaintSkillPanel(mGraphics g)
    {
        using (SkillDiagnostics.Measure(SkillDiagnostics.Area.Panel)) {
        SyncSkillPanel();InventoryFrame.Clip(g,SkillPanelLayout.Frame);InventoryFrame.PaintSection(g,SkillPanelLayout.SkillsGroupBounds,InventoryFrame.PanelBackground);
        Rect viewport=SkillPanelLayout.Content;SkillTemplate[] templates=SkillTabState.Templates();
        using (SkillDiagnostics.Measure(SkillDiagnostics.Area.Grid)) {
        for(int i=0;i<templates.Length;i++) {
            SkillTemplate t=templates[i];Skill skill=Char.myCharz().getSkill(t);Rect cell=SkillPanelLayout.CellBounds(i);
            InventoryFrame.Clip(g,viewport);
            InventoryItemVisuals.PaintSkillIcon(g,t.iconId,cell,viewport,skillDetailOpen&&skillSelectedId==t.id);
            InventoryItemVisuals.PaintLevel(g,cell,viewport,skill?.point??0,false,true);
            if(SkillTabState.Studying(t.id))PaintStudyClock(g,cell,viewport);
        }
        }
        using (SkillDiagnostics.Measure(SkillDiagnostics.Area.Intrinsic)) {
        viewport=SkillPanelLayout.Content;
        Rect intrinsic=SkillPanelLayout.IntrinsicBounds;
        Rect intrinsicIcon=SkillPanelLayout.IntrinsicIconBounds;
        // One frame encloses the icon and description, exactly as tall as the skill icon.
        InventoryFrame.Clip(g,viewport);
        InventoryFrame.PaintSection(g,SkillPanelLayout.IntrinsicBarBounds,InventoryFrame.EquipmentBackground);
        InventoryItemVisuals.PaintSkillIcon(g,spearcialImage,intrinsicIcon,viewport);
        Rect intrinsicText=new Rect(intrinsicIcon.xMax+SkillPanelLayout.Gap,intrinsic.y,SkillPanelLayout.IntrinsicTextWidth,intrinsic.height);
        string[] lines=SkillIntrinsicLines;
        float lineHeight=Mathf.Min(SkillPanelLayout.LineHeight,(intrinsicText.height-2)/Mathf.Max(1,lines.Length));
        float textY=intrinsicText.center.y-lines.Length*lineHeight/2f;
        for(int i=0;i<lines.Length;i++) {
            InventoryItemVisuals.PaintCompactText(g,i==0?mFont.tahoma_7b_dark:mFont.tahoma_7,lines[i],new Rect(intrinsicText.x,textY,intrinsicText.width,lineHeight),viewport,i==0?0x00552D:0x6E5A3D);
            textY+=lineHeight;
        }
        }
        using (SkillDiagnostics.Measure(SkillDiagnostics.Area.Potential)) {
        InventoryFrame.Clip(g,SkillPanelLayout.PotentialGroupBounds);
        using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.PotentialFrame))InventoryFrame.PaintSection(g,SkillPanelLayout.PotentialGroupBounds,InventoryFrame.PanelBackground);
        viewport=SkillPanelLayout.PotentialHeaderBounds;
        Rect title=new Rect(viewport.x+SkillPanelLayout.Margin,viewport.y,viewport.width-2*SkillPanelLayout.Margin,SkillPanelLayout.LineHeight);
        using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.PotentialText))InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7b_dark,SkillPanelLayout.PotentialTitle+(SkillPanelLayout.CombinedTitle?" · "+SkillPanelLayout.ActivityTitle:""),title,viewport,0x362413);
        if(!SkillPanelLayout.CombinedTitle) {
            title.y+=SkillPanelLayout.LineHeight;
            using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.PotentialText))InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7,SkillPanelLayout.ActivityTitle,title,viewport,0x362413);
        }
        PaintSkillPotential(g,SkillPanelLayout.ScrollBounds);
        }
        }
    }
    internal static string[] SkillIntrinsicLines {
        get {
            using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.IntrinsicLines)) {
            string text=CleanSkillText(specialInfo);
            if(text.Length==0)text="Nội tại: chưa kích hoạt";
            return mFont.tahoma_7_green2.splitFontArray(text,Mathf.Max(1,(int)SkillPanelLayout.IntrinsicTextWidth));
            }
        }
    }
    private static string CleanSkillText(string text) => string.IsNullOrWhiteSpace(text)||string.Equals(text.Trim(),"null",StringComparison.OrdinalIgnoreCase)?string.Empty:text.Trim();
    private static void PaintSkillFade(mGraphics g,Rect viewport,bool more)
    {
        if(!more)return;
        InventoryFrame.Clip(g,viewport);
        for(int i=0;i<8;i++) {g.setColor(InventoryFrame.PanelBackground,(i+1)/9f);g.fillRect((int)viewport.x,(int)viewport.yMax-8+i,(int)viewport.width,1);}
    }
    private static void PaintStudyClock(mGraphics g,Rect cell,Rect viewport)
    {
        InventoryFrame.Clip(g,viewport);Rect clock=new Rect(cell.x-2,cell.yMax-7,10,10);
        InventoryFrame.Rounded(g,clock,InventoryFrame.Accent,255,InventoryFrame.Border);
        g.setColor(InventoryFrame.Border);g.drawLine((int)clock.center.x,(int)clock.y+2,(int)clock.center.x,(int)clock.center.y);
        g.drawLine((int)clock.center.x,(int)clock.center.y,(int)clock.xMax-2,(int)clock.center.y);
    }
    private void PaintSkillPotential(mGraphics g,Rect viewport)
    {
        long[] values,costs;string[] names;int[] icons;
        using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.PotentialData)) { SkillDiagnostics.Count(SkillDiagnostics.Counter.NewArray,4);
        Char c=Char.myCharz();values=new long[]{c.cHPGoc,c.cMPGoc,c.cDamGoc,c.cDefGoc,c.cCriticalGoc};
        names=new string[]{mResources.HP,mResources.KI,mResources.hit_point,mResources.armor,mResources.critical};
        icons=new int[]{567,569,568,721,719};
        costs=new long[]{c.cHPGoc+1000L,c.cMPGoc+1000L,c.cDamGoc*100L,500000L+c.cDefGoc*100000L,t_tiemnang[Mathf.Clamp(c.cCriticalGoc,0,t_tiemnang.Length-1)]};
        }
        for(int i=0;i<5;i++) {
            Rect row;using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.PotentialRect))row=SkillPanelLayout.PotentialBounds(i);if(row.yMax<=viewport.y||row.y>=viewport.yMax)continue;
            InventoryFrame.Clip(g,viewport);
            using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.PotentialFrame))InventoryFrame.PaintSection(g,row,InventoryFrame.EquipmentBackground);
            using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.PotentialIcon))InventoryItemVisuals.PaintSkillIcon(g,icons[i],new Rect(row.x,row.y,SkillPanelLayout.Size,SkillPanelLayout.Size),viewport);
            float lineHeight=Mathf.Min(SkillPanelLayout.LineHeight,(row.height-2)/2);
            Rect line=new Rect(row.x+SkillPanelLayout.Size+SkillPanelLayout.Gap,row.center.y-lineHeight,row.width-SkillPanelLayout.Size-SkillPanelLayout.Gap-InventoryFrame.Padding-30,lineHeight);
            string label;using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.Concat)){SkillDiagnostics.Count(SkillDiagnostics.Counter.StringConcat);label=names[i]+" "+mResources.root+": "+NinjaUtil.getMoneys(values[i])+(i==4?"%":"");}
            using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.PotentialText))InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7b_blue,label,line,viewport,0x536DCC);
            line.y+=lineHeight;
            using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.Concat)){SkillDiagnostics.Count(SkillDiagnostics.Counter.StringConcat);label=NinjaUtil.getMoneys(costs[i])+" "+mResources.potential;}
            using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.PotentialText))InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7_green2,label,line,viewport,0x00552D);
        }
    }
    private sealed class SkillLine {internal string Text;internal mFont Font;}
    private static void AddSkillLines(List<SkillLine> lines,string text,mFont font,float width)
    {
        using (SkillDiagnostics.Probe(SkillDiagnostics.Stage.AddLines)) {
        if(string.IsNullOrWhiteSpace(text))return;
        foreach(string line in font.splitFontArray(text,Mathf.Max(1,(int)width-4))){SkillDiagnostics.Count(SkillDiagnostics.Counter.NewLine);lines.Add(new SkillLine{Text=line,Font=font});}
        }
    }
    private static int PaintSkillLines(mGraphics g,List<SkillLine> lines,Rect viewport,int scroll,int initial=0)
    {
        InventoryFrame.Clip(g,viewport);int y=(int)viewport.y+initial-scroll;
        foreach(SkillLine line in lines){InventoryItemVisuals.PaintCompactText(g,line.Font,line.Text,new Rect(viewport.x,y,viewport.width,12),viewport,line.Font==mFont.tahoma_7_green2?0x00552D:InventoryFrame.Border);y+=12;}
        return initial+lines.Count*12;
    }
    private void PaintSkillDetail(mGraphics g)
    {
        using (SkillDiagnostics.Measure(SkillDiagnostics.Area.Detail)) {
        if(!SkillDetailOpen)return;
        Rect r=SkillPanelLayout.DetailBounds,upper=SkillPanelLayout.UpperBounds,lower=upper;
        SkillTemplate t=SelectedSkillTemplate;Skill current=CurrentPanelSkill;int level=current?.point??0;
        InventoryFrame.Clip(g,r);InventoryFrame.PaintSection(g,r,InventoryFrame.EquipmentBackground);
        int slot=SkillTabState.SlotFor(t.id);
        InventoryFrame.Clip(g,upper);
        Rect icon=new Rect(upper.x+1,upper.y+1-skillTopScroll,32,32);
        InventoryItemVisuals.PaintSkillIcon(g,t.iconId,icon,upper,true);
        string[] nameLines=mFont.tahoma_7b_dark.splitFontArray(t.name,Mathf.Max(1,(int)(upper.xMax-icon.xMax-25)));
        InventoryFrame.Clip(g,upper);
        for(int i=0;i<nameLines.Length;i++)mFont.tahoma_7b_dark.drawString(g,nameLines[i],(int)icon.xMax+5,(int)icon.y+i*12,0);
        SkillLabel(g,mFont.tahoma_7,"Cấp "+level+" / "+t.maxPoint+" · "+(slot>=0?"ô "+(slot+1):"chưa gán"),InventoryFrame.Intersection(new Rect(icon.xMax+5,icon.y+nameLines.Length*12,Mathf.Max(0,upper.xMax-icon.xMax-5),15),upper));
        int headingHeight=Mathf.Max(39,nameLines.Length*12+17);
        Skill display=current!=null&&current.point>0?current:(t.skills.Length>0?t.skills[0]:null);
        SkillDiagnostics.Count(SkillDiagnostics.Counter.NewList);var top=new List<SkillLine>();
        if(display!=null)AddSkillLines(top,mResources.KI_consume+display.manaUse+(t.manaUseType==1?"%":"")+" · "+mResources.cooldown+": "+display.strTimeReplay()+"s",mFont.tahoma_7,upper.width);
        AddSkillDamage(top,t,level,level==t.maxPoint?" (hiện tại, tối đa)":" (hiện tại)",mFont.tahoma_7b_dark,upper.width);
        if(level<t.maxPoint)AddSkillDamage(top,t,level+1,level+1==t.maxPoint?" (kế tiếp, tối đa)":" (kế tiếp)",mFont.tahoma_7_green2,upper.width);
        if(level+1<t.maxPoint)AddSkillDamage(top,t,t.maxPoint," (tối đa)",mFont.tahoma_7,upper.width);
        int tableEnd=PaintSkillLines(g,top,upper,skillTopScroll,headingHeight);
        InventoryFrame.Clip(g,upper);g.setColor(InventoryFrame.Border);
        g.fillRect((int)upper.x,(int)upper.y+tableEnd-skillTopScroll+3,(int)upper.width,1);
        SkillDiagnostics.Count(SkillDiagnostics.Counter.NewList);var bottom=new List<SkillLine>();
        AddSkillLines(bottom,SkillDescription(t,current),mFont.tahoma_7,lower.width);
        bool studying=SkillTabState.Studying(t.id);
        if(studying)AddSkillLines(bottom,"Đang học cấp "+SkillTabState.LearningLevel+" · còn "+SkillTabState.Clock(SkillTabState.Remaining),mFont.tahoma_7b_dark,lower.width);
        else if(level<t.maxPoint&&level<t.skills.Length) {
            AddSkillLines(bottom,"Cấp "+(level+1)+": "+NinjaUtil.getMoneys(t.skills[level].powRequire)+" "+mResources.potential,mFont.tahoma_7,lower.width);
            AddSkillLines(bottom,"Thời gian học: "+SkillTabState.Clock(SkillTabState.StudyDuration(level+1)),mFont.tahoma_7,lower.width);
            AddSkillLines(bottom,"Học tại: "+SkillTabState.TeacherName,mFont.tahoma_7,lower.width);
        }
        skillUpperHeight=PaintSkillLines(g,bottom,upper,skillTopScroll,tableEnd+9)+(studying?10:0);
        skillTopScroll=Mathf.Clamp(skillTopScroll,0,Mathf.Max(0,skillUpperHeight-(int)upper.height));
        if(studying) {
            int y=(int)upper.y+tableEnd+9+bottom.Count*12-skillTopScroll+3;
            InventoryFrame.Clip(g,lower);g.setColor(InventoryFrame.SlotBackground);g.fillRect((int)lower.x,y,(int)lower.width,5);
            long elapsed=System.Math.Max(0,SkillTabState.Duration-SkillTabState.Remaining);
            int width=SkillTabState.Duration<=0?0:(int)(lower.width*System.Math.Min(1d,elapsed/(double)SkillTabState.Duration));
            g.setColor(InventoryFrame.Accent);g.fillRect((int)lower.x,y,width,5);
        }
        PaintSkillFade(g,upper,skillTopScroll<skillUpperHeight-(int)upper.height);
        SkillLabel(g,mFont.tahoma_7,"Ô phím",new Rect(SkillPanelLayout.DetailContent.x,SkillPanelLayout.ControlsTop,SkillPanelLayout.DetailContent.width,12));
        for(int i=0;i<8;i++)PaintSkillButton(g,SkillPanelLayout.KeyBounds(i),(i+1).ToString(),true,i==slot);
        bool maximum=level>=t.maxPoint;
        string caption=maximum?"Đã tối đa":studying?"Học nhanh · "+SkillTabState.QuickPrice+" ngọc":SkillHasBook?"Dùng sách":SkillTabState.Waiting?"Đang tải...":"Đến NPC";
        PaintSkillButton(g,SkillPanelLayout.ActionBounds(0),caption,!maximum&&(!SkillTabState.Waiting||SkillHasBook)||studying,false);
        if(SkillHasBook)PaintSkillButton(g,SkillPanelLayout.ActionBounds(1),"Đến NPC",true,false);
        Rect close=HudLayout.GetRect(SkillPanelLayout.Close);InventoryFrame.Clip(g,close);InventoryFrame.Rounded(g,close,InventoryFrame.EquipmentBackground);
        g.drawImage(imgX,(int)close.center.x,(int)close.center.y,mGraphics.HCENTER|mGraphics.VCENTER);
        InventoryFrame.Clip(g,HudLayout.ScreenBounds);
        }
    }
    private static string SkillDescription(SkillTemplate template,Skill current)
    {
        using (SkillDiagnostics.Probe(SkillDiagnostics.Stage.Description)) {
        SkillDiagnostics.Count(SkillDiagnostics.Counter.NewList);var description=new List<string>();
        foreach(string line in template.description??DiagnosticEmptyDescription()) {
            string clean=CleanSkillText(line);if(clean.Length>0)description.Add(clean);
        }
        if(description.Count>0){SkillDiagnostics.Count(SkillDiagnostics.Counter.NewArray);return string.Join(" ",description.ToArray());}
        // The server sends "null" for the template description; level info comes from the game data.
        string info=CleanSkillText(current?.moreInfo);
        if(info.Length==0&&template.skills.Length>0)info=CleanSkillText(template.skills[0].moreInfo);
        return info;
        }
    }
    private static string[] DiagnosticEmptyDescription() {SkillDiagnostics.Count(SkillDiagnostics.Counter.NewArray);return new string[0];}
    private static void AddSkillDamage(List<SkillLine> rows,SkillTemplate t,int level,string suffix,mFont font,float width)
    {
        if(level<=0||level>t.skills.Length)return;
        Skill s=t.skills[level-1];string value=s.damage+(t.damInfo!=null&&t.damInfo.Contains("%")?"%":"");
        SkillDiagnostics.Count(SkillDiagnostics.Counter.NewLine);rows.Add(new SkillLine{Text="Cấp "+level+suffix+": "+value,Font=font});
    }
    private static void PaintSkillButton(mGraphics g,Rect r,string text,bool enabled,bool selected)
    {
        InventoryFrame.Clip(g,r);
        InventoryItemVisuals.PaintNativeButton(g,r,enabled,selected);
        InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7b_dark,text,InventoryFrame.Inset(r),r,enabled?InventoryFrame.Border:0x626262,true);
    }
}
