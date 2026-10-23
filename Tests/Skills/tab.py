"""Run the real skill panel/state/registry with pointer and transport fixtures."""
from pathlib import Path
import ast
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
tree = ast.parse((ROOT / "Tests/Inventory/run.py").read_text(encoding="utf-8-sig"))
base = next(ast.literal_eval(n.value) for n in tree.body if isinstance(n, ast.Assign)
            and any(isinstance(t, ast.Name) and t.id == "fixture" for t in n.targets))
fixture = base[:base.index("partial class Item")]
fixture = fixture.replace("public static float Max(float a,float b)", "public static int Clamp(int n,int lo,int hi)=>Math.Max(lo,Math.Min(n,hi)); public static float Max(float a,float b)")
fixture += r'''
class Item { public const int OPT_LVITEM=72,OPT_STARSLOT=107;public ItemTemplate template;public int quantity;public ItemOption[] itemOption; }
class ItemTemplate {public short id;}
class ItemOption {public ItemOptionTemplate optionTemplate;public int param;}
class ItemOptionTemplate {public int id;}
class Image {}
static class InventoryBag {public const int Columns=5;}
static class InventoryFrameSkin {public static Image Corners(int a,int b,byte c)=>null;}
static class InventoryItemVisuals {public static void PaintSkillIcon(mGraphics g,int id,Rect bounds,Rect viewport,bool selected=false){}public static void PaintNativeButton(mGraphics g,Rect r,bool enabled,bool selected){}public static void PaintCompactText(mGraphics g,mFont f,string text,Rect bounds,Rect viewport,int color,bool center=false){if(bounds.yMax>viewport.y&&bounds.y<viewport.yMax)g.texts.Add(text);}public static void PaintCompactIcon(mGraphics g,int id,Rect bounds,Rect viewport){}public static void PaintLevel(mGraphics g,Rect a,Rect b,int l,bool bag,bool compact=false){} }
class PopUp {public static void paintPopUp(mGraphics g,int a,int b,int c,int d,int e,bool f,Image corner=null,int border=0){} }
class mGraphics {
 public static int zoomLevel=2,addYWhenOpenKeyBoard;public List<string> texts=new List<string>();
 public const int HCENTER=1,VCENTER=2;
 public void setColor(int c){}public void setColor(int c,float alpha){} public void fillRect(int a,int b,int c,int d){} public void setClip(int a,int b,int c,int d){}
 public void drawLine(int a,int b,int c,int d){}public void drawImage(Image image,int x,int y,int anchor){}
}
class mFont {
 public const int LEFT=0,CENTER=2;
 public static mFont tahoma_7_yellow=new mFont(),tahoma_7=new mFont(),tahoma_7b_white=new mFont(),tahoma_7_green2=new mFont(),tahoma_7b_dark=new mFont(),tahoma_7b_blue=new mFont(),tahoma_7_grey=new mFont();
 public static int widthCalls;public int getWidth(string s){widthCalls++;return s.Length*5;}public int getHeight()=>10;
 public string[] splitFontArray(string s,int width){var result=new List<string>();int count=Math.Max(1,width/5);while(s.Length>count){result.Add(s.Substring(0,count));s=s.Substring(count);}result.Add(s);return result.ToArray();}
 public void drawString(mGraphics g,string s,int x,int y,int align){g.texts.Add(s);}
}
static class SmallImage {public static void drawSmallImage(mGraphics g,int id,int x,int y,int flip,int anchor){} }
class SkillTemplate {public sbyte id;public string name="Kamejoko",damInfo="Sát thương # %";public int maxPoint=7,iconId,manaUseType=1;public string[] description={"Mô tả kỹ năng"};public Skill[] skills;}
class Skill {public SkillTemplate template;public string moreInfo="Thông tin cấp từ dữ liệu game";public int point=1,manaUse=20,damage;public long powRequire=5000000000L;public string strTimeReplay()=>"8";}
class NClass {public SkillTemplate[] availableSkillTemplates;}
class MovePoint {public int xEnd,yEnd;public MovePoint(int x,int y){xEnd=x;yEnd=y;}}
class Npc {public int cx,cy;}
class Char {
 static Char current=new Char();public static Char myCharz()=>current;
 public NClass nClass=new NClass();public Skill[] learned=new Skill[8];public Item[] arrItemBag=new Item[80];
 public int cgender,cx,cy,cHP=100,statusMe,cHPGoc=50100,cMPGoc=30200,cDamGoc=5010,cDefGoc,cCriticalGoc,cNangdong;
 public long cTiemNang=9999999999;public bool meDead;
 public object mobFocus,charFocus,itemFocus,endMovePointCommand;public Npc npcFocus;public MovePoint currentMovePoint;
 public static bool isLoadingMap,ischangingMap;
 public Skill getSkill(SkillTemplate t){foreach(Skill s in learned)if(s?.template==t)return s;return null;}
}
static class Main {public static bool isPC;}
static class mSystem {public static long now=100000;public static long currentTimeMillis()=>now;}
static class mResources {public const string YES="Có",CANCEL="Hủy",can_not_do_when_die="Đã chết",potential="Tiềm năng",active_point="Năng động",HP="HP",KI="KI",hit_point="Sức đánh",armor="Giáp",critical="Chí mạng",root="gốc",KI_consume="KI: ",cooldown="Hồi chiêu";}
static class NinjaUtil {public static string getMoneys(long value)=>value.ToString();public static string replace(string s,string from,string to)=>s.Replace(from,to);}
interface IActionListener {void perform(int action,object parameter);}
class Command {
 public static Image btn1left,btn1mid,btn1right,btn0left,btn0mid,btn0right;
 public IActionListener target;public int action;
 public Command(string text,IActionListener target,int action,object parameter){this.target=target;this.action=action;}
 public static void paintOngMau(Image a,Image b,Image c,int x,int y,int w,mGraphics g){}
 public void Click()=>target.perform(action,null);
}
class Menu {public bool showMenu;}
static class GameCanvas {
 public static int w=640,h=360,px,py,pxMouse,pyMouse;public static bool[] keyPressed=new bool[30];
 public static bool isPointerJustDown,isPointerJustRelease,isPointerClick,isPointerDown;
 public static object currentDialog,currentScreen=GameScr.instance;public static Menu menu=new Menu();public static Panel panel=new Panel();
 public static Command yes,no;
 public static void endDlg(){currentDialog=null;}
 public static void startYesNoDlg(string s,Command y,Command n){currentDialog=new object();yes=y;no=n;}
 public static void startOKDlg(string s){currentDialog=new object();}
 public static void clearAllPointerEvent(){isPointerJustDown=isPointerJustRelease=isPointerClick=isPointerDown=false;}
}
class Service {
 public static Service current=new Service();public static Service gI()=>current;
 public List<long[]> requests=new List<long[]>();public int opened;
 public void skillTabRequest(int action,int value=0,int extra=0,long token=0){requests.Add(new long[]{action,value,extra,token});}
 public void openMenu(int id){opened++;}
}
static class TileMap {public static int mapID=5;}
namespace Mod.CuongLe {public static class AutoMap {public static int IdMapEnd;public static bool isXmaping;public static void FinishXmap(){isXmaping=false;}}}
class Message {
 public Queue<long> values;public Message(params long[] v){values=new Queue<long>(v);}public Message reader()=>this;
 public sbyte readByte()=>(sbyte)values.Dequeue();public short readShort()=>(short)values.Dequeue();public int readInt()=>(int)values.Dequeue();public long readLong()=>values.Dequeue();
}
namespace Notifications {public static class NotificationCenter {public static bool IsOpen;}}
class GamePad {public Rect HudVisualBounds=>new Rect();}
static class CombatMobileHud {public const string JoystickTouch="stick";}
static class CombatPcHud {public static bool Visible=>false;}
class GameScr {
 public static GameScr instance=new GameScr();public static GameScr gI()=>instance;public static int isAnalog;public static GamePad gamePad;
 public static Skill[] onScreenSkill,keySkill;public static Npc npc;
 public static Npc findNPCInMap(short id)=>npc;
 public static void RegisterHudRects(){InventoryFrame.Register();SkillPanelLayout.Register();}
 public void onOSkill(sbyte[] order){onScreenSkill=Bind(order);}public void onKSkill(sbyte[] order){keySkill=Bind(order);}
 static Skill[] Bind(sbyte[] ids){var a=new Skill[8];for(int i=0;i<8;i++)foreach(Skill s in Char.myCharz().learned)if(s?.template.id==ids[i])a[i]=s;return a;}
}
partial class Panel {
 public bool InventoryFrameActive=true,isShow=true,InventoryBagDetailOpen;
 public int currentTabIndex=2,cmy,cmtoY,cmyLim,currentListLength,selected,waitToPerform,ITEM_HEIGHT;
 public object cp;internal Image imgX;public static string specialInfo;public static short spearcialImage;public static long[] t_tiemnang={50000000,250000000,1250000000};
 public int InventoryFrameScroll=>cmy;public int[] InventoryFrameTabIds=>new[]{0,1,2,3,6};public int InventoryBagCellCount=>0;
 public Rect InventoryDetailUpperBounds=>new Rect();public Rect InventoryDetailLowerBounds=>new Rect();public int InventoryDetailButtonCount=>0;
 public Rect InventoryDetailButtonBounds(int n)=>new Rect();
 private void CloseInventoryFrame(){isShow=false;}private void SelectInventoryFrameTab(int id){currentTabIndex=id;}
 private void doFireSkill(){}public void hide(){isShow=false;}
 public void Show(){currentTabIndex=2;isShow=true;OpenSkillPanel();HudLayout.Invalidate();}
 public void Tick(){HudLayout.BeginFrame();UpdateSkillPanelInput();}
 internal void Paint(mGraphics g){PaintSkillPanel(g);PaintSkillDetail(g);}
 public void Wheel(int n){ScrollSkillPanelMouse(n);}
}
class SkillTabTests {
 static int checks;
 static void Check(bool b,string s){checks++;if(!b)throw new Exception(s);}
 static void Near(float a,float b,string s)=>Check(Math.Abs(a-b)<.1,s);
 static void Click(Rect r){
  GameCanvas.px=(int)r.center.x;GameCanvas.py=(int)r.center.y;
  GameCanvas.isPointerJustDown=GameCanvas.isPointerDown=GameCanvas.isPointerClick=true;GameCanvas.panel.Tick();
  GameCanvas.isPointerDown=false;GameCanvas.isPointerJustRelease=GameCanvas.isPointerClick=true;GameCanvas.panel.Tick();GameCanvas.clearAllPointerEvent();
 }
 static void State(int[] order,int studying=-1){
  var data=new List<long>{0,studying<0?-1:456789,studying<0?0:30000,studying<0?0:900000,studying<0?-1:94,studying,studying<0?0:2,studying<0?0:5};
  foreach(int id in order)data.Add(id);SkillTabState.Read(new Message(data.ToArray()));
 }
 static void Main(){
  int[] ids={0,1,6,9,10,20,22,19};var c=Char.myCharz();c.nClass.availableSkillTemplates=new SkillTemplate[8];
  for(int i=0;i<8;i++){
   var t=new SkillTemplate{id=(sbyte)ids[i]};t.skills=new Skill[7];for(int n=0;n<7;n++)t.skills[n]=new Skill{template=t,point=n+1,damage=100+n*15};
   c.nClass.availableSkillTemplates[i]=t;c.learned[i]=t.skills[0];
  }
  foreach(bool mobile in new[]{false,true})foreach(int zoom in new[]{2,3,4})foreach(int[] viewport in mobile?new[]{new[]{422,195},new[]{585,270},new[]{844,390}}:new[]{new[]{640,360},new[]{960,540},new[]{1280,720}}){
   global::Main.isPC=!mobile;UnityEngine.Device.Application.platform=mobile?RuntimePlatform.Android:RuntimePlatform.WindowsPlayer;
   GameCanvas.w=viewport[0];GameCanvas.h=viewport[1];mGraphics.zoomLevel=zoom;SkillTabState.Reset();GameCanvas.panel.Show();
   Check(SkillTabState.Waiting,"initial request must have bounded loading state");
   foreach(Skill learned in c.learned)learned.point=7;
   var maxPaint=new mGraphics();GameCanvas.panel.Paint(maxPaint);
   Check(maxPaint.texts.Contains("Đã tối đa")&&!maxPaint.texts.Contains("Đang tải..."),"max level waits for server");
   foreach(Skill learned in c.learned)learned.point=1;
   mSystem.now+=2001;SkillTabState.Update();
   Check(!SkillTabState.Ready&&!SkillTabState.Waiting,"retries extend loading deadline");
   var fallback=new mGraphics();GameCanvas.panel.Paint(fallback);
   Check(fallback.texts.Contains("Đến NPC"),"missing server response has no NPC fallback");
   foreach(SkillTemplate template in c.nClass.availableSkillTemplates)template.description=new[]{"null"," ","Mô tả thật"};
   State(ids);HudLayout.BeginFrame();
   Check(!GameCanvas.panel.SkillPanelActive||mobile||GameCanvas.h<540||GameCanvas.panel.cmyLim==0,"tall PC unnecessarily scrolls potential");
   Rect root=HudLayout.GetRect(InventoryFrame.Root),detail=HudLayout.GetRect(SkillPanelLayout.Detail),money=HudLayout.GetRect(InventoryFrame.Money);
   Near(root.height,GameCanvas.h,"left not full height");Near(detail.height,GameCanvas.h-2*InventoryFrame.FrameInset,"detail frame inset");
   Check(root.xMax<detail.x,"panels overlap");
   mFont.widthCalls=0;
   for(int repeat=0;repeat<100;repeat++){for(int slotIndex=0;slotIndex<8;slotIndex++)_ = SkillPanelLayout.CellBounds(slotIndex);for(int rowIndex=0;rowIndex<5;rowIndex++)_ = SkillPanelLayout.PotentialBounds(rowIndex);}
   Check(mFont.widthCalls==0,"unchanged layout repeated font measurement: "+mFont.widthCalls);
   long oldPotential=c.cTiemNang;c.cTiemNang++;
   Check(SkillPanelLayout.PotentialTitle.Contains(c.cTiemNang.ToString()),"potential update did not refresh cached layout data");
   c.cTiemNang=oldPotential;
   Check(SkillPanelLayout.PotentialGroupBounds.y>=SkillPanelLayout.SkillsGroupBounds.yMax+SkillPanelLayout.Gap,"skill and potential groups are not separated");
   Near(SkillPanelLayout.PotentialGroupBounds.x,SkillPanelLayout.SkillsGroupBounds.x,"group borders misaligned");
   Check(SkillPanelLayout.SkillsGroupBounds.yMax>=SkillPanelLayout.IntrinsicBounds.yMax+InventoryFrame.Padding,"intrinsic outside skill group");
   Check(SkillPanelLayout.Size<=SkillPanelLayout.DesignSize,"responsive skill frames exceed design size");
   for(int i=0;i<8;i++){
    Rect cell=HudLayout.GetRect(SkillPanelLayout.Cell(i));Check(cell.width>0&&cell.height>0,"missing grid cell");
    Check(HudLayout.HitTest(cell.center)==SkillPanelLayout.Cell(i),"cell not clickable");
    Near(SkillPanelLayout.CellBounds(i).y,SkillPanelLayout.CellBounds(i/4*4).y,"not a four-column grid");
    Rect key=HudLayout.GetRect(SkillPanelLayout.Key(i));Check(key.x>=detail.x&&key.xMax<=detail.xMax&&key.yMax<SkillPanelLayout.ActionBounds(0).y,"key outside detail/overlaps action");
    Check(HudLayout.HitTest(key.center)==SkillPanelLayout.Key(i),"assignment key not clickable");
   }
   var g=new mGraphics();GameCanvas.panel.Paint(g);Check(!g.texts.Contains("null"),"null description rendered");Check(g.texts.Exists(t=>t.StartsWith("Cấp 1")),"detail missing real current level");
   Service.gI().requests.Clear();Click(SkillPanelLayout.CellBounds(1));Click(SkillPanelLayout.KeyBounds(6));
   var req=Service.gI().requests[0];Check(req[0]==2&&req[1]==1&&req[2]==6,"swap selected wrong template or slot");
   var swapped=(int[])ids.Clone();swapped[1]=ids[6];swapped[6]=1;State(swapped);
   for(int i=0;i<8;i++)Check(SkillTabState.Templates()[i].id==ids[i],"shortcut swap reordered original skill grid");
   Check(SkillTabState.SlotFor(1)==6&&SkillTabState.SlotFor(22)==1&&GameScr.onScreenSkill[6].template.id==1&&GameScr.keySkill[1].template.id==22,"actual slot highlight/number diverged from HUD");
   c.arrItemBag[3]=new Item{template=new ItemTemplate{id=95},quantity=1};
   Service.gI().requests.Clear();Click(SkillPanelLayout.ActionBounds(0));Check(Service.gI().requests[0][0]==3&&Service.gI().requests[0][1]==95,"next book action wrong");
   c.arrItemBag[3]=null;State(swapped,1);Service.gI().requests.Clear();Click(SkillPanelLayout.ActionBounds(0));
   Check(GameCanvas.currentDialog!=null&&Service.gI().requests.Count==0,"quick study skipped confirmation");
   GameCanvas.no.Click();Check(Service.gI().requests.Count==0,"cancel sent quick request");
   Click(SkillPanelLayout.ActionBounds(0));GameCanvas.yes.Click();req=Service.gI().requests[0];Check(req[0]==1&&req[1]==94&&req[2]==5&&req[3]==456789,"confirmation lost captured quote/token");
   mSystem.now+=1000;Check(SkillTabState.Remaining==29000,"countdown uses incorrect clock");
   float fixedGridY=SkillPanelLayout.CellBounds(0).y,fixedIntrinsicY=SkillPanelLayout.IntrinsicBounds.y,fixedPotentialHeaderY=SkillPanelLayout.PotentialHeaderBounds.y;
   GameCanvas.panel.Paint(new mGraphics());GameCanvas.pxMouse=(int)root.center.x;GameCanvas.pyMouse=(int)SkillPanelLayout.ScrollBounds.center.y;GameCanvas.panel.Wheel(-1000);
   int sharedScroll=GameCanvas.panel.cmy;
   Near(SkillPanelLayout.CellBounds(0).y,fixedGridY-sharedScroll,"grid does not share scroll offset");
   Near(SkillPanelLayout.IntrinsicBounds.y,fixedIntrinsicY-sharedScroll,"intrinsic does not share scroll offset");
   Near(SkillPanelLayout.PotentialHeaderBounds.y,fixedPotentialHeaderY-sharedScroll,"potential header does not share scroll offset");
   Check(HudLayout.GetRect(SkillPanelLayout.Cell(0)).height==0||HudLayout.GetRect(SkillPanelLayout.Cell(0)).y>=SkillPanelLayout.ScrollBounds.y,"hidden skill remains clickable above viewport");
   Near(HudLayout.GetRect(InventoryFrame.Money).y,money.y,"scroll moved currency footer");
   int potentialScroll=GameCanvas.panel.cmy;
   GameCanvas.pyMouse=(int)SkillPanelLayout.ScrollBounds.center.y;GameCanvas.panel.Wheel(1);
   Check(GameCanvas.panel.cmy==Math.Max(0,potentialScroll-24),"wheel does not scroll the whole content");
   GameCanvas.panel.Wheel(-1000);
   HudLayout.Invalidate();Rect last=HudLayout.GetRect(SkillPanelLayout.Potential(4));Check(last.height>0&&last.yMax<=money.y,"potential rows not reachable by scrolling");
   Check(SkillPanelLayout.ScrollBounds.yMax<=money.y,"scroll viewport overlaps currency");
   Check(SkillPanelLayout.PotentialBounds(4).yMax<=SkillPanelLayout.ScrollBounds.yMax,"last stat is not fully reachable");
   Check(SkillPanelLayout.UpperBounds.yMax<SkillPanelLayout.ControlsTop,"detail body overlaps fixed controls");
   Near(SkillPanelLayout.ScrollBounds.height,SkillPanelLayout.Frame.height,"scroll does not use the full left viewport");
   Near(SkillPanelLayout.CellBounds(0).x+SkillPanelLayout.CellBounds(3).xMax,SkillPanelLayout.Content.center.x*2,"grid not centered");
   for(int i=0;i<3;i++)Near(SkillPanelLayout.CellBounds(i+1).x-SkillPanelLayout.CellBounds(i).xMax,SkillPanelLayout.CellBounds(0).x-SkillPanelLayout.Content.x,"skill columns not spread evenly across panel");
   Check(SkillPanelLayout.CellBounds(4).y-SkillPanelLayout.CellBounds(0).yMax>=7,"row gap crowds Lv badges");
   Near(SkillPanelLayout.DetailContent.x-SkillPanelLayout.DetailBounds.x,InventoryFrame.Padding,"right panel padding");
   Near(SkillPanelLayout.Content.x-SkillPanelLayout.Frame.x,InventoryFrame.Padding,"left panel padding");
   for(int i=0;i<5;i++)Check(SkillPanelLayout.PotentialBounds(i).height>=SkillPanelLayout.IntrinsicBounds.height&&SkillPanelLayout.PotentialBounds(i).height>=2*SkillPanelLayout.LineHeight+2,"compact stat row cannot contain two text lines");
   GameCanvas.panel.cmy=GameCanvas.panel.cmtoY=0;HudLayout.Invalidate();
   foreach(int origin in new[]{0,1,2}) {
    GameCanvas.panel.cmy=GameCanvas.panel.cmtoY=0;HudLayout.Invalidate();
    Rect drag=origin==0?SkillPanelLayout.CellBounds(0):origin==1?SkillPanelLayout.IntrinsicBounds:SkillPanelLayout.PotentialHeaderBounds;
    Rect visibleDrag=InventoryFrame.Intersection(drag,SkillPanelLayout.ScrollBounds);
    if(visibleDrag.height<2||GameCanvas.panel.cmyLim==0)continue;
    int beforeSkill=Service.gI().requests.Count;
    GameCanvas.px=(int)visibleDrag.center.x;GameCanvas.py=(int)visibleDrag.center.y;
    GameCanvas.isPointerJustDown=GameCanvas.isPointerDown=GameCanvas.isPointerClick=true;GameCanvas.panel.Tick();
    GameCanvas.py-=10;GameCanvas.panel.Tick();
    GameCanvas.isPointerDown=false;GameCanvas.isPointerJustRelease=GameCanvas.isPointerClick=true;GameCanvas.panel.Tick();GameCanvas.clearAllPointerEvent();
    Check(GameCanvas.panel.cmy==Math.Min(10,GameCanvas.panel.cmyLim),"drag origin failed to scroll all content: "+origin);
    Check(Service.gI().requests.Count==beforeSkill,"drag fired a click/action");
   }
   foreach(SkillTemplate template in c.nClass.availableSkillTemplates)template.description=new[]{"null"};
   GameCanvas.pxMouse=(int)detail.center.x;GameCanvas.pyMouse=(int)SkillPanelLayout.UpperBounds.center.y;GameCanvas.panel.Wheel(-1000);
   var descriptionPaint=new mGraphics();GameCanvas.panel.Paint(descriptionPaint);
   Check(descriptionPaint.texts.Exists(text=>text.Contains("Thông tin cấp")),"real per-level description missing");
  }
  Check(SkillTabState.BookId(2,7)==86&&SkillTabState.BookId(2,6)==84,"Demon book gap lost");
  Check(SkillTabState.Clock(90061000)=="25:01:01","long countdown wrapped at 24 hours");
  GameCanvas.panel.Show();TileMap.mapID=5;GameScr.npc=new Npc{cx=c.cx,cy=c.cy};SkillTabState.GoToTeacher();SkillTabState.Update();
  Check(Service.gI().opened>0,"arrival did not open existing teacher NPC");
  Console.WriteLine("PASS: "+checks+" B1 client checks (real panel/state/hitboxes; PC and S22 viewports)");
 }
}
'''
output = ROOT / "Build/skill-tab-tests"
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="b1-", dir=output) as folder:
    path = Path(folder)
    frame = (ROOT / "Assets/Scripts/Hud/InventoryFrame.cs").read_text(encoding="utf-8-sig")
    (path / "InventoryRegistry.cs").write_text(frame[:frame.index("public partial class Panel")], encoding="utf-8")
    (path / "SkillTabTests.cs").write_text(fixture, encoding="utf-8")
    exe = path / "SkillTabTests.exe"
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", "/target:exe", f"/out:{exe}",
                    str(path / "InventoryRegistry.cs"), str(path / "SkillTabTests.cs"),
                    *[str(ROOT / "Assets/Scripts/Hud" / name) for name in ["HudLayout.cs", "SkillPanel.cs", "SkillTabState.cs"]]], check=True)
    subprocess.run([str(exe)], check=True)
