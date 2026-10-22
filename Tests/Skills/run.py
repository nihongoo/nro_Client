"""Exercise production Char charge methods without Unity or an APK build.

Only rendering, transport, world objects and the clock are fixtures. The actual
setSkillPaint/start/finish/cancel methods and timed completion block are extracted
unchanged from Char.cs and compiled by standalone Roslyn into a temporary folder.
Run: python Tests/Skills/run.py [--ref develop]
"""
from pathlib import Path
import argparse
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument("--ref", help="Read Char.cs from a Git revision for regression checks")
parser.add_argument("--compiler", default=r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe")
args = parser.parse_args()
source = (subprocess.check_output(["git", "show", f"{args.ref}:Assets/Scripts/Char.cs"],
                                 cwd=ROOT).decode("utf-8") if args.ref else
          (ROOT / "Assets/Scripts/Char.cs").read_text(encoding="utf-8-sig"))


def block_at(start):
    opening = source.index("{", start)
    depth = 0
    # Ignore braces inside comments and C# strings.
    tokens = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"|[{}]')
    for match in tokens.finditer(source, opening):
        if match[0] == "{":
            depth += 1
        elif match[0] == "}":
            depth -= 1
            if depth == 0:
                return source[start:match.end()]
    raise AssertionError("Unclosed production block")


methods = []
for signature in ["public void setSkillPaint(", "public void useChargeSkill(",
                  "public void stopUseChargeSkill("]:
    methods.append(block_at(source.index(signature)))
signature = "private void finishTuSatCharge("
methods.append(block_at(source.index(signature)) if signature in source else
               "private void finishTuSatCharge(int sType) {}")
start = source.index('Res.outz("  7.5 gong namekLazer ')
completion = block_at(source.index("if (cur - last > seconds || cur - last > 10000)", start))

fixture = r'''
#pragma warning disable 0649 // Unassigned fields belong to deliberately minimal world fixtures.
using System;
using System.Collections.Generic;
class SkillTemplate { public int id=14, manaUseType=1; public bool isAttackSkill()=>false; }
class Skill { public SkillTemplate template=new SkillTemplate(); public int skillId=98, coolDown=120000, manaUse=50; public long lastTimeUseThisSkill; public bool paintCanNotUseSkill; }
class SkillPaint { public int id; public SkillPaint(int n){id=n;} }
class Mob { public int status=2,holdEffID,x,y; }
class MovePoint { public int xEnd,yEnd; }
static class TileMap {
    public static int mapID; public static bool offline;
    public static bool tileTypeAt(int x,int y,int t)=>true;
    public static bool isOfflineMap()=>offline;
    public static MovePoint getSafeBossApproachPoint(int x,int y,int z)=>new MovePoint();
}
static class mSystem { public static long now=1000000; public static long currentTimeMillis()=>now; }
static class Res { public static void outz(string s){} }
class SoundMn {
    public static SoundMn gI()=>new SoundMn();
    public void hoisinh(){} public void thaiduonghasan(){} public void taitaoPause(){}
    public void gong(){} public void gongName(){}
}
class Service {
    static Service instance=new Service(); public static Service gI()=>instance;
    public List<int> packets=new List<int>(); public void skill_not_focus(sbyte status)=>packets.Add(status);
}
class Effect { public Effect(int a,int b,int c,int d,int e,int f){} }
static class EffecMn { public static void addEff(Effect e){} }
static class ServerEffect { public static void addServerEffect(int a,int b,int c,int d){} }
class GameScr {
    static GameScr instance=new GameScr(); public static GameScr gI()=>instance;
    public static SkillPaint[] sks=Create(); static SkillPaint[] Create(){var a=new SkillPaint[200];for(int i=0;i<a.Length;i++)a[i]=new SkillPaint(i);return a;}
    public int auto; public long twMp; public bool isInjureMp,isUseFreez;
    public void activeSuperPower(int x,int y){}
}
class Char {
    public static Char current; public static Char myCharz()=>current;
    public bool me=true,stone,sleepEff,blindEff,isStandAndCharge,isFlyAndCharge,isUseSkillAfterCharge,isCreateDark,isLockMove,isWaitMonkey,isCharge,telePortSkill,isFlying,hasSendAttack;
    public int statusMe=1,cgender=2,cx,cy,ch,charID,holdEffID,isMonkey,chargeCount,posDisY,cdir,skillTemplateId,cStamina=100;
    public long cHP=1000,cHPFull=1000,cMP=1000,cMPFull=1000,seconds,last,cur;
    public object dart,currentMovePoint; public Char charFocus; public Mob mobFocus;
    public string cName="test"; public Skill myskill=new Skill(); public int paints;
    public bool isSelectingSkillBuffToPlayer()=>false;
    public void setAutoSkillPaint(SkillPaint paint,int type){paints++;}
    public void CompleteAgain()=>finishTuSatCharge(0);
    public void UpdateCharge() {
        if (!isStandAndCharge) return;
        bool flag6=false;
        cur=mSystem.currentTimeMillis();
        __COMPLETION__
    }
    __METHODS__
}
class UltiTests {
    static int checks;
    static void Check(bool b,string s){checks++;if(!b)throw new Exception(s);}
    static Char Create(){var c=new Char();Char.current=c;Service.gI().packets.Clear();TileMap.offline=false;return c;}
    static void Start(Char c){c.setSkillPaint(GameScr.sks[c.myskill.skillId],0);}
    static void Main(){
        var c=Create();Start(c);
        Check(Service.gI().packets.Count==1 && Service.gI().packets[0]==7,"start sends exactly one -45/7");
        Check(c.isStandAndCharge && c.cMP==500 && c.cStamina==99,"start charges and pays locally once");
        long cooldown=c.myskill.lastTimeUseThisSkill;
        // Simulate the actual server's -45/7 prepare response.
        c.seconds=2000;c.last=mSystem.now;mSystem.now+=2001;c.UpdateCharge();
        Check(Service.gI().packets.Count==2 && Service.gI().packets[1]==7,"completion must send exactly one explosion despite active cooldown");
        Check(!c.isStandAndCharge && c.paints==2,"completion consumes charge and paints explosion");
        Check(c.cMP==500 && c.cStamina==99 && c.myskill.lastTimeUseThisSkill==cooldown,"completion must not pay or restart cooldown");
        for(int i=0;i<10;i++){c.UpdateCharge();c.CompleteAgain();}
        Check(Service.gI().packets.Count==2,"later frames and duplicate completion must not send another explosion");
        Start(c);Check(Service.gI().packets.Count==2,"fresh cast still observes cooldown");
        c=Create();Start(c);c.stopUseChargeSkill();mSystem.now+=11000;c.UpdateCharge();c.CompleteAgain();
        Check(Service.gI().packets.Count==1,"cancelled charge cannot explode");
        for(int state=0;state<4;state++){
            c=Create();Start(c);c.seconds=2000;c.last=mSystem.now;
            if(state==0)c.stone=true;else if(state==1)c.sleepEff=true;else if(state==2)c.blindEff=true;else c.statusMe=14;
            mSystem.now+=2001;c.UpdateCharge();c.CompleteAgain();
            Check(Service.gI().packets.Count==1 && !c.isStandAndCharge,"blocked/dead charge cannot explode");
        }
        c=Create();c.me=false;c.isStandAndCharge=true;c.CompleteAgain();
        Check(Service.gI().packets.Count==0,"remote character cannot send explosion");
        Console.WriteLine("PASS: "+checks+" client ulti checks (production methods and completion block; no Unity build)");
    }
}
'''
program = fixture.replace("__COMPLETION__", completion).replace("__METHODS__", "\n".join(methods))
output = (ROOT / "Build/ulti-tests").resolve()
assert output.is_relative_to(ROOT.resolve())
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="client-ulti-tests-", dir=output) as folder:
    path = Path(folder)
    cs = path / "UltiTests.cs"
    exe = path / "UltiTests.exe"
    cs.write_text(program, encoding="utf-8")
    subprocess.run([args.compiler, "/nologo", "/target:exe", f"/out:{exe}", str(cs)], check=True)
    subprocess.run([str(exe)], check=True)
