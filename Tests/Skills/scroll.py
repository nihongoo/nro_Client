"""Check the production desktop wheel dispatch under multiple OnGUI calls per frame."""
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
source = (ROOT / 'Assets/Scripts/Main.cs').read_text(encoding='utf-8-sig')
method = source[source.index('    private void PollDesktopWheel('):source.index('\tprivate void OnApplicationQuit()')]
field = re.search(r'private int lastWheelInputFrame[^;]+;', source)[0]
fixture = r'''
using System;
static class Time {public static int frameCount;}
static class Input {public static float axis;public static float GetAxis(string name)=>axis;}
static class GameMidlet {public static Canvas gameCanvas=new Canvas();}
class Canvas {
 public int x,y,calls,total,expectedX,expectedY;
 public void pointerMouse(int px,int py){x=px;y=py;}
 public void scrollMouse(int amount){if(x!=expectedX||y!=expectedY)throw new Exception("wheel used stale pointer coordinates");calls++;total+=amount;}
}
class Main {
 FIELD
 METHOD
 public void Tick(int x,int y)=>PollDesktopWheel(x,y);
}
class Tests {
 static int checks;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static void Main(){
  var main=new Main();var canvas=GameMidlet.gameCanvas;
  for(int frame=1;frame<=100;frame++){
   Time.frameCount=frame;Input.axis=frame%2==0?.1f:-.1f;
   canvas.expectedX=frame;canvas.expectedY=frame+10;
   for(int guiEvent=0;guiEvent<5;guiEvent++)main.Tick(frame,frame+10);
   Check(canvas.calls==frame,"wheel consumed more than once per Unity frame");
   Check(canvas.total==(frame%2==0?0:-1),"wheel distance depends on OnGUI event count");
  }
  Console.WriteLine("PASS: "+checks+" production wheel dispatch checks");
 }
}
'''.replace('FIELD', field).replace('METHOD', method)
output = ROOT / 'Build/skill-scroll-tests'
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix='scroll-', dir=output) as folder:
    path = Path(folder)
    cs = path / 'Scroll.cs'
    cs.write_text(fixture, encoding='utf-8')
    exe = path / 'Scroll.exe'
    subprocess.run([r'D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe', '/nologo', '/main:Tests', f'/out:{exe}', str(cs)], check=True)
    subprocess.run([str(exe)], check=True)
