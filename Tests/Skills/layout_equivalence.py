"""Temporary SKILLDIAG comparison against the approved pre-memoization source.
No Unity build. Compare unchanged geometry and apply the intended shared scroll/clip
to every skills.* / inventory.* registry rect; preserve cache reuse.
"""
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
BASELINE = "62cd6af"
source = (ROOT / "Tests/Skills/tab.py").read_text(encoding="utf-8-sig")
# Evaluate just the fixture setup, before test compilation/execution.
scope = {"__file__": str(ROOT / "Tests/Skills/tab.py")}
exec(source[:source.index('output = ROOT / "Build/skill-tab-tests"')], scope)
fixture = scope['fixture']
fixture = fixture[:fixture.index('class SkillTabTests')]
fixture = fixture.replace('internal static void Count(Counter counter,int amount=1){}',
    'internal static readonly long[] Totals=new long[(int)Counter.Count]; internal static void Count(Counter counter,int amount=1){Totals[(int)counter]+=amount;}')
fixture += r"""
class LayoutEquivalence {
 static int checks, cases;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception("[SKILLDIAG] layoutMismatch "+message);}
 static void RectEqual(Rect a,Rect b,string name){
  // Exact floats, not a pixel tolerance: also catch rounding/order changes.
  Check(a.x==b.x&&a.y==b.y&&a.width==b.width&&a.height==b.height,
   name+" old="+a.x+","+a.y+","+a.width+","+a.height+" new="+b.x+","+b.y+","+b.width+","+b.height);
 }
 static List<string> Names(Type type){
  var dictionary=(System.Collections.IDictionary)type.GetField("byName",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).GetValue(null);
  var result=new List<string>();foreach(string key in dictionary.Keys)if(key.StartsWith("skills.")||key.StartsWith("inventory."))result.Add(key);result.Sort();return result;
 }
 static void Compare(){
  HudLayout.BeginFrame();LegacyHudLayout.BeginFrame();
  _=HudLayout.GetRect(InventoryFrame.Root);_=LegacyHudLayout.GetRect(LegacyInventoryFrame.Root);
  List<string> names=Names(typeof(HudLayout)),oldNames=Names(typeof(LegacyHudLayout));
  Check(names.Count==oldNames.Count,"registered name count");
  for(int i=0;i<names.Count;i++){
   Check(names[i]==oldNames[i],"registered names differ");
   Rect expected=LegacyHudLayout.GetRect(names[i]);
   if(names[i]==SkillPanelLayout.Left)expected=LegacySkillPanelLayout.Frame;
   else if(names[i].StartsWith("skills.cell.")) {expected=LegacySkillPanelLayout.CellBounds(int.Parse(names[i].Substring("skills.cell.".Length)));expected.y-=GameCanvas.panel.cmy;expected=InventoryFrame.Intersection(expected,LegacySkillPanelLayout.Frame);}
   else if(names[i]==SkillPanelLayout.Intrinsic) {expected=LegacySkillPanelLayout.IntrinsicBounds;expected.y-=GameCanvas.panel.cmy;expected=InventoryFrame.Intersection(expected,LegacySkillPanelLayout.Frame);}
   else if(names[i].StartsWith("skills.potential."))expected=InventoryFrame.Intersection(LegacySkillPanelLayout.PotentialBounds(int.Parse(names[i].Substring("skills.potential.".Length))),LegacySkillPanelLayout.Frame);
   RectEqual(expected,HudLayout.GetRect(names[i]),names[i]);
  }
  foreach(var property in typeof(SkillPanelLayout).GetProperties(System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)) {
   if(property.Name=="RectVersion")continue;
   var old=typeof(LegacySkillPanelLayout).GetProperty(property.Name,System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
   if(old==null)continue;
   object expected=old.GetValue(null,null),actual=property.GetValue(null,null);
   if(expected is Rect) {
    Rect before=(Rect)expected,after=(Rect)actual;
    if(property.Name=="ScrollBounds")before=LegacySkillPanelLayout.Frame;
    else if(property.Name=="IntrinsicBounds"||property.Name=="IntrinsicIconBounds"||property.Name=="IntrinsicBarBounds"||property.Name=="SkillsGroupBounds"||property.Name=="PotentialGroupBounds"||property.Name=="PotentialContent"||property.Name=="PotentialHeaderBounds")before.y-=GameCanvas.panel.cmy;
    if(property.Name=="PotentialGroupBounds"||property.Name=="PotentialContent")before.height=after.height; // Expanded to contain all five stat rows.
    RectEqual(before,after,"property:"+property.Name);
   }
   else if(property.Name=="PotentialGroupTop")Check((float)expected-GameCanvas.panel.cmy==(float)actual,"potential group offset");
   else Check(expected.Equals(actual),"property:"+property.Name+" old="+expected+" new="+actual);
  }
  cases++;
 }
 static void Main(){
  var c=Char.myCharz();c.nClass.availableSkillTemplates=new SkillTemplate[8];
  for(int i=0;i<8;i++){var t=new SkillTemplate{id=(sbyte)i};t.skills=new Skill[7];for(int n=0;n<7;n++)t.skills[n]=new Skill{template=t,point=n+1};c.nClass.availableSkillTemplates[i]=t;c.learned[i]=t.skills[0];}
  foreach(bool mobile in new[]{false,true})foreach(int zoom in new[]{1,2,3,4})
  foreach(int[] viewport in mobile?new[]{new[]{422,195},new[]{585,270},new[]{844,390}}:new[]{new[]{640,360},new[]{960,540},new[]{1280,720}}){
   UnityEngine.Device.Application.platform=mobile?RuntimePlatform.Android:RuntimePlatform.WindowsPlayer;
   global::Main.isPC=!mobile;GameCanvas.w=viewport[0];GameCanvas.h=viewport[1];mGraphics.zoomLevel=zoom;
   GameCanvas.panel.Show();
   foreach(int tab in new[]{1,2})foreach(bool book in new[]{false,true}) {
    GameCanvas.panel.currentTabIndex=tab;SkillTabState.Ready=true;SkillTabState.FinishToken=-1;
    c.arrItemBag[0]=book?new Item{template=new ItemTemplate{id=67},quantity=1}:null;
    GameCanvas.panel.InventoryBagDetailOpen=tab==1;HudLayout.Invalidate();LegacyHudLayout.Invalidate();
    Compare();
    long builds=SkillDiagnostics.Totals[(int)SkillDiagnostics.Counter.MetricsRebuild];
    GameCanvas.panel.cmy=37;HudLayout.Invalidate();LegacyHudLayout.Invalidate();
    // Only the modern provider is observed here: scroll must not rebuild geometry.
    _=HudLayout.GetRect(SkillPanelLayout.Potential(0));
    Check(builds==SkillDiagnostics.Totals[(int)SkillDiagnostics.Counter.MetricsRebuild],"scroll changed layout key");
    Compare();GameCanvas.panel.cmy=0;
   }
   GameCanvas.panel.currentTabIndex=2;GameCanvas.panel.Show();
   var closed=typeof(Panel).GetField("skillDetailOpen",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
   closed.SetValue(GameCanvas.panel,false);HudLayout.Invalidate();LegacyHudLayout.Invalidate();Compare();
   closed.SetValue(GameCanvas.panel,true);c.cTiemNang=7;c.cNangdong=123456;Compare();c.cTiemNang=9999999999;c.cNangdong=0;
  }
  // A font replacement, platform switch at the same dimensions, or viewport change
  // must invalidate the geometry key without relying on scroll or selection.
  mFont.tahoma_7_yellow=new mFont();mFont.tahoma_7b_dark=new mFont();mFont.tahoma_7b_blue=new mFont();mFont.tahoma_7_green2=new mFont();Compare();
  UnityEngine.Device.Application.platform=RuntimePlatform.WindowsPlayer;Compare();
  UnityEngine.Device.Application.platform=RuntimePlatform.Android;Compare();
  GameCanvas.panel.Show();_=HudLayout.GetRect(SkillPanelLayout.Potential(0));
  long metrics=SkillDiagnostics.Totals[(int)SkillDiagnostics.Counter.MetricsRebuild],frameMetrics=SkillDiagnostics.Totals[(int)SkillDiagnostics.Counter.FrameMetricsRebuild],misses=SkillDiagnostics.Totals[(int)SkillDiagnostics.Counter.RectMiss];
  int measured=mFont.widthCalls;long metricChecks=SkillDiagnostics.Totals[(int)SkillDiagnostics.Counter.MetricsCall];
  for(int repeat=0;repeat<100;repeat++) {
   HudLayout.BeginFrame();
   for(int i=0;i<5;i++)_=HudLayout.GetRect(SkillPanelLayout.Potential(i));
   for(int i=0;i<8;i++)_=HudLayout.GetRect(SkillPanelLayout.Cell(i));
   _=HudLayout.GetRect(InventoryFrame.Root);_=HudLayout.GetRect(InventoryFrame.Detail);
  }
  // Warm the remaining providers before verifying all subsequent frames are cache hits.
  misses=SkillDiagnostics.Totals[(int)SkillDiagnostics.Counter.RectMiss];
  for(int repeat=0;repeat<100;repeat++){HudLayout.BeginFrame();for(int i=0;i<5;i++)_=HudLayout.GetRect(SkillPanelLayout.Potential(i));}
  Check(metrics==SkillDiagnostics.Totals[(int)SkillDiagnostics.Counter.MetricsRebuild],"idle rebuilt skill layout");
  Check(frameMetrics==SkillDiagnostics.Totals[(int)SkillDiagnostics.Counter.FrameMetricsRebuild],"idle rebuilt frame layout");
  Check(misses==SkillDiagnostics.Totals[(int)SkillDiagnostics.Counter.RectMiss],"BeginFrame expired static skills rects");
  Check(measured==mFont.widthCalls,"idle repeated font measurement");
  long idleMetricChecks=SkillDiagnostics.Totals[(int)SkillDiagnostics.Counter.MetricsCall]-metricChecks;
  Check(idleMetricChecks<=200,"idle repeated full metric validation within a tick");
  int dynamicCalls=0;HudLayout.Register("diag.dynamic",()=>{dynamicCalls++;return new Rect(1,2,3,4);},0,()=>false);
  _=HudLayout.GetRect("diag.dynamic");_=HudLayout.GetRect("diag.dynamic");Check(dynamicCalls==1,"dynamic rect missed twice in a generation");
  HudLayout.BeginFrame();_=HudLayout.GetRect("diag.dynamic");Check(dynamicCalls==2,"other dynamic HUD lost frame invalidation");
  int hiddenCalls=0;HudLayout.Register("diag.hidden",()=>{hiddenCalls++;return new Rect();},1000,()=>false);
  HudLayout.HitTest(new Vector2(-100,-100));Check(hiddenCalls==0,"HitTest resolved an invisible entry");
  Console.WriteLine("[SKILLDIAG] layout-equivalence cases="+cases+" checks="+checks+" mismatches=0 idleFrames=200 layoutRebuilds=0 repeatedFontMeasures=0 metricChecks="+idleMetricChecks);
 }
}
"""
output = ROOT / 'Build/skill-layout-equivalence'
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix='compare-', dir=output) as folder:
    path = Path(folder)
    sources=[]
    for name in ['SkillPanel.cs','InventoryFrame.cs','HudLayout.cs']:
        old=subprocess.run(['git','show',f'{BASELINE}:Assets/Scripts/Hud/{name}'],cwd=ROOT,capture_output=True,check=True).stdout.decode('utf-8-sig')
        if name!='HudLayout.cs':old=old[:old.index('public partial class Panel')]
        for before,after in [('SkillPanelLayout','LegacySkillPanelLayout'),('InventoryFrame','LegacyInventoryFrame'),('HudLayout','LegacyHudLayout')]:old=re.sub(r'\b'+before+r'\b',after,old)
        if name=='HudLayout.cs':
            old=old.replace('GameScr.RegisterHudRects();','LegacyInventoryFrame.Register();LegacySkillPanelLayout.Register();')
            old=re.sub(r'public enum (?:HorizontalAnchor|VerticalAnchor) \{[^}]*\}', '',old)
        target=path/('Legacy'+name);target.write_text(old,encoding='utf-8');sources.append(str(target))
    frame=(ROOT/'Assets/Scripts/Hud/InventoryFrame.cs').read_text(encoding='utf-8-sig')
    target=path/'InventoryRegistry.cs';target.write_text(frame[:frame.index('public partial class Panel')],encoding='utf-8');sources.append(str(target))
    target=path/'Compare.cs';target.write_text(fixture,encoding='utf-8');sources.append(str(target))
    exe=path/'Compare.exe'
    subprocess.run([r'D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe','/nologo',f'/out:{exe}',*sources,
        *[str(ROOT/'Assets/Scripts/Hud'/name) for name in ['HudLayout.cs','SkillPanel.cs','SkillTabState.cs']]],check=True)
    subprocess.run([str(exe)],check=True)
