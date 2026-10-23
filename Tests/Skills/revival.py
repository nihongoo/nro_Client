"""Standalone checks of production revival/skill selection; no Unity or APK build."""
from pathlib import Path
import argparse
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument("--hud-ref", help="Compare HUD selection with an older Git revision")
args = parser.parse_args()
scripts = ROOT / "Assets/Scripts"
char = (scripts / "Char.cs").read_text(encoding="utf-8-sig")
controller = (scripts / "Controller.cs").read_text(encoding="utf-8-sig")
game = (scripts / "GameScr.cs").read_text(encoding="utf-8-sig")
hud = subprocess.check_output(["git", "show", f"{args.hud_ref}:Assets/Scripts/GameScr.cs"], cwd=ROOT).decode("utf-8") if args.hud_ref else game


def block(source, signature):
    start = source.index(signature)
    opening = source.index("{", start)
    depth = 0
    for token in re.finditer(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"|[{}]', source[opening:]):
        if token[0] == "{":
            depth += 1
        elif token[0] == "}":
            depth -= 1
            if depth == 0:
                return source[start:opening + token.end()]
    raise AssertionError(signature)


revival = controller[controller.index("\t\t\tcase 15:"):controller.index("\t\t\tcase 35:", controller.index("\t\t\tcase 15:"))]
assert "GameScr.findCharInMap(charID) ?? new Char()" in controller
assert "c.vEff.removeAllElements();" in controller
fixture = r'''
#pragma warning disable 0649
using System;
using System.Collections.Generic;
class MovePoint {public int xEnd,yEnd;public MovePoint(int x,int y){xEnd=x;yEnd=y;}}
static class TileMap {public static int tileTypeAtPixel(int x,int y)=>2;}
class MyVector : List<object> { public void removeAllElements(){Clear();} }
class SkillTemplate {public int id,type=1; public bool isBuffToPlayer()=>type==2;}
class Skill {public SkillTemplate template=new SkillTemplate();public static bool isDisabled(int id)=>id>=24&&id<=26;}
class Task {public int taskId=10;}
class Char {
    public static Char current=new Char(); public static Char myCharz()=>current;
    public int charID,cx,cy,xSd,ySd,cxSend,cySend,cp1,cp2,cp3,cvx,cvy,statusMe=1;
    public short wdx,wdy;public long cHP=100,cHPFull=100,cMP=37,cMPFull=100;
    public bool me,meDead,isDie,isLockMove,isCreateDark;public Task taskMaint=new Task();
    public MyVector vMovePoints=new MyVector(); public MovePoint currentMovePoint;public object endMovePointCommand,mobFocus;
    public Char charFocus;public Skill myskill;
    public bool isUseSkillSpec()=>myskill.template.type==4;
    public bool isSelectingSkillUseAlone()=>myskill.template.type==3;
    public bool isSelectingSkillBuffToPlayer()=>myskill.template.type==2;
    public bool isMeCanAttackOtherPlayer(Char c)=>myskill.template.type!=2&&c.statusMe!=14;
    public void sendNewAttack(int id){}
    public static int abs(int n)=>Math.Abs(n);
    public void checkPerformEndMovePointAction(){} public void setCharFallFromJump(){}
    public void FinishMovement(){ __ARRIVAL__ }
    __CHAR_METHODS__
}
class ServerEffect {public static void addServerEffect(int id,Char c,int type){} }
class Res {public static void outz(string text){} }
class GameCanvas {public static void debug(string text,int n){} }
class Service {
    static Service instance=new Service();public static Service gI()=>instance;
    public List<int> selections=new List<int>();public void selectSkill(int id){selections.Add(id);} public void charMove(){}
}
class GameScr {
    public static GameScr instance=new GameScr();public static GameScr gI()=>instance;
    public static bool isHaveSelectSkill;public static MyVector players=new MyVector();
    public static Char findCharInMap(int id){foreach(Char c in players)if(c.charID==id)return c;return null;}
    public object center;public Skill lastSkill;public int selectedIndexSkill,auto,casts;public bool doSeleckSkillFlag,clickMoving;
    public bool isCharging()=>false;public void saveRMSCurrentSkill(int id){} public void resetButton(){}
    public void doUseSkillNotFocus(Skill skill){}
    void doFire(bool shortcut,bool skipWaypoint){casts++;}
    __SELECT__
}
class Reader {
    readonly Queue<int> data;public Reader(params int[] values){data=new Queue<int>(values);}
    public int readInt()=>data.Dequeue();public short readShort()=>(short)data.Dequeue();
}
class Message {
    public Reader data;public Message(params int[] values){data=new Reader(values);}
    public Reader reader()=>data;public int readInt3Byte()=>data.readInt();
}
class Controller {
    public void Receive(Message msg){switch(15){__REVIVAL__}}
}
class RevivalTests {
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static void Main(){
        foreach(bool local in new[]{false,true}) {
            var c=new Char{charID=70001,me=local,meDead=local,isDie=true,isLockMove=true,statusMe=5,
                cx=117,cy=289,wdx=90,wdy=300,cp1=5,cp2=90,cp3=300,cvx=7,cvy=-5,
                currentMovePoint=new MovePoint(90,300),endMovePointCommand=new object()};
            c.vMovePoints.Add(new object());Char.current=local?c:new Char{charID=1,me=true};
            GameScr.players.Clear();GameScr.players.Add(local?new Char{charID=2}:c);
            var neighbor=GameScr.players[0];
            new Controller().Receive(new Message(c.charID,250,1000,123,300));
            Check(c.cx==123&&c.cy==300&&c.xSd==123&&c.ySd==300,"revival ignored authoritative position");
            Check(c.cHP==250&&c.cHPFull==1000&&c.cMP==37,"revival corrupted partial HP, max HP or MP");
            Check(c.wdx==0&&c.wdy==0&&!c.isDie&&c.cp1==0&&c.cp2==0&&c.cp3==0,"pending death replayed after revival");
            Check(c.vMovePoints.Count==0&&c.currentMovePoint==null&&c.endMovePointCommand==null&&c.cvx==0&&c.cvy==0,"stale movement survived revival");
            Check(c.statusMe==1&&GameScr.players.Count==1&&ReferenceEquals(neighbor,GameScr.players[0]),"revival cleared nearby characters");
            if(local)Check(!c.meDead&&!c.isLockMove&&c.cxSend==123&&c.cySend==300,"self movement baseline not synchronized");
        }
        for(int dx=-16;dx<=16;dx++) {
            var remote=new Char{cx=200+dx,cy=300,currentMovePoint=new MovePoint(200,300)};
            remote.FinishMovement();
            Check(remote.cx==200&&remote.cy==300&&remote.currentMovePoint==null,"remote stopped short of server coordinate");
        }
        foreach(int status in new[]{1,5,14})foreach(bool changed in new[]{false,true}) {
            var skill=new Skill();skill.template.id=7;skill.template.type=2;
            Char.current=new Char{me=true,charFocus=new Char{statusMe=status}};
            var game=GameScr.instance=new GameScr();game.lastSkill=changed?new Skill():skill;
            Service.gI().selections.Clear();game.doSelectSkill(skill,true);
            Check(game.casts==1,"support HUD tap did not cast immediately (target status "+status+", changed "+changed+")");
            Check(Service.gI().selections.Count==(changed?1:0),"selection packet sent incorrectly");
        }
        Char.current=new Char{me=true,cHP=0,statusMe=14,charFocus=new Char()};
        var deadGame=GameScr.instance=new GameScr();var buff=new Skill();buff.template.type=2;
        deadGame.doSelectSkill(buff,true);Check(deadGame.casts==0,"dead caster used support");
        var attack=new Skill();Char.current=new Char{me=true,charFocus=new Char()};
        var attackGame=GameScr.instance=new GameScr{lastSkill=new Skill()};attackGame.doSelectSkill(attack,true);
        Check(attackGame.casts==0,"changed attack skill selection behavior");
        Console.WriteLine("PASS: "+checks+" client revival/support checks (production methods; no Unity build)");
    }
}
'''
methods = [block(char, signature) for signature in ["public void resetRevivalMovement(", "public void reviveAt(", "public bool focusToAttack("]]
arrival = block(char, "if (abs(cx - currentMovePoint.xEnd) <= 16 && abs(cy - currentMovePoint.yEnd) <= 16)")
program = fixture.replace("__ARRIVAL__", arrival).replace("__CHAR_METHODS__", "\n".join(methods)).replace("__SELECT__", block(hud, "public void doSelectSkill(")).replace("__REVIVAL__", revival)
output = ROOT / "Build/revival-tests"
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="phase-a-", dir=output) as folder:
    path = Path(folder)
    cs, exe = path / "RevivalTests.cs", path / "RevivalTests.exe"
    cs.write_text(program, encoding="utf-8")
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", "/target:exe", f"/out:{exe}", str(cs)], check=True)
    subprocess.run([str(exe)], check=True)
