"""Exercise production inventory header painters with a recording graphics fixture.

No Unity startup. --ref develop demonstrates the original missing-potential bug.
"""
from pathlib import Path
import argparse
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument("--ref")
args = parser.parse_args()

def read(path):
    if args.ref:
        return subprocess.check_output(["git", "show", f"{args.ref}:{path}"], cwd=ROOT).decode("utf-8-sig")
    return (ROOT / path).read_text(encoding="utf-8-sig")

def method(source, name):
    start = re.search(r"^\s*(?:private|public) [^\n]*\b" + name + r"\(", source, re.M).start()
    begin = source.index("{", start)
    depth = 0
    for token in re.finditer(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"|[{}]', source[begin:]):
        if token[0] == "{": depth += 1
        elif token[0] == "}":
            depth -= 1
            if not depth: return source[start:begin + token.end()]
    raise AssertionError(name)

frame = read("Assets/Scripts/Hud/InventoryFrame.cs")
panel = read("Assets/Scripts/Panel.cs")
paint = method(frame, "PaintInventoryFrame")
if "PaintInventoryHeader(g);" in paint:
    header = method(frame, "PaintInventoryHeader")
    assert ".cHP" not in paint, "Stats must not overpaint the selected tab header"
else:
    start = paint.index("        Char character = Char.myCharz();")
    end = paint.index("        Rect close =", start)
    body = paint[start:end]
    body = re.sub(r"^.*InventoryFrame.Clip.*\n", "", body, flags=re.M)
    header = "private void PaintInventoryHeader(mGraphics g) {\n" + body + "}"

fixture = r'''
using System;
using System.Collections.Generic;
using System.Globalization;
class mGraphics {
    public List<string> text=new List<string>(); public List<int> ys=new List<int>();
}
class mFont {
    public const int CENTER=2;
    public static mFont tahoma_7_white=new mFont(),tahoma_7_yellow=new mFont(),tahoma_7b_white=new mFont();
    public void drawString(mGraphics g,string text,int x,int y,int align){g.text.Add(text);g.ys.Add(y);}
}
static class NinjaUtil { public static string getMoneys(long n)=>n.ToString("N0",CultureInfo.InvariantCulture); }
static class mResources {
    public const string HP="HP",KI="KI",hit_point="Damage",critical="Critical",giamsatthuong="Reduction",critdame="CriticalDamage",
        potential_point="Potential",active_point="Activity",vitality="Stamina",power="Power";
}
class Char {
    static Char value=new Char(); public static Char myCharz()=>value;
    public string cName="hero",currStrLevel="level"; public bool me=true;
    public int rank=42,cStamina=17,cMaxStamina=25,cHP=101,cHPFull=202,cMP=303,cMPFull=404,cDamFull=505,
        cCriticalFull=7,cGiamST=8,cCritDameFull=9;
    public long cTiemNang,cNangdong=61,cPower=77; public string getStrLevel()=>"own-level";
}
static class GameCanvas { public static int gameTick; }
class Panel {
    public int currentTabIndex,X,W; public string[][] mainTabName=new string[7][];
    __HEADER__
    __SKILL__
    void paintClanInfo(mGraphics g){g.text.Add("clan");}
    void paintToolInfo(mGraphics g){g.text.Add("tools");}
    void paintToolInfoMod(mGraphics g){g.text.Add("mod-tools");}
    public mGraphics Paint(int tab){currentTabIndex=tab;var g=new mGraphics();PaintInventoryHeader(g);return g;}
}
class HeaderTests {
    static int checks;
    static void Check(bool value,string text){checks++;if(!value)throw new Exception(text);}
    static void Main(){
        var p=new Panel();var c=Char.myCharz();
        foreach(int width in new[]{150,190,300,380}) foreach(long potential in new[]{0L,123456L,10000000000001234L})
        foreach(int tick in new[]{0,12,19}) {
            p.W=width;c.cTiemNang=potential;GameCanvas.gameTick=tick;
            var skill=p.Paint(2);
            Check(skill.text.Contains("Potential"),"Skill header lost its potential label");
            Check(skill.text.Contains(NinjaUtil.getMoneys(potential)),"Skill header lost its current potential value");
            Check(skill.text.Count==4&&!skill.text.Exists(t=>t.StartsWith("HP:")),"Equipment stats overpaint skill header");
            Check(skill.ys[2]==26&&skill.ys[2]+10<=47,"Potential value is clipped by the header/body boundary");
            var task=p.Paint(0);
            Check(task.text.Exists(t=>t=="Stamina: 17/25")&&task.text.Contains("hero"),"Task stamina/name changed");
            Check(task.text.Contains("own-level")&&!task.text.Contains("Potential"),"Task level/header ownership changed");
            var inventory=p.Paint(1);
            Check(inventory.text.Count==4&&inventory.text[0]=="HP: 101/202"&&inventory.text[1]=="KI: 303/404","Equipment HP/KI changed");
            Check(inventory.text[2].Contains("505")&&inventory.text[3].Contains("9%"),"Equipment combat stats changed");
            Check(p.Paint(3).text[0]=="clan","Clan header replaced by equipment stats");
            p.mainTabName=new string[5][];Check(p.Paint(3).text[0]=="tools","Five-tab layout must keep its tools header");
            p.mainTabName=new string[7][];
            Check(p.Paint(4).text[0]=="mod-tools"&&p.Paint(5).text[0]=="mod-tools","Mod header changed");
            Check(p.Paint(6).text[0]=="tools","Tools header replaced by equipment stats");
        }
        Console.WriteLine("PASS: "+checks+" production inventory header checks; no Unity");
    }
}
'''
fixture = fixture.replace("__HEADER__", header).replace("__SKILL__", method(panel, "paintSkillInfo"))
output = ROOT / "Build/inventory-header-tests"
output.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="header-", dir=output) as folder:
    path = Path(folder)
    cs = path / "Header.cs"
    cs.write_text(fixture, encoding="utf-8")
    exe = path / "Header.exe"
    subprocess.run([r"D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe", "/nologo", f"/out:{exe}", str(cs)], check=True)
    subprocess.run([str(exe)], check=True)
