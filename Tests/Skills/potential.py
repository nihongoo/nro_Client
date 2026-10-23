"""Cross-check production C#/Java cumulative pricing against unbounded integer arithmetic."""
from pathlib import Path
import subprocess
import tempfile
ROOT=Path(__file__).resolve().parents[2]
SERVER=Path(r'D:\dowload\SRC NRO NEW 03\SRC')
vectors=[]
for kind in range(-1,6):
 for current in [-1,0,1,2,3,9,13,14,50100,2000000000,9223372036854775807]:
  for count in [-1,0,1,2,3,10,1000,1001,1000000,2147483647]:vectors.append((kind,current,count))
def expected(t,c,n):
 if t<0 or t>4 or c<0 or n<=0 or (t==4 and (c>=14 or n>14-c)):return 'ERR'
 if t==4:cost=50000000*5**c*(5**n-1)//4
 else:
  first=c+1000 if t<=1 else c*100 if t==2 else (c+5)*100000
  slope=20 if t<=1 else 100 if t==2 else 100000
  cost=n*first+n*(n-1)//2*slope
 return str(cost) if cost<=9223372036854775807 else 'ERR'
cs='''using System;using Hud;
class Tests {
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 static void Main(){
  foreach(string line in System.IO.File.ReadAllLines(Environment.GetCommandLineArgs()[1])){var p=line.Split(',');try{Console.WriteLine(PotentialCost.Calculate(int.Parse(p[0]),long.Parse(p[1]),int.Parse(p[2])));}catch(ArgumentException){Console.WriteLine("ERR");}catch(OverflowException){Console.WriteLine("ERR");}}
  Check(PotentialCost.MaxTarget(0,50100,102220,220000)==50140,"HP budget maximum");
  Check(PotentialCost.Normalize(0,50100,99999,220000)==99980,"step floor");
  Check(PotentialCost.Normalize(0,50100,long.MaxValue,50140)==50140,"budget clamp");
  Check(PotentialCost.Normalize(0,50100,50000,50140)==50000,"below-current input");
  Check(PotentialCost.MaxTarget(2,0,0,100)==1,"free initial damage tier");
  Check(PotentialCost.MaxTarget(4,0,long.MaxValue,100)==14,"critical table cap");
  Check(PotentialCost.MaxTarget(4,0,long.MaxValue,10)==10,"server power cap");
  Check(PotentialCost.MaxTarget(0,0,long.MaxValue,int.MaxValue)==2147483640,"large batch maximum");
  Check(PotentialCost.Parse("100.000")==100000&&PotentialCost.Parse("99999999999999999999999")==long.MaxValue,"formatted/overflow input");
  for(int t=0;t<5;t++)for(long budget=0;budget<1000000000;budget+=1234567){long current=t==4?0:100,limit=t==4?14:10000000;long max=PotentialCost.MaxTarget(t,current,budget,limit),cost;
   Check(max==current||PotentialCost.TryCalculate(t,current,max,out cost)&&cost<=budget,"maximum costs too much");
   long next=max+PotentialCost.Step(t);Check(next>limit||!PotentialCost.TryCalculate(t,current,next,out cost)||cost>budget,"maximum not maximal");
  }
 }
}'''
java='''import java.nio.file.*;public class PriceVectors {public static void main(String[] args)throws Exception{for(String line:Files.readAllLines(Path.of(args[0]))){String[] p=line.split(",");try{System.out.println(nro.models.utils.PotentialCost.calculate(Integer.parseInt(p[0]),Long.parseLong(p[1]),Integer.parseInt(p[2])));}catch(ArithmeticException|IllegalArgumentException e){System.out.println("ERR");}}}}'''
out=ROOT/'Build/potential-tests';out.mkdir(parents=True,exist_ok=True)
with tempfile.TemporaryDirectory(prefix='parity-',dir=out) as folder:
 p=Path(folder);(p/'vectors.txt').write_text('\n'.join(','.join(map(str,v)) for v in vectors),encoding='ascii');(p/'Tests.cs').write_text(cs,encoding='utf-8');(p/'PriceVectors.java').write_text(java,encoding='utf-8')
 subprocess.run([r'D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe','/nologo',f'/out:{p / "Tests.exe"}',str(p/'Tests.cs'),str(ROOT/'Assets/Scripts/Hud/PotentialCost.cs')],check=True)
 client=subprocess.run([str(p/'Tests.exe'),str(p/'vectors.txt')],capture_output=True,text=True,check=True).stdout.splitlines()
 subprocess.run(['javac','--release','17','-encoding','UTF-8','-d',str(p),str(p/'PriceVectors.java'),str(SERVER/'src/nro/models/utils/PotentialCost.java')],check=True)
 server=subprocess.run(['java','-cp',str(p),'PriceVectors',str(p/'vectors.txt')],capture_output=True,text=True,check=True).stdout.splitlines()
 target=[expected(*v) for v in vectors]
 for i,(a,b,want) in enumerate(zip(client,server,target)):
  assert a==b==want,(vectors[i],a,b,want)
 assert len(client)==len(server)==len(target)
 print(f'PASS: {len(vectors)} C#/Java/big-integer price vectors and 8,100+ affordability/rounding checks')
