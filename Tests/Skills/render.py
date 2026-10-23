"""Exercise the production icon resolver, font states and button rendering, not panel stubs."""
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
source = (ROOT / 'Assets/Scripts/Hud/InventoryItemVisuals.cs').read_text(encoding='utf-8-sig')
methods = source[source.index('        private static readonly Dictionary<mFont, GUIStyle> compactStyles'):source.index('        internal static void PaintLevel(')]
fixture = r'''


using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine {
 public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public float xMax=>x+width;public float yMax=>y+height;public Vector2 center=>new Vector2(x+width/2,y+height/2);}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1,1);}
 public struct Color32 {public byte r,g,b,a;public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public class Texture2D {public int width,height;public string name;public Texture2D(int w,int h,string n){width=w;height=h;name=n;}}
 public class Font {public int fontSize=20;}
 public enum TextAnchor {MiddleLeft,MiddleCenter}
 public class RectOffset {}
 public class GUIContent {public string text;public GUIContent(string t){text=t;}}
 public class GUIStyleState {public Color textColor;}
 public class GUIStyle {
  public Font font;public int fontSize;public TextAnchor alignment;public RectOffset padding,margin;public bool wordWrap,richText;
  public GUIStyleState normal=new GUIStyleState(),hover=new GUIStyleState(),active=new GUIStyleState(),focused=new GUIStyleState(),onNormal=new GUIStyleState(),onHover=new GUIStyleState(),onActive=new GUIStyleState(),onFocused=new GUIStyleState();
  public static int measures;public GUIStyle(GUIStyle s){}public Vector2 CalcSize(GUIContent c){measures++;return new Vector2(c.text.Length*fontSize*.5f,fontSize);}
 }
 public class GUISkin {public GUIStyle label=new GUIStyle(null);}
 public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);}
 public static class GUI {
  public static Color color=Color.white;public static GUISkin skin=new GUISkin();public static List<Texture2D> textures=new List<Texture2D>();public static List<Rect> rectangles=new List<Rect>();public static GUIStyle lastStyle;public static string lastText;public static Rect lastUv;public static int groups;
  public static void BeginGroup(Rect r){groups++;}public static void EndGroup(){groups--;}
  public static void Label(Rect r,string t,GUIStyle s){lastStyle=s;lastText=t;}
  public static void DrawTexture(Rect r,Texture2D t){textures.Add(t);rectangles.Add(r);}
  public static void DrawTextureWithTexCoords(Rect r,Texture2D t,Rect uv){DrawTexture(r,t);lastUv=uv;}
 }
}
class Image {public Texture2D texture;public Image(int w,int h,string name){texture=new Texture2D(w,h,name);}}
class GameScr {public static Image imgSkill=new Image(56,56,"native-orange-frame"),imgSkill2=new Image(56,56,"native-green-frame");}
class Small {public Image img;}
class SmallImage {
 public static Image[] imgbig;public static int[][] smallImg;public static Small[] imgNew;public static Image imgEmpty=new Image(1,1,"empty");public static int requests;
 public static void createImage(int id){requests++;imgNew[id]=new Small{img=imgEmpty};}
}
class mFont {public Font myFont=new Font();public static mFont tahoma_7b_dark=new mFont();}
class mGraphics {public static int zoomLevel=2;}
class Command {public static Image btn0left,btn0mid,btn0right,btn1left,btn1mid,btn1right;}
namespace Hud {
 static class InventoryFrame {
  public const int SlotBackground=9993045,Accent=16383818,Border=6702080;public static int fallbackFrames;
  public static Rect Intersection(Rect a,Rect b){float x=Math.Max(a.x,b.x),y=Math.Max(a.y,b.y);return new Rect(x,y,Math.Max(0,Math.Min(a.xMax,b.xMax)-x),Math.Max(0,Math.Min(a.yMax,b.yMax)-y));}
  public static Rect Inset(Rect r,float padding)=>new Rect(r.x+padding,r.y+padding,r.width-2*padding,r.height-2*padding);public static void Clip(mGraphics g,Rect r){}public static void Rounded(mGraphics g,Rect r,int color,int alpha,int border){fallbackFrames++;}
 }
 static class HudButtonSkin {
  public static Image CopyNative(Image image,Action<Color32[],int,int> edit){var pixels=new[]{new Color32(255,170,0,255)};edit(pixels,1,1);RenderTests.Check(pixels[0].r==pixels[0].g&&pixels[0].g==pixels[0].b,"disabled button not grey");return new Image(image.texture.width,image.texture.height,"grey");}
 }
 static class InventoryItemVisuals { METHODS }
}
class RenderTests {
 static int checks;public static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static void Main(){
  var g=new mGraphics();Rect viewport=new Rect(0,0,200,200),bounds=new Rect(8,8,24,24);
  foreach(int zoom in new[]{1,2,3,4}) {
   mGraphics.zoomLevel=zoom;SmallImage.imgbig=new[]{new Image(256*zoom,256*zoom,"atlas")};
   SmallImage.smallImg=new int[14][];SmallImage.imgNew=new Small[14];
   for(int id=0;id<13;id++) {
    bool separate=id%3!=1;SmallImage.smallImg[id]=separate?new[]{0,300+id,0,30,30}:new[]{0,id*4,8,24,24};
    SmallImage.imgNew[id]=new Small{img=new Image(30*zoom,30*zoom,"individual"+id)};
    GUI.textures.Clear();Hud.InventoryItemVisuals.PaintCompactIcon(g,id,bounds,viewport);
    Check(GUI.textures.Count==1,"skill/stat icon disappeared");
    Check(GUI.textures[0].name==(separate?"individual"+id:"atlas"),"selected wrong atlas/individual source");
    Check(GUI.lastUv.x>=0&&GUI.lastUv.y>=0&&GUI.lastUv.xMax<=1&&GUI.lastUv.yMax<=1,"UV outside texture");
   }
   SmallImage.smallImg[13]=new[]{0,900,0,30,30};GUI.textures.Clear();
   Hud.InventoryItemVisuals.PaintCompactIcon(g,13,bounds,viewport);
   Check(GUI.lastText=="?"&&GUI.textures.Count==0,"missing texture has no default icon");
   Check(SmallImage.requests>0,"missing dedicated image was never requested");
   Hud.InventoryItemVisuals.PaintCompactIcon(g,-1,bounds,viewport);Check(GUI.lastText=="?","invalid ID left blank");
   foreach(int ink in new[]{0x536DCC,0x00552D}) {
    var cachedFont=new mFont();
    Hud.InventoryItemVisuals.PaintCompactText(g,cachedFont,"HP gốc: 52.100",bounds,viewport,ink);
    int measured=GUIStyle.measures;
    for(int repeat=0;repeat<100;repeat++)Hud.InventoryItemVisuals.PaintCompactText(g,cachedFont,"HP gốc: 52.100",bounds,viewport,ink);
    Check(GUIStyle.measures==measured,"unchanged text was repeatedly measured");
    Hud.InventoryItemVisuals.PaintCompactText(g,cachedFont,"HP gốc: 52.100",new Rect(bounds.x,bounds.y,bounds.width/2,bounds.height),viewport,ink);
    Check(GUIStyle.measures>measured,"resizing failed to invalidate font fit");
    Hud.InventoryItemVisuals.PaintCompactText(g,cachedFont,"HP gốc: 52.100",bounds,viewport,ink);
    var s=GUI.lastStyle;
    foreach(var state in new[]{s.normal,s.hover,s.active,s.focused,s.onNormal,s.onHover,s.onActive,s.onFocused})
     Check(Math.Abs(state.textColor.r-(ink>>16&255)/255f)<.001&&Math.Abs(state.textColor.g-(ink>>8&255)/255f)<.001&&Math.Abs(state.textColor.b-(ink&255)/255f)<.001,"font changes color on hover/focus");
   }
   Command.btn0left=new Image(10*zoom,24*zoom,"orange-left");Command.btn0mid=new Image(10*zoom,24*zoom,"orange-mid");Command.btn0right=new Image(10*zoom,24*zoom,"orange-right");
   Command.btn1left=new Image(10*zoom,24*zoom,"green-left");Command.btn1mid=new Image(10*zoom,24*zoom,"green-mid");Command.btn1right=new Image(10*zoom,24*zoom,"green-right");
   foreach(bool enabled in new[]{false,true})foreach(bool selected in new[]{false,true})foreach(int width in new[]{22,40,130}) {
    Rect button=new Rect(10,20,width,22);GUI.textures.Clear();GUI.rectangles.Clear();
    Hud.InventoryItemVisuals.PaintNativeButton(g,button,enabled,selected);
    Check(GUI.textures.Count==3,"button lost native rounded caps");
    Check(GUI.textures[0].name==(!enabled?"grey":selected?"green-left":"orange-left"),"wrong button state skin");
    foreach(Rect r in GUI.rectangles)Check(r.x>=button.x*zoom&&r.xMax<=button.xMax*zoom+.01&&r.y==button.y*zoom&&r.height==button.height*zoom,"button clipped/outside hit rectangle");
   }
   GUI.textures.Clear();
   Hud.InventoryItemVisuals.PaintSkillIcon(g,0,bounds,viewport);
   Check(GUI.textures.Count==2&&GUI.textures[0].name=="native-orange-frame","original orange frame missing");
   GUI.textures.Clear();Hud.InventoryItemVisuals.PaintSkillIcon(g,0,bounds,viewport,true);
   Check(GUI.textures.Count==2&&GUI.textures[0].name=="native-green-frame","selected skill did not use original green frame");
   Check(GUI.groups==0,"rendering leaked GUI clipping group");
  }
  Console.WriteLine("PASS: "+checks+" production icon/font/button rendering checks");
 }
}
'''.replace('METHODS', methods)
output = ROOT / 'Build/skill-render-tests'
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix='render-', dir=output) as folder:
    path = Path(folder)
    cs = path / 'Render.cs'
    cs.write_text(fixture, encoding='utf-8')
    exe = path / 'Render.exe'
    subprocess.run([r'D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe', '/nologo', f'/out:{exe}', str(cs)], check=True)
    subprocess.run([str(exe)], check=True)
