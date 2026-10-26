"""Run real inventory rect/option helpers without Unity. No game/server startup.

Run: python Tests/Inventory/run.py
Rendering/world types are fixtures. Compile production HudLayout and the actual
InventoryFrame registry/helpers; the whole client is checked separately with its
real assembly references so fixtures are not a substitute for a compile check.
"""
from pathlib import Path
import subprocess
import tempfile
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
source = (ROOT / "Assets/Scripts/Hud/InventoryFrame.cs").read_text(encoding="utf-8-sig")
bag_panel = (ROOT / "Assets/Scripts/Hud/InventoryBagPanel.cs").read_text(encoding="utf-8-sig")
assert "InventoryBag.DisplaySlots(inventoryBagMap.Count, inventoryBagFilter)" in source
assert "InventoryBag.DisplaySlots(inventoryBagMap.Count, inventoryBagFilter)" in bag_panel
locked_start = bag_panel.index("    private static void PaintClosedBagSlot(")
locked_begin = bag_panel.index("{", locked_start)
depth, locked_end = 1, locked_begin + 1
while depth:
    depth += (bag_panel[locked_end] == "{") - (bag_panel[locked_end] == "}")
    locked_end += 1
locked_painter = bag_panel[locked_start:locked_end]
assert "InventoryFrame.Option(item, Item.OPT_STARSLOT)" in source
assert "InventoryFrame.Option(item, Item.OPT_MAXSTARSLOT)" not in source
assert "inventoryBodySlots = { 0, 1, 2, 3, 4, 6, 5, 7, -1 }" in source
assert "if (slot < 5 && item != null)" in source and "if (slot < 6)" in source
assert "luongKhoa" not in source and "imgLuongKhoa" not in source
assert "Service.gI()" not in source  # Existing Panel handlers remain responsible for packets.
assert "X = Y = cmx = cmtoX = 0" not in source  # Do not defeat hide animation.
assert "new[] { 0, 1, 2, 3, 7, 6 }" in source
assert "color = InventoryFrame.SetColor(item)" in source
assert "0x3b2515" not in source and "0x27170c" not in source
assert "g.fillRoundRect(" not in source
assert "TextAnchor.MiddleLeft" in source and "padding = new RectOffset(0, 0, 0, 0)" in source

currency_assets = []
for gold, filename in ((True, "myTexture2dimgMoney.png"), (False, "myTexture2dimgDiamond.png")):
    for zoom in (1, 2, 3, 4):
        with Image.open(ROOT / f"Assets/Resources/res/x{zoom}/mainImage/{filename}") as asset:
            left, top, right, bottom = asset.getbbox()
            currency_assets.append([int(gold), zoom, *asset.size, left, asset.height-bottom, right-left, bottom-top])

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
    public static class GUI { public static Color color; }
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
partial class Item { public ItemOption[] itemOption; public ItemTemplate template; public int quantity; public string info,content; }
class ItemTemplate { public short id; public sbyte type; }
class ItemOption { public ItemOptionTemplate optionTemplate; public int param; }
class ItemOptionTemplate { public int id; }
class mFont {
    public static mFont tahoma_7_yellow=new mFont();
    public static mFont tahoma_7_grey=new mFont();public const int CENTER=2;
    public readonly List<string> drawn=new List<string>();
    public int getHeight()=>8;
    public void drawString(mGraphics g,string text,int x,int y,int align){drawn.Add(text);}
    public int getWidth(string text)=>text.Length*5;
}
class Image {}
static class InventoryFrameSkin { public static Image Corners(int fill,int border,byte opacity)=>null; }
class PopUp { public static bool fail; public static void paintPopUp(mGraphics g,int x,int y,int w,int h,int color,bool isButton,Image corner=null,int borderColor=0){if(fail)throw new Exception("skin fixture");} }
class mGraphics {
    public static int zoomLevel=2,addYWhenOpenKeyBoard;
    public int lockOutlines;public void drawRect(int x,int y,int w,int h){lockOutlines++;}
    public void setColor(int color){} public void fillRect(int x,int y,int w,int h){} public void setClip(int x,int y,int w,int h){}
}
partial class Panel {
    public bool isShow=true,InventoryFrameActive=true,InventoryBagDetailOpen;
    public int currentTabIndex=1,cmy,InventoryBagCellCount=30;
    public bool DisciplePanelActive=>currentTabIndex==7; public bool DiscipleSkillOpen;
    public Rect InventoryDetailUpperBounds=>new Rect(); public Rect InventoryDetailLowerBounds=>new Rect();
    public int InventoryDetailButtonCount=>0; public Rect InventoryDetailButtonBounds(int i)=>new Rect();
    public int InventoryFrameScroll=>currentTabIndex==1?cmy:0;
    public string[][] currentTabName=new string[7][];
}
class Menu { public bool showMenu; }
static class GameCanvas { public static int w=640,h=360; public static bool modal; public static object currentDialog,currentScreen=GameScr.instance; public static Menu menu=new Menu(); public static Panel panel=new Panel(); }
namespace Notifications { public static class NotificationCenter { public static bool IsOpen; } }
class GamePad { public Rect HudVisualBounds=>new Rect(); }
class Char { static Char p=new Char(); public static Char myCharz()=>p; public int statusMe; public bool meDead,havePet=true; public int cHP=1; }
static class CombatMobileHud { public const string JoystickTouch="stick"; }
static class CombatPcHud { public static bool Visible=>false; }
class GameScr {
    public static GameScr instance=new GameScr(); public static int isAnalog; public static GamePad gamePad;
    public static void RegisterHudRects(){InventoryFrame.Register();DiscipleLayout.Register();HudLayout.Register("test.modal",()=>HudLayout.ScreenBounds,300,()=>GameCanvas.modal);}
}
class InventoryTests {
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static void Near(float a,float b,string s)=>Check(Math.Abs(a-b)<.1,s);
    static void Main(){
        Panel savedPanel=GameCanvas.panel;
        mFont savedFont=mFont.tahoma_7_yellow;
        GameCanvas.currentScreen=null; GameCanvas.panel=null; mFont.tahoma_7_yellow=null;
        HudLayout.BeginFrame();
        Check(HudLayout.HitTest(new Vector2(10,10))==null,"startup without panel/fonts must not resolve inventory");
        int painted=0; HudLayout.ForEachBackToFront((name,rect)=>painted++);
        Check(painted==0,"startup traversal must skip hidden inventory providers");
        Check(HudLayout.GetRect(InventoryFrame.Tab(0)).width==0,"explicit tab resolution without panel must be empty");
        GameCanvas.panel=savedPanel; savedPanel.isShow=false; savedPanel.currentTabName=null;
        HudLayout.BeginFrame();
        Check(HudLayout.HitTest(new Vector2(10,10))==null,"closed uninitialized panel must not resolve inventory");
        Check(savedPanel.InventoryFrameTabIds.Length==0,"uninitialized tabs must be empty");
        GameCanvas.currentScreen=GameScr.instance; savedPanel.isShow=true; HudLayout.BeginFrame();
        Check(HudLayout.HitTest(new Vector2(10,10))==null,"in-game panel with uninitialized tabs must be hidden");
        GameCanvas.currentScreen=null;
        mFont.tahoma_7_yellow=savedFont; savedPanel.currentTabName=new string[7][];
        Check(object.ReferenceEquals(savedPanel.InventoryFrameTabIds,savedPanel.InventoryFrameTabIds),"tab reads must reuse the array");
        savedPanel.isShow=true;
        HudLayout.BeginFrame();
        Check(HudLayout.HitTest(new Vector2(10,10))==null,"inventory must not capture login input");
        GameCanvas.currentScreen=GameScr.instance;
        int[][] assets = new int[][] { CURRENCY_ASSETS };
        foreach(int[] asset in assets){
            Rect uv=InventoryFrame.MoneySpriteUv(asset[0]==1,asset[1]);
            Near(uv.x*asset[2],asset[4],"currency left alpha crop");
            Near(uv.y*asset[3],asset[5],"currency bottom alpha crop");
            Near(uv.width*asset[2],asset[6],"currency visible width");
            Near(uv.height*asset[3],asset[7],"currency visible height");
        }
        foreach(bool mobile in new[]{true,false})foreach(int zoom in new[]{2,3,4}){
            foreach(int[] size in mobile?new[]{new[]{422,195},new[]{585,270},new[]{844,390}}:new[]{new[]{640,360},new[]{960,540},new[]{1280,720}}){
                UnityEngine.Device.Application.platform=mobile?RuntimePlatform.Android:RuntimePlatform.WindowsPlayer;
                GameCanvas.w=size[0];GameCanvas.h=size[1];mGraphics.zoomLevel=zoom;HudLayout.BeginFrame();
                Rect root=HudLayout.GetRect(InventoryFrame.Root),money=HudLayout.GetRect(InventoryFrame.Money);
                Near(root.x,0,"left panel not flush left");Near(root.y,0,"panel not flush top");Near(root.height,GameCanvas.h,"panel not full height");
                Check(root.width<GameCanvas.w,"character view fully covered");
                Check(GameCanvas.panel.InventoryFrameTabIds.Length==6,"main menu must contain six tabs");
                GameCanvas.panel.currentTabIndex=7; HudLayout.Invalidate();
                for(int d=0;d<12;d++){
                    Rect cell=DiscipleLayout.Cell(d);
                    Check(cell.x>=root.x&&cell.xMax<=root.xMax&&cell.yMax<=money.y,"disciple grid outside frame");
                    Check(HudLayout.HitTest(cell.center)=="disciple.cell."+d,"disciple cell not clickable");
                    Near(cell.y,DiscipleLayout.Cell(d/4*4).y,"disciple grid must have four columns");
                }
                for(int d=0;d<5;d++)Check(HudLayout.HitTest(DiscipleLayout.Chip(d).center)=="disciple.status."+d,"status chip not clickable");
                GameCanvas.panel.currentTabIndex=1; HudLayout.Invalidate();
                // Fixed maximum resource fields and their icons must not overlap.
                Check(root.width>=mFont.tahoma_7_yellow.getWidth("99.999.999.999")+mFont.tahoma_7_yellow.getWidth("1.000.000")+42,"resource fields collide");
                int[] goldAsset=Array.Find(assets,a=>a[0]==1&&a[1]==zoom),gemAsset=Array.Find(assets,a=>a[0]==0&&a[1]==zoom);
                float goldAspect=(float)goldAsset[6]/goldAsset[7],gemAspect=(float)gemAsset[6]/gemAsset[7];
                Rect goldText=InventoryFrame.MoneyTextBounds(money,mFont.tahoma_7_yellow.getWidth("99.999.999.999")+1,true,goldAspect);
                Rect gemText=InventoryFrame.MoneyTextBounds(money,mFont.tahoma_7_yellow.getWidth("1.000.000")+1,false,goldAspect);
                Rect goldIcon=InventoryFrame.MoneyIconBounds(money,goldText.x,goldAspect),gemIcon=InventoryFrame.MoneyIconBounds(money,gemText.x,gemAspect);
                Near(goldIcon.height,gemIcon.height,"currency icons have different visible heights");
                Near(goldIcon.center.y,goldText.center.y,"gold icon/text not centered");
                Near(gemIcon.center.y,gemText.center.y,"gem icon/text not centered");
                Near(goldText.x-goldIcon.xMax,InventoryFrame.MoneyGap,"gold icon/number gap");
                Near(gemText.x-gemIcon.xMax,InventoryFrame.MoneyGap,"gem icon/number gap");
                Check(goldText.xMax<=gemIcon.x,"maximum currency values overlap");
                Check(goldIcon.x>=money.x&&gemText.xMax<=money.xMax,"currency content outside footer");
                Check(goldIcon.y>=money.y&&goldIcon.yMax<=money.yMax,"currency icon clipped vertically");
                for(int i=0;i<9;i++){
                    Rect cell=HudLayout.GetRect(InventoryFrame.Slot(i));
                    Near(InventoryFrame.EquipmentBounds(i).width,InventoryFrame.Unit(mobile?40:64),"equipment size changed");
                    Check(cell.xMin>=0&&cell.xMax<=root.xMax,"equipment outside left panel");
                    if(cell.height<=0)continue;
                    Check(cell.yMin>=72&&cell.yMax<=money.y,"equipment clip overlaps header/footer");
                    Check(HudLayout.HitTest(cell.center)==InventoryFrame.Slot(i),"equipment hit-test does not match painting");
                    GameCanvas.modal=true;Check(HudLayout.HitTest(cell.center)=="test.modal","equipment fires through modal");GameCanvas.modal=false;
                }
                GameCanvas.panel.currentTabIndex=2;
                Check(HudLayout.HitTest(HudLayout.GetRect(InventoryFrame.Slot(0)).center)!=InventoryFrame.Slot(0),"hidden equipment still captures input");
                GameCanvas.panel.currentTabIndex=1;GameCanvas.panel.isShow=false;
                Check(HudLayout.HitTest(HudLayout.GetRect(InventoryFrame.Close).center)==null,"closed frame captures input");GameCanvas.panel.isShow=true;
                HudLayout.Invalidate();
                Rect viewport=HudLayout.GetRect(InventoryFrame.Bag);
                foreach(int capacity in new[]{0,30,35,36,40,55,75,79,80,100,255}){
                    GameCanvas.panel.InventoryBagCellCount=capacity;
                    int display=InventoryBag.DisplaySlots(capacity,InventoryBagFilter.All);
                    Check(display>=Math.Max(80,capacity)&&display%5==0,"locked-slot layout hides legacy capacity");
                    int rows=InventoryBag.Rows(capacity);
                    Check(rows>=7&&rows*5>=capacity,"bag capacity hidden or manufactured");
                    Rect last=InventoryFrame.BagCellBounds(rows*5-1);
                    GameCanvas.panel.cmy=(int)Math.Ceiling(last.yMax+5+GameCanvas.panel.cmy-viewport.yMax);
                    HudLayout.Invalidate();
                    for(int i=0;i<Math.Max(35,capacity);i++){
                        Rect cell=InventoryFrame.BagCellBounds(i);
                        Near(cell.width,InventoryFrame.Unit(mobile?44:56),"bag cell size changed");
                        Check(cell.x>=viewport.x&&cell.xMax+2<=viewport.xMax,"bag badge at outer column is clipped");
                        if(i%5!=4)Near(InventoryFrame.BagCellBounds(i+1).y,cell.y,"bag is not five columns");
                        Rect badge=InventoryBag.BadgeBounds(cell,cell.width+InventoryFrame.BagGap-2,10);
                        Check(badge.x>=viewport.x&&badge.xMax<=viewport.xMax,"maximum badge width outside viewport");
                    }
                    if(capacity>0){
                        Rect real=HudLayout.GetRect(InventoryFrame.BagCell(capacity-1));
                        Check(real.height>0&&HudLayout.HitTest(real.center)==InventoryFrame.BagCell(capacity-1),"last real bag slot inaccessible");
                    }
                    if(capacity<35){
                        Rect ghost=InventoryFrame.BagCellBounds(34);
                        Check(HudLayout.HitTest(ghost.center)!=InventoryFrame.BagCell(34),"padding slot accepts input");
                    }
                    if(capacity>=35&&capacity<80){
                        Rect closed=InventoryFrame.BagCellBounds(capacity);
                        GameCanvas.panel.cmy+=(int)(closed.y-viewport.y);HudLayout.Invalidate();
                        closed=InventoryFrame.BagCellBounds(capacity);
                        Check(HudLayout.HitTest(closed.center)!=InventoryFrame.BagCell(capacity),"unopened slot accepts input");
                    }
                    Check(HudLayout.HitTest(new Vector2(10,50))==InventoryFrame.Tab(0),"scrolled inventory steals tab input");
                    GameCanvas.panel.cmy=0;HudLayout.Invalidate();
                }
                GameCanvas.panel.InventoryBagCellCount=30;HudLayout.Invalidate();
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
        GUI.color=new Color{r=123}; PopUp.fail=true;
        try{InventoryFrame.Rounded(new mGraphics(),new Rect(0,0,40,40),InventoryFrame.SlotBackground);}catch(Exception){}
        Check(GUI.color.r==123,"native skin tint leaked into other UI");
        Check(InventoryBag.Quantity(758)=="758"&&InventoryBag.Quantity(999)=="999","quantity below display cap changed");
        Check(InventoryBag.Quantity(1000)=="999+"&&InventoryBag.Quantity(1148)=="999+","quantity display cap boundary");
        Check(InventoryBag.Quantity(9999)=="999+"&&InventoryBag.Quantity(24300)=="999+","large quantity still abbreviated");
        Check(InventoryBag.Quantity(int.MaxValue)=="999+","largest server quantity display cap");
        Item equipment=new Item{template=new ItemTemplate{id=1,type=0}},material=new Item{template=new ItemTemplate{id=442,type=30}};
        Item refreshed=new Item{template=equipment.template,quantity=7};
        Check(InventoryBag.SameDetailItem(equipment,refreshed),"quantity-only server refresh must preserve detail");
        Check(!InventoryBag.SameDetailItem(equipment,null)&&!InventoryBag.SameDetailItem(equipment,material),"removed/replaced item must invalidate detail");
        refreshed.content="changed";Check(!InventoryBag.SameDetailItem(equipment,refreshed),"changed metadata accepted as same item");
        refreshed.content=null;refreshed.itemOption=new[]{new ItemOption{optionTemplate=new ItemOptionTemplate{id=0},param=5}};
        Check(!InventoryBag.SameDetailItem(equipment,refreshed),"changed options accepted as same item");
        equipment.itemOption=new[]{new ItemOption{optionTemplate=new ItemOptionTemplate{id=0},param=5}};
        Check(InventoryBag.SameDetailItem(equipment,refreshed),"new option objects for same stats rejected");
        refreshed.itemOption[0].param=6;Check(!InventoryBag.SameDetailItem(equipment,refreshed),"changed stat accepted as same item");
        Item consumable=new Item{template=new ItemTemplate{id=13,type=6}},skin=new Item{template=new ItemTemplate{id=200,type=5}};
        Item[] bag=new[]{null,equipment,material,consumable,skin}; var map=new List<int>();
        InventoryBag.Map(bag,InventoryBagFilter.All,map);Check(map.Count==5&&map[1]==1,"all slots must retain server indices");
        InventoryBag.Map(bag,InventoryBagFilter.Equipment,map);Check(map.Count==2&&map[0]==1&&map[1]==4,"equipment filter remapped packet indices");
        InventoryBag.Map(bag,InventoryBagFilter.Material,map);Check(map.Count==1&&map[0]==2,"material filter index");
        InventoryBag.Map(bag,InventoryBagFilter.Consumable,map);Check(map.Count==1&&map[0]==3,"consumable filter index");
        Check(InventoryBag.HasLevel(equipment)&&!InventoryBag.HasLevel(skin)&&!InventoryBag.HasLevel(material),"level badge type gate");
        InventoryBagDoubleTap tap=new InventoryBagDoubleTap();
        Check(!tap.Tap(1,equipment,100)&&tap.Tap(1,equipment,200)&&!tap.Tap(1,equipment,250),"double tap must fire once per pair");
        tap.Reset();Check(!tap.Tap(2,material,100)&&!tap.Tap(2,material,200),"double tap used material");
        tap.Reset();Check(!tap.Tap(3,consumable,100)&&!tap.Tap(3,consumable,200),"double tap used consumable");
        tap.Reset();Check(!tap.Tap(1,equipment,100)&&!tap.Tap(1,new Item{template=equipment.template},200),"server replacement must cancel double equip");
        tap.Reset();tap.Tap(1,equipment,100);Check(!tap.Tap(1,equipment,500),"slow second click equipped");
        tap.Reset();tap.Tap(1,equipment,100);tap.Reset();Check(!tap.Tap(1,equipment,200),"drag/filter cancellation equipped");
        Item earring=new Item{template=new ItemTemplate{id=454,type=27}};
        Check(InventoryBag.CanEquip(earring)&&!InventoryBag.HasLevel(earring),"known earring equip/level classification");
        tap.Reset();Check(!tap.Tap(4,earring,100)&&tap.Tap(4,earring,200),"known earring should dispatch equip, never consumable use");
        Check(InventoryBag.Category(new Item{template=new ItemTemplate{id=933,type=27}})==InventoryBagFilter.Material,"earring fragment misclassified as consumable");
        foreach(int capacity in new[]{35,40,55,75,80,100}){
            Item[] legacy=new Item[capacity];
            for(int i=0;i<capacity;i++)legacy[i]=new Item{template=new ItemTemplate{id=(short)i,type=0}};
            var slots=new System.Collections.Generic.List<int>();
            InventoryBag.Map(legacy,InventoryBagFilter.All,slots);
            Check(slots.Count==capacity&&slots[capacity-1]==capacity-1,"full legacy bag lost an occupied slot");
        }
        Check(InventoryBag.DisplaySlots(3,InventoryBagFilter.Equipment)==35,"filtered padding presented as locked capacity");
        var lockedGraphics=new mGraphics();
        Panel.PaintClosedForTest(lockedGraphics,new Rect(0,0,30,30));
        Check(string.Join(" ",mFont.tahoma_7_grey.drawn)=="Chưa mở","closed slot label missing");
        mFont.tahoma_7_grey.drawn.Clear();
        Panel.PaintClosedForTest(lockedGraphics,new Rect(0,0,12,12));
        Check(mFont.tahoma_7_grey.drawn.Count==0&&lockedGraphics.lockOutlines==1,"small closed slot lacks lock icon");
        Console.WriteLine("PASS: "+checks+" inventory registry/metadata checks; no Unity build");
    }
}
'''
fixture = fixture.replace("CURRENCY_ASSETS", ",".join("new int[]{" + ",".join(map(str, asset)) + "}" for asset in currency_assets))
output = (ROOT / "Build/inventory-tests").resolve()
assert output.is_relative_to(ROOT.resolve())
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="inventory-", dir=output) as folder:
    path = Path(folder)
    (path / "InventoryRegistry.cs").write_text(source[:source.index("public partial class Panel")], encoding="utf-8")
    tab_members = source[source.index("    internal int[] InventoryFrameTabIds"):source.index("    private static readonly int[] inventoryBodySlots")]
    (path / "PanelTabs.cs").write_text("using System; partial class Panel {\n" + tab_members + "\n}", encoding="utf-8")
    (path / "PanelLocked.cs").write_text("using UnityEngine; using Hud; partial class Panel {\n" + locked_painter
        + "\npublic static void PaintClosedForTest(mGraphics g,Rect cell){PaintClosedBagSlot(g,cell);}\n}", encoding="utf-8")
    disciple = (ROOT / "Assets/Scripts/Hud/DisciplePanel.cs").read_text(encoding="utf-8-sig")
    (path / "DiscipleLayout.cs").write_text(disciple[:disciple.index("public partial class Panel")], encoding="utf-8")
    (path / "InventoryTests.cs").write_text(fixture, encoding="utf-8")
    item_source = (ROOT / "Assets/Scripts/Item.cs").read_text(encoding="utf-8-sig")
    item_body = item_source[item_source.index("    public bool isTypeBody()"):item_source.index("    public string getLockstring()")]
    (path / "ItemBody.cs").write_text("partial class Item {\n" + item_body + "\n}", encoding="utf-8")
    exe = path / "InventoryTests.exe"
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", "/target:exe", f"/out:{exe}",
                    str(path / "InventoryRegistry.cs"), str(path / "DiscipleLayout.cs"), str(path / "InventoryTests.cs"), str(path / "PanelTabs.cs"), str(path / "PanelLocked.cs"),
                    str(path / "ItemBody.cs"), str(ROOT / "Assets/Scripts/Hud/InventoryBag.cs"),
                    str(ROOT / "Assets/Scripts/Hud/HudLayout.cs")], check=True)
    subprocess.run([str(exe)], check=True)
