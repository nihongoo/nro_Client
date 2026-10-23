"""Real pointer -> Service serializer -> Java B2 router/handler -> client decoder.

Uses isolated player/transport fixtures; does not connect to or restart the live server.
"""
from pathlib import Path
import subprocess
import tempfile

ROOT=Path(__file__).resolve().parents[2]
SERVER=Path(r'D:\dowload\SRC NRO NEW 03\SRC')
scope={'__file__':str(ROOT/'Tests/Skills/tab.py')}
source=(ROOT/'Tests/Skills/tab.py').read_text(encoding='utf-8-sig')
exec(source[:source.index('output = ROOT / "Build/skill-tab-tests"')],scope)
fixture=scope['fixture'].split('class SkillTabTests')[0]
service=(ROOT/'Assets/Scripts/Service.cs').read_text(encoding='utf-8-sig')
method=service[service.index('    public void potentialRequest('):service.index('    public void skillTabRequest(')]
fixture=fixture.replace('public void potentialRequest(int a,int id,int type,long expected,long target){potentialRequests.Add(new long[]{a,id,type,expected,target});}', 'public Session session=new Session();\n'+method)
start=fixture.index('class Message {');end=fixture.index('namespace Notifications',start)
fixture=fixture[:start]+fixture[end:]
fixture=fixture.replace('public static long now=100000;', 'public static sbyte[] convertToSbyte(byte[] b)=>Array.ConvertAll(b,x=>(sbyte)x);public static long now=100000;')
fixture+=r'''
namespace UnityEngine {public class TextAsset {public byte[] bytes;}public static class Resources {public static object Load(string f,Type t)=>null;}}
static class Cout {public static void println(string s){throw new Exception(s);}}
class Session {
 public static string ClassPath;public System.Diagnostics.Process java;public byte[] last;public int sent;
 public void sendMessage(Message m){
  if(java==null){java=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("java","-cp \""+ClassPath+"\" ProtocolBridge"){UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,CreateNoWindow=true});}
  byte[] payload=myReader.convertSbyteToByte(m.getData());last=new byte[payload.Length+1];last[0]=(byte)m.command;Array.Copy(payload,0,last,1,payload.Length);sent++;
  java.StandardInput.WriteLine(BitConverter.ToString(last).Replace("-",""));java.StandardInput.Flush();
  string hex=java.StandardOutput.ReadLine();if(hex==null)throw new Exception("Java bridge failed");
  byte[] response=new byte[hex.Length/2];for(int i=0;i<response.Length;i++)response[i]=Convert.ToByte(hex.Substring(i*2,2),16);
  if(response[0]!=104)throw new Exception("wrong response command");
  SkillTabState.Read(new Message((sbyte)104,mSystem.convertToSbyte(new List<byte>(response).GetRange(1,response.Length-1).ToArray())));
 }
}
class ProtocolTests {
 static int checks;static void Check(bool b,string m){checks++;if(!b)throw new Exception(m);}
 static void Click(int i){HudLayout.BeginFrame();Rect row=SkillPanelLayout.PotentialBounds(i),v=SkillPanelLayout.ScrollBounds;var p=GameCanvas.panel;p.cmy=p.cmtoY=Mathf.Clamp(p.cmy+(int)(row.y-v.y),0,p.cmyLim);HudLayout.Invalidate();Rect r=InventoryFrame.Intersection(SkillPanelLayout.PotentialAddBounds(i),v);Check(r.width>0&&r.height>0,"button clipped");GameCanvas.px=(int)r.center.x;GameCanvas.py=(int)r.center.y;Check(HudLayout.HitTest(new Vector2(GameCanvas.px,GameCanvas.py))==SkillPanelLayout.PotentialAdd(i),"parent intercepted button "+i+" hit="+HudLayout.HitTest(new Vector2(GameCanvas.px,GameCanvas.py)));GameCanvas.isPointerDown=GameCanvas.isPointerJustDown=GameCanvas.isPointerClick=true;p.Tick();GameCanvas.isPointerDown=false;GameCanvas.isPointerJustRelease=GameCanvas.isPointerClick=true;p.Tick();GameCanvas.clearAllPointerEvent();}
 static void Main(string[] args){Session.ClassPath=args[0];var c=Char.myCharz();c.nClass.availableSkillTemplates=new SkillTemplate[0];
  foreach(bool mobile in new[]{false,true}){
   Main.isPC=!mobile;UnityEngine.Device.Application.platform=mobile?RuntimePlatform.Android:RuntimePlatform.WindowsPlayer;GameCanvas.w=mobile?422:640;GameCanvas.h=mobile?195:360;mGraphics.zoomLevel=2;
   SkillTabState.Reset();Service.current=new Service();GameCanvas.currentDialog=null;GameCanvas.panel.Show();GameCanvas.panel.Tick(); // OpenSkillPanel requests B2 through actual Service method.
   var transport=Service.gI().session;
   try {
    Check(BitConverter.ToString(transport.last)=="68-04","snapshot request bytes");PotentialState.Update();Check(PotentialState.Ready,"action 4 not decoded");
    Check(c.cTiemNang==1207721156L&&c.cHPGoc==52100,"server snapshot not applied");
    for(int i=0;i<5;i++){
     Check(PotentialState.CanRaise(i),"affordable button disabled "+i);long old=PotentialState.Value(i),budget=c.cTiemNang,target=old+PotentialCost.Step(i);Check(PotentialCost.TryCalculate(i,old,target,out long cost),"price");int sent=transport.sent;
     Click(i);Check(transport.sent==sent+1&&transport.last.Length==23&&transport.last[0]==104&&transport.last[1]==5&&transport.last[6]==i,"upgrade not sent as 104/5");
     Check(PotentialState.Pending,"pending missing");PotentialState.Update();Check(!PotentialState.Pending&&PotentialState.Value(i)==target&&c.cTiemNang==budget-cost,"round trip update mismatch");
    }
    long before=c.cTiemNang;Check(PotentialCost.TryCalculate(0,c.cHPGoc,c.cHPGoc+200,out long total),"batch price");Check(PotentialState.Submit(0,c.cHPGoc+200),"batch submit");PotentialState.Update();Check(c.cHPGoc==52320&&c.cTiemNang==before-total,"batch result");
    before=c.cTiemNang;int hp=c.cHPGoc;Service.gI().potentialRequest(5,0,0,hp,hp+1);PotentialState.Update();Check(c.cHPGoc==hp&&c.cTiemNang==before,"invalid target mutation");
    c.cTiemNang=0;Check(!PotentialState.CanRaise(0)&&!PotentialState.Submit(0,hp+20),"empty budget enabled");
   }finally{if(transport.java!=null){transport.java.StandardInput.Close();transport.java.WaitForExit();}}
  }
  Console.WriteLine("PASS: "+checks+" PC/mobile pointer, exact 104/4/5 bytes, Java handler and real client decode checks");
 }
}
'''.replace('Main.isPC=!mobile','global::Main.isPC=!mobile')

server_scope={'__file__':str(SERVER/'tests/skills/batch.py')}
batch=(SERVER/'tests/skills/batch.py').read_text(encoding='utf-8-sig')
exec(batch[:batch.index("out=ROOT/")],server_scope)
files=server_scope['files'];files.pop('BatchTests.java')
# Extract the production dispatch prefix verbatim; B1 actions are outside this test.
learning=(SERVER/'src/nro/models/services/SkillLearningService.java').read_text(encoding='utf-8-sig')
route=learning[learning.index('    public static void handle('):learning.index('        synchronized (player) {',learning.index('    public static void handle('))]
files['nro/models/services/SkillLearningService.java']='package nro.models.services;import java.io.*;import nro.models.player.Player;import nro.models.network.Message;public class SkillLearningService {public static final byte COMMAND=104;'+route+'throw new AssertionError("unexpected B1 action");}}'
files['ProtocolBridge.java']=r'''import java.io.*;import java.util.*;import nro.models.player.*;import nro.models.network.*;import nro.models.services.*;
public class ProtocolBridge {public static void main(String[] args)throws Exception {Player p=new Player();p.nPoint.hpg=52100;p.nPoint.mpg=30200;p.nPoint.tiemNang=1207721156L;p.nPoint.limit=220000;Scanner s=new Scanner(System.in);while(s.hasNextLine()){byte[] b=HexFormat.of().parseHex(s.nextLine());if(b[0]!=104)throw new AssertionError("command");Message m=new Message(104);m.writer().write(b,1,b.length-1);SkillLearningService.handle(p,m);System.out.println("68"+HexFormat.of().formatHex(p.last));}}}'''

out=ROOT/'Build/potential-protocol-tests';out.mkdir(parents=True,exist_ok=True)
with tempfile.TemporaryDirectory(prefix='wire-',dir=out) as folder:
 p=Path(folder);java=[]
 for name,text in files.items():
  f=p/name;f.parent.mkdir(parents=True,exist_ok=True);f.write_text(text,encoding='utf-8');java.append(str(f))
 subprocess.run(['javac','--release','17','-encoding','UTF-8','-d',str(p),*java,str(SERVER/'src/nro/models/utils/PotentialCost.java'),str(SERVER/'src/nro/models/services/PotentialUpgradeService.java')],check=True)
 frame=(ROOT/'Assets/Scripts/Hud/InventoryFrame.cs').read_text(encoding='utf-8-sig');(p/'Frame.cs').write_text(frame.split('public partial class Panel')[0],encoding='utf-8');(p/'Wire.cs').write_text(fixture,encoding='utf-8');exe=p/'Wire.exe'
 sources=[ROOT/'Assets/Scripts'/f for f in ['Message.cs','myReader.cs','myWriter.cs']]+[ROOT/'Assets/Scripts/Hud'/f for f in ['HudLayout.cs','SkillPanel.cs','SkillTabState.cs','PotentialPanel.cs','PotentialCost.cs','PotentialState.cs']]
 subprocess.run([r'D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe','/nologo',f'/out:{exe}',str(p/'Frame.cs'),str(p/'Wire.cs'),*map(str,sources)],check=True)
 subprocess.run([str(exe),str(p)],check=True)
