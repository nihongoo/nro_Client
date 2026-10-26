"""Test production button eligibility/transfer commands without Unity or a game session."""
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
disciple = (ROOT / "Assets/Scripts/Hud/DisciplePanel.cs").read_text(encoding="utf-8-sig")
detail = (ROOT / "Assets/Scripts/Hud/InventoryDetailPanel.cs").read_text(encoding="utf-8-sig")


def block(source, marker):
    start = source.index(marker)
    brace = source.index("{", start)
    depth, end = 1, brace + 1
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start:end]


methods = "\n".join(block(disciple, marker) for marker in (
    "private static int DiscipleSlot(", "private bool CanEquipForDisciple(", "private void EquipForDisciple("))
methods += "\n" + block(detail, "private void BuildInventoryDetail(")
fixture = r'''
using System;
using System.Collections.Generic;
class ItemTemplate { public int type,gender,id; }
class Item { public ItemTemplate template; }
class Char {
 public static Char owner=new Char(),pet=new Char();
 public bool havePet=true;public int cgender,petStatus;
 public Item[] arrItemBag=new Item[2],arrItemBody=new Item[8];
 public static Char myCharz()=>owner;public static Char myPetz()=>pet;
}
enum InventoryBagFilter { Equipment,Consumable }
static class InventoryBag {
 public static bool CanEquip(Item i)=>i.template.type>=0&&i.template.type<=5||i.template.type==32;
 public static InventoryBagFilter Category(Item i)=>CanEquip(i)?InventoryBagFilter.Equipment:InventoryBagFilter.Consumable;
}
class MyVector:List<object>{public int size()=>Count;public object elementAt(int i)=>this[i];public void addElement(object o)=>Add(o);}
class Command {
 public int idAction;public object actionListener;
 public Command(string s,object owner,int action,object item){idAction=action;actionListener=owner;}
}
class AutoItem{} class AutoVutDo{} class MobileQuickItems { public static int Template(int slot)=>-1; }
static class HudLayout { public static void Invalidate(){} }
class Service {
 public static Service instance=new Service();public List<int> slots=new List<int>();public int type;
 public static Service gI()=>instance;public void getItem(int command,sbyte slot){type=command;slots.Add(slot);}
}
class Panel {
 const int DiscipleRuneType=80,BAG_PET=6;
 bool discipleLoaded=true;int discipleKind;Char discipleOwner=Char.myCharz();
 bool DiscipleFused=>Char.myPetz().petStatus>=4;
 bool DisciplePanelActive,inventoryDetailQuick,inventoryDetailDiscard,inventoryDetailPet;
 int selected=8,inventoryDetailTopScroll,inventoryDetailBottomScroll;
 MyVector inventoryDetailCommands;Item inventoryBagDetailItem;
 class InventoryDetailButton { public string Text;public Command Action;public int Group,Position; }
 List<InventoryDetailButton> inventoryDetailButtons=new List<InventoryDetailButton>();
 void AddInventoryDetailButton(string text,Command action,int group,int pos){inventoryDetailButtons.Add(new InventoryDetailButton{Text=text,Action=action,Group=group,Position=pos});}
 void PrepareInventoryBagDetail(){} void CloseInventoryBagDetail(){closed=true;} bool closed;
 void doFireInventory(bool build){
  inventoryDetailCommands=new MyVector();
  inventoryDetailCommands.addElement(new Command("use",this,2001,inventoryBagDetailItem));
  inventoryDetailCommands.addElement(new Command("discard",this,2003,inventoryBagDetailItem));
 }
 __METHODS__
 static int checks;
 static void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
 static Item Item(int type,int gender=3)=>new Item{template=new ItemTemplate{type=type,gender=gender}};
 bool Has(string label)=>inventoryDetailButtons.Exists(b=>b.Text==label);
 public static void Main(){
  Panel p=new Panel();
  for(int kind=0;kind<=5;kind++)for(int gender=0;gender<3;gender++)for(int itemGender=0;itemGender<=3;itemGender++)for(int type=0;type<128;type++){
   p.discipleKind=kind;Char.pet.cgender=gender;Item item=Item(type,itemGender);
   bool allowed=type>=0&&type<=5||type==32||type==80;
   Check(p.CanEquipForDisciple(item)==(allowed&&(kind>0||itemGender==3||gender==itemGender)),"type/planet eligibility");
  }
  p.discipleKind=0;p.inventoryBagDetailItem=Item(0);Char.owner.arrItemBag[0]=p.inventoryBagDetailItem;
  foreach(int status in new[]{0,1,2,3,4,5}){
   Char.pet.petStatus=status;p.BuildInventoryDetail();
   Check(p.Has("Mặc cho đệ tử")== (status<4),"hide fused pet button");
   Check(p.Has("Mặc"),"master wear must remain available while fused");
  }
  Char.pet.petStatus=0;p.inventoryBagDetailItem=Item(80);p.BuildInventoryDetail();
  Check(p.inventoryDetailButtons.Count==2&&p.Has("Mặc cho đệ tử")&&p.Has("Bỏ"),"rune exposes extra actions");
  Char.pet.petStatus=4;p.BuildInventoryDetail();Check(p.inventoryDetailButtons.Count==1&&p.Has("Bỏ"),"fused rune actions");
  Char.pet.petStatus=0;Char.owner.havePet=false;p.BuildInventoryDetail();Check(!p.Has("Mặc cho đệ tử"),"absent pet button");
  Char.owner.havePet=true;p.discipleLoaded=false;p.BuildInventoryDetail();Check(!p.Has("Mặc cho đệ tử"),"unknown pet metadata button");
  p.discipleLoaded=true;p.DisciplePanelActive=true;p.BuildInventoryDetail();Check(p.inventoryDetailButtons.Count==1&&p.Has("Tháo"),"pet worn item actions");
  Char.pet.petStatus=4;p.BuildInventoryDetail();Check(p.inventoryDetailButtons.Count==0,"fused remove must be hidden");
  Char.pet.petStatus=0;p.DisciplePanelActive=false;
  Item equip=Item(2);Char.owner.arrItemBag[0]=equip;p.EquipForDisciple(equip);
  Check(Service.instance.type==6&&Service.instance.slots.Count==1&&Service.instance.slots[0]==0&&p.closed,"bag transfer/close");
  p.EquipForDisciple(Item(2));Check(Service.instance.slots.Count==1,"stale item must not equip replacement");
  Char.pet.petStatus=4;p.EquipForDisciple(equip);Check(Service.instance.slots.Count==1,"fused click race");
  Console.WriteLine("PASS: "+checks+" disciple client eligibility/action checks; no Unity build");
 }
}
'''.replace("__METHODS__", methods)
out = ROOT / "Build/disciple-tests"
out.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(dir=out) as folder:
    folder = Path(folder)
    src, exe = folder / "DiscipleTests.cs", folder / "DiscipleTests.exe"
    src.write_text(fixture, encoding="utf-8")
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", "/target:exe",
                    f"/out:{exe}", str(src)], check=True)
    subprocess.run([str(exe)], check=True)
