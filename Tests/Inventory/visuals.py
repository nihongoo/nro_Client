"""Verify production aura pixels and star/Lv geometry without Unity."""
from pathlib import Path
import subprocess
import tempfile
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
source = (ROOT / "Assets/Scripts/Hud/InventoryItemVisuals.cs").read_text(encoding="utf-8-sig")
pure = source[source.index("        internal static Rect StarBounds"):source.index("        private static Texture2D Aura")]
renderer = source[source.index("        internal static void PaintStars("):source.index("        private static readonly Dictionary<mFont")]
frame = (ROOT / "Assets/Scripts/Hud/InventoryFrame.cs").read_text(encoding="utf-8-sig")
star_data = frame[frame.index("        internal static int Option("):frame.index("        // Set identities")]
assets = []
for zoom in (1, 2, 3, 4):
    with Image.open(ROOT / f"Assets/Resources/res/x{zoom}/mainImage/myTexture2dbd3.png") as image:
        image = image.convert("RGBA")
        pixels = ",".join("new Color32(" + ",".join(map(str, image.getpixel((x, y)))) + ")"
                          for y in range(image.height - 1, -1, -1) for x in range(image.width))
        assets.append(f"Test(new Color32[]{{{pixels}}},{image.width},{image.height});")
    for name in ("star.png", "starE.png", "star8.png"):
        assert (ROOT / f"Assets/Resources/res/x{zoom}/mainImage/{name}").exists()

fixture = r'''
using System;
using UnityEngine;
using Hud;
class Item { public const int OPT_STARSLOT=102, OPT_MAXSTARSLOT=107; public ItemOption[] itemOption; }
class ItemOption { public ItemOptionTemplate optionTemplate; public int param; }
class ItemOptionTemplate { public int id; }
class Image { public Texture2D texture=new Texture2D(); }
class Panel { public static Image imgStar=new Image(), imgMaxStar=new Image(), imgStar8=new Image(), imgStarCuongHoa; }
class mGraphics { public static int zoomLevel=2; }
namespace Hud { static class InventoryFrame { STAR_DATA } }
namespace UnityEngine {
 public class Texture2D {}
 public struct Color { public int value; public static Color white=>new Color{value=1}; }
 public static class Mathf { public static int Max(int a,int b)=>Math.Max(a,b); public static int Min(int a,int b)=>Math.Min(a,b); }
 public static class GUI {
  public static Color color; public static int groups;
  public static System.Collections.Generic.List<Texture2D> drawn=new System.Collections.Generic.List<Texture2D>();
  public static void BeginGroup(Rect rect){groups++;} public static void EndGroup(){groups--;}
  public static void DrawTexture(Rect rect,Texture2D image){drawn.Add(image);}
 }
 public struct Color32 { public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;} }
 public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
 public struct Rect {
  public float x,y,width,height; public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}
  public float xMax=>x+width; public float yMax=>y+height; public Vector2 center=>new Vector2(x+width/2,y+height/2);
 }
}
class VisualTests {
 static int checks;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static void Test(Color32[] corner,int width,int height){
  var original=(Color32[])corner.Clone();
  foreach(int color in new[]{0xff914d,0x62d98b,0x66aaff,0xc58aff}){
   Color32[] pixels=InventoryItemVisuals.AuraPixels(color,corner,width,height,64);
   Check(pixels.Length==4096,"aura resolution");
   for(int y=0;y<64;y++)for(int x=0;x<64;x++){
    Color32 p=pixels[y*64+x];
    if(p.a>0)Check(p.r==(byte)(color>>16)&&p.g==(byte)(color>>8)&&p.b==(byte)color,"aura lost set hue");
    if(x==0||x==63||y==0||y==63)Check(p.a==0,"aura covers native outline/outside frame");
    if(x>=15&&x<=48&&y>=15&&y<=48)Check(p.a==0,"aura obscures center icon");
   }
   Check(pixels[2*64+32].a>100,"set aura too faint at inner edge");
   for(int y=2;y<31;y++)Check(pixels[(y+1)*64+32].a<=pixels[y*64+32].a,"aura does not fade inward");
  }
  for(int i=0;i<corner.Length;i++)Check(corner[i].a==original[i].a&&corner[i].r==original[i].r,"native corner sprite changed");
 }
 static void Main(){
  ASSETS
  foreach(float size in new float[]{20,22,28,32,40,44,56,64}){
   Rect cell=new Rect(10,20,size,size),level=InventoryItemVisuals.LevelBounds(cell,24,false);
   for(int count=1;count<=9;count++){
   Rect first=InventoryItemVisuals.StarBounds(cell,0,count),last=InventoryItemVisuals.StarBounds(cell,count-1,count);
   Rect compact=InventoryItemVisuals.CompactLevelBounds(cell,24);
   Check(compact.height==level.height&&compact.width<=cell.width,"skill Lv badge squashes native corners or exceeds icon width");
   Check(Math.Abs((first.x+last.xMax)/2-cell.center.x)<.01,"star row not centered");
   for(int i=0;i<count;i++){
    Rect star=InventoryItemVisuals.StarBounds(cell,i,count);
    Check(star.x>=cell.x&&star.xMax<=cell.xMax,"stars exceed equipment width");
    Check(star.y>=level.yMax&&star.yMax<cell.yMax+9,"stars overlap Lv or next equipment row");
   }
   }
   Rect bagLevel=InventoryItemVisuals.LevelBounds(cell,24,true);
   Check(Math.Abs(bagLevel.x-(cell.x-3))<.01&&Math.Abs(bagLevel.y-(cell.y-3))<.01,"bag Lv not anchored outside corner");
   Check(Math.Abs(level.xMax-cell.xMax-3)<.01&&Math.Abs(level.yMax-cell.yMax-3)<.01,"equipped Lv corner offset changed");
   Check(bagLevel.yMax<=cell.yMax-2,"Lv pill overlaps lower quantity badge area");
  }
  foreach(int zoom in new[]{1,2,3,4})for(int slots=0;slots<=9;slots++)for(int filled=0;filled<=slots;filled++){
   mGraphics.zoomLevel=zoom;
   var item=new Item{itemOption=new[]{new ItemOption{optionTemplate=new ItemOptionTemplate{id=107},param=slots},
    new ItemOption{optionTemplate=new ItemOptionTemplate{id=102},param=filled},
    new ItemOption{optionTemplate=new ItemOptionTemplate{id=228},param=9}}};
   GUI.drawn.Clear();GUI.color=new Color{value=42};
   InventoryItemVisuals.PaintStars(new Rect(10,10,44,44),new Rect(0,0,100,100),item);
   Check(GUI.drawn.Count==slots,"renderer paints phantom or missing sockets");
   for(int i=0;i<slots;i++)Check(object.ReferenceEquals(GUI.drawn[i],(i<filled?(i<7?Panel.imgStar:Panel.imgStar8):Panel.imgMaxStar).texture),"renderer used wrong empty/filled star asset");
   Check(GUI.color.value==42&&GUI.groups==0,"star renderer leaks GUI color or clip");
  }
  Panel.imgStarCuongHoa=new Image();
  var enhanced=new Item{itemOption=new[]{new ItemOption{optionTemplate=new ItemOptionTemplate{id=107},param=9},new ItemOption{optionTemplate=new ItemOptionTemplate{id=228},param=8}}};
  GUI.drawn.Clear();InventoryItemVisuals.PaintStars(new Rect(0,0,44,44),new Rect(0,0,100,100),enhanced);
  Check(object.ReferenceEquals(GUI.drawn[7],Panel.imgStarCuongHoa.texture)&&object.ReferenceEquals(GUI.drawn[8],Panel.imgMaxStar.texture),"reinforced slot asset not distinguished from ordinary empty slot");
  Console.WriteLine("PASS: "+checks+" aura pixel/star/Lv layout checks; no Unity build");
 }
}
'''.replace("ASSETS", "\n".join(assets)).replace("STAR_DATA", star_data)
output = ROOT / "Build/inventory-visual-tests"
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="visuals-", dir=output) as folder:
    path = Path(folder)
    (path / "VisualTests.cs").write_text(fixture, encoding="utf-8")
    (path / "Geometry.cs").write_text("using UnityEngine; namespace Hud { internal static class InventoryItemVisuals {\n" + pure + renderer + "\n}}", encoding="utf-8")
    exe = path / "VisualTests.exe"
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", f"/out:{exe}",
                    str(path / "VisualTests.cs"), str(path / "Geometry.cs")], check=True)
    subprocess.run([str(exe)], check=True)
