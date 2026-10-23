"""Exercise the production launcher loop without network or real sleeps."""
from pathlib import Path
import re, subprocess, tempfile
ROOT=Path(__file__).resolve().parents[2]
source=(ROOT/'Assets/Scripts/Mod.CuongLe/MainMod.cs').read_text(encoding='utf-8-sig')
def method(signature):
 start=source.index(signature);opening=source.index('{',start);depth=0
 for m in re.finditer(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"|[{}]',source[opening:]):
  if m[0]=='{':depth+=1
  elif m[0]=='}':
   depth-=1
   if depth==0:return source[start:opening+m.end()]
fixture=r'''
using System;using System.Threading;
class Thread {public Thread(Action a){}public void Start(){Tests.started++;}public static void Sleep(int n){}}
class Char {public string cName="";static Char c=new Char();public static Char myCharz()=>c;}
class Rms {public static void saveRMSString(string k,string v){}public static void saveRMSInt(string k,int v){}}
class ServerListScreen {public static bool loadScreen=true;public static int ipSelect;}
class Session_ME {public static Session_ME gI()=>new Session_ME();public bool isConnected()=>true;}
class Field {public void setText(string s){}}
class LoginScr {public Field tfUser=new Field(),tfPass=new Field();public bool isLogin2;}
class Screen {public void selectServer(){}public void perform(int a,object o){Tests.attempts++;}}
class GameCanvas {public static Screen serverScreen=new Screen();public static LoginScr loginScr=new LoginScr();public static int gameTick;}
class Tests {
 public static int started,attempts;static bool accountManagerLoginStopped,accountManagerActive,loginAgain;static string accountManagerFailure,accountManagerNotice,account="",password;static int server=1,LasterLogin;
 static bool RunAccountManagerAction(Action a){if(accountManagerLoginStopped)return false;a();return true;}
 static bool WaitForAccountManager(Func<bool> ready,int ms,string message)=>true;
 static void StopAccountManagerLogin(){accountManagerLoginStopped=true;}
 static string DecryptString(string value,string key)=>"password";
 __LOGIN__
 __READ__
 static void Main(){Login();if(attempts!=3||!accountManagerLoginStopped||accountManagerFailure==null)throw new Exception("launcher did not stop after three attempts");
  foreach(string value in new[]{"", "no-number", "999999999999999999999", "0", "-1"}){
   accountManagerLoginStopped=false;account="unchanged";started=0;Args.value=new[]{"game","launch|user|"+value+"|encrypted"};GetLoginDataFromAccountManager();
   if(started!=0||accountManagerNotice==null)throw new Exception("invalid server started login");
  }
  Console.WriteLine("PASS: production launcher stops at 3 attempts; malformed/overflow numeric launch data rejected");
 }
}
class Args {public static string[] value;public static string[] GetCommandLineArgs()=>value;}
'''.replace('__LOGIN__',method('public static void Login()')).replace('__READ__',method('public static void GetLoginDataFromAccountManager()').replace('Environment.GetCommandLineArgs()','Args.GetCommandLineArgs()'))
out=ROOT/'Build/login-retry-tests';out.mkdir(parents=True,exist_ok=True)
with tempfile.TemporaryDirectory(dir=out) as folder:
 p=Path(folder);(p/'Test.cs').write_text(fixture,encoding='utf-8');exe=p/'Test.exe'
 subprocess.run([r'D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe','/nologo','/warn:0',f'/out:{exe}',str(p/'Test.cs')],check=True)
 subprocess.run([str(exe)],check=True)
