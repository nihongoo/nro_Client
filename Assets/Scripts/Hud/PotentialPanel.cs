using System;
using Hud;
using UnityEngine;

namespace Hud
{
    internal static partial class SkillPanelLayout
    {
        internal const float PotentialEditorHeight=80;
        internal const string PotentialInput="skills.potential.input",PotentialMax="skills.potential.max",PotentialApply="skills.potential.apply",PotentialEditor="skills.potential.editor";
        internal static string PotentialAdd(int i)=>"skills.potential.add."+i;
        private static readonly Rect[] potentialAdds=new Rect[5];
        private static Rect potentialEditor,potentialInput,potentialMax,potentialCost,potentialApply;
        private static void BuildPotentialControls(LayoutMetrics m)
        {
            for(int i=0;i<5;i++) {
                Rect r=potentialRows[i];float height=r.height*2/3;
                potentialAdds[i]=new Rect(r.xMax-Margin-30,r.center.y-height/2,30,height);
            }
            if(m.Expanded<0)return;
            Rect row=potentialRows[m.Expanded];
            potentialEditor=new Rect(row.x,row.yMax,row.width,PotentialEditorHeight);
            Rect content=InventoryFrame.Inset(potentialEditor);
            potentialInput=new Rect(content.x,content.y,Mathf.Max(1,content.width-Gap-44),22);
            potentialMax=new Rect(potentialInput.xMax+Gap,content.y,44,22);
            potentialCost=new Rect(content.x,potentialInput.yMax+Gap,content.width,14);
            potentialApply=new Rect(content.x,potentialCost.yMax+Gap,content.width,24);
        }
        internal static Rect PotentialAddBounds(int i){EnsureMetrics();return Scrolled(potentialAdds[i]);}
        internal static Rect PotentialEditorBounds {get{EnsureMetrics();return Scrolled(potentialEditor);}}
        internal static Rect PotentialInputBounds {get{EnsureMetrics();return Scrolled(potentialInput);}}
        internal static Rect PotentialMaxBounds {get{EnsureMetrics();return Scrolled(potentialMax);}}
        internal static Rect PotentialCostBounds {get{EnsureMetrics();return Scrolled(potentialCost);}}
        internal static Rect PotentialApplyBounds {get{EnsureMetrics();return Scrolled(potentialApply);}}
        private static bool PotentialEditorVisible=>Visible&&GameCanvas.panel.PotentialExpandedRow>=0;
        private static void RegisterPotentialControls()
        {
            for(int i=0;i<5;i++) {int row=i;HudLayout.Register(PotentialAdd(i),()=>InventoryFrame.Intersection(PotentialAddBounds(row),ScrollBounds),HudLayout.OverlayOrder+13,()=>Visible,cacheVersion:()=>RectVersion);}
            HudLayout.Register(PotentialEditor,()=>InventoryFrame.Intersection(PotentialEditorBounds,ScrollBounds),HudLayout.OverlayOrder+12,()=>PotentialEditorVisible,cacheVersion:()=>RectVersion);
            HudLayout.Register(PotentialInput,()=>InventoryFrame.Intersection(PotentialInputBounds,ScrollBounds),HudLayout.OverlayOrder+13,()=>PotentialEditorVisible,cacheVersion:()=>RectVersion);
            HudLayout.Register(PotentialMax,()=>InventoryFrame.Intersection(PotentialMaxBounds,ScrollBounds),HudLayout.OverlayOrder+13,()=>PotentialEditorVisible,cacheVersion:()=>RectVersion);
            HudLayout.Register(PotentialApply,()=>InventoryFrame.Intersection(PotentialApplyBounds,ScrollBounds),HudLayout.OverlayOrder+13,()=>PotentialEditorVisible,cacheVersion:()=>RectVersion);
        }
    }
}

public partial class Panel
{
    private sealed class PotentialInputDone : IActionListener {
        private readonly Panel owner;internal PotentialInputDone(Panel value){owner=value;}
        public void perform(int action,object parameter){owner.BlurPotentialInput(true);}
    }
    private int potentialExpanded=-1;
    private TField potentialInput;
    private string potentialQuoteText,potentialCostText="Tốn 0";
    private long potentialQuoteCurrent=-1,potentialQuoteBudget=-1,potentialQuoteLimit=-1,potentialTarget,potentialMaximum,potentialTotal;
    internal int PotentialExpandedRow=>potentialExpanded;
    private void ClosePotentialEditor()
    {
        BlurPotentialInput(false);potentialExpanded=-1;potentialQuoteText=null;HudLayout.Invalidate();
    }
    private void TogglePotentialEditor(int row)
    {
        if(potentialExpanded>=0){ClosePotentialEditor();SyncSkillPanel();return;}
        if(!PotentialState.RequireSupport())return;
        if(!PotentialState.CanRaise(row))return;
        potentialExpanded=row;
        if(potentialInput==null) {potentialInput=new TField();potentialInput.name=string.Empty;potentialInput.loginInput=true;potentialInput.setIputType(TField.INPUT_TYPE_NUMERIC);potentialInput.setMaxTextLenght(20);potentialInput.cmdDoneAction=new Command(string.Empty,new PotentialInputDone(this),0,null);}
        potentialInput.setText(string.Empty);
        potentialQuoteText=null;HudLayout.Invalidate();SyncSkillPanel();
        Rect editor=SkillPanelLayout.PotentialEditorBounds,viewport=SkillPanelLayout.ScrollBounds;
        cmy=cmtoY=Mathf.Clamp(cmy+Mathf.Max(0,(int)System.Math.Ceiling(editor.yMax-viewport.yMax)),0,cmyLim);
        HudLayout.Invalidate();RefreshPotentialQuote(false);
    }
    private void SyncPotentialInput()
    {
        if(potentialExpanded<0||potentialInput==null)return;
        Rect r=SkillPanelLayout.PotentialInputBounds;
        potentialInput.x=(int)r.x;potentialInput.y=(int)r.y+1;potentialInput.width=(int)r.width;potentialInput.height=Mathf.Max(1,(int)r.height-5);
    }
    private void RefreshPotentialQuote(bool normalize)
    {
        if(potentialExpanded<0||potentialInput==null)return;
        long current=PotentialState.Value(potentialExpanded),budget=Char.myCharz().cTiemNang,limit=PotentialState.Limits[potentialExpanded];
        string text=potentialInput.getText();
        if(text!=potentialQuoteText||current!=potentialQuoteCurrent||budget!=potentialQuoteBudget||limit!=potentialQuoteLimit) {
            potentialMaximum=PotentialCost.MaxTarget(potentialExpanded,current,budget,limit);
            potentialTarget=PotentialCost.Normalize(potentialExpanded,current,PotentialCost.Parse(text),potentialMaximum);
            if(!PotentialCost.TryCalculate(potentialExpanded,current,potentialTarget,out potentialTotal))potentialTotal=0;
            potentialCostText="Tốn "+NinjaUtil.getMoneys(potentialTotal);
            potentialQuoteText=text;potentialQuoteCurrent=current;potentialQuoteBudget=budget;potentialQuoteLimit=limit;
        }
        if(normalize||(PotentialState.Ready&&PotentialCost.Parse(potentialInput.getText())>potentialMaximum)) {
            string value=NinjaUtil.getMoneys(potentialTarget);
            if(value!=potentialInput.getText()) {
                potentialInput.setText(value);potentialQuoteText=value;
                if(potentialInput.isFocus&&TField.currentTField==potentialInput&&TField.kb!=null)TField.kb.text=value;
            }
        }
    }
    private void BlurPotentialInput(bool normalize)
    {
        if(potentialInput==null)return;
        if(normalize)RefreshPotentialQuote(true);
        bool focused=potentialInput.isFocus;
        if(focused){potentialInput.setFocusWithKb(false);GameCanvas.closeKeyBoard();}
    }
    private bool PotentialQuoteEnabled=>potentialExpanded>=0&&PotentialState.CanRaise(potentialExpanded)&&potentialTarget>PotentialState.Value(potentialExpanded)&&potentialTotal<=Char.myCharz().cTiemNang;
    private bool HandlePotentialClick(string hit)
    {
        if(hit==null)return false;
        if(hit.StartsWith("skills.potential.add.")) {
            int row=int.Parse(hit.Substring("skills.potential.add.".Length));
            PotentialState.Submit(row,PotentialState.Value(row)+PotentialCost.Step(row));return true;
        }
        if(hit==SkillPanelLayout.PotentialInput) {
            if(potentialExpanded>=0&&!PotentialState.Pending){SyncPotentialInput();potentialInput.setFocusWithKb(true);}return true;
        }
        if(hit==SkillPanelLayout.PotentialMax) {
            if(!PotentialState.RequireSupport())return true;
            if(potentialExpanded>=0&&!PotentialState.Pending){BlurPotentialInput(false);RefreshPotentialQuote(false);potentialInput.setText(NinjaUtil.getMoneys(potentialMaximum));RefreshPotentialQuote(false);}return true;
        }
        if(hit==SkillPanelLayout.PotentialApply) {
            if(!PotentialState.RequireSupport())return true;
            BlurPotentialInput(true);RefreshPotentialQuote(true);
            if(PotentialQuoteEnabled)PotentialState.Submit(potentialExpanded,potentialTarget);return true;
        }
        if(hit.StartsWith("skills.potential.")&&int.TryParse(hit.Substring("skills.potential.".Length),out int selectedRow)) {TogglePotentialEditor(selectedRow);return true;}
        return hit==SkillPanelLayout.PotentialEditor;
    }
    private bool UpdatePotentialKeys()
    {
        if(potentialExpanded<0||potentialInput==null)return false;
        SyncPotentialInput();potentialInput.update(false);RefreshPotentialQuote(false);
        if(!potentialInput.isFocus)return false;
        if(GameCanvas.keyAsciiPress!=0){potentialInput.keyPressed(GameCanvas.keyAsciiPress);GameCanvas.keyAsciiPress=0;RefreshPotentialQuote(false);}
        if(GameCanvas.keyPressed[13]||GameCanvas.keyPressed[(!Main.isPC)?5:25]){GameCanvas.keyPressed[13]=GameCanvas.keyPressed[(!Main.isPC)?5:25]=false;BlurPotentialInput(true);}
        return true;
    }
    private void PaintPotentialEditor(mGraphics g,Rect viewport)
    {
        if(potentialExpanded<0||potentialInput==null)return;
        SyncPotentialInput();RefreshPotentialQuote(false);
        Rect input=SkillPanelLayout.PotentialInputBounds,clip=InventoryFrame.Intersection(input,viewport);
        if(clip.width>0&&clip.height>0)potentialInput.paint(g,(int)clip.x,(int)clip.y,(int)clip.width,(int)clip.height,false);
        PaintPotentialButton(g,SkillPanelLayout.PotentialMaxBounds,"Tối đa",!PotentialState.Pending&&PotentialState.CanRaise(potentialExpanded),viewport);
        InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7b_dark,potentialCostText,SkillPanelLayout.PotentialCostBounds,viewport,InventoryFrame.Border);
        PaintPotentialButton(g,SkillPanelLayout.PotentialApplyBounds,"Nâng cấp",PotentialQuoteEnabled,viewport);
    }
    private static void PaintPotentialButton(mGraphics g,Rect bounds,string text,bool enabled,Rect viewport)
    {
        Rect clip=InventoryFrame.Intersection(bounds,viewport);if(clip.width<=0||clip.height<=0)return;
        // Native button regions receive the intersection explicitly; never escape the scroll viewport.
        InventoryItemVisuals.PaintNativeButton(g,bounds,enabled,false,viewport);
        InventoryItemVisuals.PaintCompactText(g,mFont.tahoma_7b_dark,text,bounds,viewport,enabled?InventoryFrame.Border:0x626262,true);
    }
}
