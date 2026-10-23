"""Compile actual tool menu builders/dispatch to check the single account entry."""
from pathlib import Path
import subprocess
import tempfile
ROOT=Path(__file__).resolve().parents[2]
def method(source,signature):
    start=source.index(signature);opening=source.index('{',start);end=opening+1;depth=1
    while depth:
        if source[end]=='{':depth+=1
        elif source[end]=='}':depth-=1
        end+=1
    return source[start:end]
panel=(ROOT/'Assets/Scripts/Panel.cs').read_text(encoding='utf-8-sig')
sound=(ROOT/'Assets/Scripts/SoundMn.cs').read_text(encoding='utf-8-sig')
fixture=r'''
using System;
using System.Collections.Generic;
class Char {static Char c=new Char();public static Char myCharz()=>c;public bool havePet;public object arrItemBox;public int statusMe;public Task taskMaint=new Task();}
class Task {public int taskId=2;}
class LoginScr {public bool isLogin2=true;public int switched;public void backToRegister(){switched++;}}
class Menu {public bool showMenu;}
static class GameCanvas {public static bool isTouch;public static int gameTick;public static LoginScr loginScr=new LoginScr();public static Panel panel;public static Menu menu=new Menu();public static void startOKDlg(string s){}}
static class GameMidlet {public static string RegistrationUrl="web";public static bool web;public static bool HasAccountWebPage(string url)=>web;}
class Service {static Service s=new Service();public static Service gI()=>s;public int deleted;public void sendDelAcc(){deleted++;}public void openMenu(int n){}public void getFlag(int a,int b){}public void openUIZone(){}}
static class InfoDlg {public static void showWait(){}}
static class EmailLinkFlow {public static int opened;public static void Open(){opened++;}}
namespace WorldChat {static class WorldChatHud {public static void Open(){}}}
namespace Hud {static class HudLayout {public static void Invalidate(){}}}
class MyVector {public int size()=>0;public object elementAt(int i)=>null;}
class GameInfo {public bool hasRead;}
class mGraphics {public List<string> labels=new List<string>();public void setClip(int a,int b,int c,int d){}public void translate(int a,int b){}public void setColor(int c){}public void fillRect(int a,int b,int c,int d){}public void drawImage(object image,int x,int y,int align){}}
class mFont {public const int CENTER=2;public static mFont tahoma_7b_dark=new mFont();public void drawString(mGraphics g,string text,int x,int y,int align){g.labels.Add(text);}}
static class mResources {public const string radaCard="Radar",quayso="Lucky",gameInfo="Info",change_flag="Flag",change_zone="Zone",chat_world="Chat",account="Account",option="Options",change_account="Đổi tài khoản",pet="Pet",delacc="Delete",can_not_do_when_die="Dead";}
static class Rms {static Dictionary<string,string> data=new Dictionary<string,string>();internal static string loadRMSString(string k)=>data.ContainsKey(k)?data[k]:null;internal static void saveRMSString(string k,string v){data[k]=v;}}
namespace UnityEngine {static class SystemInfo {public static string deviceUniqueIdentifier="test",unsupportedIdentifier="n/a";}}
class SoundMn {static SoundMn s=new SoundMn();public static SoundMn gI()=>s;public static bool IsDelAcc;public int web;public void backToRegister(){web++;} @@SOUND@@ }
class Panel {
 public static string[] strTool;public int currentListLength,ITEM_HEIGHT,selected=-1,cmyLim,hScroll=1000,cmy,cmtoY,currentTabIndex=6;
 public int[] cmyLast=new int[8];public int xScroll,yScroll,wScroll=200;public bool InventoryFrameActive=true;
 private bool inventoryAppearanceFromSettings;private object cp;private static object imgNew;private MyVector vGameInfo=new MyVector();
 private void CloseInventoryBagDetail(){}private void SelectInventoryFrameTab(int id){currentTabIndex=id;}private void setTabBox(){}
 private void hide(){}private void doRada(){}private void setTypeGameInfo(){}private void setTypeAccount(){}private void setTypeOption(){}private void doFirePet(){}private void paintScrollArrow(mGraphics g){}
 public void Build()=>setTabTool();public void Click(int row){selected=row;doFireTool();}
 public List<string> Labels(){var g=new mGraphics();paintTools(g);return g.labels;}
 @@PANEL@@
}
class Test {
 static int checks;static void Check(bool v,string reason){checks++;if(!v)throw new Exception(reason);}
 static void Main(){
  GameCanvas.panel=new Panel();
  foreach(bool pet in new[]{false,true})foreach(bool web in new[]{false,true})foreach(bool delete in new[]{false,true})foreach(int state in new[]{0,1,2}) {
   Char.myCharz().havePet=pet;GameMidlet.web=web;SoundMn.IsDelAcc=delete;
   if(state==0)GuestCredentials.BeginLogin();else GuestCredentials.Confirmed(state==1,false);
   GameCanvas.panel.Build();var labels=GameCanvas.panel.Labels();
   Check(GameCanvas.panel.currentListLength==Panel.strTool.Length+2,"duplicate appended account row");
   Check(labels.Count==GameCanvas.panel.currentListLength,"visible row count mismatch");
   Check(labels.FindAll(t=>t==GuestCredentials.AccountMenuLabel).Count==1,"account entry not unique");
   Check(!labels.Contains(mResources.change_account),"legacy direct-switch label still present");
   Check(labels[labels.Count-2]=="Giao diện"&&labels[labels.Count-1]=="Rương đồ","other appended tools shifted");
   int row=Array.IndexOf(Panel.strTool,mResources.change_account),opened=EmailLinkFlow.opened,switched=GameCanvas.loginScr.switched;
   Check(row>=0,"missing account entry");GameCanvas.panel.Click(row);
   Check(EmailLinkFlow.opened==opened+1&&GameCanvas.loginScr.switched==switched,"account row bypassed authoritative account flow");
   if(delete){int before=Service.gI().deleted;GameCanvas.panel.Click(Panel.strTool.Length-1);Check(Service.gI().deleted==before+1,"delete account row shifted");}
   if(web){int before=SoundMn.gI().web;GameCanvas.panel.Click(Array.IndexOf(Panel.strTool,"Đăng ký trên web"));Check(SoundMn.gI().web==before+1,"web row shifted");}
  }
  Console.WriteLine("PASS: "+checks+" production account-menu construction/label/dispatch checks");
 }
}
'''
fixture=fixture.replace('@@SOUND@@',method(sound,'public void getSoundOption()')).replace('@@PANEL@@','\n'.join(method(panel,signature) for signature in ['private void setTabTool()','private void paintTools(','private void doFireTool()']))
output=ROOT/'Build/guest-tests';output.mkdir(parents=True,exist_ok=True)
with tempfile.TemporaryDirectory(prefix='menu-',dir=output) as folder:
    path=Path(folder);source=path/'Menu.cs';source.write_text(fixture,encoding='utf-8');exe=path/'Menu.exe'
    subprocess.run([r'D:\setup\VS2026\MSBuild\Current\Bin\Roslyn\csc.exe','/nologo',f'/out:{exe}',str(source),str(ROOT/'Assets/Scripts/GuestCredentials.cs')],check=True)
    subprocess.run([str(exe)],check=True)
