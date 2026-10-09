public sealed class CustomServerDialog : Dialog, IActionListener
{
    private readonly TField address = new TField();
    private readonly TField port = new TField();
    private readonly bool previousAutoConnect;
    private readonly int x, y, width;
    private string error;

    public CustomServerDialog()
    {
        previousAutoConnect = ServerListScreen.isAutoConect;
        ServerListScreen.isAutoConect = false;
        ServerListScreen.flagServer = 0;
        width = System.Math.Min(320, GameCanvas.w - 12);
        x = (GameCanvas.w - width) / 2;
        y = System.Math.Max(38, (GameCanvas.h - 210) / 2);
        address.x = port.x = x + 10;
        address.width = port.width = width - 20;
        address.height = port.height = mScreen.ITEM_HEIGHT + 2;
        address.y = y + 38;
        port.y = address.y + address.height + 20;
        address.name = "Địa chỉ";
        port.name = "Cổng";
        address.loginInput = port.loginInput = true;
        address.setMaxTextLenght(253);
        port.setMaxTextLenght(5);
        port.setIputType(TField.INPUT_TYPE_NUMERIC);
        address.setText(CustomServerAddress.Host);
        port.setText(CustomServerAddress.Enabled ? CustomServerAddress.Port.ToString() : GameMidlet.DefaultServerPort.ToString());
        address.cmdDoneAction = new Command("Next", this, 4, null);
        port.cmdDoneAction = new Command("Done", this, 1, null);
        left = new Command("Hủy", this, 2, null);
        center = new Command("Lưu", this, 1, null);
        right = new Command("Về mặc định", this, 3, null);
        address.setFocus(true);
    }

    public override void show() { GameCanvas.currentDialog = this; }

    public override void paint(mGraphics g)
    {
        g.translate(-g.getTranslateX(), -g.getTranslateY());
        g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
        PopUp.paintPopUp(g, x, y, width, 190, -1, true);
        mFont.tahoma_7b_dark.drawString(g, "Nhập IP server", GameCanvas.hw, y + 8, 2);
        mFont.tahoma_7b_dark.drawString(g, "Địa chỉ (IPv4 / hostname)", x + 10, address.y - 15, 0);
        mFont.tahoma_7b_dark.drawString(g, "Cổng (1–65535)", x + 10, port.y - 15, 0);
        address.paint(g);
        port.paint(g);
        g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
        if (error != null)
        {
            string[] lines = mFont.tahoma_7_red.splitFontArray(error, width - 20);
            for (int i = 0; i < lines.Length; i++)
                mFont.tahoma_7_red.drawString(g, lines[i], x + 10, port.y + port.height + 6 + i * 12, 0);
        }
        base.paint(g);
    }

    public override void keyPress(int keyCode)
    {
        (port.isFocus ? port : address).keyPressed(keyCode);
        base.keyPress(keyCode);
    }

    public override void update()
    {
        address.update();
        port.update();
        if (GameCanvas.keyPressed[2] || GameCanvas.keyPressed[8]
            || GameCanvas.keyPressed[21] || GameCanvas.keyPressed[22])
        {
            bool focusPort = !port.isFocus;
            address.setFocus(!focusPort);
            port.setFocus(focusPort);
        }
        if (GameCanvas.currentDialog == this) base.update();
    }

    private void Close()
    {
        if (TField.kb != null) TField.kb.active = false;
        TField.kb = null;
        TField.currentTField = null;
        Main.closeKeyBoard();
        GameCanvas.endDlg();
        ServerListScreen.isAutoConect = previousAutoConnect;
    }

    public void perform(int action, object unused)
    {
        if (GameCanvas.currentDialog != this) return;
        if (action == 4)
        {
            address.setFocus(false);
            port.setFocusWithKb(true);
            return;
        }
        if (action == 1)
        {
            string host;
            int number;
            if (!CustomServerAddress.Validate(address.getText(), port.getText(), out host, out number, out error)) return;
            CustomServerAddress.Save(host, number);
        }
        else if (action == 3) CustomServerAddress.Reset();
        Close();
    }
}
