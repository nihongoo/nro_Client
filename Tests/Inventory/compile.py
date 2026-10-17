"""Cheap standalone C# validation using existing Editor compiler response files.

Run: python Tests/Inventory/compile.py
Does not invoke Unity, build players/APKs, or publish into Library/ScriptAssemblies.
Rebuilds the source list to avoid stale response files from another Git branch.
"""
from pathlib import Path
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = (ROOT / "Build/inventory-compile" / uuid.uuid4().hex).resolve()
assert OUTPUT.is_relative_to(ROOT.resolve())
OUTPUT.mkdir(parents=True)
BEE = ROOT / "Library/Bee/artifacts/1300b0aE.dag"
DOTNET = r"D:\Unity_Editor\2022.3.62f1\Editor\Data\NetCoreRuntime\dotnet.exe"
CSC = r"D:\Unity_Editor\2022.3.62f1\Editor\Data\DotNetSdkRoslyn\csc.dll"

for assembly in ("Assembly-CSharp", "Assembly-CSharp-Editor"):
    original = BEE / (assembly + ".rsp")
    if not original.exists():
        raise SystemExit(f"Existing Editor references not found: {original}")
    lines = []
    for line in original.read_text(encoding="utf-8-sig").splitlines():
        if line.startswith('"Assets/') and line.endswith('.cs"'):
            continue
        if line.startswith("-out:"):
            line = f'-out:"{OUTPUT / (assembly + ".dll")}"'
        elif line.startswith("-refout:"):
            line = f'-refout:"{OUTPUT / (assembly + ".ref.dll")}"'
        if assembly.endswith("-Editor"):
            line = line.replace("Library/Bee/artifacts/1300b0aE.dag/Assembly-CSharp.ref.dll",
                                str(OUTPUT / "Assembly-CSharp.ref.dll"))
        lines.append(line)
    for source in sorted((ROOT / "Assets").rglob("*.cs")):
        relative = source.relative_to(ROOT)
        is_editor = "Editor" in relative.parts
        if is_editor == assembly.endswith("-Editor"):
            lines.append('"' + str(relative).replace("\\", "/") + '"')
    rsp = OUTPUT / (assembly + ".rsp")
    rsp.write_text("\n".join(lines), encoding="utf-8")
    result = subprocess.run([DOTNET, "exec", CSC, "/nostdlib", "/noconfig", "@" + str(rsp)],
                            cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    log = OUTPUT / (assembly + ".log")
    log.write_bytes(result.stdout)
    if result.returncode:
        print(result.stdout.decode("utf-8", errors="replace"))
        raise SystemExit(result.returncode)
    print(f"PASS: {assembly}; isolated C# compilation only")
print(f"Logs: {OUTPUT}")
