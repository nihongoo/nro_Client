# Exercise production action selection and names without Unity, files or build output.
$ErrorActionPreference = 'Stop'
$inventoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
function ExtractMethod([string]$source, [string]$marker) {
    $start = $source.IndexOf($marker)
    if ($start -lt 0) { throw "Missing method: $marker" }
    $brace = $source.IndexOf('{', $start)
    $depth = 1
    $end = $brace + 1
    while ($depth -gt 0) {
        if ($source[$end] -eq '{') { $depth++ }
        elseif ($source[$end] -eq '}') { $depth-- }
        $end++
    }
    return $source.Substring($start, $end - $start)
}
$details = Get-Content -LiteralPath (Join-Path $inventoryRoot 'Assets/Scripts/Hud/InventoryDetailPanel.cs') -Raw
$disciple = Get-Content -LiteralPath (Join-Path $inventoryRoot 'Assets/Scripts/Hud/DisciplePanel.cs') -Raw
$itemSource = Get-Content -LiteralPath (Join-Path $inventoryRoot 'Assets/Scripts/Item.cs') -Raw
$bag = Get-Content -LiteralPath (Join-Path $inventoryRoot 'Assets/Scripts/Hud/InventoryBag.cs') -Raw
$hasLevel = [regex]::Match($bag, 'internal static bool HasLevel\(Item item\)[\s\S]+?;').Value
$fused = [regex]::Match($disciple, 'internal bool DiscipleFused[^\r\n]+').Value
$locked = [regex]::Match($disciple, 'internal bool DiscipleEquipmentLocked[^\r\n]+').Value
$fixture = @'
using System;
using System.Collections.Generic;
public interface IActionListener {}
public class Command {public IActionListener actionListener;public int idAction;public Command(string s,IActionListener p,int id,object o){actionListener=p;idAction=id;}}
public class MyVector {public int size()=>0;public object elementAt(int i)=>null;}
public class AutoItem:IActionListener{} public class AutoVutDo:IActionListener{}
public class MobileQuickItems:IActionListener {public static int Template(int i)=>-1;}
public enum InventoryBagFilter{Equipment,Consumable}
public static class InventoryBag {public static bool CanEquip(Item i)=>false;public static InventoryBagFilter Category(Item i)=>InventoryBagFilter.Equipment;__HASLEVEL__}
public class Template{public int type,gender=3,id;public long strRequire;public string name="Ngọc thử",description="Mô tả ngọc";}
public class mResources{public const string pow_request="Sức mạnh";}
public class OptionTemplate{public int id=72;public string name="Cấp #";}
public class ItemOption{public OptionTemplate optionTemplate=new OptionTemplate();public int param=7;public string getOptiongColor()=>"Cấp 7";public string getOptionString()=>"Cấp 7";}
public class Item{public const int TYPE_DISCIPLE_GEM=80;public Template template=new Template{type=80};public ItemOption[] itemOption=new[]{new ItemOption()};__FULLNAME__}
public class Char {static Char own=new Char(),pet=new Char();public static Char myCharz()=>own;public static Char myPetz()=>pet;public bool havePet=true;public int petStatus,cgender;public Item[] arrItemBody=new Item[8];}
public class HudLayout{public static void Invalidate(){}}
public class InventoryDetailButton{public string Text,HitTarget;public Command Action;public int Group,Position;}
public class Panel:IActionListener{
 List<InventoryDetailButton> inventoryDetailButtons=new List<InventoryDetailButton>();
 bool inventoryDetailQuick,inventoryDetailDiscard,inventoryDetailPet;
 MyVector inventoryDetailCommands;Item inventoryBagDetailItem=new Item();int selected=8;
 int inventoryDetailTopScroll,inventoryDetailBottomScroll;bool discipleLoaded=true;
 int discipleKind=0;long discipleFusionUntil;Char discipleOwner=Char.myCharz();bool DisciplePanelActive;
 void doFireInventory(bool ignored){throw new Exception("Gem entered player-use/auto command builder");}
 void PrepareInventoryBagDetail(){}
 __FUSED__
 __LOCKED__
 __SLOT__
 __CANEQUIP__
 __ADDBUTTON__
 __BUILD__
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static void Run(){
  int checks=0;
  foreach(int mode in new[]{0,-1,300000}){
   Panel p=new Panel();Char.myCharz().havePet=true;Char.myPetz().petStatus=mode==0?0:4;p.discipleFusionUntil=mode;
   p.BuildInventoryDetail();
   Check(p.inventoryDetailButtons.Count==(mode>0?1:2),"Gem bag action count");
   Check(p.inventoryDetailButtons.Exists(b=>b.Text=="Bỏ"),"Gem bag must allow discard");
   Check(p.inventoryDetailButtons.Exists(b=>b.Text=="Mặc cho đệ tử")== (mode<=0),"Fusion equip lock mismatch");
   Check(!p.inventoryDetailQuick&&!p.inventoryDetailDiscard,"Gem exposes Auto/quick assignment");
   p.DisciplePanelActive=true;p.selected=7;p.BuildInventoryDetail();
   Check(p.inventoryDetailButtons.Count==(mode>0?0:1),"Gem equipped action count");
   Check(p.inventoryDetailButtons.TrueForAll(b=>b.Text=="Tháo"),"Gem equipped exposes wrong inventory action");
   Check(!InventoryBag.HasLevel(p.inventoryBagDetailItem),"Gem has inventory level");
   Check(p.inventoryBagDetailItem.getFullName()=="Ngọc thử\nMô tả ngọc","Gem exposes stale Lv option or loses description");
   checks+=8;
  }
  Panel noPet=new Panel();Char.myCharz().havePet=false;noPet.BuildInventoryDetail();
  Check(noPet.inventoryDetailButtons.Count==1&&noPet.inventoryDetailButtons[0].Text=="Bỏ","No-pet gem action contract");
  Console.WriteLine("PASS: "+(checks+1)+" disciple gem action/fusion/level checks; no Unity/build/files");
 }
}
'@
$fixture = $fixture.Replace('__HASLEVEL__', $hasLevel).Replace('__FUSED__', $fused).Replace('__LOCKED__', $locked)
$fixture = $fixture.Replace('__FULLNAME__', (ExtractMethod $itemSource 'public string getFullName()'))
$fixture = $fixture.Replace('__SLOT__', (ExtractMethod $disciple 'private static int DiscipleSlot('))
$fixture = $fixture.Replace('__CANEQUIP__', (ExtractMethod $disciple 'private bool CanEquipForDisciple('))
$fixture = $fixture.Replace('__ADDBUTTON__', (ExtractMethod $details 'private void AddInventoryDetailButton('))
$fixture = $fixture.Replace('__BUILD__', (ExtractMethod $details 'private void BuildInventoryDetail()'))
Add-Type -TypeDefinition $fixture -IgnoreWarnings -WarningAction SilentlyContinue
[Panel]::Run()
