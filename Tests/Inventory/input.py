"""Exercise production Panel input methods without Unity or a network session."""
import ast
import re
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
frame = (ROOT / "Assets/Scripts/Hud/InventoryFrame.cs").read_text(encoding="utf-8-sig")
bag = (ROOT / "Assets/Scripts/Hud/InventoryBagPanel.cs").read_text(encoding="utf-8-sig")
tree = ast.parse((ROOT / "Tests/Inventory/run.py").read_text(encoding="utf-8-sig"))
fixture = next(ast.literal_eval(n.value) for n in tree.body if isinstance(n, ast.Assign)
               and any(isinstance(t, ast.Name) and t.id == "fixture" for t in n.targets)
               and isinstance(n.value, ast.Constant))
fixture = fixture[:fixture.index("partial class Panel")] + fixture[fixture.index("class Menu"):fixture.index("class InventoryTests")]
fixture = fixture.replace("static class GameCanvas", "static partial class GameCanvas")
fixture = fixture.replace("public bool showMenu;", "public bool showMenu; public int menuY;")
fixture = fixture.replace("public int statusMe;", "public int statusMe; public Item[] arrItemBody=new Item[8],arrItemBag=new Item[80];")
fixture = fixture.replace("public static mFont tahoma_7_yellow=new mFont();", "public static mFont tahoma_7_yellow=new mFont(),tahoma_7_red=new mFont(); public string[] splitFontArray(string s,int width)=>s.Split('\\n');")
fixture = fixture.replace("public static int Max(int a,int b)=>Math.Max(a,b);", "public static int Max(int a,int b)=>Math.Max(a,b); public static int Clamp(int n,int lo,int hi)=>Math.Max(lo,Math.Min(n,hi));")

def method(source, name):
    start = re.search(r"    private [^\n]*\b" + name + r"\(", source).start()
    begin = source.index("{", start)
    arrow = source.find("=>", start, begin)
    if arrow >= 0:
        return source[start:source.index(";", arrow)+1]
    depth = 0
    for end in range(begin, len(source)):
        depth += (source[end] == "{") - (source[end] == "}")
        if depth == 0:
            return source[start:end+1]
    raise ValueError(name)

members = frame[frame.index("    internal bool InventoryFrameActive"):frame.index("    private void OpenInventoryAppearanceSettings")]
members += bag[bag.index("    private readonly List<int>"):bag.index("    private void SyncInventoryBag")]
for name in ("SyncInventoryFrame", "SelectInventoryFrameTab", "UpdateInventoryFrameInput", "CloseInventoryFrame"):
    members += method(frame, name)
for name in ("SyncInventoryBag", "CloseInventoryBagDetail", "IsInventoryBagDragTarget", "HandleInventoryBagClick",
             "PrepareInventoryBagDetail", "ScrollInventoryBagIntoView", "MoveInventoryBagSelection",
             "UpdateInventoryBagDetailDrag", "ScrollInventoryBagMouse"):
    members += method(bag, name)

fixture += r'''
static class Main { public static bool isPC=true; }
static class InfoDlg { public static bool isShow; }
static class mSystem { public static long now; public static long currentTimeMillis()=>now; }
static class mResources { public static string can_not_do_when_die="dead"; }
class Command { public int x,y,w,h; }
public class Field { public bool isShow; }
class ChatPopup { public string[] says; public int lim; public static int cmyText; }
static partial class GameCanvas {
 public static bool[] keyPressed=new bool[30]; public static int px,py,pxMouse,pyMouse;
 public static bool isPointerJustDown,isPointerJustRelease,isPointerClick,isPointerDown;
 public static void clearAllPointerEvent(){isPointerJustDown=isPointerJustRelease=isPointerClick=isPointerDown=false;}
 public static void startOKDlg(string text){currentDialog=new object();}
}
public partial class Panel {
 public int type,currentTabIndex=1,selected=-1,X,Y,W,H,cmx,cmtoX,xScroll,yScroll,wScroll,hScroll,ITEM_HEIGHT,currentListLength,cmyLim,cmy,cmtoY,newSelected,size_tab,waitToPerform;
 public bool isShow=true,isClose,isnewInventory; public Field chatTField,tabIcon;
 public string[][] currentTabName=new string[7][]; public int[] lastTabIndex=new int[30];
 private Command cmdClose=new Command(); private ChatPopup cp; private Item currItem;
 public int actions,lastAction,lastIndex=-1,menus;
 private void setTypeOption(){type=19;} private void hide(){isShow=false;isClose=true;}
 private void setTabTask(){} private void setTabInventory(bool reset){selected=-1;cmy=cmtoY=0;}
 private void setTabSkill(){} private void setTabClans(){} private void setTabMod(){} private void setTabTool(){}
 private void doFireInventory(){menus++;GameCanvas.menu.showMenu=true;addItemDetail(currItem);}
 private void addItemDetail(Item item){cp=new ChatPopup{says=new[]{"item"}};}
 private void perform(int action,object item){actions++;lastAction=action;lastIndex=selected-Char.myCharz().arrItemBody.Length;}
 public void Tick(){HudLayout.BeginFrame();UpdateInventoryFrameInput();}
 public void ScrollTo(int visual){SyncInventoryFrame();ScrollInventoryBagIntoView(InventoryFrame.BagCellBounds(visual));}
 public void ScrollToFilter(int filter){SyncInventoryFrame();ScrollInventoryBagIntoView(InventoryFrame.FilterBounds(filter));}
 public void Wheel(int amount){ScrollInventoryBagMouse(amount);}
 public void Move(int direction){MoveInventoryBagSelection(direction);}
 public bool Preview=>InventoryBagDetailOpen;
}
class InputTests {
 static int checks;
 static void Check(bool value,string text){checks++;if(!value)throw new Exception(text);}
 static void Click(Rect rect,long time){
  GameCanvas.px=(int)rect.center.x;GameCanvas.py=(int)rect.center.y;
  GameCanvas.isPointerJustDown=GameCanvas.isPointerDown=GameCanvas.isPointerClick=true;GameCanvas.isPointerJustRelease=false;
  mSystem.now=time;GameCanvas.panel.Tick();
  GameCanvas.isPointerJustDown=GameCanvas.isPointerDown=false;GameCanvas.isPointerJustRelease=GameCanvas.isPointerClick=true;
  GameCanvas.panel.Tick();GameCanvas.clearAllPointerEvent();
 }
 static void Run(bool mobile,int w,int h,int zoom){
  global::Main.isPC=!mobile;UnityEngine.Device.Application.platform=mobile?RuntimePlatform.Android:RuntimePlatform.WindowsPlayer;
  GameCanvas.panel=new Panel();GameCanvas.currentDialog=null;GameCanvas.menu.showMenu=false;GameCanvas.clearAllPointerEvent();
  Panel p=GameCanvas.panel;Char ch=Char.myCharz();ch.arrItemBag=new Item[80];ch.statusMe=0;
  GameCanvas.w=w;GameCanvas.h=h;mGraphics.zoomLevel=zoom;
  Item equipment=new Item{template=new ItemTemplate{id=1,type=0}},material=new Item{template=new ItemTemplate{id=442,type=30},quantity=758};
  Item consumable=new Item{template=new ItemTemplate{id=13,type=6},quantity=2};
  ch.arrItemBag[37]=equipment;ch.arrItemBag[8]=material;ch.arrItemBag[24]=consumable;
  p.Tick();p.ScrollToFilter(1);Click(HudLayout.GetRect(InventoryFrame.Filter(1)),100);
  p.ScrollTo(0);Click(HudLayout.GetRect(InventoryFrame.BagCell(0)),1000);
  Check(p.selected==45&&p.Preview&&p.actions==0&&p.menus==0,"first click must preview original server index 37");
  Click(HudLayout.GetRect(InventoryFrame.BagCell(0)),1100);
  Check(p.actions==1&&p.lastAction==2000&&p.lastIndex==37&&p.menus==0,"filtered double tap dispatched wrong item/action");
  Click(HudLayout.GetRect(InventoryFrame.DetailClose),1500);
  Check(!p.Preview&&p.selected==45,"detail X must keep selection");
  p.ScrollToFilter(3);Click(HudLayout.GetRect(InventoryFrame.Filter(3)),2000);p.ScrollTo(0);
  Click(HudLayout.GetRect(InventoryFrame.BagCell(0)),2200);Click(HudLayout.GetRect(InventoryFrame.BagCell(0)),2300);
  Check(p.actions==1&&p.selected==16&&p.Preview,"material double tap must not send item use");
  p.ScrollToFilter(2);Click(HudLayout.GetRect(InventoryFrame.Filter(2)),2600);p.ScrollTo(0);
  Click(HudLayout.GetRect(InventoryFrame.BagCell(0)),2800);Click(HudLayout.GetRect(InventoryFrame.BagCell(0)),2900);
  Check(p.actions==1&&p.selected==32,"consumable double tap must not send item use");
  Click(HudLayout.GetRect(InventoryFrame.DetailClose),3100);
  p.ScrollToFilter(1);Click(HudLayout.GetRect(InventoryFrame.Filter(1)),3400);p.ScrollTo(0);
  Rect cell=HudLayout.GetRect(InventoryFrame.BagCell(0));int before=p.cmy;
  GameCanvas.px=(int)cell.center.x;GameCanvas.py=(int)cell.center.y;GameCanvas.isPointerJustDown=GameCanvas.isPointerDown=GameCanvas.isPointerClick=true;p.Tick();
  GameCanvas.isPointerJustDown=false;GameCanvas.py-=12;p.Tick();
  GameCanvas.isPointerDown=false;GameCanvas.isPointerJustRelease=GameCanvas.isPointerClick=true;p.Tick();GameCanvas.clearAllPointerEvent();
  Check(p.actions==1&&!p.Preview&&p.cmy!=before,"drag selected/equipped an item or did not scroll");
  p.ScrollTo(0);Click(HudLayout.GetRect(InventoryFrame.BagCell(0)),3800);
  ch.arrItemBag[37]=new Item{template=equipment.template};p.Tick();
  Check(!p.Preview,"server item replacement left stale detail actions");
  Click(HudLayout.GetRect(InventoryFrame.BagCell(0)),3900);
  Check(p.actions==1,"server replacement retained old double tap");
  Click(HudLayout.GetRect(InventoryFrame.DetailClose),4200);
  p.ScrollTo(34);int old=p.selected;Click(InventoryFrame.BagCellBounds(34),4400);
  Check(p.selected==old&&p.actions==1,"ghost changed selection or dispatched an action");
  p.cmy=p.cmtoY=0;HudLayout.Invalidate();GameCanvas.pxMouse=10;GameCanvas.pyMouse=100;p.Wheel(-2);
  Check(p.cmy>0,"existing mouse wheel did not scroll inventory");
  p.Move(1);Check(p.selected>=0&&p.selected<ch.arrItemBody.Length||p.selected==45,"keyboard selected a filtered-out item");
  p.ScrollTo(0);ch.statusMe=14;
  Click(HudLayout.GetRect(InventoryFrame.BagCell(0)),5000);Click(HudLayout.GetRect(InventoryFrame.BagCell(0)),5100);
  Check(p.actions==1&&GameCanvas.currentDialog!=null,"dead player double tap dispatched equip");
  ch.statusMe=0;GameCanvas.currentDialog=null;
  Rect corner=InventoryFrame.BagCellBounds(0);Click(new Rect(corner.xMax+1,corner.yMax+1,0,0),6000);
  Check(p.selected==45&&p.Preview,"overlaid badge outside cell frame is not clickable");
  GameCanvas.keyPressed[global::Main.isPC?22:8]=true;p.Tick();
  Check(!GameCanvas.keyPressed[global::Main.isPC?22:8]&&!p.Preview,"existing navigation key was not consumed");
  p.selected=45;GameCanvas.keyPressed[global::Main.isPC?25:5]=true;p.Tick();
  Check(p.menus==1&&p.actions==1,"existing confirm key must retain legacy menu dispatch");
 }
 static void Main(){
  Run(false,640,360,2);Run(false,1280,720,4);Run(true,422,195,2);Run(true,844,390,4);
  Console.WriteLine("PASS: "+checks+" production Panel input/dispatch checks; no Unity/network");
 }
}
'''
output = ROOT / "Build/inventory-input-tests"
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="input-", dir=output) as folder:
    path = Path(folder)
    (path / "Fixture.cs").write_text(fixture, encoding="utf-8")
    (path / "Registry.cs").write_text(frame[:frame.index("public partial class Panel")], encoding="utf-8")
    (path / "PanelInput.cs").write_text("using System; using System.Collections.Generic; using Hud; using UnityEngine; public partial class Panel {\n" + members + "\n}", encoding="utf-8")
    item = (ROOT / "Assets/Scripts/Item.cs").read_text(encoding="utf-8-sig")
    (path / "ItemBody.cs").write_text("partial class Item {" + item[item.index("    public bool isTypeBody()"):item.index("    public string getLockstring()")] + "}", encoding="utf-8")
    exe = path / "InputTests.exe"
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", "/main:InputTests", f"/out:{exe}",
                    *map(str, path.glob("*.cs")), str(ROOT / "Assets/Scripts/Hud/HudLayout.cs"),
                    str(ROOT / "Assets/Scripts/Hud/InventoryBag.cs")], check=True)
    subprocess.run([str(exe)], check=True)
