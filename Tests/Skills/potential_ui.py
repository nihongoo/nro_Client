"""Run real B2 panel/input/state/layout with the existing PC/S22 fixtures."""
from pathlib import Path
import subprocess
import tempfile
ROOT=Path(__file__).resolve().parents[2]
scope={'__file__':str(ROOT/'Tests/Skills/tab.py')}
source=(ROOT/'Tests/Skills/tab.py').read_text(encoding='utf-8-sig')
exec(source[:source.index('output = ROOT / "Build/skill-tab-tests"')],scope)
fixture=scope['fixture'].split('class SkillTabTests')[0]
fixture+=r'''
class PotentialUiTests {
 static int checks;static void Check(bool b,string message){checks++;if(!b)throw new Exception(message);}
 static void Near(float a,float b,string s){Check(Math.Abs(a-b)<.001,s);}
 static TField Field=>(TField)typeof(Panel).GetField("potentialInput",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(GameCanvas.panel);
 static long LayoutVersion=>(long)typeof(SkillPanelLayout).GetField("metricsVersion",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
 static void Snapshot(int action,int id,long budget,int hp=50100,int ki=30200,int damage=5010,int armor=0,int crit=0){PotentialState.Read(action,new Message(id,1,budget,hp,ki,damage,armor,crit,220000,220000,11000,550,10));PotentialState.Update();}
 static void Click(Rect rect){Rect v=InventoryFrame.Intersection(rect,SkillPanelLayout.ScrollBounds);Check(v.width>0&&v.height>0,"control not reachable");GameCanvas.px=(int)v.center.x;GameCanvas.py=(int)v.center.y;GameCanvas.isPointerJustDown=GameCanvas.isPointerDown=GameCanvas.isPointerClick=true;GameCanvas.panel.Tick();GameCanvas.isPointerDown=false;GameCanvas.isPointerJustRelease=GameCanvas.isPointerClick=true;GameCanvas.panel.Tick();GameCanvas.clearAllPointerEvent();}
 static void ShowRow(int i){Rect r=SkillPanelLayout.PotentialBounds(i),v=SkillPanelLayout.ScrollBounds;GameCanvas.panel.cmy=GameCanvas.panel.cmtoY=Mathf.Clamp(GameCanvas.panel.cmy+(int)(r.y-v.y),0,GameCanvas.panel.cmyLim);HudLayout.Invalidate();}
 static void Body(int i){ShowRow(i);Rect r=SkillPanelLayout.PotentialBounds(i);r.width=SkillPanelLayout.PotentialAddBounds(i).x-r.x-2;Click(r);}
 static void Main(){
  var c=Char.myCharz();c.nClass.availableSkillTemplates=new SkillTemplate[8];for(int i=0;i<8;i++){var t=new SkillTemplate{id=(sbyte)i};t.skills=new[]{new Skill{template=t}};c.nClass.availableSkillTemplates[i]=t;c.learned[i]=t.skills[0];}
  foreach(bool mobile in new[]{false,true})foreach(int zoom in new[]{2,3,4})foreach(int[] size in mobile?new[]{new[]{422,195},new[]{585,270}}:new[]{new[]{640,360},new[]{1280,720}}){
   MainSetup(mobile,zoom,size);Snapshot(4,0,1000000000000L);GameCanvas.panel.Tick();
   Rect viewport=SkillPanelLayout.ScrollBounds,money=HudLayout.GetRect(InventoryFrame.Money),detail=SkillPanelLayout.DetailBounds;
   float grid=SkillPanelLayout.CellBounds(0).y,intrinsic=SkillPanelLayout.IntrinsicBounds.y;
   ShowRow(0);Click(SkillPanelLayout.PotentialAddBounds(0));var request=Service.gI().potentialRequests[Service.gI().potentialRequests.Count-1];
   Check(request[0]==5&&request[2]==0&&request[3]==50100&&request[4]==50120,"single step payload");
   Check(c.cHPGoc==50100&&PotentialState.Pending,"optimistic mutation / missing pending guard");int count=Service.gI().potentialRequests.Count;
   Click(SkillPanelLayout.PotentialAddBounds(0));Check(Service.gI().potentialRequests.Count==count,"repeated click sent another upgrade");
   Snapshot(5,(int)request[1],1000000000000L-51100,50120);Check(c.cHPGoc==50120&&!PotentialState.Pending,"server result not applied");
   var paint=new mGraphics();GameCanvas.panel.Paint(paint);Check(paint.texts.Exists(t=>t=="51120 Tiềm năng"),"next price did not refresh");
   Body(0);Check(GameCanvas.panel.PotentialExpandedRow==0,"body did not open inline editor");Check(Field.getText()==string.Empty,"editor should start empty for direct typing");
   Near(SkillPanelLayout.CellBounds(0).y+GameCanvas.panel.cmy,grid,"editor moved grid base geometry");Near(SkillPanelLayout.IntrinsicBounds.y+GameCanvas.panel.cmy,intrinsic,"editor moved intrinsic base geometry");
   Near(HudLayout.GetRect(InventoryFrame.Money).y,money.y,"editor moved currency footer");Near(SkillPanelLayout.DetailBounds.x,detail.x,"editor changed right detail");
   Near(SkillPanelLayout.PotentialEditorBounds.y,SkillPanelLayout.PotentialBounds(0).yMax,"editor not joined to card");
   Near(SkillPanelLayout.PotentialBounds(1).y-SkillPanelLayout.PotentialBounds(0).yMax,SkillPanelLayout.PotentialEditorHeight+SkillPanelLayout.PotentialGap,"following cards do not move below editor");
   Field.setText("99.999");GameCanvas.panel.Tick();Click(SkillPanelLayout.PotentialApplyBounds);
   request=Service.gI().potentialRequests[Service.gI().potentialRequests.Count-1];Check(request[4]==99980,"absolute target not floored to HP tier");
   Snapshot(5,(int)request[1],999999999999L,99980);Body(0);Check(GameCanvas.panel.PotentialExpandedRow==-1,"retap did not close editor");
   Body(1);Check(GameCanvas.panel.PotentialExpandedRow==1,"KI editor not opened");
   Click(SkillPanelLayout.PotentialMaxBounds);Check(PotentialCost.Parse(Field.getText())==220000,"max ignores server limit");
   Field.isFocus=true;TField.currentTField=Field;TField.kb=new NativeKeyboard{text="999999999999999999999999999"};
   Field.setText("999999999999999999999999999");GameCanvas.panel.Tick();Check(PotentialCost.Parse(Field.getText())==220000&&PotentialCost.Parse(TField.kb.text)==220000,"oversized native keyboard input was not clamped in both places");Click(SkillPanelLayout.PotentialApplyBounds);
   request=Service.gI().potentialRequests[Service.gI().potentialRequests.Count-1];Check(request[4]==220000,"oversized input not clamped");
   Snapshot(5,(int)request[1],999999999999L,99980,220000);Body(1);Check(GameCanvas.panel.PotentialExpandedRow==-1,"cannot collapse capped card");
   Snapshot(4,0,1000000000000L);Body(2);Field.setText("5000");GameCanvas.panel.Tick();count=Service.gI().potentialRequests.Count;Click(SkillPanelLayout.PotentialApplyBounds);Check(Service.gI().potentialRequests.Count==count,"below-current input submitted");
   Body(3);Check(GameCanvas.panel.PotentialExpandedRow==-1,"other card should close current editor");Body(3);Check(GameCanvas.panel.PotentialExpandedRow==3,"second tap should open other card");
   long version=LayoutVersion;int measured=mFont.widthCalls;for(int i=0;i<100;i++){HudLayout.BeginFrame();Field.setText((i+1).ToString());_=SkillPanelLayout.PotentialInputBounds;_=SkillPanelLayout.PotentialAddBounds(4);}
   Check(LayoutVersion==version&&mFont.widthCalls==measured,"typing/idle recomputed layout");
   Body(3);Snapshot(4,0,0);ShowRow(0);count=Service.gI().potentialRequests.Count;Click(SkillPanelLayout.PotentialAddBounds(0));Body(0);Check(Service.gI().potentialRequests.Count==count&&GameCanvas.panel.PotentialExpandedRow<0,"unaffordable card accepted input");
   Snapshot(4,0,1000000000000L);ShowRow(0);Click(SkillPanelLayout.PotentialAddBounds(0));request=Service.gI().potentialRequests[Service.gI().potentialRequests.Count-1];
   mSystem.now+=5001;PotentialState.Update();Check(!PotentialState.Ready&&!PotentialState.Pending,"timeout did not request resync");Check(Service.gI().potentialRequests[Service.gI().potentialRequests.Count-1][0]==4,"timeout replayed charge instead of querying state");
   int before=c.cHPGoc;Snapshot(5,(int)request[1],1,999);Check(c.cHPGoc==before,"late response from timed-out request applied");Snapshot(4,0,1000000000000L);
  }
  Console.WriteLine("PASS: "+checks+" B2 PC/S22 editor/state/input/cache checks");
 }
 static void MainSetup(bool mobile,int zoom,int[] size){TField.kb=null;global::Main.isPC=!mobile;UnityEngine.Device.Application.platform=mobile?RuntimePlatform.Android:RuntimePlatform.WindowsPlayer;GameCanvas.w=size[0];GameCanvas.h=size[1];mGraphics.zoomLevel=zoom;SkillTabState.Reset();GameCanvas.panel.Show();GameCanvas.currentDialog=null;}
}
'''
out=ROOT/'Build/potential-ui-tests';out.mkdir(parents=True,exist_ok=True)
with tempfile.TemporaryDirectory(prefix='b2-',dir=out) as folder:
 p=Path(folder);frame=(ROOT/'Assets/Scripts/Hud/InventoryFrame.cs').read_text(encoding='utf-8-sig');(p/'Frame.cs').write_text(frame.split('public partial class Panel')[0],encoding='utf-8');(p/'Ui.cs').write_text(fixture,encoding='utf-8');exe=p/'Ui.exe'
 subprocess.run([r'D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe','/nologo',f'/out:{exe}',str(p/'Frame.cs'),str(p/'Ui.cs'),*[str(ROOT/'Assets/Scripts/Hud'/f) for f in ['HudLayout.cs','SkillPanel.cs','SkillTabState.cs','PotentialPanel.cs','PotentialCost.cs','PotentialState.cs']]],check=True)
 subprocess.run([str(exe)],check=True)
