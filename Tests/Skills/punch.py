"""Standalone real Char cast-entry checks; no Unity build or new output directories."""
from pathlib import Path
import ast,re,subprocess
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/"Build/buff-icons-tests";assert OUT.is_dir()
source=(ROOT/"Assets/Scripts/Char.cs").read_text(encoding="utf-8-sig")
def block(text,marker):
    start=text.index(marker);opening=text.index("{",start);depth=0
    for token in re.finditer(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"|[{}]',text[opening:]):
        if token[0]=="{":depth+=1
        elif token[0]=="}":
            depth-=1
            if depth==0:return text[start:opening+token.end()]
    raise AssertionError(marker)
tree=ast.parse((ROOT/"Tests/Skills/run.py").read_text(encoding="utf-8-sig"))
fixture=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=="fixture" for t in n.targets))
fixture=fixture[:fixture.index("class UltiTests")]
fixture=fixture.replace("public int id=14, manaUseType=1;","public int id=14, manaUseType=1,type=1;")
skill_source=(ROOT/"Assets/Scripts/Skill.cs").read_text(encoding="utf-8-sig")
properties=skill_source[skill_source.index("    private int serverCooldown"):skill_source.index("    public long lastTimeUseThisSkill;")]
fixture=fixture.replace("public long lastTimeUseThisSkill;",properties+"public long lastTimeUseThisSkill;")
fixture=fixture.replace("public int status=2,holdEffID,x,y;","public int status=2,holdEffID,x,y,mobId;public bool isMobMe;")
fixture=fixture.replace("public int id; public SkillPaint", "public int id,effectHappenOnMob=1; public SkillPaint")
fixture=fixture.replace("public static bool isOfflineMap()=>offline;","public static bool isOfflineMap()=>offline;public static int tileTypeAtPixel(int x,int y)=>2;")
fixture=fixture.replace("public static void outz(string s){}","public static void outz(string s){}public static void err(string s){}")
fixture=fixture.replace("public object dart,currentMovePoint;","public Dart dart;public object currentMovePoint;public SkillPaint skillPaintRandomPaint;public EffectPaint[] effPaints;")
fixture=fixture.replace("public void setAutoSkillPaint(SkillPaint paint,int type){paints++;}","public void setAutoSkillPaint(SkillPaint paint,int type){paints++;skillPaintRandomPaint=paint;}public bool isSelectingSkillUseAlone()=>false;")
fixture=fixture.replace("public int skillId=98,", "public int dx=300,skillId=98,")
fixture=fixture.replace("public bool me=true,", "public bool meDead;public bool me=true,")
fixture=fixture.replace("public int auto;", "public bool isMeCanAttackMob(Mob mob)=>mob!=null;public static Info info1=new Info();public int auto;")
fixture=fixture.replace("public static SkillPaint[] sks=Create();","public static object[] efs=new object[2];public static SkillPaint[] sks=Create();")
methods="\n".join(block(source,m) for m in ("public void setSkillPaint(","public void useChargeSkill(","public void stopUseChargeSkill(","private void finishTuSatCharge(","internal void ConfirmAttack("))
attack=block(source,"public void setAttack()")
methods+="\npublic void setAttack(){"+block(attack,"if (me)")+"}"
fixture=fixture.replace("__COMPLETION__","").replace("__METHODS__",methods)
service=(ROOT/"Assets/Scripts/Service.cs").read_text(encoding="utf-8-sig")
fixture=fixture.replace("public List<int> packets=new List<int>();",block(service,"public void sendPlayerAttack(")+"public Session session=new Session();public List<int> packets=new List<int>();")
fixture=fixture.replace("public static Char myCharz()=>current;", "public static Char myCharz()=>current;public static Char ExistingLocalPlayer=>current;public bool ClearInvalidAttackFocus()=>false;")
fixture=fixture.replace("public static int mapID;", "public static int mapID,zoneID;")
fixture=fixture.replace("public int auto;", "public void FireAttackInput(){Char.myCharz().setSkillPaint(sks[Char.myCharz().myskill.skillId],0);}public int auto;")
fixture+=block(skill_source,"internal static class AttackSync")
auto=(ROOT/"Assets/Scripts/Mod.CuongLe/AutoSkill.cs").read_text(encoding="utf-8-sig")
fixture+="\nstatic class AutoDriver {static long[] lastTimeSendAttack=new long[10];static int GetMySkillIndex()=>0;static bool isMeCanAttackChar(Char c)=>c!=null;"+"\n".join(block(auto,m) for m in ("public static void AutoSendAttack(","public static void SendAttackToCharFocus(","public static void SendAttackToMobFocus(","private static long GetCoolDown("))+"}"

fixture+=r'''
namespace Hud {static class CombatPcHud{public static bool CanInput=true;public const string Attack="pc";}static class CombatMobileHud{public static bool CanInput=true;public const string Attack="mobile";}static class CombatPcPointer{public static bool Held(string s)=>false;}static class MobileCombatPointers{public static bool Held(string s)=>false;}}
static class Math {public static int abs(int n)=>System.Math.Abs(n);}
class Panel {public bool isShow;}static class GameCanvas{public static bool AttackHeld;public static Panel panel=new Panel();}
class Info {public void addInfo(string value,int type){}}
class Dart {public SkillPaint skillPaint;}
class EffectPaint {public object effCharPaint;public Mob eMob;public Char eChar;}
class MyVector {readonly List<object> values=new List<object>();public void addElement(object value){values.Add(value);}public int size()=>values.Count;public object elementAt(int i)=>values[i];}
static class UltiCastClient {public static bool InterceptAttack()=>false;}
namespace Notifications {static class NotificationProtocol{public const byte Version=2;}}
class Writer {public System.IO.MemoryStream data=new System.IO.MemoryStream();public void writeByte(int v){data.WriteByte((byte)v);}public void writeSByte(sbyte v)=>writeByte(v);public void writeInt(int v){for(int i=3;i>=0;i--)writeByte(v>>(8*i));}public void writeLong(long v){for(int i=7;i>=0;i--)writeByte((int)(v>>(8*i)));}}
class Reader {readonly byte[] data;int pos,saved;public Reader(byte[] b){data=b;}public int available()=>data.Length-pos;public void mark(int n){saved=pos;}public void reset(){pos=saved;}public sbyte readByte()=>(sbyte)data[pos++];public short readShort()=>(short)((data[pos++]<<8)|data[pos++]);public bool readBoolean()=>readByte()!=0;public int readInt(){int n=0;for(int i=0;i<4;i++)n=(n<<8)|data[pos++];return n;}public long readLong(){long n=0;for(int i=0;i<8;i++)n=(n<<8)|data[pos++];return n;}}
class Message {public int command;readonly Writer output=new Writer();readonly Reader input;public Message(sbyte c){command=c;}public Message(byte[] b){input=new Reader(b);}public Writer writer()=>output;public Reader reader()=>input;public byte[] Data=>output.data.ToArray();}
class Session {public readonly List<Message> packets=new List<Message>();public void sendMessage(Message m){packets.Add(m);}}
class PunchTests {static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}static void Main(string[] args){
 var replies=args[0].Split(',');int template=int.Parse(args[1]),cooldown=int.Parse(args[2]);bool mob=args.Length>3&&args[3]=="mob";var c=new Char();Char.current=c;if(mob)c.mobFocus=new Mob{mobId=7};else c.charFocus=new Char{charID=2};c.cdir=1;c.myskill.template.id=template;c.myskill.template.manaUseType=0;c.myskill.skillId=template*7;c.myskill.coolDown=cooldown;c.myskill.manaUse=25;c.myskill.RememberServerValues();
 for(int i=0;i<3;i++){
  mSystem.now=1000000+i*cooldown;c.setSkillPaint(GameScr.sks[c.myskill.skillId],0);
  Check(c.paints==i,"punch animation started before authoritative result");
  int sent=Service.gI().session.packets.Count;Check(sent==i+1,"one client cast must send exactly one packet");
  Message packet=Service.gI().session.packets[sent-1];var input=new Reader(packet.Data);Check((mob?packet.command==54&&input.readByte()==7:packet.command==-60&&input.readInt()==2)&&input.readByte()==1&&input.readLong()==i+1&&input.available()==0,"client wire format differs from server replay");
  c.setSkillPaint(GameScr.sks[0],0);c.setAttack();Check(Service.gI().session.packets.Count==sent,"repeated input/frame duplicated a pending cast");
  Check(AttackSync.TryRead(new Message(Convert.FromBase64String(replies[i]))),"real server result not decoded");
  Check(c.paints==i+1,"accepted hit/miss must start one local animation");
  for(int f=0;f<5;f++)c.setAttack();Check(Service.gI().session.packets.Count==sent,"confirmed animation sent another attack at its hit frame");
 }
 mSystem.now+=cooldown;c.setSkillPaint(GameScr.sks[c.myskill.skillId],0);Check(c.paints==3,"rejected attempt must not predict animation");
 AttackSync.TryRead(new Message(Convert.FromBase64String(replies[3])));Check(c.paints==3,"cooldown rejection fabricated an attack");
 int count=Service.gI().session.packets.Count;c.setSkillPaint(GameScr.sks[0],0);Check(Service.gI().session.packets.Count==count,"retry delay ignored");
 AttackSync.Reset();c.myskill.template.id=1;c.hasSendAttack=true;c.myskill.lastTimeUseThisSkill=mSystem.now;c.setSkillPaint(GameScr.sks[0],0);Check(c.hasSendAttack,"cooldown refusal reset the existing cast's send-once guard");
 c.myskill.coolDown=0;c.myskill.manaUse=0;c.myskill.RestoreServerValues();Check(c.myskill.coolDown==cooldown&&c.myskill.manaUse==25,"local override must restore authoritative DB values");
 AttackSync.Reset();c.myskill.template.id=0;c.myskill.lastTimeUseThisSkill=0;mSystem.now+=cooldown+1000;c.charFocus=new Char{charID=2};c.mobFocus=null;
 count=Service.gI().session.packets.Count;long mana=c.cMP;int painted=c.paints;
 AutoDriver.AutoSendAttack();Check(Service.gI().session.packets.Count==count+1&&c.paints==painted,"real auto-attack must wait for authoritative animation");
 AutoDriver.SendAttackToCharFocus();Check(Service.gI().session.packets.Count==count+1&&c.cMP==mana,"auto retry duplicated a request or charged local mana twice");
 byte[] late=Convert.FromBase64String(replies[0]);late[9]=5;c.myskill=new Skill();
 AttackSync.TryRead(new Message(late));Check(c.paints==painted,"late result overwrote a newly selected skill animation");
 Console.WriteLine("PASS: "+checks+" real cast/transport/server-result checks; 3 paced casts = 3 acknowledged animations");
}}
'''
cs=OUT/"PunchTests.cs";exe=OUT/"PunchTests.exe";cs.write_text(fixture,encoding="utf-8")
subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe","/nologo","/target:exe",f"/out:{exe}",str(cs)],check=True)
server=Path(r"D:\dowload\SRC NRO NEW 03\SRC")
for template,cooldown,kind in ((0,500,"player"),(2,400,"player"),(4,500,"player"),(0,1000,"player"),(0,500,"mob"),(1,2000,"player"),(3,800,"player"),(5,1000,"player")):
    result=subprocess.run(["java","-cp",str(server/"build/buff-icons-tests")+";"+str(server/"lib/*"),str(server/"tests/disciple/PacketGemTest.java"),"punch-mob" if kind=="mob" else "punch-replies",str(template),str(cooldown)],cwd=server,check=True,text=True,encoding="utf-8",stdout=subprocess.PIPE)
    print(next(line for line in result.stdout.splitlines() if line.startswith("PASS:")))
    replies=next(line.partition("=")[2] for line in result.stdout.splitlines() if line.startswith("PUNCH_REPLIES="))
    subprocess.run([str(exe),replies,str(template),str(cooldown),kind],check=True)
