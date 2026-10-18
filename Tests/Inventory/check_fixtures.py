"""Analyze the generated C# regression fixtures without compiling/running binaries."""
from pathlib import Path
import runpy
import subprocess
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
real_run = subprocess.run
analyzed = 0


def analyze_only(args, **kwargs):
    global analyzed
    if str(args[0]).lower().endswith('csc.exe'):
        sources = [Path(p).resolve() for p in args if str(p).endswith('.cs')]
        source_list = sources[0].parent / 'analysis-sources.txt'
        source_list.write_text('\n'.join(map(str, sources)), encoding='utf-8')
        real_run(['pwsh', '-NoProfile', '-File', str(ROOT / 'Tests/Inventory/analyze.ps1'), '-SourceList', str(source_list)], check=True, cwd=ROOT)
        analyzed += 1
        return subprocess.CompletedProcess(args, 0)
    if str(args[0]).lower().endswith('.exe'):
        # Harness assertions are deliberately NOT executed; no executable was built.
        return subprocess.CompletedProcess(args, 0)
    raise RuntimeError(f'Unexpected process in no-build fixture analysis: {args[0]}')


with patch('subprocess.run', analyze_only):
    for name in ('run.py', 'input.py', 'porata.py'):
        runpy.run_path(str(ROOT / 'Tests/Inventory' / name), run_name='__main__')
print(f'PASS: {analyzed} C# fixture groups analyzed; runtime assertions NOT run; no build/DLL/exe')
