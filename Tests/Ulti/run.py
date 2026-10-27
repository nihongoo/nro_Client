"""Run production C# cast driver/codec against deterministic transport and clock fixtures."""
from pathlib import Path
import subprocess, sys
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Build/ulti-tests';OUT.mkdir(parents=True,exist_ok=True)
packet=Path(sys.argv[1]) if len(sys.argv)>1 else Path(r'D:\dowload\SRC NRO NEW 03\SRC\build\ulti-tests\contract.bin')
exe=OUT/'UltiTests.exe'
subprocess.run([r'D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe','/nologo','/target:exe',f'/out:{exe}',str(ROOT/'Assets/Scripts/UltiCastClient.cs'),str(ROOT/'Assets/Scripts/myReader.cs'),str(ROOT/'Assets/Scripts/myWriter.cs'),str(ROOT/'Tests/Ulti/UltiTests.cs')],check=True)
subprocess.run([str(exe),str(packet)],check=True)
