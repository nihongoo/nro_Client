"""Validate actual diagnostic aggregation, tab isolation, counters and exception de-duplication."""
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
fixture = r'''
using System;
using System.Collections.Generic;
using System.Threading;
using System.Text.RegularExpressions;
using System.Globalization;
using Hud;
namespace UnityEngine {
 public static class Time {public static int frameCount;}
 public static class Debug {
  public static List<string> logs=new List<string>(),warnings=new List<string>();
  public static void Log(object text){logs.Add(text.ToString());}
  public static void LogWarning(object text){warnings.Add(text.ToString());}
 }
}
class Panel {public bool isShow=true,InventoryFrameActive=true;public int currentTabIndex=2;}
static class GameCanvas {public static Panel panel=new Panel();public static bool isPointerJustDown,isPointerDown,isPointerJustRelease,isPointerClick;}
namespace Hud {static class InventoryFrameSkin {internal static int DiagnosticCacheCount=>11;}}
class Tests {
 static int checks;static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static void Exception(){try{throw new InvalidOperationException("diag test");}catch(Exception ex){SkillDiagnostics.CaughtException("test",ex);}}
 static void Advance(){Thread.Sleep(1050);UnityEngine.Time.frameCount++;SkillDiagnostics.Tick();}
 static void Main(){
  UnityEngine.Time.frameCount=1;
  using(SkillDiagnostics.Measure(SkillDiagnostics.Area.Frame)) {
   using(SkillDiagnostics.Measure(SkillDiagnostics.Area.Grid)) {
    Thread.Sleep(3);SkillDiagnostics.CopyNativeCalled();SkillDiagnostics.CopyNativeCalled();
    SkillDiagnostics.CacheEntry(0x123,10,20,128,30);SkillDiagnostics.CacheEntry(0x456,10,20,255,30);
   }
   SkillDiagnostics.CacheCleared(true);Exception();Exception();
  }
  using(SkillDiagnostics.Measure(SkillDiagnostics.Area.Frame)){Thread.Sleep(2);}
  using(SkillDiagnostics.Measure(SkillDiagnostics.Area.Input)){
   using(SkillDiagnostics.Probe(SkillDiagnostics.Stage.HitTest)){Thread.Sleep(2);}
   SkillDiagnostics.Count(SkillDiagnostics.Counter.LayoutScrollBounds,3);
   SkillDiagnostics.Count(SkillDiagnostics.Counter.BeginFrame);
   GameCanvas.isPointerDown=true;SkillDiagnostics.Invalidated("Fixture.cs",42);SkillDiagnostics.Invalidated("Fixture.cs",42);GameCanvas.isPointerDown=false;
   long started=SkillDiagnostics.StartRect();Thread.Sleep(1);SkillDiagnostics.RectResolved("skills.left",started);
  }
  Advance();
  Check(UnityEngine.Debug.logs.Count==1,"expected one summary per completed second");
  string first=UnityEngine.Debug.logs[0];
  Check(first.StartsWith("[SKILLDIAG] tab=Skills"),"skill summary label missing");
  foreach(string field in new[]{"ms(avg/max)","PaintInventoryFrame=","PaintSkillPanel=","grid=","intrinsic=","potential=","PaintSkillDetail=","UpdateSkillPanelInput=","CopyNative=2","copyByArea=grid:2,","cacheClearFull=1","cacheSize=11","cacheNew=2","key:0x0000000000000456","entryChanges=alpha,","exceptions=2"})
   Check(first.Contains(field),"summary missing: "+field);
  foreach(string field in new[]{"deepMs(avg/max;calls/window,inclusive)=", "UpdateSkillPanelInput.HitTest:", "inputHeaviest=HitTest:", "UpdateSkillPanelInput.LayoutScrollBounds:3", "Fixture.cs:42 calls:2 justDown/held/release/click:0/2/0/0", "total:2 rectMiss/window", "skills.left:1;"})Check(first.Contains(field),"deep summary missing: "+field);
  var totals=Regex.Match(first,@" total=([0-9.]+)/([0-9.]+)");
  var painted=Regex.Match(first,@" PaintInventoryFrame=([0-9.]+)/([0-9.]+)");
  var input=Regex.Match(first,@" UpdateSkillPanelInput=([0-9.]+)/([0-9.]+)");
  double total=double.Parse(totals.Groups[1].Value,CultureInfo.InvariantCulture),paint=double.Parse(painted.Groups[1].Value,CultureInfo.InvariantCulture),inputMs=double.Parse(input.Groups[1].Value,CultureInfo.InvariantCulture);
  Check(System.Math.Abs(total-paint-inputMs)<.005,"nested timers double-count total");
  Check(total==double.Parse(totals.Groups[2].Value,CultureInfo.InvariantCulture)&&paint>=5,"per-frame accumulation/maximum is incorrect");
  Check(UnityEngine.Debug.warnings.Count==1&&UnityEngine.Debug.warnings[0].Contains("Tests.Exception"),"exception stack was lost or printed twice");
  using(SkillDiagnostics.Measure(SkillDiagnostics.Area.Frame)){Exception();Thread.Sleep(2);}
  Advance();
  Check(UnityEngine.Debug.logs.Count==2&&UnityEngine.Debug.logs[1].Contains("CopyNative=0")&&UnityEngine.Debug.logs[1].Contains("exceptions=1"),"per-second counters did not reset");
  Check(UnityEngine.Debug.warnings.Count==1,"same exception message printed stack again");
  Check(!UnityEngine.Debug.logs[1].Contains("Fixture.cs:42")&&!UnityEngine.Debug.logs[1].Contains("LayoutScrollBounds:3")&&!UnityEngine.Debug.logs[1].Contains("skills.left:1;"),"deep counters did not reset");
  GameCanvas.panel.currentTabIndex=1;UnityEngine.Time.frameCount++;
  using(SkillDiagnostics.Measure(SkillDiagnostics.Area.Frame))using(SkillDiagnostics.Measure(SkillDiagnostics.Area.InventoryBody)){Thread.Sleep(2);}
  Advance();
  string inventory=UnityEngine.Debug.logs[2];
  Check(inventory.Contains("tab=Inventory")&&inventory.Contains("inventoryBody=")&&inventory.Contains("InventoryDetail=")&&inventory.Contains("UpdateInventoryFrameInput="),"inventory baseline missing");
  Check(inventory.Contains("CopyNative=0")&&inventory.Contains("exceptions=0"),"tab switch mixed counters");
  GameCanvas.panel.isShow=false;UnityEngine.Time.frameCount++;SkillDiagnostics.Tick();
  SkillDiagnostics.CopyNativeCalled();Exception();
  Check(UnityEngine.Debug.logs.Count==3&&UnityEngine.Debug.warnings.Count==1,"diagnostics active outside selected tabs");
  Console.WriteLine("PASS: "+checks+" production diagnostic aggregation/counter checks");
 }
}
'''
output = ROOT / 'Build/skill-diag-tests'
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix='diag-', dir=output) as folder:
    path = Path(folder)
    cs = path / 'DiagTests.cs'
    cs.write_text(fixture, encoding='utf-8')
    exe = path / 'DiagTests.exe'
    subprocess.run([r'D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe', '/nologo', f'/out:{exe}',
                    str(cs), str(ROOT / 'Assets/Scripts/Hud/SkillDiagnostics.cs')], check=True)
    subprocess.run([str(exe)], check=True)
