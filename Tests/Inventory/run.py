"""Run real inventory rect/option helpers without Unity. No game/server startup.

Run: python Tests/Inventory/run.py
Rendering/world types are fixtures. Compile production HudLayout and the actual
InventoryFrame registry/helpers; the whole client is checked separately with its
real assembly references so fixtures are not a substitute for a compile check.
"""
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
source = (ROOT / "Assets/Scripts/Hud/InventoryFrame.cs").read_text(encoding="utf-8-sig")
assert "InventoryFrame.Option(item, Item.OPT_STARSLOT)" in source
assert "InventoryFrame.Option(item, Item.OPT_MAXSTARSLOT)" not in source
assert "inventoryBodySlots = { 0, 1, 2, 3, 4, 6, 5, 7, -1 }" in source
assert "if (slot < 5 && item != null)" in source and "if (slot < 6)" in source
assert "luongKhoa" not in source and "imgLuongKhoa" not in source
assert "Service.gI()" not in source  # Existing Panel handlers remain responsible for packets.
assert "X = Y = cmx = cmtoX = 0" not in source  # Do not defeat hide animation.
assert "new[] { 0, 1, 2, 3, 6 }" in source
assert "color = InventoryFrame.SetColor(item)" in source

fixture = r'''
#pragma warning disable 0649
using System;
using System.Collections.Generic;
using UnityEngine;
using Hud;
namespace UnityEngine {
    public enum RuntimePlatform { WindowsPlayer,Android,IPhonePlayer }
    public struct Color { public byte r; }
    public struct Color32 {
        byte r; public Color32(byte r,byte g,byte b,byte a){this.r=r;}
        public static implicit operator Color(Color32 value)=>new Color{r=value.r};
    }
    public struct Vector2 {
        public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} public static Vector2 zero=>new Vector2();
        public static Vector2 operator *(Vector2 v,float n)=>new Vector2(v.x*n,v.y*n);
        public static Vector2 operator /(Vector2 v,float n)=>new Vector2(v.x/n,v.y/n);
    }
    public struct Rect {
        public float x,y,width,height; public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}
        public float xMin=>x; public float yMin=>y; public float xMax=>x+width; public float yMax=>y+height;
        public Vector2 center=>new Vector2(x+width/2,y+height/2);
        public bool Contains(Vector2 p)=>width>0&&height>0&&p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;
        public static Rect MinMaxRect(float l,float t,float r,float b)=>new Rect(l,t,r-l,b-t);
    }
    public static class Mathf {
        public static float Max(float a,float b)=>Math.Max(a,b); public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b); public static int Min(int a,int b)=>Math.Min(a,b);
    }
}
namespace UnityEngine.Device { public static class Application { public static RuntimePlatform platform; } }
class Item { public ItemOption[] itemOption; }
class ItemOption { public ItemOptionTemplate optionTemplate; public int param; }
class ItemOptionTemplate { public int id; }
class mFont {
    public static mFont tahoma_7_yellow=new mFont(); public Color color1; public bool fail;
    public int getWidth(string text)=>text.Length*5;
    public void drawString(mGraphics g,string s,int x,int y,int a){}
    public void _drawString(mGraphics g,string s,int x,int y,int a){if(fail)throw new Exception("render fixture");}
}
class mGraphics {
    public static int zoomLevel=2,addYWhenOpenKeyBoard;
    public void setColor(int color){} public void fillRect(int x,int y,int w,int h){} public void setClip(int x,int y,int w,int h){}
}
class Panel { public bool isShow=true,InventoryFrameActive=true; public int currentTabIndex=1; public int[] InventoryFrameTabIds={0,1,2,3,6}; }
class Menu { public bool showMenu; }
static class GameCanvas { public static int w=640,h=360; public static bool modal; public static object currentDialog; public static Menu menu=new Menu(); public static Panel panel=new Panel(); }
namespace Notifications { public static class NotificationCenter { public static bool IsOpen; } }
class GamePad { public Rect HudVisualBounds=>new Rect(); }
class Char { static Char p=new Char(); public static Char myCharz()=>p; public int statusMe; }
static class CombatMobileHud { public const string JoystickTouch="stick"; }
static class CombatPcHud { public static bool Visible=>false; }
class GameScr {
    public static GameScr instance=new GameScr(); public static int isAnalog; public static GamePad gamePad;
    public static void RegisterHudRects(){InventoryFrame.Register();HudLayout.Register("test.modal",()=>HudLayout.ScreenBounds,300,()=>GameCanvas.modal);}
}
class InventoryTests {
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static void Near(float a,float b,string s)=>Check(Math.Abs(a-b)<.1,s);
    static void Main(){
        foreach(bool mobile in new[]{true,false})foreach(int zoom in new[]{2,3,4}){
            foreach(int[] size in mobile?new[]{new[]{422,195},new[]{585,270},new[]{844,390}}:new[]{new[]{640,360},new[]{960,540},new[]{1280,720}}){
                UnityEngine.Device.Application.platform=mobile?RuntimePlatform.Android:RuntimePlatform.WindowsPlayer;
                GameCanvas.w=size[0];GameCanvas.h=size[1];mGraphics.zoomLevel=zoom;HudLayout.BeginFrame();
                Rect root=HudLayout.GetRect(InventoryFrame.Root),money=HudLayout.GetRect(InventoryFrame.Money);
                Near(root.x,0,"left panel not flush left");Near(root.y,0,"panel not flush top");Near(root.height,GameCanvas.h,"panel not full height");
                Check(root.width<GameCanvas.w,"character view fully covered");
                // Fixed maximum resource fields and their icons must not overlap.
                Check(root.width>=mFont.tahoma_7_yellow.getWidth("99.999.999.999")+mFont.tahoma_7_yellow.getWidth("1.000.000")+42,"resource fields collide");
                for(int i=0;i<9;i++){
                    Rect cell=HudLayout.GetRect(InventoryFrame.Slot(i));
                    Near(cell.width,InventoryFrame.Unit(mobile?40:64),"equipment size changed");
                    Check(cell.xMin>=0&&cell.xMax<=root.xMax,"equipment outside left panel");
                    Check(cell.yMin>=72&&cell.yMax+7<money.y,"equipment metadata overlaps header/footer");
                    Check(HudLayout.HitTest(cell.center)==InventoryFrame.Slot(i),"equipment hit-test does not match painting");
                    GameCanvas.modal=true;Check(HudLayout.HitTest(cell.center)=="test.modal","equipment fires through modal");GameCanvas.modal=false;
                }
                GameCanvas.panel.currentTabIndex=2;
                Check(HudLayout.HitTest(HudLayout.GetRect(InventoryFrame.Slot(0)).center)!=InventoryFrame.Slot(0),"hidden equipment still captures input");
                GameCanvas.panel.currentTabIndex=1;GameCanvas.panel.isShow=false;
                Check(HudLayout.HitTest(HudLayout.GetRect(InventoryFrame.Close).center)==null,"closed frame captures input");GameCanvas.panel.isShow=true;
            }
        }
        Check(InventoryFrame.Option(null,72)==0,"null item");
        Item item=new Item{itemOption=new[]{null,new ItemOption(),new ItemOption{optionTemplate=new ItemOptionTemplate{id=102},param=7},new ItemOption{optionTemplate=new ItemOptionTemplate{id=107},param=9}}};
        Check(InventoryFrame.Option(item,102)==7&&InventoryFrame.Option(item,107)==9,"filled stars confused with capacity");
        Check(InventoryFrame.SetColor(null)==InventoryFrame.Border,"empty set border");
        Check(InventoryFrame.SetColor(item)==InventoryFrame.Border,"unrecognized set must retain neutral border");
        var colors=new HashSet<int>();
        foreach(int id in new[]{127,128,129,130,131,132,133,134,135,233,237,241,245}){
            item.itemOption=new[]{null,new ItemOption(),new ItemOption{optionTemplate=new ItemOptionTemplate{id=id}}};
            int color=InventoryFrame.SetColor(item);
            Check(color!=InventoryFrame.Border&&colors.Add(color),"set palette must distinguish every set");
        }
        item.itemOption=new[]{new ItemOption{optionTemplate=new ItemOptionTemplate{id=34}}};
        Check(InventoryFrame.SetColor(item)==InventoryFrame.Border,"rarity is not a set identity");
        mFont font=new mFont{color1=new Color{r=123},fail=true};
        try{InventoryFrame.Text(new mGraphics(),font,"test",0,0,0);}catch(Exception){}
        Check(font.color1.r==123,"temporary text palette leaked into other UI");
        Console.WriteLine("PASS: "+checks+" inventory registry/metadata checks; no Unity build");
    }
}
'''
output = (ROOT / "Build/inventory-tests").resolve()
assert output.is_relative_to(ROOT.resolve())
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="inventory-", dir=output) as folder:
    path = Path(folder)
    (path / "InventoryRegistry.cs").write_text(source[:source.index("public partial class Panel")], encoding="utf-8")
    (path / "InventoryTests.cs").write_text(fixture, encoding="utf-8")
    exe = path / "InventoryTests.exe"
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", "/target:exe", f"/out:{exe}",
                    str(path / "InventoryRegistry.cs"), str(path / "InventoryTests.cs"),
                    str(ROOT / "Assets/Scripts/Hud/HudLayout.cs")], check=True)
    subprocess.run([str(exe)], check=True)
