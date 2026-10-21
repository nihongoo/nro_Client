"""Execute the account keyboard handler against physical/text GUI event sequences."""
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
main = (ROOT / "Assets/Scripts/Main.cs").read_text(encoding="utf-8-sig")
keymap = (ROOT / "Assets/Scripts/MyKeyMap.cs").read_text(encoding="utf-8-sig")
signature = "private bool HandleAccountKeyboard()"
if signature in main:
    start = main.index(signature)
    brace = main.index("{", start)
    depth = 1
    end = brace + 1
    while depth:
        depth += (main[end] == "{") - (main[end] == "}")
        end += 1
    handler = main[start:end]
else:
    # The pre-fix account input path, used to reproduce the reported double insertion.
    start = main.index("if (Input.anyKeyDown && Event.current.type == EventType.KeyDown)")
    end = main.index("\t\tif (isPC)", start)
    handler = signature + " { " + main[start:end] + "return true; }"

keys = sorted(set(re.findall(r"KeyCode\.(\w+)", main + keymap)) | {"None", "KeypadEnter"})
fixture = r'''
using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine {
 public enum KeyCode { @@KEYS@@ }
 public enum EventType { KeyDown,KeyUp,Layout,Repaint,Used }
 public class Event {
  public static Event current;public EventType type;public KeyCode keyCode;public char character;public bool control,command;
  public void Use(){type=EventType.Used;}
 }
 public static class Input {public static bool anyKeyDown=true;public static string compositionString="";public static bool GetKey(KeyCode code)=>false;}
}
class LoginScr { }
namespace Assets.src.g { class ClientInput { } }
static class GameCanvas {public static object currentScreen=new LoginScr();}
class Canvas {
 public List<int> pressed=new List<int>(),released=new List<int>();public string text="";
 public void keyPressedz(int code){pressed.Add(code);if(code>=32)text+=(char)code;else if(code==-8&&text.Length>0)text=text.Substring(0,text.Length-1);}
 public void keyReleasedz(int code){released.Add(code);}
 public void Reset(){pressed.Clear();released.Clear();text="";}
}
static class GameMidlet {public static Canvas gameCanvas=new Canvas();}
class Main {
 public static bool isPC=true;
 @@HANDLER@@
 public bool Handle()=>HandleAccountKeyboard();
}
class Tests {
 static int checks;static Main main=new Main();
 static void Check(bool value,string reason){checks++;if(!value)throw new Exception(reason);}
 static void Send(KeyCode key,char character,EventType type=EventType.KeyDown,bool control=false) {
  Event.current=new Event{type=type,keyCode=key,character=character,control=control};main.Handle();
 }
 static void Main(){
  Canvas canvas=GameMidlet.gameCanvas;
  Send(KeyCode.A,'\0');Send(KeyCode.None,'a');
  Check(canvas.text=="a"&&canvas.pressed.Count==1,"one physical key plus text event inserted twice");
  Check(Event.current.type==EventType.Used,"handled account event was not consumed");
  main.Handle();Check(canvas.pressed.Count==1,"same consumed event was processed twice");
  Send(KeyCode.A,'\0');Send(KeyCode.None,'a');Check(canvas.text=="aa","two intentional repeated letters were deduplicated");
  canvas.Reset();Send(KeyCode.A,'A');Check(canvas.text=="A","combined key/text event lost uppercase");
  Send(KeyCode.Alpha2,'@');Send(KeyCode.Equals,'+');Check(canvas.text=="A@+","shifted account characters were remapped");
  Send(KeyCode.Backspace,'\b');Send(KeyCode.None,'\b');Check(canvas.text=="A@"&&canvas.pressed.Count==4,"Backspace deleted more than one character");
  Send(KeyCode.Return,'\n');Send(KeyCode.None,'\n');Check(canvas.pressed[canvas.pressed.Count-1]==-5,"Enter control not routed");
  Send(KeyCode.KeypadEnter,'\n');Check(canvas.pressed[canvas.pressed.Count-1]==-5,"keypad Enter not routed");
  Send(KeyCode.Tab,'\t');Check(canvas.pressed[canvas.pressed.Count-1]==-26,"Tab control not routed");
  int before=canvas.pressed.Count;Send(KeyCode.Equals,'\0');Check(canvas.pressed.Count==before,"physical Equals submitted form before text commit");
  Send(KeyCode.V,'v',control:true);Check(canvas.pressed.Count==before,"Ctrl shortcut inserted a literal letter");
  Input.anyKeyDown=false;Send(KeyCode.None,'z');Check(canvas.text=="A@z","committed text was dropped outside physical polling frame");
  Input.compositionString="composing";before=canvas.pressed.Count;Send(KeyCode.A,'a');Check(canvas.pressed.Count==before,"uncommitted IME text inserted");Input.compositionString="";
  before=canvas.pressed.Count;Send(KeyCode.A,'\0',EventType.KeyUp);Check(canvas.pressed.Count==before&&canvas.released.Count==1,"KeyUp inserted text / did not release key");
  GameCanvas.currentScreen=new Assets.src.g.ClientInput();Send(KeyCode.None,'B');Check(canvas.text.EndsWith("B"),"OTP/password form still used legacy keyboard route");
  GameCanvas.currentScreen=new object();Event.current=new Event{type=EventType.KeyDown,keyCode=KeyCode.A,character='a'};
  Check(!main.Handle()&&Event.current.type==EventType.KeyDown,"gameplay keyboard event was consumed by account handler");
  GameCanvas.currentScreen=new LoginScr();global::Main.isPC=false;Check(!main.Handle(),"mobile native keyboard was intercepted");
  Console.WriteLine("PASS: "+checks+" production account keyboard event checks; no Unity/network");
 }
}
'''.replace("@@KEYS@@", ",".join(keys)).replace("@@HANDLER@@", handler)
output = ROOT / "Build/account-input-tests"
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="input-", dir=output) as folder:
    path = Path(folder)
    source, exe = path / "Tests.cs", path / "Tests.exe"
    source.write_text(fixture, encoding="utf-8")
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", "/target:exe",
                    f"/out:{exe}", str(source), str(ROOT / "Assets/Scripts/MyKeyMap.cs")], check=True)
    subprocess.run([str(exe)], check=True)
