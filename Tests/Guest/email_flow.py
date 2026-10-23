"""Exercise the real email flow/state helper with lightweight screen/network fixtures."""
from pathlib import Path
import subprocess
import tempfile
import re

ROOT = Path(__file__).resolve().parents[2]
legacy = (ROOT / "Assets/Scripts/Assets.src.f/Controller2.cs").read_text(encoding="utf-8-sig")
account = (ROOT / "Assets/Scripts/GuestCredentials.cs").read_text(encoding="utf-8-sig")
command = int(re.search(r"const sbyte Command = (-?\d+)", account).group(1))
assert command not in {int(value) for value in re.findall(r"case (-?\d+):", legacy)}, "Account command collides with a legacy packet handler"
assert "case -124:" in legacy, "HOLD/effect packet handler must remain intact"
controller = (ROOT / "Assets/Scripts/Controller.cs").read_text(encoding="utf-8-sig")
assert controller.index("if (msg.command == GuestCredentials.Command)") < controller.index("Controller2.readMessage(msg)")
panel_source = (ROOT / "Assets/Scripts/Panel.cs").read_text(encoding="utf-8-sig")
account_switch = panel_source[panel_source.index("        if (idAction == 10019)"):panel_source.index("        if (idAction == 10020)")]
fixture = r'''
using System;
using System.Collections.Generic;
using Assets.src.g;
interface IActionListener { void perform(int action,object value); }
class Command {
 public string caption; public IActionListener actionListener; public int idAction,x=123,y=456;
 public Command(string s,IActionListener listener,int action,object p){caption=s;actionListener=listener;idAction=action;}
 public void Click()=>actionListener.perform(idAction,null);
}
class MyVector { internal List<Command> commands=new List<Command>(); public void addElement(Command c)=>commands.Add(c); }
class TField {
 public const int INPUT_TYPE_NUMERIC=1,INPUT_TYPE_PASSWORD=2; public bool loginInput; public string name; public int max,type;
 string text=""; public void setText(string s){text=s;}public string getText()=>text;
 public void setMaxTextLenght(int value){max=value;}public void setIputType(int value){type=value;max=500;}
}
class Panel : IActionListener {public int switches;public void hide(){} public void perform(int idAction,object value){@@ACCOUNT_SWITCH@@} }
class CanvasActions {public void switchAccount(){GameCanvas.panel.switches++;}}
class Menu {public bool showMenu;public MyVector options;public void startAt(MyVector value,int index){options=value;} }
class LoginScr {public bool isLogin2=true;public TField tfUser=new TField(),tfPass=new TField();}
static class GameCanvas {
 public static CanvasActions instance=new CanvasActions();public static object currentScreen,currentDialog;public static Panel panel=new Panel();public static Menu menu=new Menu();public static LoginScr loginScr=new LoginScr();
 public static string error;public static void startOKDlg(string value){error=value;currentDialog=new object();} public static void endDlg(){currentDialog=null;}
 public static void closeKeyBoard(){} public static void clearAllPointerEvent(){}public static void clearKeyPressed(){}public static void clearKeyHold(){}
}
class GameScr {public static GameScr instance=new GameScr();public void switchToMe(){GameCanvas.currentScreen=this;} }
static class mResources {public const string CANCEL="Cancel";}
static class mSystem {public static long now=1000;public static long currentTimeMillis()=>now; public static sbyte[] convertToSbyte(byte[] bytes)=>Array.ConvertAll(bytes,b=>(sbyte)b);}
class Service {
 static Service instance=new Service();public static Service gI()=>instance;
 public int requests,confirms,stateRequests;public string email,code,password;
 public void requestAccountState(){stateRequests++;}
 public void requestEmailLink(string value){requests++;email=value;}
 public void confirmEmailLink(string value,string secret){confirms++;code=value;password=secret;}
}
namespace Assets.src.g {
 class ClientInput {
  static ClientInput instance;public TField[] tf;public Command center,left;
  public static ClientInput gI()=>instance??(instance=new ClientInput());
  public void setInput(int count,string title){tf=new TField[count];for(int i=0;i<count;i++)tf[i]=new TField();center=new Command("OK",null,0,null);left=new Command("Close",null,0,null);GameCanvas.currentScreen=this;}
  public void clearScreen(){instance=null;}
  public bool hasPasswordToggle;public void enablePasswordToggle(){hasPasswordToggle=true;}
 }
}
static class Rms {
 internal static Dictionary<string,string> data=new Dictionary<string,string>();
 internal static string loadRMSString(string key)=>data.ContainsKey(key)?data[key]:null;
 internal static void saveRMSString(string key,string value){data[key]=value;}
}
namespace UnityEngine { class TextAsset {public byte[] bytes;} static class Resources {public static object Load(string name,Type type)=>null;} static class Debug {public static void Log(object value){} } static class SystemInfo {internal static string deviceUniqueIdentifier="test-device",unsupportedIdentifier="n/a";} }
class Tests {
 static myReader Packet(params object[] values) {
  var data=new List<sbyte>();
  foreach(object value in values) {
   if(value is int number)data.Add((sbyte)number);
   else if(value is bool flag)data.Add((sbyte)(flag?1:0));
   else {byte[] bytes=System.Text.Encoding.UTF8.GetBytes((string)value);data.Add((sbyte)(bytes.Length>>8));data.Add((sbyte)bytes.Length);foreach(byte b in bytes)data.Add((sbyte)b);}
  }
  return new myReader(data.ToArray());
 }
 static int checks;static void Check(bool value,string reason){checks++;if(!value)throw new Exception(reason);}
 static void Main(){
  GameScr.instance.switchToMe();
  Check(!GuestCredentials.StatusKnown&&!GuestCredentials.IsPermanent,"default false guest flag was interpreted as permanent");
  EmailLinkFlow.Open();Check(Service.gI().stateRequests==0&&!GameCanvas.error.Contains("đã là"),"email button queried/guessed account state");
  EmailLinkFlow.Update();Check(Service.gI().stateRequests==1,"automatic in-game state synchronization missing");
  mSystem.now+=3001;EmailLinkFlow.Update();Check(Service.gI().stateRequests==2&&!GuestCredentials.IsPermanent,"background retry guessed permanent");
  EmailLinkFlow.ResetForLogin();GameCanvas.endDlg();
  EmailLinkFlow.Read(Packet(7,false,false));
  Check(!GuestCredentials.StatusKnown,"network reader modified authoritative UI state");EmailLinkFlow.Update();
  Check(GuestCredentials.IsPermanent,"login state was not populated before clicking email");
  int beforeOpen=Service.gI().stateRequests;EmailLinkFlow.Open();
  Check(GameCanvas.menu.options.commands.Count==1&&GameCanvas.menu.options.commands[0].caption=="Đổi tài khoản"&&Service.gI().stateRequests==beforeOpen,"permanent account did not open switch menu immediately");
  Check(object.ReferenceEquals(GameCanvas.menu.options.commands[0].actionListener,GameCanvas.panel)&&GameCanvas.menu.options.commands[0].idAction==10019,"switch did not reuse existing Panel account-switch action");
  GameCanvas.menu.options.commands[0].Click();Check(GameCanvas.panel.switches==1,"switch menu did not dispatch account switch");
  Command staleSwitch=GameCanvas.menu.options.commands[0];
  // Reproduce stale state: receiving server guest state must update the menu before any click.
  GuestCredentials.GetSecret();EmailLinkFlow.Read(Packet(7,true,false));EmailLinkFlow.Update();
  Check(GuestCredentials.IsGuest&&!GuestCredentials.IsPermanent,"stale permanent state overrode server guest restrictions");
  beforeOpen=Service.gI().stateRequests;GameCanvas.endDlg();
  staleSwitch.Click();Check(GameCanvas.panel.switches==1,"stale account menu switched a guest account");
  Check(GameCanvas.currentScreen is ClientInput,"stale account menu did not route guest to linking");
  ((ClientInput)GameCanvas.currentScreen).left.Click();EmailLinkFlow.Open();
  Check(Service.gI().stateRequests==beforeOpen,"guest email button sent a state query");
  ClientInput email=(ClientInput)GameCanvas.currentScreen;
  Check(email.tf.Length==1&&email.tf[0].loginInput&&email.tf[0].max==254,"email input length/keyboard");
  Check(email.center.x==123&&email.left.y==456,"native command geometry lost");
  email.tf[0].setText("invalid");email.center.Click();Check(Service.gI().requests==0,"invalid email submitted");
  email.tf[0].setText("  Player@Example.COM ");email.center.Click();
  Check(Service.gI().requests==1&&Service.gI().email=="player@example.com","email request normalization");
  email.center.Click();Check(Service.gI().requests==1,"duplicate request in flight");
  EmailLinkFlow.Read(Packet(3,true,"sent","player@example.com"));
  Check(GameCanvas.currentScreen==email,"network reader modified UI");EmailLinkFlow.Update();
  ClientInput verify=(ClientInput)GameCanvas.currentScreen;
  Check(verify!=email&&verify.tf.Length==3,"delivery did not open OTP form");
  Check(verify.hasPasswordToggle&&!email.hasPasswordToggle,"password visibility control missing / shown on email-only form");
  Check(verify.tf[0].max==6&&verify.tf[1].max==64&&verify.tf[1].type==2&&verify.tf[2].type==2,"OTP limit/password masking");
  verify.tf[0].setText("123");verify.tf[1].setText("NewPassword9");verify.tf[2].setText("NewPassword9");verify.center.Click();
  Check(Service.gI().confirms==0,"short OTP accepted");
  verify.tf[0].setText("123456");verify.tf[2].setText("OtherPassword9");verify.center.Click();Check(Service.gI().confirms==0,"password mismatch accepted");
  verify.tf[2].setText("NewPassword9");verify.center.Click();
  Check(Service.gI().confirms==1&&Service.gI().password=="NewPassword9","valid conversion / password case");
  EmailLinkFlow.Read(Packet(5,false,"wrong code",""));EmailLinkFlow.Update();
  Check(GuestCredentials.IsGuest&&GameCanvas.currentScreen==verify,"rejection retired guest/lost form");
  verify.center.Click();Check(Service.gI().confirms==2,"rejection prevented retry");
  verify.left.Click();Check(verify.tf[1].getText()==""&&verify.tf[2].getText()=="","cancel retained password");
  GameCanvas.endDlg();EmailLinkFlow.Open();Check(GameCanvas.menu.options.commands.Count==2,"pending OTP resume/resend missing");
  GameCanvas.menu.options.commands[0].Click();Check(((ClientInput)GameCanvas.currentScreen).tf.Length==3,"resume OTP");
  EmailLinkFlow.Read(Packet(5,true,"converted","player@example.com"));EmailLinkFlow.Update();
  Check(!GuestCredentials.IsGuest&&!GuestCredentials.HasToken&&!GuestCredentials.Preferred,"conversion retained guest credentials");
  Check(GameCanvas.currentScreen==GameScr.instance&&!GameCanvas.loginScr.isLogin2,"conversion did not return to game / retire guest login");
  Check(GameCanvas.loginScr.tfUser.getText()=="player@example.com"&&Rms.loadRMSString("pass")=="","login email/password persistence");
  EmailLinkFlow.Read(Packet(3,true,"late","other@example.com"));EmailLinkFlow.Update();
  Check(GameCanvas.currentScreen==GameScr.instance,"late guest response reopened linked account form");
  EmailLinkFlow.ResetForLogin();GuestCredentials.NormalLogin();
  Check(!GuestCredentials.StatusKnown&&!GuestCredentials.IsPermanent,"normal login attempt proved permanence without server confirmation");
  EmailLinkFlow.Read(Packet(1,false));EmailLinkFlow.Update();
  Check(GuestCredentials.IsGuest&&GuestCredentials.StatusKnown&&!GuestCredentials.IsDev,"P1 guest acknowledgement compatibility");
  Console.WriteLine("PASS: "+checks+" email form/protocol/state checks; no Unity, database or network");
 }
}
'''
fixture=fixture.replace("@@ACCOUNT_SWITCH@@",account_switch)
output = ROOT / "Build/guest-tests"
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="email-flow-", dir=output) as folder:
    path = Path(folder)
    source, exe = path / "Tests.cs", path / "Tests.exe"
    source.write_text(fixture, encoding="utf-8")
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", "/target:exe",
                    f"/out:{exe}", str(source), str(ROOT / "Assets/Scripts/GuestCredentials.cs"),
                    str(ROOT / "Assets/Scripts/EmailLinkFlow.cs"), str(ROOT / "Assets/Scripts/myReader.cs")], check=True)
    subprocess.run([str(exe)], check=True)
