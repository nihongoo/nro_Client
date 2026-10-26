"""Exercise the production native-tab renderer with mipmapped sources and pixel recording."""
from pathlib import Path
import subprocess
import tempfile
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
source = (ROOT / 'Assets/Scripts/Hud/InventoryItemVisuals.cs').read_text(encoding='utf-8-sig')
production = source[source.index('        private static readonly Texture2D[] nativeTabSources'):source.index('        internal static void PaintNativeButton(')]
assets = []
for zoom in (2, 3, 4):
    textures = []
    for name in ('myTexture2dimgPopup.png', 'myTexture2dimgPopup2.png'):
        im = Image.open(ROOT / f'Assets/Resources/res/x{zoom}/mainImage/{name}').convert('RGBA')
        pixels = ','.join('new Color(' + ','.join(f'{v}/255f' for v in im.getpixel((x,y))) + ')'
                          for y in range(im.height-1,-1,-1) for x in range(im.width))
        textures.append(f'new Texture2D({im.width},{im.height},new Color[]{{{pixels}}},4)')
    assets.append(f'Test({zoom},{textures[0]},{textures[1]});')
fixture = r'''
using System;
using UnityEngine;
using Hud;
namespace UnityEngine {
 public enum FilterMode {Point,Bilinear}
 public struct Color {
  public float r,g,b,a; public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}
  public static Color white=>new Color(1,1,1,1);
 }
 public struct Rect {
  public float x,y,width,height; public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}
  public float xMax=>x+width;public float yMax=>y+height;
 }
 public static class Mathf {
  public static int Max(int a,int b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);
  public static float Round(float n)=>(float)Math.Round(n);
 }
 public class Texture2D {
  public int width,height,mipmapCount;public Color[] pixels;public FilterMode filterMode=FilterMode.Bilinear;
  public Texture2D(int w,int h,Color[] p,int m){width=w;height=h;pixels=p;mipmapCount=m;}
  public static Texture2D whiteTexture=new Texture2D(1,1,new[]{Color.white},1){filterMode=FilterMode.Point};
 }
 public static class Object { public static int destroyed;public static void Destroy(Texture2D t){destroyed++;} }
 public static class GUI {
  public static Color color=Color.white;public static int groups;static Rect group;
  public static Color[] canvas;public static int width,height;
  public static void BeginGroup(Rect r){group=r;groups++;}public static void EndGroup(){groups--;}
  public static void DrawTexture(Rect r,Texture2D t){DrawTextureWithTexCoords(r,t,new Rect(0,0,1,1));}
  public static void DrawTextureWithTexCoords(Rect r,Texture2D t,Rect uv){
   if(t.mipmapCount!=1||t.filterMode!=FilterMode.Point)throw new Exception("UI samples shared mipmapped/filtered atlas");
   float left=r.x+group.x,top=r.y+group.y;
   for(int y=(int)Math.Ceiling(top-.5);y<top+r.height-.5;y++)for(int x=(int)Math.Ceiling(left-.5);x<left+r.width-.5;x++){
    if(x<0||y<0||x>=width||y>=height||x<group.x||x>=group.xMax||y<group.y||y>=group.yMax)continue;
    int sx=Math.Max(0,Math.Min(t.width-1,(int)((uv.x+(x+.5-left)/r.width*uv.width)*t.width)));
    int sy=Math.Max(0,Math.Min(t.height-1,(int)((uv.y+uv.height-(y+.5-top)/r.height*uv.height)*t.height)));
    Color p=t.pixels[sy*t.width+sx];if(p.a>0.99)canvas[y*width+x]=new Color(p.r*color.r,p.g*color.g,p.b*color.b,1);
   }
  }
 }
}
class Image {public Texture2D texture;}
class mGraphics {public static int zoomLevel;}
class PopUp {public static Image imgPopUp,imgPopUp2;public static void loadBg(){throw new Exception("unexpected missing atlas");}}
namespace Hud {
 static class InventoryFrame {
  public static void Clip(mGraphics g,Rect r){}
  public static Rect Intersection(Rect a,Rect b){float x=Math.Max(a.x,b.x),y=Math.Max(a.y,b.y);return new Rect(x,y,Math.Max(0,Math.Min(a.xMax,b.xMax)-x),Math.Max(0,Math.Min(a.yMax,b.yMax)-y));}
 }
 static class HudButtonSkin {
  public static int copies;
  public static Image CopyNative(Image source,Action<Color[],int,int> edit,FilterMode filter){copies++;return new Image{texture=new Texture2D(source.texture.width,source.texture.height,(Color[])source.texture.pixels.Clone(),1){filterMode=filter}};}
 }
 static class InventoryItemVisuals { PRODUCTION }
}
class Tests {
 static int checks;
 static void Check(bool b,string text){checks++;if(!b)throw new Exception(text);}
 static bool Same(Color a,Color b)=>Math.Abs(a.r-b.r)<.005&&Math.Abs(a.g-b.g)<.005&&Math.Abs(a.b-b.b)<.005;
 static void Test(int zoom,Texture2D normal,Texture2D selected){
  mGraphics.zoomLevel=zoom;PopUp.imgPopUp=new Image{texture=normal};PopUp.imgPopUp2=new Image{texture=selected};
  int copies=HudButtonSkin.copies;
  foreach(bool active in new[]{false,true})foreach(float w in new[]{31.25f,39.5f,42.75f,60f,83.25f})foreach(float h in new[]{22f,24f}){
   GUI.width=400*zoom;GUI.height=40*zoom;GUI.canvas=new Color[GUI.width*GUI.height];GUI.color=new Color(.2f,.3f,.4f,1);
   Rect button=new Rect(3.25f,2.5f,w,h);
   InventoryItemVisuals.PaintNativeTab(new mGraphics(),button,new Rect(0,0,400,40),active);
   Check(GUI.groups==0&&GUI.color.r==.2f,"tab renderer leaks GUI clip/color");
   Color fill=active?new Color(183/255f,232/255f,176/255f,1):new Color(255/255f,229/255f,199/255f,1);
   int y=(int)Math.Round((button.y+h/2)*zoom);
   for(int x=(int)Math.Round((button.x+10)*zoom);x<(int)Math.Round((button.xMax-10)*zoom);x++)Check(Same(GUI.canvas[y*GUI.width+x],fill),"horizontal seam or gap through button center");
  }
  Check(HudButtonSkin.copies==copies+2,"tab textures copied every frame instead of cached");
  Check(normal.mipmapCount==4&&normal.filterMode==FilterMode.Bilinear,"shared native atlas modified");
 }
 static void Main(){ ASSETS Console.WriteLine("PASS: "+checks+" native-tab pixel/cache checks; no Unity build");}
}
'''.replace('PRODUCTION', production).replace('ASSETS','\n'.join(assets))
output=ROOT/'Build/inventory-skin-tests'
with tempfile.TemporaryDirectory(prefix='tabs-',dir=output) as folder:
    path=Path(folder);src=path/'Test.cs';src.write_text(fixture,encoding='utf-8');exe=path/'Test.exe'
    subprocess.run([r'D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe','/nologo',f'/out:{exe}',str(src)],check=True)
    subprocess.run([str(exe)],check=True)
