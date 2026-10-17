"""Run actual shortcut/quick-item/Panel use routing without Unity or networking."""
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]


def block(source, start):
    begin = source.index("{", start)
    depth = 0
    for end in range(begin, len(source)):
        depth += (source[end] == "{") - (source[end] == "}")
        if depth == 0:
            return source[start:end + 1]
    raise ValueError(start)


def method(source, name):
    return block(source, re.search(r"^[ \t]*(?:public|private|internal) [^\n]*\b" + name + r"\(", source, re.M).start())


main = (ROOT / "Assets/Scripts/Mod.CuongLe/MainMod.cs").read_text(encoding="utf-8-sig")
quick = (ROOT / "Assets/Scripts/Hud/MobileQuickItems.cs").read_text(encoding="utf-8-sig")
panel = (ROOT / "Assets/Scripts/Panel.cs").read_text(encoding="utf-8-sig")
fixture = r'''
using System; using System.Collections.Generic; using Hud;
namespace UnityEngine { struct Rect { public float x,y,width,height; public float xMax=>x+width; public float yMax=>y+height;
 public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;} } }
class ItemTemplate { public short id; public sbyte type; }
partial class Item { public ItemTemplate template; public int indexUI,quantity=1; }
class Char { static Char p=new Char(); public static Char myCharz()=>p;
 public Item[] arrItemBag=new Item[30],arrItemBody=new Item[11]; }
class Service { static Service s=new Service(); public static Service gI()=>s; public int calls,where,index;
 public void useItem(sbyte type,sbyte where,sbyte index,short id){calls++;this.where=where;this.index=index;} }
class mSystem { public static long currentTimeMillis()=>50000; }
class GameScr { public static GameScr instance; public void MarkMobilePotionUse(long now){} }
class GameCanvas { public static Panel panel=new Panel(); }
class Panel {
 public bool isnewInventory; public int selected,newSelected; public Item currItem;
 public void hide(){} int GetInventorySelect_bag(int s,int n,Item[] body)=>s-body.Length;
 int GetInventorySelect_body(int s,int n)=>s; bool GetInventorySelect_isbody(int s,int n,Item[] body)=>s<body.Length;
 public void Use(int idAction,object p){ @@PANEL@@ }
}
class Res { public static void outz(string s){} }
class MainMod { @@MAIN@@ }
namespace Hud { class MobileQuickItems {
 public static int[] templates={454,921,454}; static readonly Dictionary<int,long> readyAt=new Dictionary<int,long>();
 static readonly List<int> expired=new List<int>(); const int CooldownMillis=10000;
 static void Load(){} static int Template(int slot)=>templates[slot]; static long RemainingAt(Item i,long now)=>0;
 static int CooldownGroup(Item i)=>i.template.id;
 @@QUICK@@
} }
class PorataTests {
 static int checks; static void Check(bool b,string s){checks++;if(!b)throw new Exception(s);}
 static Item Item(int id,int index)=>new Item{template=new ItemTemplate{id=(short)id,type=27},indexUI=index};
 static void Sent(int where,int index,string text){var s=Service.gI();Check(s.calls==1&&s.where==where&&s.index==index,text);s.calls=0;}
 static void Main(){
  var c=Char.myCharz();var s=Service.gI();var porata=Item(454,7);c.arrItemBody[7]=porata;
  Check(InventoryBag.EquippedPorataIndex(null)==-1&&InventoryBag.EquippedPorataIndex(new Item[7])==-1,"startup short body");
  Check(InventoryBag.EquippedPorataIndex(c.arrItemBody)==7,"equipped Porata missing");
  MainMod.UseItem(454);Sent(0,7,"shortcut must use body7 once");
  c.arrItemBag[2]=Item(454,2);MainMod.UseItem(454);Sent(0,7,"shortcut preferred duplicate bag Porata");
  Check(ReferenceEquals(MobileQuickItems.Get(0),porata),"quick icon ignored body item");
  MobileQuickItems.Use(0,454);Sent(0,7,"quick item did not use body7 once");
  c.arrItemBag[3]=Item(921,3);MobileQuickItems.Use(1,921);Sent(1,3,"higher tier bag route changed");
  MainMod.UseItem(921);Sent(1,3,"higher tier shortcut changed");
  MobileQuickItems.templates[0]=921;MobileQuickItems.Use(0,454);Check(s.calls==0,"stale quick assignment used item");
  MobileQuickItems.templates[0]=454;c.arrItemBody[7]=null;
  MobileQuickItems.Use(0,454);Sent(1,2,"quick item did not resolve move back to bag");
  MainMod.UseItem(454);Sent(1,2,"shortcut bag fallback changed");
  c.arrItemBag[2]=null;MainMod.UseItem(454);MobileQuickItems.Use(0,454);
  Check(s.calls==0&&MobileQuickItems.Get(0)==null,"missing Porata dispatched packet");
  c.arrItemBody[7]=Item(921,7);Check(InventoryBag.EquippedPorataIndex(c.arrItemBody)==-1,"higher tier treated as base Porata");
  c.arrItemBody[7]=porata;var p=GameCanvas.panel;p.selected=7;p.Use(2001,porata);Sent(0,7,"Panel use body route changed");
  p.selected=c.arrItemBody.Length+3;p.Use(2001,c.arrItemBag[3]);Sent(1,3,"Panel use bag route changed");
  Console.WriteLine("PASS: "+checks+" production Porata shortcut/quick-item/Panel packet checks; no Unity/network");
 }
}
'''
fixture = fixture.replace("@@MAIN@@", method(main, "UseItem"))
fixture = fixture.replace("@@QUICK@@", "\n".join(method(quick, name) for name in ("BagIndex", "Get", "Use")))
fixture = fixture.replace("@@PANEL@@", block(panel, panel.index("        if (idAction == 2001)")))
item = (ROOT / "Assets/Scripts/Item.cs").read_text(encoding="utf-8-sig")
fixture += "\npartial class Item {" + method(item, "isTypeBody") + "}\n"
assert "MainMod.UseItem(454);" in (ROOT / "Assets/Scripts/Mod.CuongLe/Yardat.cs").read_text(encoding="utf-8-sig")
assert 'if (currItem.template.id == 454)' in panel
output = ROOT / "Build/porata-tests"
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="test-", dir=output) as folder:
    path = Path(folder)
    source = path / "PorataTests.cs"
    source.write_text(fixture, encoding="utf-8")
    exe = path / "PorataTests.exe"
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", "/warn:0", f"/out:{exe}",
                    str(source), str(ROOT / "Assets/Scripts/Hud/InventoryBag.cs")], check=True)
    subprocess.run([str(exe)], check=True)
