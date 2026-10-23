"""Exercise production OnGUI, pointer dispatch and FPS accounting with extra editor repaints."""
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
main = (ROOT / 'Assets/Scripts/Main.cs').read_text(encoding='utf-8-sig')
mod = (ROOT / 'Assets/Scripts/Mod.CuongLe/MainMod.cs').read_text(encoding='utf-8-sig')
on_gui = main[main.index('\tprivate void OnGUI()'):main.index('\tpublic void setsizeChange()')]
pointer = main[main.index('    private void PollLegacyPointer('):main.index('    private void PollDesktopWheel(')]
fps = mod[mod.index('        public static int CalculateFPS()'):mod.index('        public static void Paint(mGraphics g)')]
fields = '\n'.join(re.search(r'private static (?:int|long) ' + name + r'\s*=[^;]+;', mod)[0]
                   for name in ['frameCount', 'lastTime', 'lastRenderedFrame', 'fps'])
fixture = r'''
using System;
using UnityEngine;
namespace Hud {static class SkillDiagnostics {public static void Tick(){}}}
namespace UnityEngine {
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 operator/(Vector2 p,int n)=>new Vector2(p.x/n,p.y/n);}
 public struct Rect {public bool Contains(Vector2 p)=>p.x>=0&&p.x<100&&p.y>=0&&p.y<100;}
 public static class Time {public static int frameCount;}
 public enum EventType {Layout,Repaint,MouseDrag,ScrollWheel}
 public class Event {public EventType type;public static Event current=new Event();}
 public static class Input {public static bool down,held,up;public static bool GetMouseButtonDown(int n)=>down;public static bool GetMouseButton(int n)=>held;public static bool GetMouseButtonUp(int n)=>up;}
}
static class mSystem {public static long now;public static long currentTimeMillis()=>now;}
class mGraphics {public static int zoomLevel=2;public void reset(){}}
static class ScaleGUI {public static Rect Viewport=>new Rect();public static int groups;public static void BeginGUI(){groups++;}public static void EndGUI(){groups--;}}
class Session_ME {public static void update(){}}
class Session_ME2 {public static void update(){}}
class Canvas {
 public int paints,pressed,dragged,released;
 public void paint(mGraphics g){paints++;}
 public void pointerPressed(int x,int y){pressed++;}public void pointerDragged(int x,int y){dragged++;}public void pointerReleased(int x,int y){released++;}
}
static class GameMidlet {public static Canvas gameCanvas=new Canvas();}
class Main {
 private int count=10,lastLegacyPointerFrame=-1;private bool pointerInViewport;private Vector2 lastMousePos;private mGraphics g=new mGraphics();
 ON_GUI
 POINTER
 private void checkInput(){PollLegacyPointer(false,false,new Vector2(50,50),new Vector2(50,50),25,25);}
 public void Gui(EventType type){Event.current.type=type;OnGUI();}
}
namespace Mod.CuongLe {static class MainMod {__FIELDS__ __FPS_METHODS__}}
class Tests {
 static int checks;static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static void Main(){
  var main=new Main();var canvas=GameMidlet.gameCanvas;
  // No FixedUpdate runs in this fixture: repaint must remain possible independently.
  for(int frame=1;frame<=120;frame++) {
   Time.frameCount=frame;mSystem.now=frame*10;
   Input.down=frame==1;Input.held=frame<=2;Input.up=frame==3;
   main.Gui(EventType.Layout);main.Gui(EventType.Repaint);main.Gui(EventType.ScrollWheel);main.Gui(EventType.Repaint);main.Gui(EventType.MouseDrag);main.Gui(EventType.Repaint);
   Check(canvas.paints==frame*3,"extra repaints exhausted the update count and stopped drawing");
   Check(ScaleGUI.groups==0,"GUI matrix stack leaked");
   Check(canvas.pressed==1&&canvas.dragged==Math.Min(frame,2)&&canvas.released==(frame>=3?1:0),"pointer press/drag/release repeated within one Unity frame");
  }
  Check(Mod.CuongLe.MainMod.CalculateFPS()==100,"FPS counts repaint callbacks instead of unique frames");
  int original=Mod.CuongLe.MainMod.CalculateFPS();
  for(int i=0;i<1000;i++)Mod.CuongLe.MainMod.CalculateFPS();
  Time.frameCount++;mSystem.now+=10;Mod.CuongLe.MainMod.RecordRenderedFrame();
  Check(Mod.CuongLe.MainMod.CalculateFPS()==original,"reading FPS changes the measurement");
  Console.WriteLine("PASS: "+checks+" production repaint/pointer/FPS checks");
 }
}
'''.replace('ON_GUI', on_gui).replace('POINTER', pointer).replace('__FIELDS__', fields).replace('__FPS_METHODS__', fps)
output = ROOT / 'Build/skill-frame-pacing-tests'
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix='frame-', dir=output) as folder:
    path = Path(folder)
    cs = path / 'Frames.cs'
    cs.write_text(fixture, encoding='utf-8')
    exe = path / 'Frames.exe'
    subprocess.run([r'D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe', '/nologo', '/main:Tests', f'/out:{exe}', str(cs)], check=True)
    subprocess.run([str(exe)], check=True)
