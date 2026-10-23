"""Run real screen/panel routing, ClientInput and TField focus/native polling.

Rendering, OS keyboard and text editing are fixtures; no Unity or server starts.
--ref develop reproduces the bug. --input-ref fix/input-keyboard checks compatibility.
"""
from pathlib import Path
import argparse
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument("--ref")
parser.add_argument("--input-ref")
args = parser.parse_args()

def read(path, ref=None):
    ref = ref or args.ref
    if ref:
        return subprocess.check_output(["git", "show", f"{ref}:{path}"], cwd=ROOT).decode("utf-8-sig")
    return (ROOT / path).read_text(encoding="utf-8-sig")

def block(source, start):
    opening = source.index("{", start)
    depth = 0
    for token in re.finditer(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"|[{}]', source[opening:]):
        if token[0] == "{": depth += 1
        elif token[0] == "}":
            depth -= 1
            if not depth: return source[start:opening + token.end()]
    raise AssertionError("unclosed block")

def method(source, name):
    found = re.search(r"^\s*(?:public|private|internal) [^\n]*\b" + name + r"\(", source, re.M)
    return block(source, found.start())

canvas = read("Assets/Scripts/GameCanvas.cs")
update = method(canvas, "update")
dispatch = block(update, update.index("if (currentScreen != null)"))
paint = method(canvas, "paint")
condition = "if (IsGameplayPanelActive)" if "if (IsGameplayPanelActive)" in paint else "if (panel.isShow)"
panel_paint = block(paint, paint.index(condition))
prop = re.search(r"internal static bool IsGameplayPanelActive =>[^;]+;", canvas)
field = read("Assets/Scripts/TField.cs")
focus_methods = "\n".join(method(field, name) for name in ["update", "setTextBox", "setFocus", "setFocusWithKb"])
if "public void update(bool handlePointer)" in field:
    focus_methods += "\n" + block(field, field.index("public void update(bool handlePointer)"))
client = read("Assets/Scripts/Assets.src.g/ClientInput.cs", args.input_ref)
assert "currentScreen == GameScr.instance" in read("Assets/Scripts/Hud/InventoryFrame.cs")

fixture = r'''
#pragma warning disable 0649
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using Assets.src.g;
namespace UnityEngine {
    public enum TouchScreenKeyboardType {Default,ASCIICapable,NumberPad}
    public class TouchScreenKeyboard {
        public static bool hideInput; public string text;public bool done,active=true;
        public static TouchScreenKeyboard Open(string value,TouchScreenKeyboardType t,bool a,bool b,bool secure,bool d,string name)
            =>Main.isPC?null:new TouchScreenKeyboard{text=value};
    }
}
public interface IActionListener {void perform(int action,object value);}
public class Command {
    public int x,y,w=76,h=24;public string caption;IActionListener listener;int action;
    public Command(string text,IActionListener target,int id,object value){caption=text;listener=target;action=id;}
    public void performAction(){listener?.perform(action,null);}
    public bool isPointerPressInside()=>false;public void paint(mGraphics g){}
}
public class mScreen {
    public static int ITEM_HEIGHT=24;public Command left,right,center;
    public virtual void switchToMe(){GameCanvas.clearKeyPressed();GameCanvas.currentScreen=this;}
    public virtual void update(){}public virtual void paint(mGraphics g){}public virtual void keyPress(int key){}
    public virtual void updateKey(){if(GameCanvas.keyPressed[25]){GameCanvas.keyPressed[25]=false;center?.performAction();}}
}
public class TField {
    public const int INPUT_TYPE_PASSWORD=2,INPUT_TYPE_NUMERIC=1,INPUT_TYPE_ANY=0;
    public static TField currentTField;public static TouchScreenKeyboard kb;
    public int x,y,width,height,inputType,maxTextLenght=64,mode,lastKey,timeChangeMode,timeDelayKyCode,counter,
        keyInActiveState,indexOfActiveChar,changeModeKey,showCaretCounter,indexDau=-1,timeDau;
    public bool isFocus,loginInput,isPaintCarret,showSubTextField,revealPassword;
    public string name="",text="";public Command cmdClear,cmdDoneAction;
    public void keyPressed(int code){if(code>=32&&isFocus)text+=(char)code;}
    public void paint(mGraphics g){}
    public string getText()=>text;public void setText(string s){text=s;}
    public void clearAllText(){text="";}void setPasswordTest(){}
    public int getIputType()=>inputType;public void setIputType(int n){inputType=n;}
    __FOCUS__
}
public class mGraphics {public List<string> layers=new List<string>();public void setClip(int a,int b,int c,int d){}}
public class mFont {
    public const int CENTER=2;public static mFont tahoma_7b_dark=new mFont(),tahoma_7b_green2=new mFont();
    public string[] splitFontArray(string s,int w)=>new[]{s};
    public void drawString(mGraphics g,string s,int x,int y,int align){g.layers.Add("form");}
}
static class PopUp {public static void paintPopUp(mGraphics g,int x,int y,int w,int h,int c,bool b){g.layers.Add("form");}}
static class mResources {public const string CLOSE="Close",OK="OK",vuilongnhapduthongtin="required";}
static class Res {public static void outz(string s){}}
static class Cout {public static void LogWarning(string s){}}
static class Main {public static bool isPC;public static string mainThreadName="inventory-test";}
class GameScr:mScreen {
    public static GameScr instance=new GameScr();public static GameScr gI()=>instance;
    public override void paint(mGraphics g){g.layers.Add("world");}
}
class Popup {public void update(){}public void updateKey(){}}
static class ChatPopup {public static Popup currChatPopup,serverChatPopUp;}
class Dialog {public void update(){}}
class Menu {public bool showMenu;public void updateMenu(){}public void updateMenuKey(){}}
class ChatField {public bool isShow;public void paint(mGraphics g){}}
class Panel {
    public bool isShow=true,isDoneCombine;public int X,Y,W=190,H=360,updates,keys,scrolls;public ChatField chatTField;
    public void update(){updates++;}public void updateKey(){keys++;GameCanvas.clearKeyPressed();}
    public void paint(mGraphics g){g.layers.Add("panel");}public void chatTFUpdateKey(){}
    public void hide(){isShow=false;}public void updateScroolMouse(int n){scrolls++;}
}
class ChatTextField {
    static ChatTextField value=new ChatTextField();public static ChatTextField gI()=>value;
    public bool isShow;public void UpdateModalInput(){}
}
static class Hint {public static void update(){}}
class SoundMn {public static SoundMn gI()=>new SoundMn();public void update(){}}
class Service {
    public static Service value=new Service();public static Service gI()=>value;
    public int sends;public string sent;public void sendClientInput(TField[] fields){sends++;sent=fields[fields.Length-1].getText();}
}
static class GameCanvas {
    public static int w=640,h=360,px,py,pXYScrollMouse;
    public static bool isTouch=true,isLoading,isFocusPanel2,isPointerJustRelease,isPointerDown;
    public static bool[] keyPressed=new bool[30];public static mScreen currentScreen=GameScr.instance;
    public static Dialog currentDialog;public static Menu menu=new Menu();public static Panel panel=new Panel(),panel2;
    public static void debug(string s,int n){}public static void startOKDlg(string s){}
    public static void clearKeyPressed(){Array.Clear(keyPressed,0,keyPressed.Length);isPointerJustRelease=false;}
    public static void clearAllPointerEvent(){isPointerJustRelease=isPointerDown=false;}
    public static bool isPointer(int x,int y,int w,int h)=>isPointerHoldIn(x,y,w,h);
    public static bool isPointerHoldIn(int x,int y,int w,int h)=>(isPointerDown||isPointerJustRelease)&&px>=x&&px<=x+w&&py>=y&&py<=y+h;
    __PROPERTY__
    public static void Tick(){__DISPATCH__}
    public static mGraphics Paint(){var g=new mGraphics();currentScreen.paint(g);__PANEL_PAINT__ return g;}
    __SCROLL__
}
class GiftTests {
    static int checks;static void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
    static void Click(TField field){GameCanvas.px=field.x+5;GameCanvas.py=field.y+5;GameCanvas.isPointerJustRelease=true;GameCanvas.Tick();}
    static void Main(){
        Thread.CurrentThread.Name=global::Main.mainThreadName;
        foreach(bool pc in new[]{true,false})foreach(bool secondPanel in new[]{false,true})foreach(int fields in new[]{1,2,5}){
            global::Main.isPC=pc;TField.kb=null;TField.currentTField=null;
            GameCanvas.panel=new Panel();GameCanvas.panel2=secondPanel?new Panel():null;
            var form=ClientInput.gI();form.setInput(fields,"Gift/name input");
            var image=GameCanvas.Paint();Check(!image.layers.Contains("panel"),"Retained inventory paints over the input screen");
            Check(GameCanvas.panel.isShow,"Opening input discarded the underlying inventory");
            for(int i=0;i<fields;i++){
                var field=form.tf[i];field.name="Recipient";Click(field);
                Check(GameCanvas.panel.keys==0&&GameCanvas.panel.updates==0,"Inventory consumed form input");
                Check(field.isFocus&&TField.currentTField==field,"Clicked form field lost focus");
                if(pc){form.keyPress('A');form.keyPress('b');Check(field.getText()=="Ab","PC text not routed to selected field");}
                else {
                    Check(TField.kb!=null&&TField.kb.active,"Android keyboard did not open");
                    TField.kb.text="S22player";GameCanvas.Tick();
                    Check(field.getText()=="S22player"&&field.isFocus,"Android keyboard text/focus not retained");
                }
            }
            int before=GameCanvas.panel.scrolls;GameCanvas.scrollMouse(1);
            Check(GameCanvas.panel.scrolls==before,"Hidden inventory consumed form scroll");
            int sends=Service.gI().sends;GameCanvas.keyPressed[25]=true;GameCanvas.Tick();
            Check(Service.gI().sends==sends+1,"Submit key blocked by retained inventory");
            Check(GameCanvas.currentScreen==GameScr.instance&&GameCanvas.panel.isShow,"Submit did not restore the retained inventory");
            GameCanvas.Tick();Check(GameCanvas.panel.keys>0,"Inventory input not restored after form");
            Check(GameCanvas.Paint().layers.Contains("panel"),"Inventory not painted after form");
            form=ClientInput.gI();form.setInput(1,"Rename");form.perform(1,null);
            Check(GameCanvas.currentScreen==GameScr.instance&&GameCanvas.panel.isShow,"Cancel lost inventory state");
        }
        Console.WriteLine("PASS: "+checks+" production form/panel/focus/Android keyboard checks; no Unity");
    }
}
'''
fixture = (fixture.replace("__FOCUS__", focus_methods)
           .replace("__PROPERTY__", prop[0] if prop else "")
           .replace("__DISPATCH__", dispatch).replace("__PANEL_PAINT__", panel_paint)
           .replace("__SCROLL__", method(canvas, "scrollMouse").replace("public void", "public static void")))
output = ROOT / "Build/inventory-gift-tests"
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="gift-", dir=output) as folder:
    path = Path(folder)
    cs = path / "Fixture.cs"
    cs.write_text(fixture, encoding="utf-8")
    source = path / "ClientInput.cs"
    source.write_text(client, encoding="utf-8")
    exe = path / "Gift.exe"
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", "/main:GiftTests",
                    f"/out:{exe}", str(cs), str(source)], check=True)
    subprocess.run([str(exe)], check=True)
