using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Assets.src.g;

// Forms run on the Unity update thread; the network reader only enqueues results.
internal sealed class EmailLinkFlow : IActionListener
{
    private static readonly EmailLinkFlow instance = new EmailLinkFlow();
    private sealed class Reply { internal int Action; internal bool Success, Dev; internal string Text, Email; }
    private static readonly ConcurrentQueue<Reply> replies = new ConcurrentQueue<Reply>();
    private static string pendingEmail = string.Empty;
    private static ClientInput form;
    private static long waitingUntil;
    private static bool verifying;
    private static int stateAttempts;
    private static long nextStateRequest;

    internal static void ResetForLogin() {
        while (replies.TryDequeue(out _)) { }
        pendingEmail = string.Empty; waitingUntil = 0;
        stateAttempts = 0; nextStateRequest = 0;
        ClearForm();
    }

    internal static void Read(myReader reader) {
        int action = reader.readByte();
        if(action==9){GuestSwitchFlow.Read(reader);return;}
        if (action == 1) { replies.Enqueue(new Reply { Action = 1, Success = true, Dev = reader.readBool() }); return; }
        if (action == 7) {
            bool guest = reader.readBool(), dev = reader.readBool();
            replies.Enqueue(new Reply { Action = 7, Success = guest, Dev = dev }); return;
        }
        if (action != 3 && action != 5) return;
        var reply = new Reply { Action = action, Success = reader.readBool(), Text = reader.readUTF(), Email = reader.readUTF() };
        replies.Enqueue(reply);
    }
    internal static void Update() {
        while (replies.TryDequeue(out Reply reply)) {
            if (reply.Action == 1 || reply.Action == 7) {
                GuestCredentials.Confirmed(reply.Success, reply.Dev);
                continue;
            }
            if (!GuestCredentials.IsGuest) continue;
            waitingUntil = 0;
            bool ownsForm = form != null && GameCanvas.currentScreen == form;
            if (reply.Action == 5 && reply.Success) {
                GuestCredentials.Linked(reply.Email);
                if (GameCanvas.loginScr != null) {
                    GameCanvas.loginScr.isLogin2 = false;
                    GameCanvas.loginScr.tfUser.setText(reply.Email);
                    GameCanvas.loginScr.tfPass.setText(string.Empty);
                }
                pendingEmail = string.Empty;
                ClearForm();
                if (ownsForm) GameScr.instance.switchToMe();
                GameCanvas.startOKDlg(reply.Text);
            } else if (reply.Action == 3 && reply.Success) {
                pendingEmail = reply.Email;
                if (ownsForm) OpenVerify();
                else GameCanvas.startOKDlg(reply.Text + " Vào Liên kết email để nhập mã.");
            } else {
                GameCanvas.startOKDlg(reply.Text);
            }
        }
        // Login normally pushes the state. Recover a dropped packet or an Editor
        // domain reload automatically, without making the email button do a query.
        if (!GuestCredentials.StatusKnown && GameScr.instance != null
            && GameCanvas.currentScreen == GameScr.instance && stateAttempts < 3
            && mSystem.currentTimeMillis() >= nextStateRequest) {
            stateAttempts++;
            nextStateRequest = mSystem.currentTimeMillis() + stateAttempts * 3000;
            Service.gI().requestAccountState();
        }
    }
    internal static void Open() {
        if (!GuestCredentials.StatusKnown) {
            GameCanvas.startOKDlg("Trạng thái tài khoản chưa được đồng bộ. Vui lòng đăng nhập lại."); return;
        }
        OpenConfirmed();
    }
    private static void OpenConfirmed() {
        if (GuestCredentials.IsPermanent) {
            GameCanvas.menu.showMenu = false;
            MyVector accountOptions = new MyVector();
            accountOptions.addElement(new Command("Đổi tài khoản", GameCanvas.panel, 10019, null));
            GameCanvas.menu.startAt(accountOptions, 0);
            return;
        }
        if (!GuestCredentials.IsGuest) return;
        GameCanvas.menu.showMenu = false;
        MyVector options = new MyVector();
        if (string.IsNullOrEmpty(pendingEmail)) options.addElement(new Command("Liên kết email", instance, 4, null));
        else {
            options.addElement(new Command("Nhập mã xác minh", instance, 3, null));
            options.addElement(new Command("Gửi lại / Đổi email", instance, 4, null));
        }
        options.addElement(new Command("Đổi tài khoản", GameCanvas.panel, 10019, null));
        GameCanvas.menu.startAt(options, 0);
    }
    private static ClientInput NewForm(int count, string title, string submit) {
        ClearForm();
        GameCanvas.panel.hide(); GameCanvas.menu.showMenu = false;
        ClientInput input = ClientInput.gI(); input.setInput(count, title);
        foreach (TField field in input.tf) field.loginInput = true;
        input.center.caption = submit; input.center.actionListener = instance; input.center.idAction = 1;
        input.left.caption = mResources.CANCEL; input.left.actionListener = instance; input.left.idAction = 2;
        GameCanvas.clearAllPointerEvent(); GameCanvas.clearKeyPressed(); GameCanvas.clearKeyHold();
        return form = input;
    }
    private static void OpenEmail() {
        verifying = false;
        ClientInput input = NewForm(1, "Liên kết email để giữ tài khoản vĩnh viễn", "Gửi mã");
        input.tf[0].name = "Email của bạn"; input.tf[0].setMaxTextLenght(254);
        input.tf[0].setText(pendingEmail);
    }
    private static void OpenVerify() {
        verifying = true;
        ClientInput input = NewForm(3, "Nhập mã email (10 phút) và đặt mật khẩu", "Liên kết");
        input.tf[0].name = "Mã xác minh 6 số"; input.tf[0].setIputType(TField.INPUT_TYPE_NUMERIC); input.tf[0].setMaxTextLenght(6);
        input.tf[1].name = "Mật khẩu mới (8-64 ký tự)";
        input.tf[2].name = "Nhập lại mật khẩu";
        for (int i = 1; i < 3; i++) { input.tf[i].setIputType(TField.INPUT_TYPE_PASSWORD); input.tf[i].setMaxTextLenght(64); }
        input.enablePasswordToggle();
    }
    private static void ClearForm() {
        if (form == null) return;
        foreach (TField field in form.tf) field.setText(string.Empty);
        GameCanvas.closeKeyBoard();
        form.clearScreen(); form = null;
    }
    public void perform(int action, object unused) {
        if (action == 2) { ClearForm(); GameScr.instance.switchToMe(); return; }
        if (action == 3) { OpenVerify(); return; }
        if (action == 4) { OpenEmail(); return; }
        if (action != 1 || form == null || GameCanvas.currentScreen != form) return;
        if (mSystem.currentTimeMillis() < waitingUntil) { GameCanvas.startOKDlg("Đang xử lý yêu cầu, vui lòng chờ."); return; }
        if (!verifying) {
            string email = form.tf[0].getText().Trim().ToLowerInvariant();
            if (email.Length > 254 || !Regex.IsMatch(email, @"\A[^\s@<>]+@[^\s@<>]+\.[^\s@<>]+\z")) {
                GameCanvas.startOKDlg("Vui lòng nhập email hợp lệ."); return;
            }
            waitingUntil = mSystem.currentTimeMillis() + 90000;
            Service.gI().requestEmailLink(email);
        } else {
            string code = form.tf[0].getText(), password = form.tf[1].getText();
            if (!Regex.IsMatch(code, @"\A[0-9]{6}\z")) { GameCanvas.startOKDlg("Mã xác minh phải gồm 6 chữ số."); return; }
            if (!Regex.IsMatch(password, @"\A[ -~]{8,64}\z") || password != password.Trim()) {
                GameCanvas.startOKDlg("Mật khẩu cần từ 8-64 ký tự và không có khoảng trắng ở đầu/cuối."); return;
            }
            if (password != form.tf[2].getText()) { GameCanvas.startOKDlg("Hai mật khẩu không khớp."); return; }
            waitingUntil = mSystem.currentTimeMillis() + 30000;
            Service.gI().confirmEmailLink(code, password);
        }
    }
}
