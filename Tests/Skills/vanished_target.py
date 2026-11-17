"""Replay mob-death packets and the real standing attack-key/doFire/mob-attack path."""
from pathlib import Path
import re,subprocess
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/"Build/buff-icons-tests";assert OUT.is_dir()
scripts=ROOT/"Assets/Scripts"
def block(text,marker):
 start=text.index(marker);opening=text.index("{",start);depth=0
 for token in re.finditer(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"|[{}]',text[opening:]):
  if token[0]=="{":depth+=1
  elif token[0]=="}":
   depth-=1
   if depth==0:return text[start:opening+token.end()]
 raise AssertionError(marker)
game=(scripts/"GameScr.cs").read_text(encoding="utf-8-sig");char=(scripts/"Char.cs").read_text(encoding="utf-8-sig");mob=(scripts/"Mob.cs").read_text(encoding="utf-8-sig");controller=(scripts/"Controller.cs").read_text(encoding="utf-8-sig")
attack=block(game,"public bool isAttack()")
attack=attack[:attack.index("        if (Char.myCharz().npcFocus != null)")]+"return false;}"
methods=block(game,"private void doFire(")+"\n"+block(game,"public bool isMeCanAttackMob(")+"\n"+attack
for name in ("internal void FireAttackInput(","internal static bool IsLiveMobTarget(","internal static bool IsLivePlayerTarget("):
 if name in game:methods+="\n"+block(game,name)
char_methods=""
for name in ("internal bool ClearInvalidAttackFocus(","internal void ForgetAttackMob(","private void CancelAttackApproach(","internal void RestoreRespawnFocus(","internal void ClearRespawnFocus(","public void focusManualTo("):
 if name in char:char_methods+="\n"+block(char,name)
if "private Mob rememberedRespawnMob;" in char:
 char_methods+="\n"+char[char.index("    private Mob rememberedRespawnMob;"):char.index("    private void CancelAttackApproach()")]
key=block(game,'if (GameCanvas.keyPressed[(!Main.isPC) ? 5 : 25])')
respawn=controller[controller.index("case -13:"):controller.index("case -75:",controller.index("case -13:"))]
death=controller[controller.index("case -12:"):controller.index("case 74:",controller.index("case -12:"))]
fixture=r'''
#pragma warning disable 0649
using System;using System.Collections.Generic;
class MyVector{public List<object> values=new List<object>();public void addElement(object o)=>values.Add(o);public object elementAt(int i)=>values[i];public int size()=>values.Count;public bool contains(object o)=>values.Contains(o);}
class SkillTemplate{public int id,type=1;}class Skill{public SkillTemplate template=new SkillTemplate();public int dx=32,dy=18,skillId;public bool paintCanNotUseSkill;}
class MovePoint{public int xEnd,yEnd;public MovePoint(int x,int y){xEnd=x;yEnd=y;}}
class Command{public int idAction;readonly Action action;public Command(object a,object b,int id,object c){idAction=id;}public Command(Action a){action=a;}public void performAction()=>action?.Invoke();}
class PopUp{public Command command;}class Waypoint{public PopUp popup=new PopUp{command=new Command(()=>Service.gI().requestChangeMap())};}
static class TileMap{public static int mapID=6,zoneID,pxw=1248,pxh=480;public static bool tileTypeAt(int x,int y,int t)=>true;public static int tileTypeAtPixel(int x,int y)=>2;}
static class Res{public static int random(int a,int b)=>a;public static int abs(int n)=>System.Math.Abs(n);public static void outz(string s){}}static class Math{public static int abs(int n)=>System.Math.Abs(n);}static class Cout{public static void println(string s){}}
static class mSystem{public static long currentTimeMillis()=>100000;}
static class mGraphics{public static int zoomLevel=1;}static class Main{public static bool isPC=true;}
static class AutoMap{public static bool isXmaping;}
static class InfoDlg{public static bool isLock;}class ChatTextField{public bool isShow;static ChatTextField one=new ChatTextField();public static ChatTextField gI()=>one;}
static class GameCanvas{public static bool[] keyPressed=new bool[30];public static void debug(string s,int n){}public static void clearKeyHold(){}public static void clearKeyPressed(){Array.Clear(keyPressed,0,keyPressed.Length);}}
class MobTemplate{public int type=1;}
class Mob{public int sys,levelBoss,typeSuperEff;public long maxHp;public void updateHp_bar(){}public int x=228,y=336,xFirst=228,yFirst=336,status=5,templateId=19,mobId,h=24,p1,p2,p3,dir=1;public bool isMobMe,injureThenDie;public long hp=12000,cTimeDie;public bool isBigBoss()=>false;public int getX()=>x;public int getY()=>y;public MobTemplate getTemplate()=>new MobTemplate();
__DIE__
}class BigBoss:Mob{}class BigBoss2:Mob{}
static class MucTieu{public static bool deselectMob,deselectChar,deselectNpc;}class Npc:Char{}class Char{public bool isManualFocus;public void deFocusNPC(){npcFocus=null;}public static Char current;public static Char myCharz()=>current;public static Char ExistingLocalPlayer=>current;public int cx=228,cy=336,cvx,cvy,cdir=1,statusMe=1,cTypePk,charID=1,delayFall;public long cHP=1000;public bool isLockAttack,currentFireByShortcut;public static bool isLockKey;public Skill myskill=new Skill();public object skillPaint,itemFocus,npcFocus;public Mob mobFocus,mobMe;public Char charFocus;public MovePoint currentMovePoint;public Command endMovePointCommand;
 public Waypoint isInEnterOfflinePoint()=>null;public Waypoint isInEnterOnlinePoint()=>cx<=24?new Waypoint():null;
 public bool isSelectingSkillUseAlone()=>false;public bool isSelectingSkillBuffToPlayer()=>false;public bool isUseChargeSkill()=>false;public bool focusToAttack()=>mobFocus!=null;public bool isAttacPlayerStatus()=>false;public bool isMeCanAttackOtherPlayer(Char p)=>true;
 public void createShadow(int x,int y,int n){}public void useSkillNotFocus(){}public void sendUseChargeSkill(){}public void stopUseChargeSkill(){}public int casts;public void setSkillPaint(object p,int t){casts++;}public void AttackFrame(){GameScr.instance.KeyTick();}
__CHAR_METHODS__
}
class GameScr{public static GameScr instance=new GameScr();public static GameScr gI()=>instance;public static MyVector vMob=new MyVector(),vCharInMap=new MyVector(),vItemMap=new MyVector();public static object[] sks=new object[64];public static bool isAutoPlay;public static int cmx;public object mobCapcha;public int tam,auto;
 public static Char findCharInMap(int id)=>null;public bool checkClickToBotton(object o)=>false;public void pickItem(){}public bool checkSkillValid()=>true;public static void startFlyText(string t,int x,int y,int a,int b,object f){}
 public void doSelectSkill(Skill s,bool isShortcut){} public void KeyTick(){__KEY__}
__METHODS__
}
class Service{static Service one=new Service();public static Service gI()=>one;public int maps,moves;public void charMove(){moves++;}__MAP_REQUEST__ public Session session=new Session();}
class Session{public void sendMessage(Message m){if(m.command==-23)Service.gI().maps++;}}class Reader{readonly byte[] data;int p;public Reader(byte[] b){data=b;}public byte readUnsignedByte()=>data[p++];public sbyte readByte()=>(sbyte)data[p++];public int readInt(){int n=0;for(int i=0;i<4;i++)n=(n<<8)|data[p++];return n;}public short readShort()=>(short)((data[p++]<<8)|data[p++]);public bool readBool()=>readByte()!=0;}
class Message{public int command;Reader r;public Message(sbyte c){command=c;}public void cleanup(){}public Message(int c,byte[] b){command=c;r=new Reader(b);}public Reader reader()=>r;public int readInt3Byte()=>r.readInt();}
class ItemMap{public int playerId,x,y;public ItemMap(short a,short b,int x,int y,short c,short d){this.x=x;this.y=y;}}
static class ServerEffect{public static void addServerEffect(int a,int b,int c,int d){}}static class mFont{public static object FATAL,ORANGE;}
class Receiver{public void Receive(Message msg){switch(msg.command){__DEATH__ __RESPAWN__}}}
class TargetTests{static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}static void Main(){
 var c=new Char();Char.current=c;var m=new Mob();GameScr.vMob.addElement(m);c.mobFocus=m;
 new Receiver().Receive(new Message(-12,new byte[]{0,0,0,46,224,0,0}));
 Check(m.hp==0&&m.status==1,"real death packet did not kill target");Check(c.mobFocus==null,"death packet retained mob focus");
 for(int i=0;i<20;i++){GameCanvas.keyPressed[25]=true;GameScr.instance.KeyTick();}
 Check(c.currentMovePoint==null&&c.cx==228&&c.cy==336&&Service.gI().maps==0&&Service.gI().moves==0,"attack on vanished target moved player or changed map");
 GameScr.vMob.addElement(new Mob{mobId=1});new Receiver().Receive(new Message(-13,new byte[]{1,0,0,0,0,46,224}));Check(c.mobFocus==null,"another mob respawn restored the remembered target too early");
 new Receiver().Receive(new Message(-13,new byte[]{0,0,0,0,0,46,224}));Check(c.mobFocus==m,"respawn packet did not restore previously selected mob");
 GameCanvas.keyPressed[25]=true;GameScr.instance.KeyTick();Check(c.casts==1,"respawned mob required manual reselection before attack");c.mobFocus=null;
 c.cx=12;for(int i=0;i<5;i++){GameCanvas.keyPressed[25]=true;GameScr.instance.KeyTick();}Check(Service.gI().maps==0,"attack key executed a portal command with no target");
 foreach(int scenario in new[]{0,1,2,3,4}){
  var stale=new Mob();GameScr.vMob.addElement(stale);c.mobFocus=stale;c.currentMovePoint=new MovePoint(0,0);c.endMovePointCommand=new Command(null,null,8002,null);
  if(scenario==0)stale.status=0;else if(scenario==1)stale.hp=0;else if(scenario==2)stale.x=-10;else if(scenario==3)stale.y=TileMap.pxh+1;else GameScr.vMob.values.Remove(stale);
  GameCanvas.keyPressed[25]=true;GameScr.instance.KeyTick();Check(c.mobFocus==null&&c.currentMovePoint==null&&Service.gI().maps==0,"invalid target retained focus/approach scenario="+scenario);
 }
 // Each new death is delivered through Controller; cancellation precedes its real respawn packet.
 for(int scenario=0;scenario<6;scenario++){
  c.cx=228;c.cy=336;c.cHP=1000;c.statusMe=1;c.mobFocus=null;c.charFocus=null;c.npcFocus=null;c.itemFocus=null;TileMap.mapID=6;TileMap.zoneID=0;
  c.focusManualTo(m);new Receiver().Receive(new Message(-12,new byte[]{0,0,0,46,224,0,0}));
  if(scenario==0)c.focusManualTo(new Mob());else if(scenario==1)c.focusManualTo(new Char());else if(scenario==2)c.focusManualTo(new Npc());else if(scenario==3)TileMap.zoneID++;else if(scenario==4)TileMap.mapID++;else {c.cHP=0;c.ClearInvalidAttackFocus();c.cHP=1000;}
  new Receiver().Receive(new Message(-13,new byte[]{0,0,0,0,0,46,224}));Check(c.mobFocus!=m,"cancelled respawn memory restored old target scenario="+scenario);
 }
 Console.WriteLine("PASS: "+checks+" real mob-death/standing attack-key/no-map-change checks");
}}
'''.replace("__DIE__",block(mob,"public void startDie()")).replace("__CHAR_METHODS__",char_methods).replace("__KEY__",key).replace("__METHODS__",methods).replace("__DEATH__",death).replace("__RESPAWN__",respawn)
fixture=fixture.replace("__MAP_REQUEST__",block((scripts/"Service.cs").read_text(encoding="utf-8-sig"),"public void requestChangeMap()"))
cs=OUT/"TargetTests.cs";exe=OUT/"TargetTests.exe";cs.write_text(fixture,encoding="utf-8")
subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe","/nologo","/target:exe",f"/out:{exe}",str(cs)],check=True)
subprocess.run([str(exe)],check=True)
