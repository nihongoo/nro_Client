using UnityEngine;

public class ChatTextField : IActionListener
{
	private static ChatTextField instance;

	public TField tfChat;

	public bool isShow;

	public IChatable parentScreen;

	private long lastChatTime;

	public Command left;

	public Command cmdChat;

	public Command right;

	public Command center;

	private int x;

	private int y;

	private int w;

	private int h;

    private readonly WorldChat.WorldChatComposer composer = new WorldChat.WorldChatComposer();
    private int worldResultRevision;
    private long submittedRequest;
    private string submittedText;
    private bool ownsSubmittedDraft;
    private bool modalPointerHeld, modalPointerClick;
    private bool HasChannelTabs => parentScreen == GameScr.instance && parentScreen != null && string.IsNullOrEmpty(to);
    private int TabBand => HasChannelTabs ? mScreen.cmdH + 6 : 0;
    private Rect TabBounds(bool world) => new Rect(tfChat.x + (world ? (tfChat.width + 6) / 2 : 0),
        tfChat.y - mScreen.cmdH - 4, (tfChat.width - 6) / 2, mScreen.cmdH);

    public static void ResetWorldSession()
    {
        if (instance == null) return;
        if (instance.composer.WorldMode || instance.ownsSubmittedDraft) { instance.tfChat.setText(string.Empty); instance.isShow = false; }
        instance.modalPointerHeld = instance.modalPointerClick = false;
        instance.composer.Reset(); instance.submittedRequest = 0; instance.ownsSubmittedDraft = false;
        instance.worldResultRevision = WorldChat.WorldChatStore.ResultRevision;
    }
    // Consume the entire frame, including the click that closes/sends the form.
    public void UpdateModalInput()
    {
        if (!isShow) return;
        // Canvas flags are cleared before gameplay each frame. Keep the gesture here
        // until release so tabs, commands and the input retain normal click semantics.
        if (GameCanvas.isPointerJustDown || (!modalPointerHeld && GameCanvas.isPointerDown)) {
            modalPointerHeld = true; modalPointerClick = GameCanvas.isPointerClick;
        }
        if (GameCanvas.isPointerMove) modalPointerClick = false;
        if (modalPointerHeld) {
            GameCanvas.isPointerDown = !GameCanvas.isPointerJustRelease;
            GameCanvas.isPointerClick = modalPointerClick;
        }
        if (GameCanvas.isPointerJustRelease) { modalPointerHeld = false; modalPointerClick = false; }
        if (GameCanvas.keyAsciiPress != 0) keyPressed(GameCanvas.keyAsciiPress);
        if (left != null && (GameCanvas.keyPressed[12] || mScreen.getCmdPointerLast(left))) left.performAction();
        else if (right != null && (GameCanvas.keyPressed[13] || mScreen.getCmdPointerLast(right))) right.performAction();
        else if (center != null && (GameCanvas.keyPressed[(!Main.isPC) ? 5 : 25] || mScreen.getCmdPointerLast(center))) center.performAction();
        if (isShow) update();
        ConsumeInput();
    }
    public void InsertDesktopText(string text)
    {
        if (!isShow || text == null) return;
        var committed = new System.Text.StringBuilder();
        foreach (char c in text) if (!char.IsControl(c)) committed.Append(c);
        int length = System.Math.Min(committed.Length, System.Math.Max(0, tfChat.getMaxTextLenght() - tfChat.getText().Length));
        if (length > 0 && char.IsHighSurrogate(committed[length - 1])) length--;
        if (length > 0) tfChat.insertText(committed.ToString(0, length));
    }
    public void OpenWorldChat()
    {
        startChat(GameScr.gI(), string.Empty);
        composer.Select(true); ApplyLimit();
    }
    private void ApplyLimit()
    {
        tfChat.setMaxTextLenght(composer.WorldMode ? (WorldChat.WorldChatStore.Ready ? WorldChat.WorldChatStore.MaxCharacters * 2 : 200) : 80);
    }
    private void PrepareNormal(IChatable listener, string target)
    {
        modalPointerHeld = modalPointerClick = false;
        parentScreen = listener; to = target; composer.Select(false); ownsSubmittedDraft = false; ApplyLimit(); ConsumeInput();
    }
    private static void ConsumeInput()
    {
        GameCanvas.clearAllPointerEvent(); GameCanvas.clearKeyHold(); GameCanvas.clearKeyPressed(); GameCanvas.keyAsciiPress = 0;
    }
    private string BalanceError()
    {
        bool gems = Char.myCharz().luong < WorldChat.WorldChatStore.GemFee;
        bool gold = Char.myCharz().xu < WorldChat.WorldChatStore.GoldFee;
        return gems || gold ? "Không đủ " + (gems && gold ? "gem và vàng" : gems ? "gem" : "vàng") + "." : null;
    }
    private void ResumeWorldForm()
    {
        modalPointerHeld = modalPointerClick = false;
        GameCanvas.endDlg(); if (GameCanvas.currentScreen != GameScr.instance) { isShow = false; return; }
        isShow = true; tfChat.isFocus = true; right.caption = mResources.CLOSE; ConsumeInput();
    }
    private void HideWorldKeyboard()
    {
        GameCanvas.closeKeyBoard();
        if (TField.kb != null) TField.kb.active = false;
        TField.kb = null;
    }
    private void WorldError(string error)
    {
        isShow = false; HideWorldKeyboard(); ConsumeInput();
        GameCanvas.msgdlg.setInfo(error, null, new Command(mResources.OK, this, 8005, null), null);
        GameCanvas.currentDialog = GameCanvas.msgdlg;
    }
    private void SendCurrent(bool throttleNormal = true)
    {
        if (parentScreen == null) return;
        if (!HasChannelTabs) composer.Select(false);
        if (composer.WorldMode && WorldChat.WorldChatStore.Ready) {
            string funds = BalanceError(); if (funds != null) { WorldError(funds); return; }
        }
        if (throttleNormal && !composer.WorldMode && mSystem.currentTimeMillis() - lastChatTime < 1000) return;
        string error = composer.Submit(tfChat.getText(), text => {
            lastChatTime = mSystem.currentTimeMillis();
            parentScreen.onChatFromMe(text, to); tfChat.setText(string.Empty);
            right.caption = mResources.CLOSE; tfChat.clearKb();
        }, question => {
            isShow = false; HideWorldKeyboard(); ConsumeInput();
            GameCanvas.startYesNoDlg(question, new Command("Đồng ý", this, 8003, null), new Command("Từ chối", this, 8004, null));
        });
        if (error != null) WorldError(error);
    }
    public void HandleWorldResult()
    {
        if (worldResultRevision == WorldChat.WorldChatStore.ResultRevision) return;
        worldResultRevision = WorldChat.WorldChatStore.ResultRevision;
        if (submittedRequest == 0 || WorldChat.WorldChatStore.ResultText == null) return;
        submittedRequest = 0;
        if (WorldChat.WorldChatStore.ResultAccepted && ownsSubmittedDraft && composer.WorldMode && tfChat.getText() == submittedText) {
            tfChat.setText(string.Empty); isShow = false; HideWorldKeyboard(); ConsumeInput();
        }
        if (!WorldChat.WorldChatStore.ResultAccepted && isShow && composer.WorldMode && GameCanvas.currentDialog == null)
            WorldError(WorldChat.WorldChatStore.ResultText);
        else GameScr.info1.addInfo(WorldChat.WorldChatStore.ResultText, 0);
    }

	public Command cmdChat2;

	public int yBegin;

	public int yUp;

	public int KC;

	public string to;

	public string strChat = "Chat ";

	public ChatTextField()
	{
		tfChat = new TField();
		if (Main.isWindowsPhone)
		{
			tfChat.showSubTextField = false;
		}
		if (Main.isIPhone)
		{
			tfChat.isPaintMouse = false;
		}
		tfChat.name = "chat";
		if (Main.isWindowsPhone)
		{
			tfChat.strInfo = tfChat.name;
		}
		tfChat.width = GameCanvas.w - 6;
		if (Main.isPC && tfChat.width > 250)
		{
			tfChat.width = 250;
		}
		tfChat.height = mScreen.ITEM_HEIGHT + 2;
		tfChat.x = GameCanvas.w / 2 - tfChat.width / 2;
		tfChat.isFocus = true;
		tfChat.setMaxTextLenght(80);
	}

	public void initChatTextField()
	{
		left = new Command(mResources.OK, this, 8000, null, 1, GameCanvas.h - mScreen.cmdH + 1);
		right = new Command(mResources.DELETE, this, 8001, null, GameCanvas.w - 70, GameCanvas.h - mScreen.cmdH + 1);
		center = null;
		w = tfChat.width + 20;
		h = tfChat.height + 26;
		x = GameCanvas.w / 2 - w / 2;
		y = tfChat.y - 18;
		if (Main.isPC && w > 320)
		{
			w = 320;
		}
		left.x = x;
		right.x = x + w - 68;
		if (GameCanvas.isTouch)
		{
			tfChat.y -= 5;
			y -= 20;
			h += 30;
			left.x = GameCanvas.w / 2 - 68 - 5;
			right.x = GameCanvas.w / 2 + 5;
			left.y = GameCanvas.h - 30;
			right.y = GameCanvas.h - 30;
		}
		cmdChat = new Command();
        cmdChat.actionChat = delegate(string str) {
            tfChat.justReturnFromTextBox = false; tfChat.setText(str); SendCurrent(false);
        };
        cmdChat2 = new Command();
        cmdChat2.actionChat = delegate(string str) {
            tfChat.justReturnFromTextBox = false; tfChat.setText(str); SendCurrent(false);
            if (!composer.WorldMode && tfChat.getText() == string.Empty) {
                tfChat.clearKb(); if (right != null) right.performAction(); isShow = false;
            }
        };
		yBegin = tfChat.y;
		yUp = GameCanvas.h / 2 - 2 * tfChat.height;
		if (Main.isWindowsPhone)
		{
			tfChat.showSubTextField = false;
		}
		if (Main.isIPhone)
		{
			tfChat.isPaintMouse = false;
		}
	}

	public void updateWhenKeyBoardVisible()
	{
	}

	public void keyPressed(int keyCode)
	{
		if (isShow)
		{
			tfChat.keyPressed(keyCode);
		}
		if (HasChannelTabs || tfChat.getText().Equals(string.Empty))
		{
			right.caption = mResources.CLOSE;
		}
		else
		{
			right.caption = mResources.DELETE;
		}
	}

	public static ChatTextField gI()
	{
		return (instance != null) ? instance : (instance = new ChatTextField());
	}

	public void startChat(int firstCharacter, IChatable parentScreen, string to)
	{
        PrepareNormal(parentScreen, to);
		right.caption = mResources.CLOSE;
		if (Main.isWindowsPhone)
		{
			tfChat.showSubTextField = false;
		}
		if (Main.isIPhone)
		{
			tfChat.isPaintMouse = false;
		}
		tfChat.keyPressed(firstCharacter);
		if (!tfChat.getText().Equals(string.Empty) && GameCanvas.currentDialog == null)
		{
			this.parentScreen = parentScreen;
			isShow = true;
		}
	}

	public void startChat(IChatable parentScreen, string to)
	{
        PrepareNormal(parentScreen, to);
		right.caption = mResources.CLOSE;
		if (Main.isWindowsPhone)
		{
			tfChat.showSubTextField = false;
		}
		if (Main.isIPhone)
		{
			tfChat.isPaintMouse = false;
		}
		if (GameCanvas.currentDialog == null)
		{
			isShow = true;
			tfChat.isFocus = true;
			if (!Main.isPC)
			{
				ipKeyboard.openKeyBoard(strChat, ipKeyboard.TEXT, string.Empty, cmdChat);
				tfChat.setFocusWithKb(true);
			}
		}
		tfChat.setText(string.Empty);
		tfChat.clearAll();
	}

	public void startChat2(IChatable parentScreen, string to)
	{
        PrepareNormal(parentScreen, to);
		tfChat.setFocusWithKb(true);
		if (Main.isWindowsPhone)
		{
			tfChat.showSubTextField = false;
		}
		if (Main.isIPhone)
		{
			tfChat.isPaintMouse = false;
		}
		if (GameCanvas.currentDialog == null)
		{
			isShow = true;
			if (!Main.isPC)
			{
				ipKeyboard.openKeyBoard(strChat, ipKeyboard.TEXT, string.Empty, cmdChat2);
				tfChat.setFocusWithKb(true);
			}
		}
		tfChat.setText(string.Empty);
		tfChat.clearAll();
	}

	public void updateKey()
	{
	}

	public void update()
	{
		if (!isShow)
		{
			return;
		}
        if (HasChannelTabs && GameCanvas.currentDialog == null && GameCanvas.isPointerJustRelease && GameCanvas.isPointerClick) {
            Vector2 point = new Vector2(GameCanvas.px, GameCanvas.py);
            if (TabBounds(false).Contains(point) || TabBounds(true).Contains(point)) {
                composer.Select(TabBounds(true).Contains(point)); if (!composer.WorldMode) ownsSubmittedDraft = false; ApplyLimit();
                if (composer.WorldMode && !WorldChat.WorldChatStore.Ready) WorldChat.WorldChatProtocol.RequestSnapshot();
                ConsumeInput(); return;
            }
        }
		tfChat.update();
		if (Main.isWindowsPhone)
		{
			updateWhenKeyBoardVisible();
		}
		if (tfChat.justReturnFromTextBox)
		{
			tfChat.justReturnFromTextBox = false;
            SendCurrent(false);
		}
		if (!Main.isPC)
		{
			return;
		}
		if (GameCanvas.keyPressed[15])
		{
			if (left != null && (composer.WorldMode || tfChat.getText() != string.Empty))
			{
				left.performAction();
			}
			GameCanvas.keyPressed[15] = false;
			GameCanvas.keyPressed[(!Main.isPC) ? 5 : 25] = false;
		}
		if (GameCanvas.keyPressed[14])
		{
			if (right != null)
			{
				right.performAction();
			}
			GameCanvas.keyPressed[14] = false;
		}
	}

	public void close()
	{
        modalPointerHeld = modalPointerClick = false;
		tfChat.setText(string.Empty);
		isShow = false;
	}

	public void paint(mGraphics g)
	{
		if (isShow && !Main.isIPhone)
		{
			int num = ((!Main.isWindowsPhone) ? (y - KC) : (tfChat.y - 5));
			int num2 = ((!Main.isWindowsPhone) ? x : 0);
			int num3 = ((!Main.isWindowsPhone) ? w : GameCanvas.w);
			PopUp.paintPopUp(g, num2, num - TabBand, num3, h + TabBand, -1, true);
			if (Main.isPC)
			{
				mFont.tahoma_7b_green2.drawString(g, strChat + to, tfChat.x, tfChat.y - ((!GameCanvas.isTouch) ? 12 : 17) - TabBand, 0);
				GameCanvas.paintz.paintCmdBar(g, left, center, right);
			}
            if (HasChannelTabs) {
                for (int i = 0; i < 2; i++) {
                    Rect bounds = TabBounds(i == 1);
                    Notifications.NotificationSkin.PaintFrame(g, (int)bounds.x, (int)bounds.y, (int)bounds.width, (int)bounds.height, composer.WorldMode == (i == 1));
                    mFont font = composer.WorldMode == (i == 1) ? mFont.tahoma_7_green2 : mFont.tahoma_7_grey;
                    font.drawString(g, i == 0 ? "Thường" : "Thế giới", (int)bounds.center.x, (int)bounds.center.y - font.getHeight() / 2, 2);
                }
            }
			tfChat.paint(g);
		}
	}

	public void perform(int idAction, object p)
	{
		switch (idAction)
		{
        case 8000:
            SendCurrent(); break;
        case 8003:
            string funds = BalanceError();
            if (funds != null) { composer.Decline(); WorldError(funds); break; }
            string error; long request = composer.Agree(out error);
            if (error != null) { WorldError(error); break; }
            if (request == 0) break;
            submittedRequest = request; submittedText = composer.Draft; ownsSubmittedDraft = true;
            worldResultRevision = WorldChat.WorldChatStore.ResultRevision;
            ResumeWorldForm();
            if (!Service.gI().submitWorldChat(request, submittedText)) {
                WorldChat.WorldChatStore.SendFailed(request); submittedRequest = 0;
                WorldError("Chưa gửi được tin. Kiểm tra kết nối.");
            }
            break;
        case 8004:
            composer.Decline(); ResumeWorldForm(); break;
        case 8005:
            ResumeWorldForm(); break;
		case 8001:
			Cout.LogError("perform chat 8001");
			if (HasChannelTabs || tfChat.getText().Equals(string.Empty))
			{
				isShow = false;
                tfChat.clearKb();
				parentScreen.onCancelChat();
			}
			tfChat.clear();
			break;
		case 8002:
			break;
		}
	}
}
