"""Check actual corner pixels and production recolor/cache code without Unity."""
from pathlib import Path
import subprocess
import tempfile
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
assets = []
for zoom in (1, 2, 3, 4):
    with Image.open(ROOT / f"Assets/Resources/res/x{zoom}/mainImage/myTexture2dbd3.png") as image:
        image = image.convert("RGBA")
        assets.append("new Texture2D(new Color32[]{" + ",".join(
            "new Color32(" + ",".join(map(str, image.getpixel((x, y)))) + ")"
            for y in range(image.height) for x in range(image.width)) + "})")

fixture = r'''
using System;
using UnityEngine;
using Hud;
namespace UnityEngine {
 public struct Color32 { public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;} }
 public class Texture2D { public Color32[] pixels; public Texture2D(Color32[] data){pixels=data;} }
 public static class Object { public static int destroyed; public static void Destroy(Texture2D t){destroyed++;} }
}
class Image { public Texture2D texture; }
static class PopUp { public static Image goc; public static void loadBg(){throw new Exception("unexpected uninitialized sprite");} }
namespace Hud {
 static class HudButtonSkin {
  public static int copies;
  public static Image CopyNative(Image source,Action<Color32[],int,int> edit){
   copies++; var data=(Color32[])source.texture.pixels.Clone(); edit(data,data.Length,1);return new Image{texture=new Texture2D(data)};
  }
 }
}
class SkinTests {
 static int checks;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static void Main(){
  Texture2D[] assets=new Texture2D[]{ASSETS};
  foreach(Texture2D source in assets){
   PopUp.goc=new Image{texture=source};
   foreach(int fill in new[]{15196114,9993045,11837316,16383818})foreach(int border in new[]{0,0x66aaff})foreach(byte opacity in new byte[]{100,255}){
    Color32[] original=(Color32[])source.pixels.Clone();
    Image skin=InventoryFrameSkin.Corners(fill,border,opacity);int copies=HudButtonSkin.copies;
    Check(object.ReferenceEquals(skin,InventoryFrameSkin.Corners(fill,border,opacity)),"skin not cached");
    Check(HudButtonSkin.copies==copies,"repeated paint copies GPU texture");
    for(int i=0;i<original.Length;i++){
     Color32 before=original[i],after=skin.texture.pixels[i],unchanged=source.pixels[i];
     Check(after.a==before.a*opacity/255,"native rounded alpha changed");
     Check(before.r==unchanged.r&&before.g==unchanged.g&&before.b==unchanged.b&&before.a==unchanged.a,"original sprite mutated");
     if(before.r==255&&before.g==255&&before.b==255&&before.a>0)
      Check(after.r==(byte)(fill>>16)&&after.g==(byte)(fill>>8)&&after.b==(byte)fill,"white corner did not match fill");
     if(before.r==0&&before.g==0&&before.b==0&&before.a>0)
      Check(after.r==(byte)(border>>16)&&after.g==(byte)(border>>8)&&after.b==(byte)border,"corner outline did not match border");
    }
   }
  }
  Check(UnityEngine.Object.destroyed>0,"zoom/source change leaked copied textures");
  int previous=HudButtonSkin.copies;
  for(int i=0;i<10000;i++)InventoryFrameSkin.Corners(9993045,0,255);
  Check(HudButtonSkin.copies==previous,"idle repaint creates new textures");
  Console.WriteLine("PASS: "+checks+" native corner color/alpha/cache checks; no Unity build");
 }
}
'''.replace("ASSETS", ",".join(assets))
output = ROOT / "Build/inventory-skin-tests"
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="skin-", dir=output) as folder:
    path = Path(folder)
    (path / "SkinTests.cs").write_text(fixture, encoding="utf-8")
    exe = path / "SkinTests.exe"
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", f"/out:{exe}",
                    str(path / "SkinTests.cs"), str(ROOT / "Assets/Scripts/Hud/InventoryFrameSkin.cs")], check=True)
    subprocess.run([str(exe)], check=True)
