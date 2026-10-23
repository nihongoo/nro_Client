"""Production guest-switch confirmation, durable retry and packet serialization."""
from pathlib import Path
import re,subprocess,tempfile
ROOT=Path(__file__).resolve().parents[2]
service=(ROOT/'Assets/Scripts/Service.cs').read_text(encoding='utf-8-sig')
def method(name):
 start=service.index('    public void '+name+'(');opening=service.index('{',start);depth=0
 for m in re.finditer(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"|[{}]',service[opening:]):
  if m[0]=='{':depth+=1
  elif m[0]=='}':
   depth-=1
   if depth==0:return service[start:opening+m.end()]
fixture=r'''
using System;using System.Collections.Generic;
interface IActionListener{void perform(int action,object p);}
class Command{IActionListener listener;int action;public Command(string s,IActionListener l,int a,object p){listener=l;action=a;}public void Click()=>listener.perform(action,null);}
class Canvas{public int switched;public void completeAccountSwitch(){switched++;}}
class GameCanvas{public static Canvas instance=new Canvas();public static object currentScreen;public static Command yes,no;public static string message;public static void endDlg(){}public static void startOKDlg(string s){message=s;}public static void startYesNoDlg(string s,Command y,Command n){message=s;yes=y;no=n;}}
class GameScr{public static GameScr instance=new GameScr();}
class Rms{public static Dictionary<string,string> data=new Dictionary<string,string>();public static string loadRMSString(string k)=>data.ContainsKey(k)?data[k]:null;public static void saveRMSString(string k,string v){data[k]=v;}}
class Session{public int sent;public sbyte command;public sbyte[] payload;public void sendMessage(Message m){sent++;command=m.command;payload=m.getData();}}
class Service{static Service s=new Service();public static Service gI()=>s;public Session session=new Session();__SEND__ __LOGIN__}
class mSystem{public static long now=1000;public static long currentTimeMillis()=>now;public static sbyte[] convertToSbyte(byte[] b)=>Array.ConvertAll(b,x=>(sbyte)x);}
namespace UnityEngine{public static class SystemInfo{public static string deviceUniqueIdentifier="s22",unsupportedIdentifier="n/a";}public class TextAsset{public byte[] bytes;}public static class Resources{public static object Load(string s,Type t)=>null;}}
class Tests{
 static int n;static void Check(bool b,string s){n++;if(!b)throw new Exception(s);}
 static void Reply(bool success){var w=new myWriter();w.writeBool(success);w.writeUTF(success?"":"Không thể xóa");GuestSwitchFlow.Read(new myReader(w.getData()));GuestSwitchFlow.Update();}
 static void Main(){
  GameCanvas.currentScreen=GameScr.instance;GuestCredentials.Confirmed(false,false);GuestSwitchFlow.Open();Check(GameCanvas.instance.switched==0,"permanent switch skipped popup");GameCanvas.no.Click();Check(GameCanvas.instance.switched==0,"cancel switched account");GuestSwitchFlow.Open();GameCanvas.yes.Click();Check(GameCanvas.instance.switched==1&&Service.gI().session.sent==0,"permanent account sent delete");
  GuestCredentials.Accepted(false);string old=GuestCredentials.GetSecret();GuestSwitchFlow.Open();Check(GameCanvas.message.Contains("XÓA VĨNH VIỄN"),"guest loss not explicit");GameCanvas.no.Click();Check(GuestCredentials.GetSecret()==old&&Service.gI().session.sent==0,"cancel deleted guest");
  GuestSwitchFlow.Open();GameCanvas.yes.Click();Check(GuestCredentials.DeletePending&&GuestCredentials.GetSecret()==old&&GameCanvas.instance.switched==1,"credentials cleared before acknowledgement");
  var s=Service.gI().session;var r=new myReader(s.payload);Check(s.command==-48&&r.readByte()==8&&r.readUTF()==old&&r.readUTF()=="s22"&&r.available()==0,"wrong deletion packet bytes");
  GuestSwitchFlow.Open();Check(s.sent==1,"duplicate click replayed mutation");mSystem.now+=5001;GuestSwitchFlow.Update();Check(GuestCredentials.DeletePending&&GuestCredentials.GetSecret()==old,"timeout discarded recovery token");
  GuestSwitchFlow.Open();GameCanvas.yes.Click();Check(s.sent==2,"retry missing");Reply(false);Check(!GuestCredentials.DeletePending&&GuestCredentials.GetSecret()==old,"failed deletion discarded guest");
  GuestSwitchFlow.Open();GameCanvas.yes.Click();Reply(true);Check(GameCanvas.instance.switched==2&&!GuestCredentials.HasToken&&!GuestCredentials.Preferred&&!GuestCredentials.DeletePending,"confirmed deletion kept old guest or auto-login");Check(GuestCredentials.GetSecret()!=old,"new guest reused retired secret");
  GuestCredentials.BeginDeletion();GuestCredentials.BeginLogin();Service.gI().guestLogin(GuestCredentials.GetSecret(),"s22");Check(new myReader(s.payload).readByte()==8,"reconnect created guest instead of replaying pending deletion");Reply(true);Check(GameCanvas.instance.switched==3&&!GuestCredentials.HasToken,"recovered deletion did not return to login");Reply(true);Check(GameCanvas.instance.switched==3,"late duplicate acknowledgement switched again");
  Console.WriteLine("PASS: "+n+" real guest confirmation/cancel/permanent/packet/retry/recovery/new-identity checks");
 }
}
'''.replace('__SEND__',method('requestGuestDeletion')).replace('__LOGIN__',method('guestLogin'))
out=ROOT/'Build/guest-tests';out.mkdir(parents=True,exist_ok=True)
with tempfile.TemporaryDirectory(dir=out) as folder:
 p=Path(folder);(p/'Switch.cs').write_text(fixture,encoding='utf-8');exe=p/'Switch.exe'
 sources=['GuestCredentials.cs','GuestSwitchFlow.cs','Message.cs','myReader.cs','myWriter.cs']
 subprocess.run([r'D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe','/nologo','/warn:0',f'/out:{exe}',str(p/'Switch.cs'),*[str(ROOT/'Assets/Scripts'/s) for s in sources]],check=True)
 subprocess.run([str(exe)],check=True)
