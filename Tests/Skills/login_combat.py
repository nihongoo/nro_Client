"""Post-login character packet -> real button/cast/animation -> real Java Controller/Mob.injured.
Only transport, persisted profile, rendering and optional pet presence are fixtures. No live DB/Unity.
"""
from pathlib import Path
import subprocess,sys
ROOT=Path(__file__).resolve().parents[2];SERVER=Path(r"D:\dowload\SRC NRO NEW 03\SRC")
ns={"__file__":str(ROOT/"Tests/Skills/cadence.py")}
t=(ROOT/"Tests/Skills/cadence.py").read_text(encoding="utf-8");exec(t[:t.index("main=r")],ns)
fixture=ns["fixture"];b=ns["b"];scripts=ROOT/"Assets/Scripts"
char=(scripts/"Char.cs").read_text(encoding="utf-8-sig");game=(scripts/"GameScr.cs").read_text(encoding="utf-8-sig");controller=(scripts/"Controller.cs").read_text(encoding="utf-8-sig");service=(scripts/"Service.cs").read_text(encoding="utf-8-sig")
fixture=fixture.replace(b(fixture,"public void setAutoSkillPaint("),b(char,"public void setAutoSkillPaint("))
paint=(scripts/"SkillPaint.cs").read_text(encoding="utf-8-sig").replace("public class SkillPaint", "class SkillPaint").replace("{", "{public SkillPaint(){}public SkillPaint(int n){id=n;}",1)
fixture=fixture.replace(b(fixture,"class SkillPaint {"),paint)
fixture+=(scripts/"SkillInfoPaint.cs").read_text(encoding="utf-8-sig")
# Use actual playback setup, local update/hit-frame and renderer (drawing operations are no-ops).
local_update=b(char,"private void updateSkillPaint()")
local_update=local_update[:local_update.index("        IMapObject mapObject6 = null;")]+"}"
char_methods=b(char,"public SkillInfoPaint[] skillInfoPaint()")+local_update+b(char,"public void paintCharWithSkill(")
fixture=fixture.replace("public class SkillInfoPaint", "class SkillInfoPaint")
fixture=fixture.replace("public int paints;", "public int paints,visibleCasts;public void paintCharWithoutSkill(mGraphics g){if(indexSkill==0)visibleCasts++;}public void Playback(){updateSkillPaint();if(skillInfoPaint()!=null&&indexSkill<skillInfoPaint().Length)paintCharWithSkill(new mGraphics());}public void saveLoadPreviousSkill(){}public int indexSkill,sType,dy2,dy1,dy0,dx2,dx1,dx0,i2,i1,i0,cp3,cf;public EffectFrame eff0,eff1,eff2;public Arrow arr;"+char_methods)
fixture=fixture.replace("public static object[] efs=new object[2];", "public static EffectFrame[] efs=new EffectFrame[200];public static Arrow[] arrs=new Arrow[200];")
fixture=fixture.replace("public bool isMeCanAttackOtherPlayer(Char p)=>true;", "public bool isMeCanAttackOtherPlayer(Char p)=>true;public bool isUseSkillSpec()=>false;public void sendNewAttack(int n){}public bool isFreez;public Task taskMaint=new Task();")
fixture=fixture.replace("public void doSelectSkill(Skill s,bool isShortcut){}", "private Skill lastSkill;public int selectedIndexSkill;public bool doSeleckSkillFlag;public bool isCharging()=>false;public void saveRMSCurrentSkill(int n){}public void resetButton(){}public void doUseSkillNotFocus(Skill s){}"+b(game,"public void doSelectSkill("))
fixture=fixture.replace("public bool isAttackSkill()=>false;", "public int maxPoint=7;public bool isBuffToPlayer()=>false;public bool isAttackSkill()=>true;")
fixture=fixture.replace("public int dy=300,dx=300,skillId=98,", "public int point=7,dy=300,dx=300,skillId=98,")
fixture=fixture.replace("public Skill myskill=new Skill();", "public Skill myskill;")
fixture=fixture.replace("static class mGraphics{", "class mGraphics{")
fixture=fixture.replace("class mGraphics{public static int zoomLevel=1;}", "class mGraphics{public static int zoomLevel=1,VCENTER,HCENTER;}")
fixture=fixture.replace("public static int abs(int x)=>System.Math.Abs(x);", "public static int random(int a,int z)=>z-1;public static int abs(int x)=>System.Math.Abs(x);")
fixture=fixture.replace("public List<int> packets=new List<int>();", b(service,"public void selectSkill(")+"public List<int> packets=new List<int>();")
fixture=fixture.replace("public void sendMessage(Message m){packets.Add(m);}", "public void sendMessage(Message m){packets.Add(m);Bridge.Send(m);}")
fixture=fixture.replace("public void writeInt(int v)", "public void writeShort(int v){writeByte(v>>8);writeByte(v);}public void writeInt(int v)")
fixture=fixture.replace("public short readShort()", "public byte readUnsignedByte()=>(byte)readByte();public string readUTF(){int n=(ushort)readShort();var a=new byte[n];for(int i=0;i<n;i++)a[i]=(byte)readByte();return System.Text.Encoding.UTF8.GetString(a);}public short readShort()")
fixture=fixture.replace("public long readLong()", "public void close(){}public long readLong()")
# Real character packet prefix through learned-skill hydration, before unrelated inventory/UI fields.
start=controller.index("Char.myCharz().charID = msg.reader().readInt();",controller.index("case 0:",controller.index("public void messageSubCommand")))
end=controller.index("useSkill(skill3);",start)+len("useSkill(skill3);")
login=controller[start:end]+"}"
login_fields="public int ctaskId,head,cPk,eff5BuffHp,eff5BuffMp;public long cPower,cTiemNang;public NClass nClass;public void applyCharLevelPercent(){}public MyVector vSkill=new MyVector(),vSkillFight=new MyVector();"
fixture=fixture.replace("public int paints,visibleCasts;",login_fields+"public int paints,visibleCasts;")
fixture=fixture.replace("public long twMp;", "public static NClass[] nClasss={new NClass(),new NClass(),new NClass()};public long dHP,dMP;public long twMp;")
fixture=fixture.replace("public bool contains(object o)", "public void removeAllElements(){values.Clear();}public bool contains(object o)")
fixture+=r'''
class Task{public int taskId=10;}class NClass{}class EffectFrame{public EfInfo[] arrEfInfo={new EfInfo()};}class EfInfo{public int idImg,dx,dy;}
static class SmallImage{public static void drawSmallImage(mGraphics g,int a,int x,int y,int z,int anchor){}}
interface IMapObject{int getX();int getY();}
class Arrow{public int life,ax,ay;public Arrow(){}public Arrow(Char c,Arrow a){}}
class PlayerDart:Dart{public PlayerDart(Char c,int a,SkillPaint s,int x,int y){skillPaint=s;}}
static class Rms{public static byte[] loadRMS(string s)=>System.IO.File.ReadAllBytes(@"__SERVER__\data\update_data\skill");}
class DataInputStream:Reader{public DataInputStream(byte[] b):base(b){}}
class Skills{public static MyVector skills=new MyVector();public static System.Collections.Generic.Dictionary<int,Skill> learned=new System.Collections.Generic.Dictionary<int,Skill>();public static Skill get(short id)=>learned[id];}
class LoginReceiver{__USE_SKILL__ public void Receive(Message msg){var subtype=msg.reader().readByte();switch(subtype){case 0:{__LOGIN__ break;}__SHORTCUTS__}}public void Cooldown(Message msg){__COOLDOWN__}}
class Bridge{
 static System.Diagnostics.Process server;public static int hits,petHits,hp,accepted;public static long admitted;public static System.Collections.Generic.List<long> stamps=new System.Collections.Generic.List<long>();
 public static void Open(string gem,string pet,string kind){var start=new System.Diagnostics.ProcessStartInfo("java", "-cp \"build/buff-icons-tests;lib/*\" tests/disciple/PacketGemTest.java combat-session "+gem+" "+pet+" "+kind){WorkingDirectory=@"__SERVER__",UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,CreateNoWindow=true};server=System.Diagnostics.Process.Start(start);Pump(true);}
 public static void Send(Message m){if(server==null)return;server.StandardInput.WriteLine(m.command+":"+Convert.ToBase64String(m.Data));server.StandardInput.Flush();Pump(false);}
 static void Pump(bool login){string line;do{line=server.StandardOutput.ReadLine();if(line==null)throw new Exception("Java combat fixture exited");}while(!line.StartsWith("COMBAT="));var parts=line.Substring(7).Split('|');var state=parts[1].Split(':');hits=int.Parse(state[0]);petHits=int.Parse(state[1]);hp=int.Parse(state[2]);admitted=long.Parse(state[3]);
  foreach(string entry in parts[0].Split(',')){if(entry.Length==0)continue;int colon=entry.IndexOf(':');int command=int.Parse(entry.Substring(0,colon));byte[] bytes=Convert.FromBase64String(entry.Substring(colon+1));var msg=new Message(bytes);if(command==-30&&login){new LoginReceiver().Receive(msg);continue;}if(command==-94){new LoginReceiver().Cooldown(msg);continue;}if(command==-109){var r=new Reader(bytes);if(r.readByte()==2&&r.readByte()==9){r.readLong();r.readShort();if(r.readBoolean()){accepted++;stamps.Add(admitted);}}AttackSync.TryRead(msg);}else if(command==-12){new Receiver().Receive(new Message(-12,bytes));}}
 }
 public static void Close(){if(server==null)return;server.StandardInput.WriteLine("quit");server.StandardInput.Flush();server.WaitForExit();server=null;accepted=0;stamps.Clear();}
}
class LoginCombatTests{
 static long Now()=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();static void Check(bool b,string why){if(!b)throw new Exception(why);}
 static void Tap(){mSystem.now=Now();GameCanvas.keyPressed[25]=true;GameScr.gI().KeyTick();}
 static void Main(string[] args){foreach(int id in new[]{34,41}){var s=new Skill();s.template.id=id==34?4:5;s.skillId=id;s.coolDown=id==34?500:2200;s.RememberServerValues();Skills.learned[id]=s;Skills.skills.addElement(s);}GameScr.gI().LoadAnimation();var c=new Char();Char.current=c;c.cx=100;c.cy=336;
  try {mSystem.now=Now();Bridge.Open(args[0],args[1],args[2]);Check(c.myskill!=null,"post-login learned-skill packet did not hydrate selection");var mob=new Mob{mobId=0};GameScr.vMob.addElement(mob);
  if(args[3]=="switched"){GameScr.gI().doSelectSkill(Skills.learned[41],false);GameScr.gI().doSelectSkill(Skills.learned[34],false);}c.focusManualTo(mob);
  for(int hit=0;hit<2;hit++){
   long inputDeadline=Now()+5000;
   while(c.skillPaint!=null||Now()<c.myskill.lastTimeUseThisSkill+500){Check(Now()<inputDeadline,"post-login cooldown/animation got stuck");mSystem.now=Now();AttackSync.Update();c.Playback();System.Threading.Thread.Sleep(16);}
   c.Playback();Tap();if(c.currentMovePoint!=null)c.Arrived();
   long animationDeadline=Now()+5000;
   while(c.skillPaint!=null){Check(Now()<animationDeadline,"actual animation did not finish");mSystem.now=Now();AttackSync.Update();c.Playback();System.Threading.Thread.Sleep(16);}
  }
  if(Bridge.stamps.Count>1)Console.WriteLine("SERVER_INTERVAL_MS="+(Bridge.stamps[1]-Bridge.stamps[0]));
  Console.WriteLine("COUNTS mode="+args[3]+" gem="+args[0]+" pet="+args[1]+" attackPackets="+Service.gI().session.packets.FindAll(p=>p.command==54||p.command==-60).Count+" accepted="+Bridge.accepted+" visible="+c.visibleCasts+" masterDamage="+Bridge.hits+" petDamage="+Bridge.petHits);
  if(args[1]=="attack")Check(Bridge.petHits>0,"real Pet.update did not independently attack");
  Check(Bridge.accepted==2&&Bridge.hits==2&&c.visibleCasts==2,"accepted casts, visible animation and master damage are not one-to-one");for(int i=1;i<Bridge.stamps.Count;i++)Check(Bridge.stamps[i]-Bridge.stamps[i-1]>=500,"server accepted faster than nominal cooldown");}finally{Bridge.Close();}
 }
}
'''
# Append production login/death/respawn readers and asset loader.
fixture=fixture.replace("__USE_SKILL__",b(controller,"private void useSkill(")) .replace("__LOGIN__",login).replace("__SERVER__",str(SERVER))
fixture=fixture.replace("public void KeyTick(){", "public void LoadAnimation(){for(int i=0;i<200;i++)Skills.skills.addElement(new object());readSkill();}"+b(game,"public void readSkill()")+"public void KeyTick(){")
fixture=fixture.replace("class Mob{", "class Mob:IMapObject{").replace("class Char {", "class Char:IMapObject {")
fixture=fixture.replace("public static Char current;", "public int getX()=>cx;public int getY()=>cy;public static Char current;")
fixture+=b(ns["wf"],"class Receiver{")

fixture=fixture.replace("class Message {", "class Message {public int readInt3Byte()=>reader().readInt();")

fixture=fixture.replace("class Skill {", "class Skill {public static bool isDisabled(int id)=>false;")
fixture=fixture.replace("public static void println(string s){}", "public static void LogError(string s){throw new Exception(s);}public static void println(string s){}")
fixture=fixture.replace("class BigBoss:Mob{}", "class BigBoss:Mob{public bool haftBody;}")
fixture=fixture.replace("public void hoisinh(){}", "public void namekKame(){}public void nameLazer(){}public void xaydaKame(){}public void traidatKame(){}public void hoisinh(){}")
fixture=fixture.replace("public bool readBoolean()", "public bool readBool()=>readBoolean();public bool readBoolean()")
fixture=fixture.replace("public int indexSkill,sType,", "public int ty,indexSkill,sType,")
fixture=fixture.replace("public Message(byte[] b){input=new Reader(b);}", "public Message(int c,byte[] b){command=c;input=new Reader(b);}public void cleanup(){}public Message(byte[] b){input=new Reader(b);}")

fixture=fixture.replace("public void charMove(){}",b(service,"public void charMove()"))
fixture=fixture.replace("public static Char current;", "public static bool ischangingMap;public bool isTeleport,canFly,isHaveMount;public int cxSend,cySend,cdirSend,cactFirst,cMPGoc=1000;public void Arrived(){cx=currentMovePoint.xEnd;cy=currentMovePoint.yEnd;currentMovePoint=null;Service.gI().charMove();checkPerformEndMovePointAction();}"+b(char,"private void checkPerformEndMovePointAction()")+"public static Char current;")
fixture=fixture.replace("public static int mapID,zoneID,pxw=1248,pxh=480;", "public static int size=24;public static int tileTypeAt(int x,int y)=>2;public static int mapID,zoneID,pxw=1248,pxh=480;")
fixture=fixture.replace("public void KeyTick(){", "public static int tickMove;public void CompleteApproach(){"+game[game.index("case 8002:"):game.index("case 11000:",game.index("case 8002:"))].replace("case 8002:","").replace("break;","")+"}public void KeyTick(){")
fixture=fixture.replace("public void performAction()=>action?.Invoke();", "public void performAction(){if(idAction==8002)GameScr.gI().CompleteApproach();else action?.Invoke();}")
fixture+="static class Controller{public static bool isStopReadMessage;}"
fixture=fixture.replace("s.RememberServerValues();Skills.learned[id]=s;", "s.dx=id==34?44:300;s.dy=18;s.RememberServerValues();Skills.learned[id]=s;")
fixture=fixture.replace("public void startDie(){}",b((scripts/"Mob.cs").read_text(encoding="utf-8-sig"),"public void startDie()"))
shortcuts=controller[controller.index("case 61:",controller.index("public void messageSubCommand")):controller.index("case 23:",controller.index("case 61:",controller.index("public void messageSubCommand")))]
cooldown=controller[controller.index("case -94:"):controller.index("case -95:")].replace("case -94:","")
cooldown=cooldown[:cooldown.rindex("break;")]
fixture=fixture.replace("__SHORTCUTS__",shortcuts).replace("__COOLDOWN__",cooldown)
fixture=fixture.replace("public void KeyTick(){", "public const int numSkill=8;public static Skill[] keySkill=new Skill[8],onScreenSkill=new Skill[8];public void loadDefaultKeySkill(){}public void loadDefaultonScreenSkill(){}"+b(game,"public void onKSkill(")+b(game,"public void onOSkill(")+b(game,"public void onCSkill(")+"public void KeyTick(){")
fixture=fixture.replace("public byte readUnsignedByte()", "public void read(ref sbyte[] data){for(int i=0;i<data.Length;i++)data[i]=readByte();}public byte readUnsignedByte()")
if "--baseline" in sys.argv:
 for path,marker in (("Controller.cs","private void useSkill("),("Skill.cs","internal static class AttackSync")):
  old=subprocess.check_output(["git","show","6c517f0:Assets/Scripts/"+path],cwd=ROOT).decode("utf-8-sig")
  fixture=fixture.replace(b(fixture,marker),b(old,marker))
# The interactive JVM bridge uses wall-clock time; do not freeze the client clock during blocking I/O.
fixture=fixture.replace("public static long currentTimeMillis()=>now;", "public static long currentTimeMillis()=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();")
cs=ROOT/"Build/buff-icons-tests/LoginCombatTests.cs";exe=cs.with_suffix(".exe");cs.write_text(fixture,encoding="utf-8")
subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe","/nologo","/target:exe",f"/out:{exe}",str(cs)],check=True)
if "--baseline" in sys.argv:
 for mode in ("cold","switched"):
  result=subprocess.run([str(exe),"0","none","kill",mode])
  assert result.returncode!=0,"baseline unexpectedly satisfied one-to-one combat contract"
 sys.exit(0)
for gem,pet,kind in (("0","none","kill"),("0","follow","kill"),("0","attack","alive"),("1994","attack","alive"),("1991","attack","alive"),("1994","fused","kill"),("1991","fused","kill")):
 for selection in ("cold","switched"):
  subprocess.run([str(exe),gem,pet,kind,selection],check=True)
