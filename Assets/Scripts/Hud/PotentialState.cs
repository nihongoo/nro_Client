using System;
using System.Collections.Concurrent;

namespace Hud
{
    internal static class PotentialState
    {
        private sealed class Snapshot {internal int Action,Id;internal bool Success;internal string Error;internal long Budget;internal readonly long[] Values=new long[5],Limits=new long[5];}
        private static readonly ConcurrentQueue<Snapshot> replies=new ConcurrentQueue<Snapshot>();
        internal static readonly long[] Limits=new long[5];
        internal static bool Ready {get;private set;}
        internal static bool Pending {get;private set;}
        internal static bool Unsupported {get;private set;}
        private static int serial,pendingId;
        private static long sentAt,firstRequest=-1,lastRequest=-1;
        internal static long Value(int type) {
            Char c=Char.myCharz();return type==0?c.cHPGoc:type==1?c.cMPGoc:type==2?c.cDamGoc:type==3?c.cDefGoc:c.cCriticalGoc;
        }
        internal static void Reset() {while(replies.TryDequeue(out _)){}Ready=Pending=Unsupported=false;pendingId=0;firstRequest=lastRequest=-1;Array.Clear(Limits,0,5);}
        internal static void Request() {
            lastRequest=mSystem.currentTimeMillis();
            if(!Ready&&firstRequest<0)firstRequest=lastRequest;
            Service.gI().potentialRequest(4,0,0,0,0);
        }
        // An old B1 server answers action 4 with action 0. Only an actual B2
        // snapshot establishes support; local prices cannot supply server caps.
        internal static bool RequireSupport() {
            if(Ready)return true;
            long now=mSystem.currentTimeMillis();
            if(firstRequest>=0&&now-firstRequest>=2000)Unsupported=true;
            if(lastRequest<0||now-lastRequest>=2000)Request();
            GameCanvas.startOKDlg(Unsupported?"Server chưa hỗ trợ":"Đang đồng bộ tiềm năng. Vui lòng thử lại.");
            return false;
        }
        internal static bool CanRaise(int type) {
            if(!Ready||Pending||type<0||type>4)return false;
            Char c=Char.myCharz();if(c.cHP<=0||c.statusMe==5||c.statusMe==14)return false;
            long current=Value(type),next=current+PotentialCost.Step(type);
            return next<=Limits[type]&&PotentialCost.TryCalculate(type,current,next,out long price)&&price<=c.cTiemNang;
        }
        internal static bool Submit(int type,long target) {
            if(!RequireSupport())return false;
            if(!CanRaise(type)||target>Limits[type]||!PotentialCost.TryCalculate(type,Value(type),target,out long cost)||cost>Char.myCharz().cTiemNang)return false;
            if(serial==int.MaxValue)serial=0;pendingId=++serial;Pending=true;sentAt=mSystem.currentTimeMillis();
            Service.gI().potentialRequest(5,pendingId,type,Value(type),target);return true;
        }
        internal static void Read(int action,Message packet) {
            var s=new Snapshot {Action=action,Id=packet.reader().readInt(),Success=packet.reader().readBool(),Error=packet.reader().readUTF(),Budget=packet.reader().readLong()};
            for(int i=0;i<5;i++)s.Values[i]=packet.reader().readLong();
            for(int i=0;i<5;i++)s.Limits[i]=packet.reader().readLong();
            if(s.Budget<0)return;
            for(int i=0;i<5;i++)if(s.Values[i]<0||s.Values[i]>int.MaxValue||s.Limits[i]<0||s.Limits[i]>int.MaxValue)return;
            replies.Enqueue(s);
        }
        internal static void Update() {
            while(replies.TryDequeue(out Snapshot s)) {
                if(s.Action==5&&s.Id!=pendingId)continue;
                if(s.Action==5){Pending=false;pendingId=0;}
                Char c=Char.myCharz();c.cHPGoc=(int)s.Values[0];c.cMPGoc=(int)s.Values[1];c.cDamGoc=(int)s.Values[2];c.cDefGoc=(int)s.Values[3];c.cCriticalGoc=(int)s.Values[4];c.cTiemNang=s.Budget;
                Array.Copy(s.Limits,Limits,5);Ready=true;Unsupported=false;firstRequest=-1;
                if(!s.Success)GameCanvas.startOKDlg(s.Error);
            }
            if(!Ready&&firstRequest>=0&&mSystem.currentTimeMillis()-firstRequest>=2000)Unsupported=true;
            if(Pending&&mSystem.currentTimeMillis()-sentAt>=5000) {
                Pending=Ready=false;pendingId=0;Request();
                GameCanvas.startOKDlg("Chưa nhận được kết quả nâng chỉ số. Đang đồng bộ lại.");
            }
        }
    }
}
