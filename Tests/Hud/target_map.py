"""Exercise production map bounds and marquee with a recording font; no Unity build."""
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
source = (ROOT / "Assets/Scripts/Hud/TargetInfoHud.cs").read_text(encoding="utf-8-sig")


def block(marker):
    start = source.index(marker)
    brace = source.index("{", start)
    depth, end = 1, brace + 1
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start:end]


def expression(marker):
    start = source.index(marker)
    return source[start:source.index(";", start) + 1]


parts = [block(marker) for marker in [
    "private sealed class Marquee", "private sealed class TextMeasure",
    "private static Rect Bounds()", "private static Rect FrameTextBounds(",
    "private static string MapValue()", "private static void Text(",
]]
parts += [expression(marker) for marker in [
    "private static float Unit(", "private static float FontScale",
    "private static float RowHeight", "private const float FramePadding",
    "private const float MapPadding", "private const float PreferredHpWidth",
]]
focus = block("if (!ReferenceEquals(textTarget,identity))")

fixture = r'''
using System;
using System.Collections.Generic;
struct Vector2 {
 public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;}
 public static Vector2 zero=>new Vector2();
}
struct Rect {
 public float x,y,width,height;
 public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}
 public float xMin=>x;public float xMax=>x+width;public float yMax=>y+height;
 public Vector2 center=>new Vector2(x+width/2,y+height/2);
}
struct Color32 { }
static class Mathf {
 public static float Max(float a,float b)=>Math.Max(a,b);
 public static int Max(int a,int b)=>Math.Max(a,b);
 public static float Max(float a,float b,float c)=>Math.Max(a,Math.Max(b,c));
 public static float Min(float a,float b)=>Math.Min(a,b);
 public static int Min(int a,int b)=>Math.Min(a,b);
 public static int FloorToInt(float a)=>(int)Math.Floor(a);
 public static int CeilToInt(float a)=>(int)Math.Ceiling(a);
 public static int RoundToInt(float a)=>(int)Math.Round(a);
}
enum HorizontalAnchor {Center} enum VerticalAnchor {Top}
static class HudLayout {
 public static bool IsMobileLayout;
 public static float PixelsToLogical(float n)=>n/Math.Max(1,mGraphics.zoomLevel);
 public static Rect GetRect(string n)=>n=="top-info.bounds"?new Rect(0,0,40,20):new Rect(GameCanvas.w-40,0,40,20);
 public static Rect Anchor(HorizontalAnchor h,VerticalAnchor v,Vector2 size,Vector2 margin)=>new Rect((GameCanvas.w-size.x)/2,0,size.x,size.y);
}
static class TopActionsHud {public const string Hub="hub";}
static class GameCanvas {public static int w,h;}
static class TileMap {public static string mapName;public static int zoneID;}
static class mSystem {public static long now;public static long currentTimeMillis()=>now;}
class mGraphics {
 public static int zoomLevel;public Rect clip;
 public void setClip(int x,int y,int w,int h){clip=new Rect(x,y,w,h);}
}
class mFont {
 public static mFont tahoma_7_white=new mFont(),tahoma_7b_white=new mFont();
 public int getHeight()=>8;public int getWidth(string s)=>s.Length*3;
 public readonly List<int> draws=new List<int>();public Rect clip;
 public void drawTintedString(mGraphics g,string s,int x,int y,Color32 c){draws.Add(x);clip=g.clip;}
}
static class MapTests {
 const string Map="map",Hp="hp",Name="name",Power="power";
 static Rect mapRect,hpRect,nameRect,powerRect;
 static float upperWidth,lowerWidth,stepY;static bool showsTarget,layoutHasPower,hasTarget;
 static object layoutFocus,textTarget;
 struct Target {public bool HasPower;}
 static bool Read(out Target t,out object id){t=new Target{HasPower=true};id=null;return hasTarget;}
 static string mapValue,mapName;static int mapZone=-1,mapZoom=-1;
 static readonly TextMeasure mapMeasure=new TextMeasure();
 static readonly Dictionary<string,Marquee> marquees=new Dictionary<string,Marquee>();
 __PRODUCTION__
 static void Focus(object identity){__FOCUS__}
 static int checks;
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static void Near(float a,float b,string message)=>Check(Math.Abs(a-b)<1.1f,message);
 static void Main(){
  foreach(string key in new[]{Map,Hp,Name,Power})marquees[key]=new Marquee();
  var g=new mGraphics();var font=mFont.tahoma_7_white;
  foreach(bool mobile in new[]{false,true})foreach(int zoom in new[]{1,2,3,4})foreach(int width in new[]{320,422,640,800}){
   HudLayout.IsMobileLayout=mobile;mGraphics.zoomLevel=zoom;GameCanvas.w=width;GameCanvas.h=mobile?195:360;
   TileMap.zoneID=16;TileMap.mapName="Vách núi đen";hasTarget=false;
   Rect root=Bounds();font.draws.Clear();mSystem.now=100;
   Text(g,Map,mapRect,MapValue(),font,new Color32(),true);
   Near(font.draws[0],mapRect.center.x-font.getWidth(MapValue())/2f,"short map name not centered");
   for(int i=0;i<20;i++){mSystem.now+=100;Text(g,Map,mapRect,MapValue(),font,new Color32(),true);}
   Check(font.draws[0]==font.draws[font.draws.Count-1],"fitting map name scrolls");
   Near(font.clip.x,GameCanvas.w/2f-upperWidth/2+Unit(4),"map clips before usable left inset");
   Near(font.clip.xMax,GameCanvas.w/2f+upperWidth/2-Unit(4),"map clips before usable right inset");
   TileMap.mapName=new string('W',400);hasTarget=false;root=Bounds();
   float available=GameCanvas.w-2*(40+Unit(6));
   Near(root.width,available,"map-only tab does not use room between neighboring HUDs");
   Check(root.x>=40+Unit(6)-1&&root.xMax<=GameCanvas.w-40-Unit(6)+1,"map-only tab overlaps neighboring HUDs");
   foreach(bool focus in new[]{false,true}){
    hasTarget=focus;root=Bounds();mSystem.now+=100;font.draws.Clear();
    Text(g,Map,mapRect,MapValue(),font,new Color32(),true);int first=font.draws[0];
    for(int i=0;i<18;i++){
     Focus(new object());mSystem.now+=100;font.draws.Clear();
     Text(g,Map,mapRect,MapValue(),font,new Color32(),true);
    }
    Check(font.draws[0]<first,"long map name does not scroll continuously across focus changes");
    Check(font.clip.x>=root.x&&font.clip.xMax<=root.xMax,"marquee clip escapes frame");
    TileMap.zoneID++;mSystem.now+=100;font.draws.Clear();
    Text(g,Map,mapRect,MapValue(),font,new Color32(),true);
    Near(font.draws[0],font.clip.x+1,"changed zone does not reset map marquee");
   }
  }
  Console.WriteLine("PASS: "+checks+" production map layout/marquee checks; no Unity build");
 }
}
'''.replace("__PRODUCTION__", "\n".join(parts)).replace("__FOCUS__", focus)
output = ROOT / "Build/target-map-tests"
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="map-", dir=output) as folder:
    path = Path(folder)
    code, exe = path / "MapTests.cs", path / "MapTests.exe"
    code.write_text(fixture, encoding="utf-8")
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", f"/out:{exe}", str(code)], check=True)
    subprocess.run([str(exe)], check=True)
