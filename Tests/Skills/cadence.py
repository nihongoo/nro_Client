"""20 ms update/network simulator around real Char/AttackSync/Service transport; no Unity build."""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[2]
ns={"__file__":str(ROOT/"Tests/Skills/punch.py")}
text=(ROOT/"Tests/Skills/punch.py").read_text()
exec(text[:text.index('cs=OUT/')],ns)
fixture=ns["fixture"];fixture=fixture[:fixture.index("class PunchTests")]
# Route cadence requests through the real attack-button entry, doFire and mob range checks.
world={"__file__":str(ROOT/"Tests/Skills/vanished_target.py")}
worldtext=(ROOT/"Tests/Skills/vanished_target.py").read_text(encoding="utf-8")
exec(worldtext[:worldtext.index('cs=OUT/')],world)
b=ns["block"]; wf=world["fixture"]
realgame=b(wf,"class GameScr{")
realgame=realgame.replace("public static object[] sks=new object[64];", "public static SkillPaint[] sks=Create();static SkillPaint[] Create(){var a=new SkillPaint[200];for(int i=0;i<a.Length;i++)a[i]=new SkillPaint(i);return a;}public static object[] efs=new object[2];public static Info info1=new Info();public long twMp;public bool isInjureMp,isUseFreez;public void activeSuperPower(int x,int y){}")
fixture=fixture.replace(b(fixture,"class GameScr {"),realgame)
for marker in ("class Command{","class PopUp{","class Waypoint{","static class mGraphics{","static class Main{","static class AutoMap{","static class InfoDlg{","class ChatTextField{","static class Cout{","class MobTemplate{","class BigBoss:","class BigBoss2:","static class mFont{","static class MucTieu{","class Npc:","class ItemMap{"):
 fixture+="\n"+b(wf,marker)
fixture=fixture.replace(b(fixture,"class Mob {"),b(wf,"class Mob{").replace(b(wf,"public void startDie()"),"public int holdEffID;public void startDie(){}"))
fixture=fixture.replace("public static int mapID,zoneID;", "public static int mapID,zoneID,pxw=1248,pxh=480;")
fixture=fixture.replace("public int dx=300,skillId=98,", "public int dy=300,dx=300,skillId=98,")
fixture=fixture.replace("public static Char ExistingLocalPlayer=>current;public bool ClearInvalidAttackFocus()=>false;", "public static Char ExistingLocalPlayer=>current;public bool isManualFocus;public void deFocusNPC(){npcFocus=null;}"+world["char_methods"])
fixture=fixture.replace("public object currentMovePoint;", "public MovePoint currentMovePoint;public Command endMovePointCommand;public int cvx,cvy,cTypePk,delayFall;public bool isLockAttack,currentFireByShortcut;public static bool isLockKey;public object npcFocus,itemFocus,skillPaint;public Mob mobMe;public Waypoint isInEnterOfflinePoint()=>null;public Waypoint isInEnterOnlinePoint()=>null;public bool focusToAttack()=>mobFocus!=null;public bool isAttacPlayerStatus()=>false;public bool isMeCanAttackOtherPlayer(Char p)=>true;public void createShadow(int x,int y,int n){}public void useSkillNotFocus(){}public void sendUseChargeSkill(){}public bool isUseChargeSkill()=>false;")
fixture=fixture.replace("public int xEnd,yEnd;", "public int xEnd,yEnd;public MovePoint(){}public MovePoint(int x,int y){xEnd=x;yEnd=y;}")
fixture=fixture.replace("public static bool AttackHeld;", "public static bool AttackHeld;public static bool[] keyPressed=new bool[30];public static void debug(string s,int n){}public static void clearKeyHold(){}public static void clearKeyPressed(){}")
fixture=fixture.replace("public void skill_not_focus(sbyte status)", "public void requestChangeMap(){throw new Exception(\"attack must not enter a portal\");}public void charMove(){}public void skill_not_focus(sbyte status)")
fixture=fixture.replace("public int size()=>values.Count;", "public bool contains(object o)=>values.Contains(o);public int size()=>values.Count;")
fixture=fixture.replace("public static void outz(string s){}", "public static int abs(int x)=>System.Math.Abs(x);public static void outz(string s){}")
main=r''' 
class CadenceTests {
 static byte[] Ack(Message sent,int cd,bool old){var r=new Reader(sent.Data);r.readByte();r.readByte();long request=r.readLong();var w=new Writer();w.writeByte(2);w.writeByte(9);w.writeLong(request);w.writeByte(Char.myCharz().myskill.skillId>>8);w.writeByte(Char.myCharz().myskill.skillId);w.writeByte(1);w.writeInt(cd-(old?49:0));w.writeInt(cd);return w.data.ToArray();}
 static void Check(bool b,string why){if(!b)throw new Exception(why);}
 static void Main(string[] args){bool old=args.Length>0;foreach(int template in new[]{0,2,4,1,3,5})foreach(int rtt in new[]{0,50,100,200})foreach(bool hold in new[]{false,true}){
  int cd=template==2?400:template==3?800:template==5?1000:template==1?2000:500;
  AttackSync.Reset();var c=new Char();Char.current=c;c.mobFocus=new Mob{mobId=7};GameScr.vMob.addElement(c.mobFocus);c.cx=228;c.cy=336;c.myskill.template.id=template;c.myskill.skillId=template*7;c.myskill.coolDown=cd;c.myskill.RememberServerValues();Service.gI().session.packets.Clear();GameCanvas.AttackHeld=hold;
  var due=new List<long>();var data=new List<byte[]>();var hits=new List<long>();var sends=new List<long>();int seen=0,paints=0;
  for(long now=1000000;hits.Count<12&&now<1100000;now+=20){mSystem.now=now;
   if(!hold){GameCanvas.keyPressed[25]=true;GameScr.gI().KeyTick();}
   AttackSync.Update();
   var packets=Service.gI().session.packets;while(seen<packets.Count){due.Add(now+rtt);data.Add(Ack(packets[seen++],cd,old));sends.Add(now);}
   for(int i=due.Count-1;i>=0;i--)if(due[i]<=now){AttackSync.TryRead(new Message(data[i]));due.RemoveAt(i);data.RemoveAt(i);}
   if(c.paints>paints){hits.Add(now);paints=c.paints;}
  }
  Check(hits.Count==12,"held/spam did not sustain real casts");long min=99999,max=0;for(int i=1;i<hits.Count;i++){long d=hits[i]-hits[i-1];min=System.Math.Min(min,d);max=System.Math.Max(max,d);}
  if(!old){Check(min>=cd&&max<=cd+20,"cadence adds RTT or exceeds nominal DPS");for(int i=1;i<sends.Count;i++)Check(sends[i]-sends[i-1]>=cd,"transport sends faster than nominal cooldown");}
  Console.WriteLine((old?"OLD":"NEW")+" template="+template+" cd="+cd+" RTT="+rtt+" "+(hold?"hold":"spam")+" interval="+min+".."+max);
 }
 if(!old){GameCanvas.AttackHeld=false;AttackSync.Reset();var c=new Char();Char.current=c;c.mobFocus=new Mob{mobId=7};GameScr.vMob.addElement(c.mobFocus);c.cx=228;c.cy=336;c.myskill.template.id=0;c.myskill.skillId=0;c.myskill.coolDown=500;c.myskill.RememberServerValues();Service.gI().session.packets.Clear();mSystem.now=2000000;c.setSkillPaint(GameScr.sks[0],0);AttackSync.TryRead(new Message(Ack(Service.gI().session.packets[0],500,false)));mSystem.now+=460;c.setSkillPaint(GameScr.sks[0],0);Check(Service.gI().session.packets.Count==1,"early input sent too early");mSystem.now+=40;AttackSync.Update();Check(Service.gI().session.packets.Count==2,"single early input was dropped instead of buffered");int paintsBefore=c.paints;c.mobFocus.hp=0;c.ClearInvalidAttackFocus();AttackSync.TryRead(new Message(Ack(Service.gI().session.packets[1],500,false)));Check(c.paints==paintsBefore+1,"accepted killing hit lost its animation");Check(c.mobFocus==null,"accepted killing hit restored an invalid attack focus");
 c.mobFocus=new Mob{mobId=7};GameScr.vMob.addElement(c.mobFocus);mSystem.now+=460;c.setSkillPaint(GameScr.sks[0],0);c.mobFocus.status=0;mSystem.now+=40;AttackSync.Update();Check(Service.gI().session.packets.Count==2,"buffer fired after target disappeared");
 Console.WriteLine("PASS: early input + real update sends once at cooldown; 48 RTT/skill/input cadence scenarios");}
 }
}
'''
for old in (True,False):
 source=fixture
 if old:
  skill=subprocess.run(["git","show","b5dd8cb:Assets/Scripts/Skill.cs"],cwd=ROOT,check=True,text=True,encoding="utf-8-sig",stdout=subprocess.PIPE).stdout
  oldsync=ns["block"](skill,"internal static class AttackSync")
  # The previous build had no held-input pump. Exercise its real Start/ACK gate on each held frame.
  oldsync=oldsync[:-1]+"internal static void Update(){if(GameCanvas.AttackHeld)GameScr.gI().FireAttackInput();}}"
  source=source.replace(ns["block"](source,"internal static class AttackSync"),oldsync)
 cs=ROOT/"Build/buff-icons-tests/CadenceTests.cs";exe=cs.with_suffix(".exe");cs.write_text(source+main,encoding="utf-8")
 subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe","/nologo","/target:exe",f"/out:{exe}",str(cs)],check=True)
 subprocess.run([str(exe)]+(["old"] if old else []),check=True)
