"""Replay TDHS and split packets from the real Java controller through client receive handlers."""
from pathlib import Path
import re
import subprocess

ROOT=Path(__file__).resolve().parents[2]
SERVER=Path(r"D:\dowload\SRC NRO NEW 03\SRC")
OUT=ROOT/"Build/buff-icons-tests"
assert OUT.is_dir(), "Use the existing buff test output directory"
scripts=ROOT/"Assets/Scripts"

def block(source, marker):
    start=source.index(marker); opening=source.index("{",start); depth=0
    for token in re.finditer(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"|[{}]',source[opening:]):
        if token[0]=="{": depth+=1
        elif token[0]=="}":
            depth-=1
            if depth==0:return source[start:opening+token.end()]
    raise AssertionError(marker)
controller=(scripts/"Controller.cs").read_text(encoding="utf-8-sig")
legacy=(scripts/"Assets.src.f/Controller2.cs").read_text(encoding="utf-8-sig")
char=(scripts/"Char.cs").read_text(encoding="utf-8-sig")
tdhs=block(controller[controller.index("case -45:"):],"if (b49 == 0)")
blind=block(legacy[legacy.index("case -124:"):],"if (num5 == 40)")
clear=block(char,"public void removeBlindEff()")
# Outgoing Porata and pet commands must have no stun/freeze gate. Death remains blocked.
service=(scripts/"Service.cs").read_text(encoding="utf-8-sig")
for signature in ("public void useItem(","public void petStatus("):
    outgoing=block(service,signature)
    assert not any(flag in outgoing for flag in ("blindEff","isFreez","sleepEff","holdEffID"))
fixture=r'''
#pragma warning disable 0649
using System;using System.Collections.Generic;using System.IO;
class Reader {readonly byte[] data;int pos;public Reader(byte[] value){data=value;}public sbyte readByte()=>(sbyte)data[pos++];public int readUnsignedByte()=>data[pos++];public int readInt(){int n=0;for(int i=0;i<4;i++)n=(n<<8)|data[pos++];return n;}public short readShort()=>(short)((data[pos++]<<8)|data[pos++]);}
class Message {readonly Reader input;public Message(byte[] data){input=new Reader(data);}public Reader reader()=>input;}
class Mob {public bool isFreez;public int seconds;public long last,cur;}
class MyVector {public object elementAt(int i)=>new Mob();}
static class Res {public static void outz(string value){}}
static class TileMap {public static int tileTypeAtPixel(int x,int y)=>2;}
static class mSystem {public static long currentTimeMillis()=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();}
class GameScr {public static GameScr instance=new GameScr();public static GameScr gI()=>instance;public bool isFreez;public int dem;public static object[] sks=new object[256];public static MyVector vMob=new MyVector();public static Char findCharInMap(int id)=>Char.all.TryGetValue(id,out var c)?c:null;}
class Char {public static Dictionary<int,Char> all=new Dictionary<int,Char>();public static Char local;public static Char myCharz()=>local;public int charID,cx,cy,freezSeconds,seconds,delayFall,holdEffID;public long lastFreez,currFreez;public bool me,isFreez,blindEff,isLockMove,isFlyAndCharge,isStandAndCharge,stone,sleepEff,isUsePlane;public void setSkillPaint(object paint,int mode){}public void saveLoadPreviousSkill(){}
__CLEAR__
}
class Receiver {public void Receive(int command,byte[] bytes){var msg=new Message(bytes);Char @char=null;
 if(command==-45){sbyte b49=msg.reader().readByte();int num119=msg.reader().readInt();short num120=msg.reader().readShort();__TDHS__}
 if(command==-124){sbyte b4=msg.reader().readByte(),b5=msg.reader().readByte();if(b5==0){int num5=msg.reader().readUnsignedByte();int num6=msg.reader().readInt();__BLIND__}}
}}
class ControlPacketsTest {static int checks;static void Check(bool value,string reason){checks++;if(!value)throw new Exception(reason);}static void Main(string[] args){
 var owner=new Char{charID=1,me=true};Char.local=owner;Char.all[1]=owner;Char.all[2]=new Char{charID=2};Char.all[-1]=new Char{charID=-1};
 var receiver=new Receiver();bool sawFreeze=false;
 foreach(string encoded in args[0].Split(',')){int split=encoded.IndexOf(':');int command=int.Parse(encoded.Substring(0,split));
  receiver.Receive(command,Convert.FromBase64String(encoded.Substring(split+1)));
  if(command==-45){Check(owner.blindEff&&owner.isFreez&&owner.isLockMove,"real TDHS packet did not set both control layers");sawFreeze=true;}
 }
 Check(sawFreeze,"no TDHS animation packet replayed");Check(!owner.blindEff,"master blind effect remained");
 Check(!owner.isFreez&&!owner.isLockMove&&!GameScr.gI().isFreez,"master remains frozen after real split control-OFF packet");
 Check(owner.freezSeconds==0&&owner.seconds==0,"TDHS countdown remained after release");
 Check(Char.all[-1].blindEff,"disciple did not receive transferred control packet");
 // A delayed duplicate OFF must not clear movement locks owned by another effect.
 owner.stone=true;owner.isLockMove=true;owner.removeBlindEff();Check(owner.isLockMove,"blind release unlocked unrelated stone control");
 Console.WriteLine("PASS: "+checks+" real TDHS receive/split/control-OFF checks");
}}
'''.replace("__TDHS__",tdhs).replace("__BLIND__",blind).replace("__CLEAR__",clear)
source=OUT/"ControlPacketsTest.cs";source.write_text(fixture,encoding="utf-8")
exe=OUT/"ControlPacketsTest.exe"
subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe","/nologo","/target:exe",f"/out:{exe}",str(source)],check=True)
result=subprocess.run(["java","-cp",str(SERVER/"build/buff-icons-tests")+";"+str(SERVER/"lib/*"),str(SERVER/"tests/disciple/PacketGemTest.java"),"control-all"],cwd=SERVER,check=True,text=True,encoding="utf-8",stdout=subprocess.PIPE)
cycles=[line.partition("=")[2] for line in result.stdout.splitlines() if line.startswith("CONTROL_PACKETS=")]
assert len(cycles)==5, "Missing Porata bag/body, disciple command, hard expiry or hard command cycle"
for packets in cycles:
    subprocess.run([str(exe),packets],check=True)
