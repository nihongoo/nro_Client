"""Exercise real buff store, protocol, grid and pointer handlers with rendering-only fixtures."""
from pathlib import Path
import subprocess
import sys
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/"Build/buff-icons-tests";OUT.mkdir(parents=True,exist_ok=True)
packet=Path(sys.argv[1]) if len(sys.argv)>1 else Path(r"D:\dowload\SRC NRO NEW 03\SRC\build\buff-icons-tests\snapshot.bin")
fixture=r"""
using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Reflection;
using Hud;using UnityEngine;
namespace UnityEngine {
 public struct Vector2 { public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;} }
 public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public float xMin=>x;public float yMin=>y;public float xMax=>x+width;public float yMax=>y+height;public Vector2 center=>new Vector2(x+width/2,y+height/2);public bool Contains(Vector2 p)=>width>0&&height>0&&p.x>=x&&p.y>=y&&p.x<xMax&&p.y<yMax;}
 public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Min(params float[] a)=>a.Min();public static int FloorToInt(float n)=>(int)Math.Floor(n);public static float Ceil(float n)=>(float)Math.Ceiling(n);public static float Round(float n)=>(float)Math.Round(n);public static int Clamp(int n,int a,int b)=>Math.Max(a,Math.Min(b,n));public static float Clamp(float n,float a,float b)=>Math.Max(a,Math.Min(b,n));}
 public struct Color {public static Color white=>new Color();}public class Texture2D{}
 public static class GUI {public static Color color;public static void DrawTextureWithTexCoords(Rect r,Texture2D t,Rect uv){} }
 public class TextAsset {public byte[] bytes;}public static class Resources{public static object Load(string s,Type t)=>null;}
}
public class Image{public Texture2D texture=new Texture2D();}
public class mGraphics {public static int zoomLevel=2,addYWhenOpenKeyBoard;public int getTranslateX()=>0;public int getTranslateY()=>0;public void translate(int x,int y){}public void setClip(int x,int y,int w,int h){}public void setColor(int c){}public void fillRect(int x,int y,int w,int h){} }
public class mFont {public static mFont tahoma_7_white=new mFont(),tahoma_7b_white=new mFont(),tahoma_7b_dark=new mFont(),tahoma_7=new mFont();public const int LEFT=0;public int getHeight()=>8;public int getWidth(string s)=>s.Length*4;public string[] splitFontArray(string s,int width)=>new[]{s};public void drawString(mGraphics g,string s,int x,int y,int align,mFont shadow=null){} }
public class Message {myReader r;public Message(byte[] data){r=new myReader(mSystem.convertToSbyte(data));}public myReader reader()=>r;}
public static class mSystem {public static sbyte[] convertToSbyte(byte[] b){var a=new sbyte[b.Length];Buffer.BlockCopy(b,0,a,0,b.Length);return a;}}
public class MyVector {public readonly List<object> list=new List<object>();public void removeElement(object o){list.Remove(o);}public void addElement(object o){list.Add(o);}public int size()=>list.Count;public object elementAt(int i)=>list[i];}
public class GameScr {public static GameScr instance=new GameScr();public static bool isPaint=true,isPaintOther;public static int isPaintRada=1;public static MyVector textTime=new MyVector();public bool isPointerDowning;public bool isPaintPopup()=>false;}
public class Char {public static Char player=new Char();public static Char ExistingLocalPlayer=>player;public static bool ischangingMap;public object currentMovePoint;public static MyVector vItemTime=new MyVector();public static Char myCharz()=>player;}
public class Panel{public bool isShow;}public class Menu{public bool showMenu;}
public static class GameCanvas {public static object currentScreen=GameScr.instance,currentDialog;public static bool isLoading,isPointerJustDown,isPointerDown,isPointerJustRelease,isPointerClick;public static int w=640,h=360,px,py,gameTick;public static Panel panel=new Panel();public static Menu menu=new Menu();public static void clearAllPointerEvent(){isPointerJustDown=isPointerDown=isPointerJustRelease=isPointerClick=false;} }
public static class TileMap{public static int mapID=1,zoneID=1;}public static class InfoDlg{public static bool isLock;}
public class ChatTextField {static ChatTextField c=new ChatTextField();public bool isShow;public static ChatTextField gI()=>c;}
public static class ChatPopup{public static object currChatPopup,serverChatPopUp;}
namespace Notifications{public static class NotificationProtocol{public const byte Version=2;}public static class NotificationCenter{public static bool IsOpen;}}
namespace Mod.CuongLe{public static class DoHoa{public static bool isShowMenuVIP;}}
public class Service{static Service s=new Service();public static Service gI()=>s;public int requests;public void requestBuffIconSnapshot(){requests++;}}
namespace Hud {
 public static class TopActionsHud{public const string Hub="hub";}
 public static class HudLayout {
  public const int MenuOrder=50;public static Rect ScreenBounds=>new Rect(0,0,GameCanvas.w,GameCanvas.h);
  class Slot{public string name;public Func<Rect> rect;public Func<bool> visible;public int order;public bool block;}
  static List<Slot> slots=new List<Slot>();public static float PixelsToLogical(float n)=>n/mGraphics.zoomLevel;
  public static void Register(string n,Func<Rect> r,int order,Func<bool> visible=null,bool blocksPointer=true){slots.Add(new Slot{name=n,rect=r,visible=visible,order=order,block=blocksPointer});}
  public static Rect GetRect(string n)=>slots.First(s=>s.name==n).rect();public static bool TryGetRect(string n,out Rect r){var s=slots.FirstOrDefault(a=>a.name==n);r=s==null?new Rect():s.rect();return s!=null;}
  public static string HitTest(Vector2 p)=>slots.Where(s=>s.block&&(s.visible==null||s.visible())&&s.rect().Contains(p)).OrderByDescending(s=>s.order).Select(s=>s.name).FirstOrDefault();public static void Invalidate(){}
 }
 public static class InventoryFrame{public const int SlotBackground=9993045,Border=6702080,PanelBackground=14338484;public static Rect Inset(Rect r,float p)=>new Rect(r.x+p,r.y+p,r.width-2*p,r.height-2*p);public static void Rounded(mGraphics g,Rect r,int c,byte a,int b){} }
 public static class InventoryFrameSkin{public static int gold;public static Image Corners(int a,int b,int c){if(b==0xffd34e)gold++;return new Image();}}
 public static class InventoryItemVisuals{public static int icons;public static void PaintCompactIcon(mGraphics g,int id,Rect b,Rect v){icons++;}public static void PaintCompactText(mGraphics g,mFont f,string s,Rect b,Rect v,int c,bool center){} }
}
class BuffTests {
 static int checks;static void Check(bool b,string m){checks++;if(!b)throw new Exception(m);}
 static List<BuffIconEntry> Entries(int count,long expiry){var list=new List<BuffIconEntry>();for(int i=count;i>=1;i--)list.Add(new BuffIconEntry{Id=i,Icon=433,ExpiresAt=expiry,Name="Bùa "+i,Effect="Hiệu ứng"});return list;}
 static void Apply(int count,long expiry){BuffIconStore.Reset();BuffIconStore.Apply(Entries(count,expiry),1,100000,BuffIconStore.Now);}
 static void Click(int x,int y){GameCanvas.gameTick++;GameCanvas.px=x;GameCanvas.py=y;GameCanvas.isPointerJustDown=GameCanvas.isPointerDown=GameCanvas.isPointerClick=true;BuffIconsHud.UpdateInput();GameCanvas.isPointerDown=false;GameCanvas.isPointerJustRelease=true;BuffIconsHud.UpdateInput();}
 static void Main(string[] args){
  BuffIconStore.Reset();var list=Entries(20,160001);Check(BuffIconStore.Apply(list,1,100000,1000),"first snapshot");var visible=new List<BuffIconEntry>();BuffIconStore.CopyVisible(visible,1000);Check(visible.Count==16&&visible[0].Id==1&&visible[15].Id==16,"order/cap");
  Check(BuffIconStore.Remaining(visible[0],1000)==61&&BuffIconStore.FormatTime(61)=="2p","ceiling minute");Check(BuffIconStore.FormatTime(60)=="1p"&&BuffIconStore.FormatTime(59)=="59s"&&BuffIconStore.FormatTime(45)=="45s","format boundary");
  Check(BuffIconStore.Remaining(visible[0],21001)==40,"frame stall drifts");Check(BuffIconStore.Remaining(visible[0],61001)==0,"exact deadline");
  Check(!BuffIconStore.Apply(Entries(1,999999),1,100000,9000),"stale sequence accepted");
  var renew=Entries(20,200000);BuffIconStore.Apply(renew,2,120000,10000);BuffIconStore.CopyVisible(visible,10000);Check(visible.Count==16&&BuffIconStore.Remaining(visible[0],10000)==80,"renew duplicates/clock correction");
  renew=Entries(20,200000);renew.First(e=>e.Id==1).ExpiresAt=120000;BuffIconStore.Apply(renew,3,120000,10000);BuffIconStore.CopyVisible(visible,10000);Check(visible.Count==16&&visible[0].Id==2&&visible[15].Id==17,"expiry compaction/overflow");
  bool rejected=false;try{BuffIconStore.Apply(new List<BuffIconEntry>{renew[0],renew[0]},4,120000,10000);}catch(ArgumentException){rejected=true;}Check(rejected,"duplicate IDs");BuffIconStore.CopyVisible(visible,10000);Check(visible[0].Id==2,"invalid snapshot destroyed valid store");
  for(int zoom=1;zoom<=4;zoom++)foreach(var screen in new[]{new[]{1280,720},new[]{1920,1080},new[]{2340,1080},new[]{844,390}}){
   float w=(float)screen[0]/zoom,h=(float)screen[1]/zoom;var info=new Rect(6,3,120,28);var bar=new Rect(32,3,94,8);var target=new Rect(w/2,0,50,80);var actions=new Rect(w-110,2,34,34);
   var grid=BuffIconGrid.Resolve(info,bar,target,actions,w,h,zoom,8,40);Check(grid.Columns<=8&&grid.Count<=16,"grid limits");
   for(int i=0;i<grid.Count;i++){Rect c=grid.Cell(i);Check(c.x>=bar.x&&c.y>info.yMax&&c.xMax<target.xMin&&c.xMax<actions.xMin&&c.yMax<=h,"HUD collision");if(i>=grid.Columns)Check(c.y>=grid.Y+grid.RowHeight,"second row");}Check(grid.Cell(grid.Count).width==0,"overflow cell");
  }
  var overlap=BuffIconGrid.Resolve(new Rect(0,0,100,30),new Rect(30,0,100,8),new Rect(0,0,120,80),new Rect(500,0,34,34),640,360,2,8,16);Check(overlap.Count==0,"target covering buff origin must hide icons");
  foreach(int count in new[]{0,1,8,9,16,17,64}){var g=BuffIconGrid.Resolve(new Rect(0,0,100,30),new Rect(30,0,100,8),new Rect(),new Rect(500,0,34,34),640,360,2,8,count);Check(g.Count==Math.Min(16,count)&&g.Columns==8,"exact wide-screen 8x2 overflow policy");}
  var raw=File.ReadAllBytes(args[0]);var reader=new myReader(mSystem.convertToSbyte(raw));Check(reader.readByte()==2&&reader.readByte()==8,"Java packet header");BuffIconProtocol.BeginSession();BuffIconProtocol.Read(reader);BuffIconStore.CopyVisible(visible,BuffIconStore.Now);Check(BuffIconStore.Ready&&visible.Count==8&&visible[0].Name=="Hợp thể cứng"&&visible.Any(e=>e.Name=="Bổ huyết"),"real Java packet UTF/layout");
  var nonBuff=new Message(new byte[]{2,4,1,2});Check(!BuffIconProtocol.TryRead(nonBuff)&&nonBuff.reader().readByte()==2,"non-buff routing consumed notification bytes");
  Check(BuffIconProtocol.TryRead(new Message(raw)),"buff routing failed");
  reader=new myReader(mSystem.convertToSbyte(raw.Take(raw.Length-1).ToArray()));reader.readByte();reader.readByte();BuffIconProtocol.Read(reader);BuffIconStore.CopyVisible(visible,BuffIconStore.Now);Check(visible.Count==8,"truncated packet cleared icons");
  BuffIconProtocol.BeginSession();Check(!BuffIconStore.Ready,"account switch leaked old buffs");BuffIconProtocol.Update();Check(Service.gI().requests==1,"login snapshot request");BuffIconProtocol.Update();Check(Service.gI().requests==1,"request spam");TileMap.mapID++;BuffIconProtocol.Update();Check(Service.gI().requests==2,"map resync");BuffIconProtocol.RequestResync();BuffIconProtocol.Update();Check(Service.gI().requests==3,"resume resync");
  HudLayout.Register("top-info.bounds",()=>new Rect(6,3,150,28),0,null,false);HudLayout.Register("top-info.hp",()=>new Rect(40,3,125,8),0,null,false);HudLayout.Register("target-info.bounds",()=>new Rect(230,0,100,80),0,null,false);HudLayout.Register("hub",()=>new Rect(520,2,34,34),40);
  mGraphics.zoomLevel=2;BuffIconsHud.Register();Apply(20,700000);Rect first=HudLayout.GetRect("buff-icons.slot.0");Check(HudLayout.GetRect("buff-icons.slot.15").width>0,"second row missing");
  Click((int)first.center.x,(int)first.center.y);Check(HudLayout.GetRect("buff-icons.tooltip").width>0&&BuffIconsHud.ConsumesInput,"icon click did not open/consume");Click(600,300);Check(HudLayout.GetRect("buff-icons.tooltip").width==0&&BuffIconsHud.ConsumesInput,"outside click did not close/consume");
  BuffIconsHud.Paint(new mGraphics());Check(InventoryItemVisuals.icons==16&&InventoryFrameSkin.gold==1,"paint cap/gold fusion outline");
  Click((int)first.center.x,(int)first.center.y);GameCanvas.panel.isShow=true;BuffIconsHud.UpdateInput();GameCanvas.panel.isShow=false;Check(HudLayout.GetRect("buff-icons.tooltip").width==0,"modal kept stale tooltip");
  var item=new ItemTime(433,120);Char.vItemTime.addElement(item);typeof(ItemTime).GetField("expiresAt",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(item,BuffIconStore.Now-1);item.update();Check(item.coutTime==0&&Char.vItemTime.size()==0,"legacy icon did not expire exactly");item.initTime(45);Check(item.time==45,"legacy renewal stale duration");
  Console.WriteLine("PASS: "+checks+" production client buff store/protocol/PC-S22 grid/input/legacy timer checks");
 }
}
"""
cs=OUT/"BuffTests.cs";cs.write_text(fixture,encoding="utf-8")
exe=OUT/"BuffTests.exe"
scripts=ROOT/"Assets/Scripts"
sources=[scripts/"Hud"/(name+".cs") for name in ["BuffIconStore","BuffIconProtocol","BuffIconGrid","BuffIconsHud"]]+[scripts/"myReader.cs",scripts/"ItemTime.cs",cs]
subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe","/nologo","/target:exe",f"/out:{exe}",*map(str,sources)],check=True)
subprocess.run([str(exe),str(packet)],check=True)
