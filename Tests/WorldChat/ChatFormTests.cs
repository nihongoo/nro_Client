using System;
using System.Collections.Generic;
using System.Reflection;
using WorldChat;
using UnityEngine;

// Compile the actual ChatTextField/composer/store; substitute rendering, keyboard and transport only.
namespace UnityEngine {
    public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
    public struct Rect {
        public float x,y,width,height; public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}
        public Vector2 center => new Vector2(x+width/2,y+height/2);
        public bool Contains(Vector2 p)=>p.x>=x&&p.x<x+width&&p.y>=y&&p.y<y+height;
    }
}
public delegate void ActionChat(string text);
public interface IActionListener { void perform(int action,object value); }
public interface IChatable { void onChatFromMe(string text,string to); void onCancelChat(); }
public class Command {
    public string caption; public int x,y; public ActionChat actionChat; private IActionListener listener; private int action;
    public Command(){} public Command(string c,IActionListener l,int a,object p){caption=c;listener=l;action=a;}
    public Command(string c,IActionListener l,int a,object p,int x,int y):this(c,l,a,p){this.x=x;this.y=y;}
    public void performAction(){listener.perform(action,null);}
}
public class FakeKeyboard { public bool active=true; public string text; }
public class TField {
    public static FakeKeyboard kb; public int width,height,x,y; public bool isFocus,isPaintMouse,showSubTextField,justReturnFromTextBox;
    public string name,strInfo; private string text=""; public int limit;
    public string getText()=>text; public void setText(string t){text=t;} public void clear(){text="";}
    public void clearAll(){} public void clearKb(){if(kb!=null)kb.text="";}
    public void insertText(string t){text+=t;} public int getMaxTextLenght()=>limit;
    public void setMaxTextLenght(int n){limit=n;} public void setFocusWithKb(bool f){isFocus=f;}
    public void keyPressed(int k){if(k==204)clear();else if(k>0)text+=(char)k;} public void update(){} public void paint(mGraphics g){}
}
public static class Main { public static bool isPC=true,isWindowsPhone,isIPhone; }
public static class mScreen { public static int ITEM_HEIGHT=20,cmdH=26; public static bool getCmdPointerLast(Command c)=>GameCanvas.isPointerClick&&GameCanvas.isPointerJustRelease&&GameCanvas.px>=c.x&&GameCanvas.px<c.x+68&&GameCanvas.py>=c.y&&GameCanvas.py<c.y+cmdH; }
public static class mSystem { public static long time=100000; public static long currentTimeMillis()=>time; }
public static class mResources { public static string OK="OK",CLOSE="Đóng",DELETE="Xóa"; }
public static class ipKeyboard { public const int TEXT=0; public static void openKeyBoard(string t,int k,string v,Command c){} }
public static class Cout { public static void LogError(string s){} }
public class mGraphics { }
public class mFont {
    public static mFont tahoma_7b_green2=new mFont(),tahoma_7_green2=new mFont(),tahoma_7_grey=new mFont();
    public static List<string> drawn=new List<string>(); public int getHeight()=>10;
    public void drawString(mGraphics g,string t,int x,int y,int a){drawn.Add(t);}
}
public static class PopUp { public static void paintPopUp(mGraphics g,int x,int y,int w,int h,int color,bool b){} }
namespace Notifications { public static class NotificationSkin { public static void PaintFrame(mGraphics g,int x,int y,int w,int h,bool b){} } }
public class Paint { public void paintCmdBar(mGraphics g,Command l,Command c,Command r){} }
public class MsgDlg {
    public string text; public Command left,center,right;
    public void setInfo(string t,Command l,Command c,Command r){text=t;left=l;center=c;right=r;}
    public void show(){GameCanvas.currentDialog=this;}
}
public static class GameCanvas {
    public static int w=480,h=270,px,py,keyAsciiPress; public static bool isTouch=true,isPointerJustRelease,isPointerClick,isPointerJustDown,isPointerDown,isPointerMove;
    public static bool[] keyPressed=new bool[32]; public static object currentScreen=GameScr.instance,currentDialog;
    public static MsgDlg msgdlg=new MsgDlg(); public static Paint paintz=new Paint();
    public static void endDlg(){currentDialog=null;} public static void closeKeyBoard(){if(TField.kb!=null)TField.kb.active=false;}
    public static void clearAllPointerEvent(){isPointerJustRelease=isPointerClick=isPointerJustDown=isPointerDown=false;}
    public static void clearKeyHold(){} public static void clearKeyPressed(){Array.Clear(keyPressed,0,keyPressed.Length);}
    public static void startYesNoDlg(string t,Command yes,Command no){msgdlg.setInfo(t,yes,null,no);currentDialog=msgdlg;}
}
public class Info { public void addInfo(string t,int c){} }
public class GameScr:IChatable {
    public static GameScr instance=new GameScr(); public static GameScr gI()=>instance; public static Info info1=new Info();
    public int normalCalls; public bool vip;
    public void onChatFromMe(string t,string target){normalCalls++;if(t=="/admin")vip=!vip;}
    public void onCancelChat(){}
}
public class Char { private static Char me=new Char(); public static Char myCharz()=>me; public int luong=1000;public long xu=1000000000; }
public class Service {
    private static Service i=new Service(); public static Service gI()=>i; public int sends; public long id; public string text; public bool connected=true;
    public bool submitWorldChat(long request,string value){if(!connected)return false;sends++;id=request;text=value;return true;}
}
namespace WorldChat { public static class WorldChatProtocol { public static void RequestSnapshot(){} } }
public static class ChatFormTests {
    private static int assertions;
    private static void Check(bool b,string t){assertions++;if(!b)throw new Exception(t);}
    private static void Policy(){WorldChatStore.Snapshot(0,1000,17,123456,100,5000,1000);}
    private static void Tab(ChatTextField f,bool world){
        Rect r=(Rect)typeof(ChatTextField).GetMethod("TabBounds",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,new object[]{world});
        Tap(f,(int)r.center.x,(int)r.center.y);
    }
    private static void Tap(ChatTextField f,int x,int y){
        GameCanvas.px=x;GameCanvas.py=y;GameCanvas.isPointerMove=false;
        GameCanvas.isPointerJustDown=GameCanvas.isPointerDown=GameCanvas.isPointerClick=true;
        f.UpdateModalInput(); // press frame: gameplay must see no pointer
        Check(!GameCanvas.isPointerDown&&!GameCanvas.isPointerClick,"press leaked to gameplay");
        f.UpdateModalInput(); // a held frame with the canvas flags already consumed
        GameCanvas.isPointerJustRelease=true;f.UpdateModalInput();
        Check(!GameCanvas.isPointerJustRelease&&!GameCanvas.isPointerClick,"release leaked to gameplay");
    }
    public static void Main(){
        WorldChatStore.BeginSession();Policy();var f=ChatTextField.gI();f.tfChat.y=210;f.initChatTextField();
        f.startChat(GameScr.instance,"");f.tfChat.setText("admin normal");f.perform(8000,null);
        Check(GameScr.instance.normalCalls==1&&!GameScr.instance.vip,"normal content treated as command");
        Check(Service.gI().sends==0&&GameCanvas.currentDialog==null,"normal opened payment/sent world");
        f.OpenWorldChat();f.tfChat.setText("admin world");TField.kb=new FakeKeyboard{text="admin world"};f.perform(8000,null);
        Check(GameCanvas.currentDialog==GameCanvas.msgdlg&&!f.isShow,"OK did not show confirmation");
        Check(GameCanvas.msgdlg.text.Contains("17 gem")&&GameCanvas.msgdlg.text.Contains("123.456"),"popup ignores snapshot price");
        Check(GameScr.instance.normalCalls==1&&Service.gI().sends==0,"world routed through admin before consent");
        Check(TField.kb==null,"keyboard can erase draft on return");
        GameCanvas.msgdlg.right.performAction();
        Check(f.isShow&&f.tfChat.getText()=="admin world"&&Service.gI().sends==0,"decline lost draft or sent");
        f.perform(8000,null);GameCanvas.msgdlg.left.performAction();
        Check(Service.gI().sends==1&&Service.gI().text=="admin world"&&GameScr.instance.normalCalls==1,"agree routing");
        Check(f.tfChat.getText()=="admin world","draft cleared before result");
        WorldChatStore.Result(Service.gI().id,true,"ok",1000,1000);f.HandleWorldResult();
        Check(f.tfChat.getText()==""&&!f.isShow,"accepted result did not clear/close sent draft");
        f.OpenWorldChat();
        f.tfChat.setText("admin native");f.cmdChat.actionChat("admin native");
        Check(Service.gI().sends==1&&GameScr.instance.normalCalls==1&&GameCanvas.currentDialog!=null,"native callback bypassed confirmation");
        GameCanvas.msgdlg.right.performAction();Tab(f,false);mSystem.time+=1001;f.perform(8000,null);
        Check(GameScr.instance.normalCalls==2&&Service.gI().sends==1,"tab Normal failed");
        Tab(f,true);f.tfChat.setText("admin enter");f.tfChat.justReturnFromTextBox=true;f.update();
        Check(GameCanvas.currentDialog!=null&&Service.gI().sends==1&&GameScr.instance.normalCalls==2,"Enter bypassed world routing");
        GameCanvas.msgdlg.right.performAction();f.tfChat.setText(" ");f.perform(8000,null);
        Check(GameCanvas.msgdlg.text.Contains("trống")&&Service.gI().sends==1,"empty world not reported");GameCanvas.msgdlg.center.performAction();
        f.tfChat.setText(new string('x',101));f.perform(8000,null);Check(GameCanvas.msgdlg.text.Contains("100"),"over length not reported");GameCanvas.msgdlg.center.performAction();
        f.tfChat.setText("valid");Char.myCharz().luong=0;f.perform(8000,null);Check(GameCanvas.msgdlg.text.Contains("gem"),"missing gems");GameCanvas.msgdlg.center.performAction();Char.myCharz().luong=1000;
        Char.myCharz().xu=0;f.perform(8000,null);Check(GameCanvas.msgdlg.text.Contains("vàng"),"missing gold");GameCanvas.msgdlg.center.performAction();Char.myCharz().xu=1000000000;
        Service.gI().connected=false;f.perform(8000,null);GameCanvas.msgdlg.left.performAction();
        Check(WorldChatStore.PendingId==0&&GameCanvas.msgdlg.text.Contains("kết nối")&&f.tfChat.getText()=="valid","disconnect lost draft/locked pending");GameCanvas.msgdlg.center.performAction();Service.gI().connected=true;
        mFont.drawn.Clear();f.paint(new mGraphics());
        Check(mFont.drawn.Contains("Chat ")&&mFont.drawn.Contains("Thường")&&mFont.drawn.Contains("Thế giới"),"title/tabs missing");
        Check(!mFont.drawn.Exists(t=>t.Contains("gem")),"fee label leaked into form");
        f.perform(8000,null);GameCanvas.msgdlg.left.performAction();long pending=Service.gI().id;
        Tab(f,false);f.tfChat.setText("valid");WorldChatStore.Result(pending,true,"ok",1000,1000);f.HandleWorldResult();
        Check(f.tfChat.getText()=="valid","world receipt erased a Normal draft");
        f.OpenWorldChat();f.tfChat.setText("stay");f.perform(8000,null);GameCanvas.currentScreen=new object();GameCanvas.msgdlg.right.performAction();
        Check(!f.isShow,"popup callback reopened chat outside game");
        GameCanvas.currentScreen=GameScr.instance;f.OpenWorldChat();
        f.InsertDesktopText("Tiếng Việt, Ì ạch /admin");
        Check(f.tfChat.getText()=="Tiếng Việt, Ì ạch /admin","Unicode/slash input dropped characters");
        f.tfChat.setText("");f.tfChat.setMaxTextLenght(5);f.InsertDesktopText("\nabc/def");
        Check(f.tfChat.getText()=="abc/d","paste bypassed limit/control filtering");
        f.tfChat.setText("abcd");f.InsertDesktopText("😀");
        Check(f.tfChat.getText()=="abcd","paste split a surrogate at character limit");f.tfChat.setMaxTextLenght(200);

        GameCanvas.px=0;GameCanvas.py=0;GameCanvas.isPointerClick=GameCanvas.isPointerJustRelease=true;GameCanvas.keyPressed[1]=true;
        f.UpdateModalInput();
        Check(!GameCanvas.isPointerClick&&!GameCanvas.keyPressed[1]&&f.isShow,"chat click/key escaped to gameplay");
        int sendsBeforeReset=Service.gI().sends;WorldChatStore.BeginSession();ChatTextField.ResetWorldSession();
        Check(f.tfChat.getText()==""&&!f.isShow,"session retained a world draft in Normal mode");
        GameCanvas.currentScreen=GameScr.instance;f.startChat(GameScr.instance,"");f.tfChat.setText("new session");mSystem.time+=1001;f.perform(8000,null);
        Check(GameScr.instance.normalCalls==3&&Service.gI().sends==sendsBeforeReset,"session reset broke normal routing");
        Policy();f.OpenWorldChat();f.tfChat.setText("multi-frame button");
        Tap(f,f.left.x+10,f.left.y+10);
        Check(GameCanvas.currentDialog!=null&&!f.isShow,"multi-frame OK did not open confirmation");
        GameCanvas.msgdlg.right.performAction();
        Tap(f,f.right.x+10,f.right.y+10);
        Check(!f.isShow&&GameCanvas.currentDialog==null,"multi-frame Close did not close form");
        Console.WriteLine("PASS: "+assertions+" real ChatTextField assertions; tabs, consent/decline, native/Enter routes, admin isolation, snapshot fees, errors and draft ownership");
    }
}
