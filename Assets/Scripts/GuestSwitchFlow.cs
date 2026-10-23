using System;
using System.Collections.Concurrent;

internal sealed class GuestSwitchFlow : IActionListener
{
    private static readonly GuestSwitchFlow instance=new GuestSwitchFlow();
    private sealed class Reply {internal bool Success;internal string Text;}
    private static readonly ConcurrentQueue<Reply> replies=new ConcurrentQueue<Reply>();
    private static bool waiting;
    private static long deadline;
    private static bool NeedsGuestDeletion => GuestCredentials.IsGuest||GuestCredentials.DeletePending
        ||(!GuestCredentials.IsPermanent&&GuestCredentials.Preferred&&GuestCredentials.HasToken);
    internal static void Open() {
        if(waiting){GameCanvas.startOKDlg("Đang xử lý đổi tài khoản, vui lòng chờ.");return;}
        bool guest=NeedsGuestDeletion;
        if(!GuestCredentials.StatusKnown&&!guest&&GameScr.instance!=null&&GameCanvas.currentScreen==GameScr.instance){GameCanvas.startOKDlg("Chưa đồng bộ tài khoản. Vui lòng thử lại.");return;}
        GameCanvas.startYesNoDlg(guest
            ?"Đổi tài khoản sẽ XÓA VĨNH VIỄN khách chưa liên kết và nhân vật hiện tại. Không thể khôi phục. Bạn xác nhận xóa khách và đổi tài khoản?"
            :"Bạn muốn đổi tài khoản và quay về màn hình đăng nhập?",
            new Command(guest?"Xóa khách và đổi":"Đổi tài khoản",instance,1,null),new Command("Hủy",instance,0,null));
    }
    public void perform(int action,object unused) {
        GameCanvas.endDlg();if(action!=1)return;
        if(!NeedsGuestDeletion){GameCanvas.instance.completeAccountSwitch();return;}
        if(waiting)return;
        try {GuestCredentials.BeginDeletion();waiting=true;deadline=mSystem.currentTimeMillis()+5000;Service.gI().requestGuestDeletion(GuestCredentials.GetSecret(),GuestCredentials.GetDevice());}
        catch(Exception){waiting=false;GameCanvas.startOKDlg("Chưa gửi được yêu cầu xóa khách. Vui lòng thử lại.");}
    }
    internal static void Read(myReader reader){replies.Enqueue(new Reply{Success=reader.readBool(),Text=reader.readUTF()});}
    internal static void Update() {
        while(replies.TryDequeue(out Reply reply)) {
            waiting=false;
            if(!GuestCredentials.DeletePending)continue;
            if(reply.Success){GuestCredentials.Deleted();GameCanvas.instance.completeAccountSwitch();}
            else {GuestCredentials.CancelDeletion();GameCanvas.startOKDlg(reply.Text);}
        }
        if(waiting&&mSystem.currentTimeMillis()>=deadline){waiting=false;GameCanvas.startOKDlg("Chưa nhận được xác nhận xóa khách. Thử lại hoặc kết nối lại để kiểm tra; chưa tự tạo khách mới.");}
    }
}
