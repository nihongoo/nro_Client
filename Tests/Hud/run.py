"""Standalone C# checks of real HudLayout/DeadStateHud and combat registrations.

Run: python Tests/Hud/run.py
No Unity invocation. Rendering/input world objects are fixtures; production rect
providers, visibility, hit tests, command sync and dead-menu sync run unchanged.
"""
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
scripts = ROOT / "Assets/Scripts"


def combat_prefix(name):
    text = (scripts / "Hud" / name).read_text(encoding="utf-8-sig")
    return text[:text.index("        internal static bool Owns(")] + "\n    }\n}\n"


# Check actual integration points as well as the compiled providers below.
game = (scripts / "GameScr.cs").read_text(encoding="utf-8-sig")
assert "Hud.DeadStateHud.Register();" in game
assert "Hud.DeadStateHud.OpenMenu(myVector);" in game
assert game.count("SyncDeadHudCommand();") == 2
assert "Hud.DeadStateHud.SyncCommand();" in game
assert "internal static bool IsDead => HudLayout.IsPlayerDead;" in (scripts / "Hud/DeadStateHud.cs").read_text(encoding="utf-8-sig")
assert "() => Hud.CombatPcHud.Visible);" in game
screen = (scripts / "mScreen.cs").read_text(encoding="utf-8-sig")
assert "return Hud.DeadStateHud.IsButtonPressed();" in screen
menu = (scripts / "Menu.cs").read_text(encoding="utf-8-sig")
assert re.search(r"public int menuW\s*;", menu), "Fixture must match Menu.menuW's instance declaration"
assert menu.count("Hud.DeadStateHud.SyncMenu(this);") == 3
assert menu.count("if (!showMenu) return;") == 2
choices = re.findall(r"new Command\(mResources.DIES\[(\d)\], (11038\d)\)", game)
assert choices == [("1", "110381"), ("2", "110382"), ("3", "110383")]

fixture = r'''


#pragma warning disable 0649
using System;
using System.Collections.Generic;
using Hud;
namespace Hud { internal static class SkillDiagnostics {
internal enum Stage { Sync, HitTest, Pointer, Release, Keys, Wheel, Select, Action, Book, Templates,
            Metrics, IntrinsicLines, Description, AddLines, FontSplit, FontWidth, FontExact, FontHeight,
            HudGetRect, HudResolve, HudVisible, PotentialData, PotentialRect, PotentialFrame, PotentialIcon,
            PotentialText, MoneyFormat, Concat, FrameSync, FrameBackground, FrameHeader, FrameTabs,
            FrameMoney, FrameDetails, PopUp, CompactText, LayoutScroll, LayoutSize, LayoutCell, LayoutPotential, LayoutIntrinsic, LayoutGroups, LayoutDetail, Learning, Platform, FrameMetrics, Count }
internal enum Counter { BeginFrame, ViewportChange, GetRect, RectMiss, HitTest, Visible,
            MetricsCall, MetricsRebuild, FrameMetricsCall, FrameMetricsRebuild, MobileLayoutReads, Split, FontMeasure, NewList, NewArray, NewLine, NewGUIStyle,
            NewGUIContent, StringConcat, FormatMoney, TemplateCall, BookCall,
            LayoutFrame, LayoutContent, LayoutLineHeight, LayoutRowHeight, LayoutDesignSize, LayoutSize,
            LayoutColumnGap, LayoutRowGap, LayoutGridBottomGap, LayoutPotentialContent, LayoutPotentialHeaderBounds,
            LayoutScrollBounds, LayoutIntrinsicTextWidth, LayoutIntrinsicBounds, LayoutIntrinsicIconBounds,
            LayoutIntrinsicBarBounds, LayoutSkillsGroupBounds, LayoutPotentialGroupTop, LayoutPotentialGroupBounds,
            LayoutPotentialTitle, LayoutActivityTitle, LayoutCombinedTitle, LayoutTitleHeight, LayoutDetailBounds,
            LayoutDetailContent, LayoutControlsTop, LayoutUpperBounds, LayoutCellBounds, LayoutPotentialBounds,
            LayoutKeyBounds, LayoutActionBounds, Count }
internal struct ProbeScope:System.IDisposable {public void Dispose(){}}
internal static ProbeScope Probe(Stage stage)=>new ProbeScope();
internal static void Count(Counter counter,int amount=1){}
internal static void Invalidated(string file,int line){}
internal static long StartRect()=>0;
internal static void RectResolved(string name,long start){}
} }
namespace UnityEngine {
    public enum RuntimePlatform { WindowsPlayer,Android,IPhonePlayer }
    public struct Vector2 {
        public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;}
        public static Vector2 zero=>new Vector2();
        public static Vector2 operator *(Vector2 v,float n)=>new Vector2(v.x*n,v.y*n);
        public static Vector2 operator /(Vector2 v,float n)=>new Vector2(v.x/n,v.y/n);
    }
    public struct Rect {
        public float x,y,width,height;
        public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}
        public float xMin=>x; public float yMin=>y; public float xMax=>x+width; public float yMax=>y+height;
        public Vector2 center=>new Vector2(x+width/2,y+height/2);
        public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;
        public static Rect MinMaxRect(float l,float t,float r,float b)=>new Rect(l,t,r-l,b-t);
    }
    public static class Mathf {
        public static float Max(float a,float b)=>Math.Max(a,b); public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b); public static int Min(int a,int b)=>Math.Min(a,b);
    }
}
namespace UnityEngine.Device { public static class Application { public static UnityEngine.RuntimePlatform platform; } }
class TaskMaint { public int taskId=2; }
class Char {
    static Char player=new Char(); public static Char myCharz()=>player;
    public long cHP=100; public int statusMe=1; public bool meDead,isTeleport,isPaintNewSkill;
    public TaskMaint taskMaint=new TaskMaint(); public static bool isLockKey,isLoadingMap;
}
class Command {
    public string caption; public int idAction,x,y,w,h; public bool isFocus;
    public Command(string text,int action){caption=text;idAction=action;}
}
class MyVector {
    public List<Command> items=new List<Command>(); public void addElement(Command c)=>items.Add(c);
    public int size()=>items.Count;
}
class Menu {
    public int menuW=60; public static int[] menuTemY;
    public int menuH=60,menuX,menuY,w; public MyVector menuItems; public bool showMenu;
    public void startAt(MyVector choices,int pos){menuItems=choices;menuTemY=new int[choices.size()];showMenu=true;}
}
class Panel { public bool isShow; }
class GamePad { public int xZone,yZone,wZone,hZone; public UnityEngine.Rect HudVisualBounds=>new UnityEngine.Rect(); }
class GameScr {
    public static GameScr instance=new GameScr(); public Command cmdDead,center;
    public static bool isPaint=true,isPaintOther,isPickNgocRong,isudungCapsun4,isudungCapsun3;
    public static int isPaintRada=1,isAnalog=1; public static GamePad gamePad=new GamePad();
    public object mobCapcha; public bool isLockKey;
    public bool isNotPaintTouchControl()=>false; public bool isPaintPopup()=>false; public bool isOpenUI()=>false;
    public static void RegisterHudRects(){
        CombatMobileHud.Register();CombatPcHud.Register();DeadStateHud.Register();
        HudLayout.Register("pc-analog.touch",()=>new UnityEngine.Rect(0,180,100,180),HudLayout.ControlsOrder,()=>CombatPcHud.Visible);
        HudLayout.Register(HudLayout.ChatButton,()=>new UnityEngine.Rect(GameCanvas.w-36,0,34,34),HudLayout.ChatOrder);
        HudLayout.Register(HudLayout.Bell,()=>new UnityEngine.Rect(GameCanvas.w-74,0,34,34),HudLayout.BellOrder);
        HudLayout.Register("top-actions.hub",()=>new UnityEngine.Rect(GameCanvas.w-112,0,34,34),HudLayout.ChatOrder);
        HudLayout.Register("legacy.overlay",()=>HudLayout.ScreenBounds,HudLayout.OverlayOrder,
            ()=>GameCanvas.menu.showMenu||GameCanvas.currentDialog!=null||GameCanvas.panel.isShow||ChatTextField.gI().isShow);
    }
}
static class GameCanvas {
    public static int w=640,h=360,px,py; public static int hw=>w/2; public static int hh=>h/2;
    public static object currentScreen=GameScr.instance,currentDialog;
    public static Panel panel=new Panel(); public static Menu menu=new Menu();
    public static bool isTouch=true,isLoading,isPointerDown,isPointerClick,isPointerJustRelease;
}
static class mGraphics { public static int zoomLevel=2,addYWhenOpenKeyBoard; }
static class mResources { public static string[] DIES={"Kiệt sức","Chờ","Về nhà","Hồi sinh ngay (1 ngọc)"}; }
static class Main { public static bool isPC=true; }
static class Controller { public static bool isStopReadMessage; }
static class InfoDlg { public static bool isLock,isShow; }
static class ChatPopup { public static object currChatPopup,serverChatPopUp; }
class ChatTextField { static ChatTextField instance=new ChatTextField(); public static ChatTextField gI()=>instance; public bool isShow; }
static class Rms { public static int loadRMSInt(string key)=>-1; public static void saveRMSInt(string key,int value){} }
namespace Notifications { public static class NotificationCenter { public static bool IsOpen; } }
namespace Mod.CuongLe { public static class DoHoa { public static bool isShowMenuVIP; } }
class DeadHudTests {
    static int checks;
    static void Check(bool b,string text){checks++;if(!b)throw new Exception(text);}
    static void Near(float a,float b,string text)=>Check(Math.Abs(a-b)<1,text);
    static void Alive(){Char.myCharz().cHP=100;Char.myCharz().statusMe=1;Char.myCharz().meDead=false;}
    static List<string> Controls(bool mobile){
        var result=new List<string>{mobile?CombatMobileHud.Attack:CombatPcHud.Attack};
        for(int i=0;i<8;i++)result.Add(mobile?CombatMobileHud.SkillName(i):CombatPcHud.SkillName(i));
        for(int i=0;i<3;i++)result.Add(mobile?CombatMobileHud.ItemName(i):CombatPcHud.ItemName(i));
        return result;
    }
    static void Main(){
        foreach(bool mobile in new[]{false,true})foreach(int zoom in new[]{2,3,4})foreach(int width in new[]{320,422,640,800})
        foreach(int height in mobile?new[]{180,195}:new[]{300,360}){
            UnityEngine.Device.Application.platform=mobile?UnityEngine.RuntimePlatform.Android:UnityEngine.RuntimePlatform.WindowsPlayer;
            mGraphics.zoomLevel=zoom;GameCanvas.w=width;GameCanvas.h=height;
            Alive();HudLayout.BeginFrame();DeadStateHud.SyncCommand();
            Check(mobile?CombatMobileHud.Visible:CombatPcHud.Visible,"living combat hidden");
            var names=Controls(mobile);var before=new List<UnityEngine.Rect>();
            foreach(string name in names)before.Add(HudLayout.GetRect(name));
            for(int state=0;state<4;state++){
                Alive();if(state==0)Char.myCharz().cHP=0;else if(state==1)Char.myCharz().statusMe=14;
                else if(state==2)Char.myCharz().statusMe=5;else Char.myCharz().meDead=true;
                // Visibility must change even within the same HudLayout generation.
                Check(!CombatMobileHud.Visible&&!CombatPcHud.Visible,"dead combat still visible");
                Check(!CombatMobileHud.CanInput&&!CombatPcHud.CanInput,"dead combat still accepts input");
                foreach(string name in names)Check(HudLayout.HitTest(HudLayout.GetRect(name).center)!=name,"invisible combat captures input: "+name);
                var obstacles=new List<UnityEngine.Rect>();HudLayout.CopyObstaclesAtOrder(HudLayout.ControlsOrder,obstacles);
                Check(obstacles.Count==1,"hidden combat/analog remains an obstacle");
                DeadStateHud.SyncCommand();var rect=HudLayout.GetRect(DeadStateHud.Button);
                Near(rect.center.x,GameCanvas.w/2f,"dead button not horizontally centered");
                var row=HudLayout.GetRect(mobile?CombatMobileHud.SkillName(4):CombatPcHud.SkillName(0));
                Check(rect.yMax<row.yMin,"dead button is not above the old skill row");
                Check(rect.xMin>=0&&rect.xMax<=GameCanvas.w&&rect.yMin>=0&&rect.yMax<=GameCanvas.h,"button outside viewport");
                Check(GameScr.instance.center==GameScr.instance.cmdDead&&GameScr.instance.cmdDead.idAction==11038,"wrong dead command");
                Near(GameScr.instance.cmdDead.x,rect.x,"paint and hit x differ");Near(GameScr.instance.cmdDead.y,rect.y,"paint and hit y differ");
                GameCanvas.px=(int)rect.center.x;GameCanvas.py=(int)rect.center.y;
                GameCanvas.isPointerClick=GameCanvas.isPointerJustRelease=true;
                Check(DeadStateHud.IsButtonPressed(),"dead button is not clickable through HudLayout");
                foreach(string top in new[]{HudLayout.ChatButton,HudLayout.Bell,"top-actions.hub"})
                    Check(HudLayout.HitTest(HudLayout.GetRect(top).center)==top,"top action blocked while dead");
                GameCanvas.panel.isShow=true;Check(!DeadStateHud.IsButtonPressed(),"button fires through panel");
                DeadStateHud.SyncCommand();Check(GameScr.instance.center==null,"hidden dead command still drawn");GameCanvas.panel.isShow=false;
            }
            var choices=new MyVector();for(int i=1;i<=3;i++)choices.addElement(new Command(mResources.DIES[i],110380+i));
            DeadStateHud.OpenMenu(choices);var menu=GameCanvas.menu;var bounds=HudLayout.GetRect(DeadStateHud.MenuBounds);
            Near(menu.menuX+bounds.width/2,GameCanvas.w/2f,"menu not horizontally centered");
            var skillRow=HudLayout.GetRect(mobile?CombatMobileHud.SkillName(4):CombatPcHud.SkillName(0));
            Near(menu.menuY+menu.menuH,skillRow.yMax,"revive choices do not share the skill row bottom edge");
            Check(bounds.xMin>=0&&bounds.xMax<=GameCanvas.w&&bounds.yMin>=0&&bounds.yMax<=GameCanvas.h,"revive menu outside viewport");
            foreach(int y in Menu.menuTemY)Check(y==menu.menuY,"menu paint and input rows differ");
            Check(!DeadStateHud.IsButtonPressed(),"button fires through its open menu");
            GameCanvas.w+=20;GameCanvas.h+=20;HudLayout.BeginFrame();DeadStateHud.SyncMenu(menu);
            skillRow=HudLayout.GetRect(mobile?CombatMobileHud.SkillName(4):CombatPcHud.SkillName(0));
            Near(menu.menuY+menu.menuH,skillRow.yMax,"revive menu does not follow skill row after viewport resize");
            Alive();DeadStateHud.SyncMenu(menu);DeadStateHud.SyncCommand();
            Check(!menu.showMenu&&GameScr.instance.center==null,"revival leaves stale menu/command");
            GameCanvas.w-=20;GameCanvas.h-=20;HudLayout.BeginFrame();
            Check(mobile?CombatMobileHud.Visible:CombatPcHud.Visible,"combat not restored on revival");
            for(int i=0;i<names.Count;i++){
                var after=HudLayout.GetRect(names[i]);Near(after.x,before[i].x,"restored x changed");Near(after.y,before[i].y,"restored y changed");
                Near(after.width,before[i].width,"restored width changed");
            }
        }
        Console.WriteLine("PASS: "+checks+" HUD checks (real layout/death state; no Unity build)");
    }
}
'''
output = (ROOT / "Build/dead-hud-tests").resolve()
assert output.is_relative_to(ROOT.resolve())
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="dead-hud-", dir=output) as folder:
    path = Path(folder)
    sources = []
    for name in ["CombatMobileHud.cs", "CombatPcHud.cs"]:
        cs = path / name
        cs.write_text(combat_prefix(name), encoding="utf-8")
        sources.append(str(cs))
    cs = path / "DeadHudTests.cs"
    cs.write_text(fixture, encoding="utf-8")
    exe = path / "DeadHudTests.exe"
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", "/target:exe",
                    f"/out:{exe}", str(cs), str(scripts / "Hud/HudLayout.cs"),
                    str(scripts / "Hud/DeadStateHud.cs"), *sources], check=True)
    subprocess.run([str(exe)], check=True)
