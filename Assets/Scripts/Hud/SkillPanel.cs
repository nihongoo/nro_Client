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
        internal static Rect Content => new Rect(3,76,HudLayout.GetRect(InventoryFrame.Root).width-6,Mathf.Max(0,HudLayout.GetRect(InventoryFrame.Money).y-78));
        internal const int Margin=6, Gap=4;
        internal static float Size => Mathf.Min(InventoryFrame.Unit(HudLayout.IsMobileLayout ? 36 : 48), (Content.width-28)/4);
        internal static Rect ScrollBounds => new Rect(Content.x,CellBounds(4).yMax+Gap,Content.width,Mathf.Max(0,Content.yMax-CellBounds(4).yMax-Gap));
        internal static Rect CellBounds(int i)
        {
            float gap=(Content.width-4*Size)/5;
            return new Rect(Content.x+gap+i%4*(Size+gap), Content.y+Gap+i/4*(Size+Gap), Size,Size);
        }
        internal static Rect IntrinsicBounds => new Rect(Content.x+3,ScrollBounds.y-(GameCanvas.panel?.SkillScroll??0),Content.width-6,16);
        internal static Rect PotentialBounds(int i) => new Rect(Content.x+3,IntrinsicBounds.yMax+34+i*20,Content.width-6,18);
        internal static Rect DetailBounds => HudLayout.GetRect(InventoryFrame.Detail);
        internal static float ControlsTop => DetailBounds.yMax-93;
        internal static Rect UpperBounds => new Rect(DetailBounds.x+Margin,Margin,DetailBounds.width-2*Margin,Mathf.Max(1,ControlsTop-2*Margin));
        internal static Rect KeyBounds(int i)
        {
            Rect r=DetailBounds; float width=(r.width-18)/4;
            return new Rect(r.x+6+i%4*(width+2),ControlsTop+13+i/4*24,width,22);
        }
        internal static Rect ActionBounds(int index)
        {
            Rect r=DetailBounds; bool book=GameCanvas.panel.SkillHasBook;
            float width=book ? (r.width-16)/2 : r.width-12;
            return new Rect(r.x+6+index*(width+4),r.yMax-29,width,24);
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
            HudLayout.Register(Close,()=>new Rect(DetailBounds.xMax-21,3,18,18),HudLayout.OverlayOrder+18,()=>DetailVisible);
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
        SyncSkillPanel();Rect viewport=SkillPanelLayout.Content;SkillTemplate[] templates=SkillTabState.Templates();
        for(int i=0;i<templates.Length;i++) {
            SkillTemplate t=templates[i];Skill skill=Char.myCharz().getSkill(t);Rect cell=SkillPanelLayout.CellBounds(i);
            InventoryFrame.Clip(g,viewport);
            InventoryFrame.Rounded(g,cell,InventoryFrame.SlotBackground,255,skillDetailOpen&&skillSelectedId==t.id?InventoryFrame.Accent:InventoryFrame.Border);
            InventoryItemVisuals.PaintCompactIcon(g,t.iconId,new Rect(cell.x+2,cell.y+2,cell.width-4,cell.height-4),viewport);
            mFont.tahoma_7b_white.drawString(g,(i+1).ToString(),(int)cell.x+2,(int)cell.y+1,0);
            InventoryItemVisuals.PaintLevel(g,cell,viewport,skill?.point??0,false,true);
            if(SkillTabState.Studying(t.id))PaintStudyClock(g,cell,viewport);
        }
        viewport=SkillPanelLayout.ScrollBounds;
        Rect intrinsic=SkillPanelLayout.IntrinsicBounds;
        InventoryFrame.Clip(g,viewport);InventoryFrame.Rounded(g,intrinsic,InventoryFrame.SlotBackground);
        string status=CleanSkillText(specialInfo).Split('\n')[0];
        // The long effect and the old second-line hint remain in the existing intrinsic popup.
        if(status.Length==0||status.StartsWith("Chưa",StringComparison.OrdinalIgnoreCase))status="chưa kích hoạt";
        else status="đã kích hoạt";
        SkillLabel(g,mFont.tahoma_7_green2,"Nội tại: "+status,InventoryFrame.Intersection(intrinsic,viewport));
        Rect title=new Rect(intrinsic.x,intrinsic.yMax+4,intrinsic.width,28);
        InventoryFrame.Clip(g,viewport);InventoryFrame.Rounded(g,title,InventoryFrame.EquipmentBackground);
        InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7b_dark,"Tiềm năng · "+NinjaUtil.getMoneys(Char.myCharz().cTiemNang),new Rect(title.x,title.y,title.width,14),viewport,InventoryFrame.Border);
        SkillLabel(g,mFont.tahoma_7,mResources.active_point+": "+NinjaUtil.getMoneys(Char.myCharz().cNangdong),InventoryFrame.Intersection(new Rect(title.x,title.y+14,title.width,12),viewport));
        PaintSkillPotential(g,viewport);
        PaintSkillFade(g,viewport,cmy<cmyLim);
    }
    private static string CleanSkillText(string text) => string.IsNullOrWhiteSpace(text)||string.Equals(text.Trim(),"null",StringComparison.OrdinalIgnoreCase)?string.Empty:text.Trim();
    private static void PaintSkillFade(mGraphics g,Rect viewport,bool more)
    {
        if(!more)return;
        InventoryFrame.Clip(g,viewport);
        for(int i=0;i<8;i++) {g.setColor(InventoryFrame.MoneyBackground,(i+1)/9f);g.fillRect((int)viewport.x,(int)viewport.yMax-8+i,(int)viewport.width,1);}
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
            InventoryFrame.Rounded(g,row,InventoryFrame.EquipmentBackground);
            InventoryItemVisuals.PaintCompactIcon(g,icons[i],new Rect(row.x+2,row.y+3,12,12),viewport);
            Rect line=new Rect(row.x+19,row.y,row.width-53,9);
            InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7b_blue,names[i]+" "+mResources.root+": "+NinjaUtil.getMoneys(values[i])+(i==4?"%":""),line,viewport,0x536DCC);
            line.y+=9;
            InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7,NinjaUtil.getMoneys(costs[i])+" "+mResources.potential,line,viewport,0x00552D);
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
        InventoryFrame.Clip(g,r);InventoryFrame.Rounded(g,r,InventoryFrame.EquipmentBackground);
        int slot=0;SkillTemplate[] templates=SkillTabState.Templates();for(int i=0;i<templates.Length;i++)if(templates[i].id==t.id)slot=i;
        InventoryFrame.Clip(g,upper);
        Rect icon=new Rect(upper.x+1,upper.y+1-skillTopScroll,32,32);
        InventoryFrame.Rounded(g,icon,InventoryFrame.SlotBackground,255,InventoryFrame.Accent);
        SmallImage.drawSmallImage(g,t.iconId,(int)icon.center.x,(int)icon.center.y,0,mGraphics.HCENTER|mGraphics.VCENTER);
        string[] nameLines=mFont.tahoma_7b_dark.splitFontArray(t.name,Mathf.Max(1,(int)(upper.xMax-icon.xMax-25)));
        InventoryFrame.Clip(g,upper);
        for(int i=0;i<nameLines.Length;i++)mFont.tahoma_7b_dark.drawString(g,nameLines[i],(int)icon.xMax+5,(int)icon.y+i*12,0);
        SkillLabel(g,mFont.tahoma_7,"Cấp "+level+" / "+t.maxPoint+" · ô "+(slot+1),InventoryFrame.Intersection(new Rect(icon.xMax+5,icon.y+nameLines.Length*12,Mathf.Max(0,upper.xMax-icon.xMax-5),15),upper));
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
        foreach(string line in t.description??new string[0])AddSkillLines(bottom,CleanSkillText(line),mFont.tahoma_7,lower.width);
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
        SkillLabel(g,mFont.tahoma_7,"Ô phím",new Rect(r.x+6,SkillPanelLayout.ControlsTop,r.width-12,12));
        for(int i=0;i<8;i++)PaintSkillButton(g,SkillPanelLayout.KeyBounds(i),(i+1).ToString(),SkillTabState.Ready,i==slot);
        bool maximum=level>=t.maxPoint;
        string caption=maximum?"Đã tối đa":studying?"Học nhanh · "+SkillTabState.QuickPrice+" ngọc":SkillHasBook?"Dùng sách":SkillTabState.Waiting?"Đang tải...":"Đến NPC";
        PaintSkillButton(g,SkillPanelLayout.ActionBounds(0),caption,!maximum&&(!SkillTabState.Waiting||SkillHasBook)||studying,false);
        if(SkillHasBook)PaintSkillButton(g,SkillPanelLayout.ActionBounds(1),"Đến NPC",true,false);
        Rect close=HudLayout.GetRect(SkillPanelLayout.Close);InventoryFrame.Clip(g,close);InventoryFrame.Rounded(g,close,InventoryFrame.EquipmentBackground);
        g.drawImage(imgX,(int)close.center.x,(int)close.center.y,mGraphics.HCENTER|mGraphics.VCENTER);
        InventoryFrame.Clip(g,HudLayout.ScreenBounds);
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
        if(!enabled)InventoryFrame.Rounded(g,r,0xB3ADA2,255,InventoryFrame.Border);
        else Command.paintOngMau(selected?Command.btn1left:Command.btn0left,selected?Command.btn1mid:Command.btn0mid,selected?Command.btn1right:Command.btn0right,(int)r.x,(int)r.y,(int)r.width,g);
        InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7b_dark,text,r,r,enabled?InventoryFrame.Border:0x626262,true);
    }
}
