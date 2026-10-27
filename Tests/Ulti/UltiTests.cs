using System;using System.Collections.Generic;using System.IO;
namespace UnityEngine {public class TextAsset{public byte[] bytes;}public static class Resources{public static object Load(string f,Type t){return null;}}}
public static class mSystem{public static long currentTimeMillis(){return Hud.BuffIconStore.Now;}public static sbyte[] convertToSbyte(byte[] b){var s=new sbyte[b.Length];Buffer.BlockCopy(b,0,s,0,b.Length);return s;}}
namespace Hud{public static class BuffIconStore{public static long Now=10000;}}
public static class TileMap{public static int mapID=5,zoneID=0;}
public class MyVector{List<object> list=new List<object>();public int size(){return list.Count;}public object elementAt(int i){return list[i];}public void addElement(object x){list.Add(x);}}
public class SkillTemplate{public int id;}public class Skill{public SkillTemplate template=new SkillTemplate();public long lastTimeUseThisSkill;public int coolDown=120000;}
public class Mob{public int mobId=3,status=2;public bool isMobMe;}
public class Char{public static Char me;public static Char ExistingLocalPlayer{get{return me;}}public Skill myskill=new Skill();public MyVector vSkill=new MyVector();public Char charFocus;public Mob mobFocus;public int charID=2,statusMe=1,seconds;public long cHP=100,last;public bool isStandAndCharge=true;public int stopped;public void stopUseChargeSkill(){stopped++;isStandAndCharge=false;}}
public class Service{static Service instance=new Service();public static Service gI(){return instance;}public List<UltiCastClient.Packet> sent=new List<UltiCastClient.Packet>();public void sendUltiCast(UltiCastClient.Packet p){sent.Add(p);}}
class UltiTests{
 static int checks;static void Check(bool ok,string s){checks++;if(!ok)throw new Exception(s);}
 static UltiCastClient.Packet Begin(int skill){UltiCastClient.Reset();Service.gI().sent.Clear();Char.me=new Char();Char.me.myskill.template.id=skill;Char.me.vSkill.addElement(Char.me.myskill);Char.me.charFocus=new Char();Check(UltiCastClient.InterceptStatus(skill==14?7:4),"intercept begin");return Service.gI().sent[0];}
 static UltiCastClient.Packet Ready(UltiCastClient.Packet p){p.Action=1;p.Cast=999;p.Duration=p.Skill==14?2000:p.Skill==10?4000:3000;UltiCastClient.Receive(p);return p;}
 public static void Main(string[] args){
  byte[] wire=File.ReadAllBytes(args[0]);myReader r=new myReader(mSystem.convertToSbyte(wire));Check(r.readUnsignedByte()==30,"subtype");var p=UltiCastClient.Read(r);
  Check(p.Action==1&&p.Skill==11&&p.Request==0x0102030405060708L&&p.Cast==0x1112131415161718L&&p.TargetKind==1&&p.Target==0x01020304&&p.Duration==3000&&p.Lock==3000,"Java golden values decoded in C#");
  myWriter w=new myWriter();w.writeByte(30);UltiCastClient.Write(w,p);sbyte[] signed=w.getData();byte[] encoded=new byte[signed.Length];Buffer.BlockCopy(signed,0,encoded,0,signed.Length);Check(Convert.ToBase64String(encoded)==Convert.ToBase64String(wire),"C# encoder exactly matches Java bytes");
  bool rejected=false;try{UltiCastClient.Read(new myReader(mSystem.convertToSbyte(new byte[]{1,1,11})));}catch(Exception){rejected=true;}Check(rejected,"truncated packet");
  foreach(int skill in new[]{10,11,14}){
   var begin=Begin(skill);UltiCastClient.InterceptAttack();Check(Service.gI().sent.Count==1,"cannot finish before ACK");var ack=Ready(begin);Check(Char.me.seconds==ack.Duration,"authoritative duration, no minus 1s");
   UltiCastClient.InterceptAttack();UltiCastClient.InterceptAttack();Check(Service.gI().sent.Count==2&&Service.gI().sent[1].Action==2&&Service.gI().sent[1].Cast==999,"one complete with identity");
   ack.Action=3;ack.Lock=3000;UltiCastClient.Receive(ack);Check(Char.me.stopped>0&&Char.me.myskill.lastTimeUseThisSkill==Hud.BuffIconStore.Now-117000,"cancel stops and only locks 3s");
   var next=Begin(skill);var stale=ack;stale.Action=1;UltiCastClient.Receive(stale);Check(Char.me.seconds==0,"stale ACK cannot affect new cast");
   ack=Ready(next);Char.me.statusMe=5;UltiCastClient.Update();Check(Service.gI().sent[Service.gI().sent.Count-1].Action==3,"death cancels");
  }
  var b=Ready(Begin(11));Char.me.charFocus=null;UltiCastClient.Update();Check(Service.gI().sent[1].Action==3,"lost target cancels");
  b=Ready(Begin(10));TileMap.zoneID++;UltiCastClient.Update();Check(Service.gI().sent[1].Action==3,"zone cancels");
  b=Ready(Begin(14));Hud.BuffIconStore.Now+=2000;UltiCastClient.Update();UltiCastClient.Update();Check(Service.gI().sent.Count==2&&Service.gI().sent[1].Action==2,"bomb completes even if normal animation update is CC-blocked");
  b=Begin(11);Char.me.charFocus=null;UltiCastClient.Update();Ready(b);UltiCastClient.Update();Check(Service.gI().sent[1].Action==3,"late ACK after losing focus is cancelled");
  b=Begin(11);b.Action=4;UltiCastClient.Receive(b);Check(!UltiCastClient.InterceptAttack(),"emergency switch legacy attack");
  Console.WriteLine("PASS: "+checks+" production C# golden contract/driver/early/replay/target/map/death/bomb/fallback checks");
 }
}
