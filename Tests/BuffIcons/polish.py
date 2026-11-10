"""Standalone buff HUD profiling/regressions. Never invokes Unity or builds the game."""
from pathlib import Path
import argparse
import ast
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument("--baseline", action="store_true")
args = parser.parse_args()

def source(name):
    path = "Assets/Scripts/Hud/" + name + ".cs"
    if args.baseline:
        return subprocess.check_output(["git", "show", "63b56f4:" + path], cwd=ROOT).decode("utf-8-sig")
    return (ROOT / path).read_text(encoding="utf-8-sig")

def replace_block(text, marker, replacement):
    start = text.index(marker)
    begin = text.index("{", start)
    end, depth = begin + 1, 1
    while depth:
        depth += (text[end] == "{") - (text[end] == "}")
        end += 1
    return text[:start] + replacement + text[end:]

tree = ast.parse((ROOT / "Tests/BuffIcons/run.py").read_text())
literal = next(n.value for n in tree.body if isinstance(n, ast.Assign) and any(isinstance(t, ast.Name) and t.id == "fixture" for t in n.targets))
while isinstance(literal, ast.Call):
    literal = literal.func.value
fixture = ast.literal_eval(literal).split("class StackLists")[0]
fixture = replace_block(fixture, " public static class HudLayout", "")
fixture = re.sub(r"public enum (HorizontalAnchor|VerticalAnchor)\{[^}]*\}", "", fixture)
fixture = fixture.replace("public struct Vector2 {", "public enum RuntimePlatform {WindowsPlayer,Android,IPhonePlayer} public struct Vector2 {public static Vector2 operator /(Vector2 v,float n)=>new Vector2(v.x/n,v.y/n);public static Vector2 operator *(Vector2 v,float n)=>new Vector2(v.x*n,v.y*n);")
fixture = fixture.replace("public static float Min(params", "public static float Min(float a,float b)=>Math.Min(a,b);public static float Min(params")
fixture = fixture.replace("public class GameScr {", "public class GameScr {public static int isAnalog;public static GamePad gamePad;public static void RegisterHudRects(){}")
fixture = fixture.replace("public class Char {", "public class Char {public bool meDead;public long cHP=100;public int statusMe;")
fixture = fixture.replace("public static int gold;", "public static int gold;private static readonly Image corner=new Image();")
fixture = fixture.replace("gold++;return new Image();", "gold++;return corner;")
fixture = fixture.replace("public int getWidth(string s)=>s.Length*4;", "public int getWidth(string s){Bench.Measures++;return s.Length*4;}")
fixture = fixture.replace("public string[] splitFontArray(string s,int width)=>new[]{s};", "public string[] splitFontArray(string s,int width){Bench.Wraps++;return new[]{s};}")
fixture += r'''
namespace UnityEngine.Device {public static class Application {public static RuntimePlatform platform;}}
public class GamePad {public Rect HudVisualBounds=>new Rect();}
namespace Hud {public static class CombatMobileHud {public const string JoystickTouch="joystick";}public static class CombatPcHud {public static bool Visible;}}
public static class Bench {
 public static long Now=100000;
 public static int Grids,Copies,Formats,Measures,Wraps,Invalidations;
 public static void Clear(){Grids=Copies=Formats=Measures=Wraps=Invalidations=0;InventoryItemVisuals.icons=0;}
}
public class PolishTests {
 static readonly mGraphics Graphics=new mGraphics();
 static readonly Rect Info=new Rect(6,3,150,28),Hp=new Rect(40,3,125,8),Target=new Rect(350,0,80,80),Hub=new Rect(700,2,34,34);
 static List<BuffIconEntry> Entries(int count){var e=new List<BuffIconEntry>();for(int i=1;i<=count;i++)e.Add(new BuffIconEntry{Id=i,Icon=433,Name="Buff "+i,Effect="Effect",ExpiresAt=700000});return e;}
 static void Frame(int i) {
  GameCanvas.gameTick++;HudLayout.BeginFrame();Rect r=HudLayout.GetRect(BuffIconsHud.Root);
  GameCanvas.pxMouse=(int)r.center.x;GameCanvas.pyMouse=(int)r.center.y;GameCanvas.pXYScrollMouse=i%2==0?-1:1;
  for(int k=0;k<20;k++)HudLayout.HitTest(r.center);
  BuffIconsHud.UpdateInput();BuffIconsHud.Paint(Graphics);BuffIconsHud.PaintTooltip(Graphics);
 }
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
#if POLISH
 static void CheckLayoutAndTimers(){
  var entries=Entries(2);entries[0].ExpiresAt=520000;
  BuffIconStore.Apply(entries,2,100000,Bench.Now);BuffIconsHud.Reset();
  for(int zoom=2;zoom<=4;zoom++)foreach(var platform in new[]{RuntimePlatform.WindowsPlayer,RuntimePlatform.Android}){
   mGraphics.zoomLevel=zoom;UnityEngine.Device.Application.platform=platform;
   HudLayout.BeginFrame();GUI.Ink.Clear();GUI.Capture=true;BuffIconsHud.Paint(Graphics);GUI.Capture=false;
   Check(GUI.Ink.Count==5,"missing countdown digits");
   for(int i=0;i<2;i++){
    int start=i==0?0:2,count=i==0?2:3;
    Rect cell=HudLayout.GetRect(i==0?"buff-icons.slot.0":"buff-icons.slot.1");
    var ink=GUI.Ink.GetRange(start,count);
    float top=ink.Min(r=>r.yMin),left=ink.Min(r=>r.xMin),right=ink.Max(r=>r.xMax);
    Check(top-cell.yMax*zoom>=0&&top-cell.yMax*zoom<=2.01f,"caption gap exceeds two design pixels");
    Check(Math.Abs((left+right)/2-cell.center.x*zoom)<=.51f,"countdown off center");
    Check(ink.Max(r=>r.yMax)+1<=BuffIconsHud.StackBounds.yMax*zoom+.01f,"shadow exceeds stack bounds");
   }
   Check(GUI.Ink[0].y==GUI.Ink[2].y&&GUI.Ink[0].height==GUI.Ink[2].height,"7p and 10p use different baselines or sizes");
  }
  Check(object.ReferenceEquals(entries[0].Countdown(Bench.Now),entries[0].Countdown(Bench.Now+59999)),"minute text recreated before value changes");
  Check(entries[0].Countdown(Bench.Now+60000)=="6p","minute boundary wrong");
  Check(entries[0].Countdown(Bench.Now+360000)=="1p"&&entries[0].Countdown(Bench.Now+361000)=="59s","minute-to-second boundary wrong");
  Check(entries[0].Countdown(Bench.Now+362000)=="58s","second boundary wrong");
  Bench.Now+=420000;
  Check(HudLayout.GetRect("buff-icons.slot.0").width>0&&HudLayout.GetRect("buff-icons.slot.1").width==0,"expired cell survived within same frame");
  BuffIconStore.Reset();Check(HudLayout.GetRect(BuffIconsHud.Root).width==0,"reset left stale row");
  Console.WriteLine("PASS: shared ink baseline, centered 7p/10p, gap/stack at zoom 2-4, countdown boundaries, expiry and reset");
 }
#endif
 public static void Main(string[] args) {
  GameCanvas.w=780;GameCanvas.h=360;mGraphics.zoomLevel=3;UnityEngine.Device.Application.platform=RuntimePlatform.Android;
  HudLayout.Register("top-info.bounds",()=>Info,0,null,false);HudLayout.Register("top-info.hp",()=>Hp,0,null,false);
  HudLayout.Register("target-info.bounds",()=>Target,0,null,false);HudLayout.Register(TopActionsHud.Hub,()=>Hub,40);
  BuffIconsHud.Register();BuffIconStore.Apply(Entries(20),1,100000,Bench.Now);
  for(int i=0;i<10;i++)Frame(i);
  Bench.Clear();long allocated=GC.GetAllocatedBytesForCurrentThread();
  for(int i=0;i<600;i++)Frame(i);
  allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
  Console.WriteLine("SCROLL 600 frames: grid="+Bench.Grids+" visibleCopies="+Bench.Copies+" timeFormats="+Bench.Formats+" globalInvalidations="+Bench.Invalidations+" icons="+InventoryItemVisuals.icons+" managedBytes="+allocated);
  Check(InventoryItemVisuals.icons==4200,"must paint exactly seven visible icons per frame");
  if(args.Length==0) {
   Check(Bench.Grids==0,"scroll recomputed unchanged geometry");Check(Bench.Copies==0,"scroll recopied unchanged buff list");
   Check(Bench.Formats==0,"scroll reformatted unchanged countdowns");Check(Bench.Invalidations==0,"scroll invalidated unrelated HUD layout");
   Check(allocated==0,"steady scroll allocated managed memory");
#if POLISH
   CheckLayoutAndTimers();
#endif
  }
 }
}
'''

output = ROOT / "Build/buff-row-polish-tests"
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="check-", dir=output) as folder:
    folder = Path(folder)
    assert folder.resolve().is_relative_to(output.resolve())
    files = []
    names = ["HudLayout", "BuffIconStore", "BuffIconGrid", "BuffIconsHud", "TopLeftHudStack"]
    if not args.baseline:
        names.append("BuffCountdownText")
    for name in names:
        text = source(name)
        if name == "BuffIconStore":
            text = re.sub(r"internal static long Now =>[^;]+;", "internal static long Now => Bench.Now;", text)
            text = text.replace("output.Clear();", "Bench.Copies++; output.Clear();")
            text = text.replace("internal static string FormatTime(int seconds) =>", "internal static string FormatTime(int seconds) { Bench.Formats++; return")
            text = text.replace('+ "s";', '+ "s"; }')
        elif name == "BuffIconGrid":
            text = text.replace("float unit =", "Bench.Grids++; float unit =")
        elif name == "HudLayout":
            text = text.replace("{  generation++; stateGeneration++; }", "{ Bench.Invalidations++; generation++; stateGeneration++; }")
        target = folder / (name + ".cs")
        target.write_text(text, encoding="utf-8")
        files.append(target)
    fixture_path = folder / "PolishTests.cs"
    fixture_path.write_text(fixture, encoding="utf-8")
    files.append(fixture_path)
    # Reader references in the shared render fixtures are unrelated to the HUD under test.
    files.append(ROOT / "Assets/Scripts/myReader.cs")
    exe = folder / "PolishTests.exe"
    define = [] if args.baseline else ["/define:POLISH"]
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", "/target:exe", *define, f"/out:{exe}", *map(str, files)], check=True)
    subprocess.run([str(exe), *(["baseline"] if args.baseline else [])], check=True)
