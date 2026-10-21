"""Exercise the real credential helper with in-memory RMS; no Unity or APK build."""
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
fixture = r'''
using System;
using System.Collections.Generic;
static class Rms {
 internal static readonly Dictionary<string,string> data = new Dictionary<string,string>();
 internal static bool fail; internal static int writes;
 internal static string loadRMSString(string key)=>data.ContainsKey(key)?data[key]:null;
 internal static void saveRMSString(string key,string value){writes++;if(!fail)data[key]=value;}
}
namespace UnityEngine { static class SystemInfo { internal static string deviceUniqueIdentifier="s22",unsupportedIdentifier="n/a"; } }
class GuestTests {
 static int checks;
 static void Check(bool value,string text){checks++;if(!value)throw new Exception(text);}
 static void Reject(Action action,string text){bool rejected=false;try{action();}catch(InvalidOperationException){rejected=true;}Check(rejected,text);}
 static void Main(){
  Check(!GuestCredentials.StatusKnown&&!GuestCredentials.IsPermanent,"unknown account reported permanent");
  string token=GuestCredentials.GetSecret();
  Check(token.Length==64&&System.Text.RegularExpressions.Regex.IsMatch(token,"\\A[0-9a-f]{64}\\z"),"token entropy/encoding");
  for(int i=0;i<100;i++)Check(GuestCredentials.GetSecret()==token,"retry regenerated token");
  Check(Rms.writes==1,"resume rewrote credential");
  GuestCredentials.Accepted(false);Check(GuestCredentials.Preferred,"guest preference not persisted over legacy RMS account");GuestCredentials.NewDevGuest();Check(GuestCredentials.GetSecret()==token,"production silently created another guest");
  GuestCredentials.Accepted(true);GuestCredentials.NewDevGuest();Check(GuestCredentials.GetSecret()!=token,"dev credential did not change");
  GuestCredentials.NormalLogin();Check(!GuestCredentials.IsDev&&!GuestCredentials.Preferred,"normal login retained guest permission/preference");
  Check(!GuestCredentials.StatusKnown&&!GuestCredentials.IsPermanent,"normal login attempt marked permanent before server accepted");
  string savedGuestToken=GuestCredentials.GetSecret();
  GuestCredentials.Accepted(false);GuestCredentials.SaveAccountChoice("  admin  ","AdminPassword9");
  Check(!GuestCredentials.Preferred&&GuestCredentials.HasToken&&GuestCredentials.GetSecret()==savedGuestToken,"saving admin credentials resumed guest or discarded guest token");
  Check(Rms.loadRMSString("acc")=="admin"&&Rms.loadRMSString("pass")=="AdminPassword9","account switch lost username/password case");
  Check(!GuestCredentials.StatusKnown&&!GuestCredentials.IsPermanent,"saved credentials treated as verified account state");
  GuestCredentials.Confirmed(false,false);
  Check(GuestCredentials.IsPermanent,"normal account state not confirmed");
  GuestCredentials.Accepted(false);
  GuestCredentials.Accepted(true);Check(GuestCredentials.IsGuest,"guest state missing");
  Rms.data["guestSecretV1"]="corrupt";int before=Rms.writes;Reject(()=>GuestCredentials.GetSecret(),"corrupt token accepted");Check(Rms.writes==before,"corrupt token overwritten");
  Rms.data.Clear();Rms.fail=true;Reject(()=>GuestCredentials.GetSecret(),"unsaved credential could reach server");
  Rms.fail=false;Check(GuestCredentials.GetDevice()=="s22","wrong device sent");
  UnityEngine.SystemInfo.deviceUniqueIdentifier="n/a";Reject(()=>GuestCredentials.GetDevice(),"unsupported device accepted");
  UnityEngine.SystemInfo.deviceUniqueIdentifier="";Reject(()=>GuestCredentials.GetDevice(),"empty device accepted");
  Console.WriteLine("PASS: "+checks+" guest credential checks; no Unity/APK/network");
 }
}
'''
output = ROOT / "Build/guest-tests"
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="nro-guest-client-", dir=output) as folder:
    path = Path(folder)
    assert path.resolve().is_relative_to(output.resolve())
    source, exe = path / "GuestTests.cs", path / "GuestTests.exe"
    source.write_text(fixture, encoding="utf-8")
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", "/target:exe",
                    f"/out:{exe}", str(source), str(ROOT / "Assets/Scripts/GuestCredentials.cs")], check=True)
    subprocess.run([str(exe)], check=True)
