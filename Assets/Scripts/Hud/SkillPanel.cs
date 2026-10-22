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
        internal static Rect Frame => new Rect(InventoryFrame.FrameInset,73,HudLayout.GetRect(InventoryFrame.Root).width-2*InventoryFrame.FrameInset,Mathf.Max(0,HudLayout.GetRect(InventoryFrame.Money).y-Gap-73));
        internal static Rect Content => InventoryFrame.Inset(Frame);
        internal static int LineHeight => Mathf.Max(12,Mathf.Max(mFont.tahoma_7b_blue.getHeight(),mFont.tahoma_7_green2.getHeight()));
        internal const int PotentialPadding=3, PotentialGap=1;
        internal static int RowHeight => 2*LineHeight+2*PotentialPadding;
        internal static float Size => Mathf.Min(InventoryFrame.Unit(HudLayout.IsMobileLayout ? 44 : 60),(Content.width-3*Gap)/4);
        internal static float ColumnGap => (Content.width-4*Size)/5;
        internal static float RowGap => Mathf.Max(Gap+3,InventoryFrame.Unit(14));
        internal static float GridBottomGap => Margin+3; // Include the three pixels by which Lv badges extend below the frames.
        internal static Rect ScrollBounds => new Rect(Content.x,CellBounds(4).yMax+GridBottomGap,Content.width,Mathf.Max(0,Content.yMax-CellBounds(4).yMax-GridBottomGap));
        internal static Rect CellBounds(int i)
        {
            return new Rect(Content.x+ColumnGap+i%4*(Size+ColumnGap),Content.y+i/4*(Size+RowGap),Size,Size);
        }
        internal static float IntrinsicTextWidth => Content.width-Size-Gap;
        internal static Rect IntrinsicBounds => new Rect(Content.x,ScrollBounds.y-(GameCanvas.panel?.SkillScroll??0),Content.width,Mathf.Max(Size,Panel.SkillIntrinsicLines.Length*LineHeight));
        internal static string PotentialTitle => "Tiềm năng · "+NinjaUtil.getMoneys(Char.myCharz().cTiemNang);
        internal static string ActivityTitle => mResources.active_point+": "+NinjaUtil.getMoneys(Char.myCharz().cNangdong);
        internal static bool CombinedTitle => mFont.tahoma_7b_dark.getWidth(PotentialTitle+" · "+ActivityTitle)<=Content.width-2*Margin;
        internal static int TitleHeight => (CombinedTitle?1:2)*LineHeight+2*Gap;
        internal static Rect PotentialBounds(int i) => new Rect(Content.x,IntrinsicBounds.yMax+Gap+TitleHeight+i*(RowHeight+PotentialGap),Content.width,RowHeight);
        internal static Rect DetailBounds => InventoryFrame.Inset(HudLayout.GetRect(InventoryFrame.Detail),InventoryFrame.FrameInset);
        internal static Rect DetailContent => InventoryFrame.Inset(DetailBounds);
        internal static float ControlsTop => DetailContent.yMax-100;
        internal static Rect UpperBounds => new Rect(DetailContent.x,DetailContent.y,DetailContent.width,Mathf.Max(1,ControlsTop-Gap-DetailContent.y));
        internal static Rect KeyBounds(int i)
        {
            Rect r=DetailContent;float width=(r.width-3*Gap)/4;
            return new Rect(r.x+i%4*(width+Gap),ControlsTop+16+i/4*28,width,24);
        }
        internal static Rect ActionBounds(int index)
        {
            Rect r=DetailContent;bool book=GameCanvas.panel.SkillHasBook;
            float width=book?(r.width-Gap)/2:r.width;
            return new Rect(r.x+index*(width+Gap),r.yMax-28,width,28);
        }
        internal static void Register()
        {
            HudLayout.Register(Left,()=>ScrollBounds,HudLayout.OverlayOrder+11,()=>Visible);
            for(int i=0;i<8;i++) {
                int slot=i;
                HudLayout.Register(Cell(i),()=>InventoryFrame.Intersection(CellBounds(slot),Content),HudLayout.OverlayOrder+12,()=>Visible&&slot<SkillTabState.Templates().Length);
                HudLayout.Register(Key(i),()=>KeyBounds(slot),HudLayout.OverlayOrder+17,()=>DetailVisible);
            }
            HudLayout.Register(Intrinsic,()=>InventoryFrame.Intersection(IntrinsicBounds,ScrollBounds),HudLayout.OverlayOrder+12,()=>Visible);
            for(int i=0;i<5;i++) { int row=i; HudLayout.Register(Potential(i),()=>InventoryFrame.Intersection(PotentialBounds(row),ScrollBounds),HudLayout.OverlayOrder+12,()=>Visible); }
            HudLayout.Register(Detail,()=>DetailBounds,HudLayout.OverlayOrder+14,()=>DetailVisible);
            HudLayout.Register(Upper,()=>UpperBounds,HudLayout.OverlayOrder+15,()=>DetailVisible);
            HudLayout.Register(Close,()=>new Rect(DetailBounds.xMax-Margin-18,DetailBounds.y+Margin,18,18),HudLayout.OverlayOrder+18,()=>DetailVisible);
            for(int i=0;i<2;i++) { int button=i; HudLayout.Register(Action(i),()=>ActionBounds(button),HudLayout.OverlayOrder+17,()=>DetailVisible&&(button==0||GameCanvas.panel.SkillHasBook)); }
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
        currentListLength=14;
        float bottom=SkillPanelLayout.PotentialBounds(4).yMax+cmy+4;
        cmyLim=Mathf.Max(0,(int)(bottom-SkillPanelLayout.Content.yMax));
        cmy=cmtoY=Mathf.Clamp(cmy,0,cmyLim);
    }
    private void SelectPanelSkill(int slot)
    {
        SkillTemplate[] templates=SkillTabState.Templates(); if(slot<0||slot>=templates.Length)return;
        skillSelectedId=templates[slot].id; skillDetailOpen=true; skillTopScroll=0; cp=null;
        HudLayout.Invalidate();
    }
    private bool UpdateSkillPanelInput()
    {
        SyncSkillPanel();
        string hit=HudLayout.HitTest(new Vector2(GameCanvas.px,GameCanvas.py));
        if(GameCanvas.isPointerJustDown) {
            skillPressed=hit;skillDragX=GameCanvas.px;skillDragY=GameCanvas.py;skillDragging=false;
            skillDragStart=hit==SkillPanelLayout.Upper ? skillTopScroll : cmy;
            GameCanvas.isPointerJustDown=false;
        }
        bool left=skillPressed!=null&&(skillPressed==SkillPanelLayout.Left||skillPressed==SkillPanelLayout.Intrinsic||skillPressed.StartsWith("skills.potential."));
        bool upper=skillPressed==SkillPanelLayout.Upper;
        if((left||upper)&&(GameCanvas.isPointerDown||GameCanvas.isPointerJustRelease)) {
            int delta=GameCanvas.py-skillDragY;
            if(System.Math.Abs(delta)>4||System.Math.Abs(GameCanvas.px-skillDragX)>4)skillDragging=true;
            if(skillDragging) {
                if(upper)skillTopScroll=Mathf.Clamp(skillDragStart-delta,0,Mathf.Max(0,skillUpperHeight-(int)SkillPanelLayout.UpperBounds.height));
                else cmy=cmtoY=Mathf.Clamp(skillDragStart-delta,0,cmyLim);
                HudLayout.Invalidate();
            }
        }
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
        return true;
    }
    private bool ScrollSkillPanelMouse(int amount)
    {
        if (!SkillPanelActive) return false;
        if (amount == 0 || GameCanvas.menu.showMenu || GameCanvas.currentDialog != null) return true;
        Vector2 point = new Vector2(GameCanvas.pxMouse, GameCanvas.pyMouse);
        if (SkillDetailOpen && SkillPanelLayout.UpperBounds.Contains(point))
            skillTopScroll = Mathf.Clamp(skillTopScroll - amount * 24, 0, Mathf.Max(0, skillUpperHeight - (int)SkillPanelLayout.UpperBounds.height));
        else if (SkillPanelLayout.ScrollBounds.Contains(point)) cmy = cmtoY = Mathf.Clamp(cmy - amount * 24, 0, cmyLim);
        HudLayout.Invalidate();
        return true;
    }

    private void ActivateSkillAction()
    {
        SkillTemplate template=SelectedSkillTemplate;if(template==null)return;
        if(CurrentPanelLevel>=template.maxPoint)return;
        if(SkillTabState.Studying(template.id)) {SkillTabState.ConfirmQuick();return;}
        if(SkillTabState.Waiting&&!SkillHasBook)return;
        int book=SkillTabState.FindBook(template.id,CurrentPanelLevel+1);
        if(book>=0)SkillTabState.UseBook(Char.myCharz().arrItemBag[book].template.id);
        else SkillTabState.GoToTeacher();
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
        SyncSkillPanel();InventoryFrame.Clip(g,SkillPanelLayout.Frame);InventoryFrame.PaintSection(g,SkillPanelLayout.Frame,InventoryFrame.PanelBackground);
        Rect viewport=SkillPanelLayout.Content;SkillTemplate[] templates=SkillTabState.Templates();
        for(int i=0;i<templates.Length;i++) {
            SkillTemplate t=templates[i];Skill skill=Char.myCharz().getSkill(t);Rect cell=SkillPanelLayout.CellBounds(i);
            InventoryFrame.Clip(g,viewport);
            InventoryItemVisuals.PaintSkillIcon(g,t.iconId,cell,viewport,skillDetailOpen&&skillSelectedId==t.id);
            InventoryItemVisuals.PaintLevel(g,cell,viewport,skill?.point??0,false,true);
            if(SkillTabState.Studying(t.id))PaintStudyClock(g,cell,viewport);
        }
        viewport=SkillPanelLayout.ScrollBounds;
        Rect intrinsic=SkillPanelLayout.IntrinsicBounds;
        Rect intrinsicIcon=new Rect(intrinsic.x,intrinsic.center.y-SkillPanelLayout.Size/2,SkillPanelLayout.Size,SkillPanelLayout.Size);
        InventoryItemVisuals.PaintSkillIcon(g,spearcialImage,intrinsicIcon,viewport);
        Rect intrinsicText=new Rect(intrinsicIcon.xMax+SkillPanelLayout.Gap,intrinsic.y,SkillPanelLayout.IntrinsicTextWidth,intrinsic.height);
        InventoryFrame.Clip(g,viewport);g.setColor(16765060);
        g.fillRect((int)intrinsicText.x,(int)intrinsicText.y,(int)intrinsicText.width,(int)intrinsicText.height);
        string[] lines=SkillIntrinsicLines;
        float textY=intrinsicText.center.y-lines.Length*SkillPanelLayout.LineHeight/2f;
        foreach(string line in lines) {
            InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7_green2,line,new Rect(intrinsicText.x,textY,intrinsicText.width,SkillPanelLayout.LineHeight),viewport,0x00552D);
            textY+=SkillPanelLayout.LineHeight;
        }
        Rect title=new Rect(intrinsic.x+SkillPanelLayout.Margin,intrinsic.yMax+SkillPanelLayout.Gap,intrinsic.width-2*SkillPanelLayout.Margin,SkillPanelLayout.LineHeight);
        InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7b_dark,SkillPanelLayout.PotentialTitle+(SkillPanelLayout.CombinedTitle?" · "+SkillPanelLayout.ActivityTitle:""),title,viewport,InventoryFrame.Border);
        if(!SkillPanelLayout.CombinedTitle) {
            title.y+=SkillPanelLayout.LineHeight;
            InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7,SkillPanelLayout.ActivityTitle,title,viewport,InventoryFrame.Border);
        }
        PaintSkillPotential(g,viewport);
    }
    internal static string[] SkillIntrinsicLines {
        get {
            string text=CleanSkillText(specialInfo);
            if(text.Length==0)text="Nội tại: chưa kích hoạt";
            return mFont.tahoma_7_green2.splitFontArray(text,Mathf.Max(1,(int)SkillPanelLayout.IntrinsicTextWidth));
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
        Char c=Char.myCharz();long[] values={c.cHPGoc,c.cMPGoc,c.cDamGoc,c.cDefGoc,c.cCriticalGoc};
        string[] names={mResources.HP,mResources.KI,mResources.hit_point,mResources.armor,mResources.critical};
        int[] icons={567,569,568,721,719};
        long[] costs={c.cHPGoc+1000L,c.cMPGoc+1000L,c.cDamGoc*100L,500000L+c.cDefGoc*100000L,t_tiemnang[Mathf.Clamp(c.cCriticalGoc,0,t_tiemnang.Length-1)]};
        for(int i=0;i<5;i++) {
            Rect row=SkillPanelLayout.PotentialBounds(i);InventoryFrame.Clip(g,viewport);
            InventoryFrame.PaintSection(g,row,InventoryFrame.EquipmentBackground);
            InventoryItemVisuals.PaintSkillIcon(g,icons[i],new Rect(row.x+InventoryFrame.Padding,row.center.y-10,20,20),viewport);
            Rect line=new Rect(row.x+20+2*InventoryFrame.Padding,row.y+SkillPanelLayout.PotentialPadding,row.width-20-3*InventoryFrame.Padding-30,SkillPanelLayout.LineHeight);
            InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7b_blue,names[i]+" "+mResources.root+": "+NinjaUtil.getMoneys(values[i])+(i==4?"%":""),line,viewport,0x536DCC);
            line.y+=SkillPanelLayout.LineHeight;
            InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7_green2,NinjaUtil.getMoneys(costs[i])+" "+mResources.potential,line,viewport,0x00552D);
        }
    }
    private sealed class SkillLine {internal string Text;internal mFont Font;}
    private static void AddSkillLines(List<SkillLine> lines,string text,mFont font,float width)
    {
        if(string.IsNullOrWhiteSpace(text))return;
        foreach(string line in font.splitFontArray(text,Mathf.Max(1,(int)width-4)))lines.Add(new SkillLine{Text=line,Font=font});
    }
    private static int PaintSkillLines(mGraphics g,List<SkillLine> lines,Rect viewport,int scroll,int initial=0)
    {
        InventoryFrame.Clip(g,viewport);int y=(int)viewport.y+initial-scroll;
        foreach(SkillLine line in lines){InventoryItemVisuals.PaintCompactText(g,line.Font,line.Text,new Rect(viewport.x,y,viewport.width,12),viewport,line.Font==mFont.tahoma_7_green2?0x00552D:InventoryFrame.Border);y+=12;}
        return initial+lines.Count*12;
    }
    private void PaintSkillDetail(mGraphics g)
    {
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
        var top=new List<SkillLine>();
        if(display!=null)AddSkillLines(top,mResources.KI_consume+display.manaUse+(t.manaUseType==1?"%":"")+" · "+mResources.cooldown+": "+display.strTimeReplay()+"s",mFont.tahoma_7,upper.width);
        AddSkillDamage(top,t,level,level==t.maxPoint?" (hiện tại, tối đa)":" (hiện tại)",mFont.tahoma_7b_dark,upper.width);
        if(level<t.maxPoint)AddSkillDamage(top,t,level+1,level+1==t.maxPoint?" (kế tiếp, tối đa)":" (kế tiếp)",mFont.tahoma_7_green2,upper.width);
        if(level+1<t.maxPoint)AddSkillDamage(top,t,t.maxPoint," (tối đa)",mFont.tahoma_7,upper.width);
        int tableEnd=PaintSkillLines(g,top,upper,skillTopScroll,headingHeight);
        InventoryFrame.Clip(g,upper);g.setColor(InventoryFrame.Border);
        g.fillRect((int)upper.x,(int)upper.y+tableEnd-skillTopScroll+3,(int)upper.width,1);
        var bottom=new List<SkillLine>();
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
    private static string SkillDescription(SkillTemplate template,Skill current)
    {
        var description=new List<string>();
        foreach(string line in template.description??new string[0]) {
            string clean=CleanSkillText(line);if(clean.Length>0)description.Add(clean);
        }
        if(description.Count>0)return string.Join(" ",description.ToArray());
        // The server sends "null" for the template description; level info comes from the game data.
        string info=CleanSkillText(current?.moreInfo);
        if(info.Length==0&&template.skills.Length>0)info=CleanSkillText(template.skills[0].moreInfo);
        return info;
    }
    private static void AddSkillDamage(List<SkillLine> rows,SkillTemplate t,int level,string suffix,mFont font,float width)
    {
        if(level<=0||level>t.skills.Length)return;
        Skill s=t.skills[level-1];string value=s.damage+(t.damInfo!=null&&t.damInfo.Contains("%")?"%":"");
        rows.Add(new SkillLine{Text="Cấp "+level+suffix+": "+value,Font=font});
    }
    private static void PaintSkillButton(mGraphics g,Rect r,string text,bool enabled,bool selected)
    {
        InventoryFrame.Clip(g,r);
        InventoryItemVisuals.PaintNativeButton(g,r,enabled,selected);
        InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7b_dark,text,InventoryFrame.Inset(r),r,enabled?InventoryFrame.Border:0x626262,true);
    }
}
