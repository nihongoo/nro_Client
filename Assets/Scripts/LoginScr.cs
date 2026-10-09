using Mod.CuongLe;
using System;

public class LoginScr : mScreen, IActionListener
{
	public TField tfUser;

	public TField tfPass;

	public static bool isContinueToLogin = false;

	private int focus;

	private int wC;

	private int yL;

	private int defYL;

	public bool isCheck;

	public bool isRes;

	public Command cmdLogin;

	public Command cmdCheck;

	public Command cmdFogetPass;

	public Command cmdRes;

	public Command cmdMenu;

	public Command cmdBackFromRegister;

	public string listFAQ = string.Empty;

	public string titleFAQ;

	public string subtitleFAQ;

	private string numSupport = string.Empty;

	public static bool isLocal = false;

	public static bool isUpdateAll;

	public static bool isUpdateData;

	public static bool isUpdateMap;

	public static bool isUpdateSkill;

	public static bool isUpdateItem;

	public static string serverName;

	public static Image imgTitle;

	public int plX;

	public int plY;

	public int lY;

	public int lX;

	public int logoDes;

	public int lineX;

	public int lineY;

	public static int[] bgId = new int[5] { 0, 8, 2, 6, 9 };

	public static bool isTryGetIPFromWap;

	public static short timeLogin;

	public static long lastTimeLogin;

	public static long currTimeLogin;

	private int yt;

	private Command cmdSelect;

	private Command cmdOK;

	private int xLog;

	private int yLog;

	public static GameMidlet m;

	private int yy = GameCanvas.hh - mScreen.ITEM_HEIGHT - 5;

	private int freeAreaHeight;

	private int xP;

	private int yP;

	private int wP;

	private int hP;

	private int t = 20;

	private bool isRegistering;

	private string passRe = string.Empty;

	public bool isFAQ;

	private int tipid = -1;

	public bool isLogin2;

	private int v = 2;

	private int g;

	private int ylogo = -40;

	private int dir = 1;

	private Command cmdCallHotline;

	private Command cmdTogglePassword;

	public static bool isLoggingIn;
	public static bool sessionReplaced;
	public const string ReplacedMessage = "Tài khoản đã đăng nhập ở nơi khác";
	public const string PreviousSessionWaitMessage = "Đang lưu phiên đăng nhập cũ, vui lòng chờ...";
	private static Action queuedLogin;
	private int loginStage;
	private int stageStarted;
	private string pendingUser, pendingPassword;
	private sbyte pendingLoginType;
	private bool pendingGuestCreation;

	public void doGuestLogin(string username, bool accountCreated = false)
	{
		if (sessionReplaced) return;
		if (System.Threading.Thread.CurrentThread.Name != Main.mainThreadName)
		{
			System.Threading.Interlocked.CompareExchange(ref queuedLogin, () => doGuestLogin(username, accountCreated), null);
			return;
		}
		if (isLoggingIn && !(accountCreated && pendingGuestCreation)) return;
		isLoggingIn = true;
		isLogin2 = true;
		pendingGuestCreation = string.IsNullOrEmpty(username);
		pendingUser = username ?? string.Empty;
		pendingPassword = string.Empty;
		pendingLoginType = 1;
		loginStage = 1;
		stageStarted = Environment.TickCount;
		if (!Session_ME.gI().isConnected() && !Session_ME.connecting) GameCanvas.connect();
		GameCanvas.startWaitDlg();
	}

	// Attempt state belongs to the UI thread. Background callers only enqueue a request.
	public static void finishLoginAttempt()
	{
		isLoggingIn = false;
		isContinueToLogin = false;
		if (GameCanvas.loginScr != null)
		{
			GameCanvas.loginScr.loginStage = 0;
			GameCanvas.loginScr.pendingPassword = null;
		}
		System.Threading.Interlocked.Exchange(ref queuedLogin, null);
	}

	public static void updateLoginAttempt()
	{
		if (GameCanvas.loginScr == null) return;
		Action request = System.Threading.Interlocked.Exchange(ref queuedLogin, null);
		if (request != null) request();
		LoginScr screen = GameCanvas.loginScr;
		if (!isLoggingIn) return;
		if ((GameCanvas.currentScreen is GameScr && !Char.isLoadingMap && !GameCanvas.isLoading)
			|| GameCanvas.currentScreen is CreateCharScr)
		{
			finishLoginAttempt();
			return;
		}
		if (screen.loginStage == 1 && Session_ME.readyForLogin()
			&& !Controller.isConnectOK && !Controller.isConnectionFail && !Controller.isDisconnected)
		{
			screen.loginStage = 2;
			screen.stageStarted = Environment.TickCount;
			if (screen.pendingGuestCreation) Service.gI().login2(string.Empty);
			else Service.gI().login(screen.pendingUser, screen.pendingPassword, GameMidlet.VERSION, screen.pendingLoginType);
			screen.pendingPassword = null;
		}
		int deadline = screen.loginStage == 1 ? 15000 : 20000;
		if (unchecked(Environment.TickCount - screen.stageStarted) >= deadline)
		{
			failLoginAttempt(screen.loginStage == 1
				? "Kết nối tới máy chủ quá lâu. Kiểm tra mạng rồi thử lại."
				: "Máy chủ phản hồi quá lâu. Vui lòng thử đăng nhập lại.");
		}
	}

	public static bool failLoginAttempt(string message)
	{
		if (!isLoggingIn) return false;
		LoginScr screen = GameCanvas.loginScr;
		Session_ME.gI().close();
		Session_ME.gI().clearSendingMessage();
		Session_ME.clearReceivedMessages();
		Controller.isConnectionFail = Controller.isDisconnected = Controller.isConnectOK = false;
		finishLoginAttempt();
		timeLogin = 0;
		Char.isLoadingMap = false;
		GameCanvas.isLoading = false;
		Controller.isLoadingData = false;
		Main.isMiniApp = true;
		if (GameCanvas.currentScreen is GameScr)
		{
			GameCanvas.instance.doResetToLoginScr(GameCanvas.serverScreen);
			GameCanvas.loginScr = screen;
		}
		GameCanvas.endDlg();
		ServerListScreen.isAutoConect = false;
		if (GameCanvas.currentScreen == GameCanvas.serverScreen) GameCanvas.loginScr.switchToMe();
		GameCanvas.loginScr.focusLoginField(false);
		if (CustomServerAddress.Enabled) CustomServerAddress.ShowConnectionError(message);
		else GameCanvas.startOKDlg(message);
		return true;
	}

	public static void failInitialMapLoad()
	{
		GameCanvas.isLoading = false;
		failLoginAttempt("Không thể tải dữ liệu bản đồ. Vui lòng đăng nhập lại.");
	}

	public static void authenticationAccepted()
	{
		timeLogin = 0;
		if (!isLoggingIn) return;
		// Bound resource/map loading separately after authentication succeeds.
		GameCanvas.loginScr.loginStage = 3;
		GameCanvas.loginScr.stageStarted = Environment.TickCount;
	}

	public LoginScr()
	{
		yLog = GameCanvas.hh - 30;
		TileMap.bgID = (sbyte)(mSystem.currentTimeMillis() % 9);
		if (TileMap.bgID == 5 || TileMap.bgID == 6)
		{
			TileMap.bgID = 4;
		}
		GameScr.loadCamera(true, -1, -1);
		GameScr.cmx = 100;
		GameScr.cmy = 200;
		Main.closeKeyBoard();
		if (GameCanvas.h > 200)
		{
			defYL = GameCanvas.hh - 80;
		}
		else
		{
			defYL = GameCanvas.hh - 65;
		}
		resetLogo();
		int num = (wC = ((GameCanvas.w < 200) ? 140 : 160));
		yt = GameCanvas.hh - mScreen.ITEM_HEIGHT - 5;
		if (GameCanvas.h <= 160)
		{
			yt = 20;
		}
		tfUser = new TField();
		tfUser.y = GameCanvas.hh - mScreen.ITEM_HEIGHT - 9;
		tfUser.width = wC;
		tfUser.height = mScreen.ITEM_HEIGHT + 2;
		tfUser.isFocus = true;
		tfUser.setIputType(TField.INPUT_TYPE_ANY);
		tfUser.loginInput = true;
		tfUser.cmdDoneAction = new Command("Next", this, 2101, null);
		tfUser.name = ((mResources.language != 2) ? (mResources.phone + "/") : string.Empty) + mResources.email;
		tfPass = new TField();
		tfPass.y = GameCanvas.hh - 4;
		tfPass.setIputType(TField.INPUT_TYPE_PASSWORD);
		tfPass.loginInput = true;
		tfPass.setMaxTextLenght(100);
		tfPass.cmdDoneAction = new Command("Done", this, 2102, null);
		tfPass.width = wC - 76;
		tfPass.height = mScreen.ITEM_HEIGHT + 2;
		cmdTogglePassword = new Command("Hiện", this, 2103, null);
		yt += 35;
		isCheck = true;
		switch (Rms.loadRMSInt("check"))
		{
		case 1:
			isCheck = true;
			break;
		case 2:
			isCheck = false;
			break;
		}
		tfUser.setText(Rms.loadRMSString("acc"));
		tfPass.setText(Rms.loadRMSString("pass"));
		if (cmdCallHotline == null)
		{
			cmdCallHotline = new Command("Gọi hotline", this, 13, null);
			cmdCallHotline.x = GameCanvas.w - 75;
			if (mSystem.clientType == 1 && !GameCanvas.isTouch)
			{
				cmdCallHotline.y = GameCanvas.h - 20;
			}
			else
			{
				int num2 = 2;
				cmdCallHotline.y = num2 + 6;
			}
		}
		focus = 0;
		cmdLogin = new Command((GameCanvas.w <= 200) ? mResources.login2 : mResources.login, GameCanvas.instance, 888393, null);
		cmdCheck = new Command(mResources.remember, this, 2001, null);
		cmdRes = new Command(mResources.register, this, 2002, null);
		cmdBackFromRegister = new Command(mResources.CANCEL, this, 10021, null);
		left = (cmdMenu = new Command(mResources.MENU, this, 2003, null));
		freeAreaHeight = tfUser.y - 2 * tfUser.height;
		if (GameCanvas.isTouch)
		{
			cmdLogin.x = GameCanvas.w / 2 + 8;
			cmdMenu.x = GameCanvas.w / 2 - mScreen.cmdW - 8;
			if (GameCanvas.h >= 200)
			{
				cmdLogin.y = yLog + 110;
				cmdMenu.y = yLog + 110;
			}
			cmdBackFromRegister.x = GameCanvas.w / 2 + 3;
			cmdBackFromRegister.y = yLog + 110;
			cmdRes.x = GameCanvas.w / 2 - 84;
			cmdRes.y = cmdMenu.y;
		}
		wP = 170;
		hP = ((!isRes) ? 100 : 110);
		xP = GameCanvas.hw - wP / 2;
		yP = tfUser.y - 15;
		int num3 = 4;
		int num4 = num3 * 32 + 23 + 33;
		if (num4 >= GameCanvas.w)
		{
			num3--;
			num4 = num3 * 32 + 23 + 33;
		}
		xLog = GameCanvas.w / 2 - num4 / 2;
		yLog = GameCanvas.hh - 30;
		lY = ((GameCanvas.w < 200) ? (tfUser.y - 30) : (yLog - 30));
		tfUser.x = xLog + 10;
		tfUser.y = yLog + 20;
		cmdOK = new Command(mResources.OK, this, 2008, null);
		cmdOK.x = GameCanvas.w / 2 - 84;
		cmdOK.y = cmdLogin.y;
		cmdFogetPass = new Command(mResources.forgetPass, this, 1003, null);
		cmdFogetPass.x = GameCanvas.w / 2 + 3;
		cmdFogetPass.y = cmdLogin.y;
		center = cmdOK;
		left = cmdFogetPass;
	}

	public static void getServerLink()
	{
		try
		{
			if (isTryGetIPFromWap)
			{
				return;
			}
			Command command = new Command();
			ActionChat actionChat = (command.actionChat = delegate(string str)
			{
				try
				{
					if (str != null && !(str == string.Empty))
					{
						Rms.saveIP(str);
						if (str.Contains(":"))
						{
							int num = str.IndexOf(":");
							string text = str.Substring(0, num);
							string s = str.Substring(num + 1);
							GameMidlet.IP = text;
							GameMidlet.PORT = int.Parse(s);
							Session_ME.gI().connect(text, int.Parse(s));
							isTryGetIPFromWap = true;
						}
					}
				}
				catch (Exception)
				{
				}
			});
			Net.connectHTTP(ServerListScreen.linkGetHost, command);
		}
		catch (Exception)
		{
		}
	}

	public override void switchToMe()
	{
		isRegistering = false;
		SoundMn.gI().stopAll();
		tfUser.isFocus = true;
		tfPass.isFocus = false;
		if (GameCanvas.isTouch)
		{
			tfUser.isFocus = false;
		}
		GameCanvas.loadBG(0);
		base.switchToMe();
	}

	public void setUserPass()
	{
		string text = Rms.loadRMSString("acc");
		if (text != null && !text.Equals(string.Empty))
		{
			tfUser.setText(text);
		}
		string text2 = Rms.loadRMSString("pass");
		if (text2 != null && !text2.Equals(string.Empty))
		{
			tfPass.setText(text2);
		}
	}

	public void updateTfWhenOpenKb()
	{
	}

	protected void doMenu()
	{
		MyVector myVector = new MyVector();
		myVector.addElement(new Command(mResources.registerNewAcc, this, 2004, null));
		if (!isLogin2)
		{
			myVector.addElement(new Command(mResources.selectServer, this, 1004, null));
		}
		myVector.addElement(new Command(mResources.forgetPass, this, 1003, null));
		myVector.addElement(new Command(mResources.website, this, 1005, null));
		if (Main.isPC)
		{
			myVector.addElement(new Command(mResources.EXIT, GameCanvas.instance, 8885, null));
		}
		GameCanvas.menu.startAt(myVector, 0);
	}

	protected void doRegister()
	{
		if (tfUser.getText().Equals(string.Empty))
		{
			GameCanvas.startOKDlg(mResources.userBlank);
			return;
		}
		char[] array = tfUser.getText().ToCharArray();
		if (tfPass.getText().Equals(string.Empty))
		{
			GameCanvas.startOKDlg(mResources.passwordBlank);
			return;
		}
		if (tfUser.getText().Length < 5)
		{
			GameCanvas.startOKDlg(mResources.accTooShort);
			return;
		}
		int num = 0;
		string text = null;
		if (mResources.language == 2)
		{
			if (tfUser.getText().IndexOf("@") == -1 || tfUser.getText().IndexOf(".") == -1)
			{
				text = mResources.emailInvalid;
			}
			num = 0;
		}
		else
		{
			try
			{
				long num2 = long.Parse(tfUser.getText());
				if (tfUser.getText().Length < 8 || tfUser.getText().Length > 12 || (!tfUser.getText().StartsWith("0") && !tfUser.getText().StartsWith("84")))
				{
					text = mResources.phoneInvalid;
				}
				num = 1;
			}
			catch (Exception)
			{
				if (tfUser.getText().IndexOf("@") == -1 || tfUser.getText().IndexOf(".") == -1)
				{
					text = mResources.emailInvalid;
				}
				num = 0;
			}
		}
		if (text != null)
		{
			GameCanvas.startOKDlg(text);
		}
		else
		{
			GameCanvas.msgdlg.setInfo(mResources.plsCheckAcc + ((num != 1) ? (mResources.email + ": ") : (mResources.phone + ": ")) + tfUser.getText(), new Command(mResources.ACCEPT, this, 4000, null), null, new Command(mResources.NO, GameCanvas.instance, 8882, null));
		}
		GameCanvas.currentDialog = GameCanvas.msgdlg;
	}

	protected void doRegister(string user)
	{
		isFAQ = false;
		GameCanvas.startWaitDlg(mResources.CONNECTING);
		GameCanvas.connect();
		GameCanvas.startWaitDlg(mResources.REGISTERING);
		passRe = tfPass.getText();
		Service.gI().requestRegister(user, tfPass.getText(), Rms.loadRMSString("userAo" + ServerListScreen.ipSelect), Rms.loadRMSString("passAo" + ServerListScreen.ipSelect), GameMidlet.VERSION);
		Rms.saveRMSString("acc", user);
		Rms.saveRMSString("pass", tfPass.getText());
		t = 20;
		isRegistering = true;
	}

	public void doViewFAQ()
	{
		if (!listFAQ.Equals(string.Empty) || !listFAQ.Equals(string.Empty))
		{
		}
		if (!Session_ME.connected)
		{
			isFAQ = true;
			GameCanvas.connect();
		}
		GameCanvas.startWaitDlg();
	}

	protected void doSelectServer()
	{
		MyVector myVector = new MyVector();
		if (isLocal)
		{
			myVector.addElement(new Command("Server LOCAL", this, 20004, null));
		}
		myVector.addElement(new Command("Server Bokken", this, 20001, null));
		myVector.addElement(new Command("Server Shuriken", this, 20002, null));
		myVector.addElement(new Command("Server Tessen (mới)", this, 20003, null));
		GameCanvas.menu.startAt(myVector, 0);
		if (loadIndexServer() != -1 && !GameCanvas.isTouch)
		{
			GameCanvas.menu.menuSelectedItem = loadIndexServer();
		}
	}

	protected void saveIndexServer(int index)
	{
		Rms.saveRMSInt("indServer", index);
	}

	protected int loadIndexServer()
	{
		return Rms.loadRMSInt("indServer");
	}

	public void focusLoginField(bool username)
	{
		focus = username ? 0 : 1;
		tfUser.isFocus = username;
		tfPass.isFocus = !username;
		if (!GameCanvas.isTouch)
		{
			right = username ? tfUser.cmdClear : tfPass.cmdClear;
		}
	}

	public bool keepFormOnLoginError(string message)
	{
		bool usernameMissing = message.IndexOf("tài khoản không tồn tại", StringComparison.OrdinalIgnoreCase) >= 0;
		bool invalidCredentials = message.Equals("Thông tin tài khoản hoặc mật khẩu không chính xác", StringComparison.Ordinal);
		if (!usernameMissing && !invalidCredentials)
		{
			return false;
		}
		isLoggingIn = false;
		isContinueToLogin = false;
		Char.isLoadingMap = false;
		timeLogin = 0;
		Main.isMiniApp = true;
		focusLoginField(usernameMissing);
		return true;
	}

	public void doLogin()
	{
		if (sessionReplaced) return;
		if (System.Threading.Thread.CurrentThread.Name != Main.mainThreadName)
		{
			System.Threading.Interlocked.CompareExchange(ref queuedLogin, () => doLogin(), null);
			return;
		}
		if (isLoggingIn || timeLogin > 0) return;
		isLoggingIn = true;
		string text = Rms.loadRMSString("acc");
		string text2 = Rms.loadRMSString("pass");
		if (GameCanvas.currentScreen == this && !isLogin2)
		{
			text = tfUser.getText().Trim();
			text2 = tfPass.getText().Trim();
		}
		if (text != null && !text.Equals(string.Empty))
		{
			isLogin2 = false;
		}
		else if (Rms.loadRMSString("userAo" + ServerListScreen.ipSelect) != null && !Rms.loadRMSString("userAo" + ServerListScreen.ipSelect).Equals(string.Empty))
		{
			isLogin2 = true;
		}
		else
		{
			isLogin2 = false;
		}
		if ((text == null || text.Equals(string.Empty)) && isLogin2)
		{
			text = Rms.loadRMSString("userAo" + ServerListScreen.ipSelect);
			text2 = "a";
		}
		if (string.IsNullOrEmpty(text))
		{
			finishLoginAttempt();
			isContinueToLogin = false;
			Char.isLoadingMap = false;
			focusLoginField(true);
			GameCanvas.startOKDlg(mResources.userBlank);
			return;
		}
		if (text2 == null || GameMidlet.VERSION == null)
		{
			finishLoginAttempt();
			Char.isLoadingMap = false;
			return;
		}
		if (text2.Equals(string.Empty))
		{
			finishLoginAttempt();
			isContinueToLogin = false;
			Char.isLoadingMap = false;
			focusLoginField(false);
			GameCanvas.startOKDlg(mResources.passwordBlank);
			return;
		}
		pendingUser = text;
		pendingGuestCreation = false;
		pendingPassword = text2;
		pendingLoginType = (sbyte)(isLogin2 ? 1 : 0);
		loginStage = 1;
		stageStarted = Environment.TickCount;
		Main.isMiniApp = true;
		if (!Session_ME.gI().isConnected() && !Session_ME.connecting)
		{
			GameCanvas.connect();
		}
		Res.outz("Login request, version=" + GameMidlet.VERSION + ", type=" + (sbyte)(isLogin2 ? 1 : 0));
		GameCanvas.startWaitDlg();
		focus = 0;
		if (!isLogin2)
		{
			actRegisterLeft();
		}
		GameCanvas.timeBreakLoading = mSystem.currentTimeMillis() + 30000;
	}

	public void savePass()
	{
		if (isCheck)
		{
			Rms.saveRMSInt("check", 1);
			Rms.saveRMSString("acc", tfUser.getText().ToLower().Trim());
			Rms.saveRMSString("pass", tfPass.getText().ToLower().Trim());
		}
		else
		{
			Rms.saveRMSInt("check", 2);
			Rms.saveRMSString("acc", string.Empty);
			Rms.saveRMSString("pass", string.Empty);
		}
	}

	public override void update()
	{
		if (Main.isWindowsPhone && isRegistering)
		{
			if (t < 0)
			{
				GameCanvas.endDlg();
				Session_ME.gI().close();
				GameCanvas.serverScreen.switchToMe();
				isRegistering = false;
			}
			else
			{
				t--;
			}
		}
		if (timeLogin > 0)
		{
			GameCanvas.startWaitDlg();
			currTimeLogin = mSystem.currentTimeMillis();
			if (currTimeLogin - lastTimeLogin >= 1000)
			{
				timeLogin--;
				if (timeLogin == 0)
				{
					GameCanvas.loginScr.doLogin();
				}
				lastTimeLogin = currTimeLogin;
			}
		}
		if (isLogin2 && !isRes)
		{
			tfUser.name = ((mResources.language != 2) ? (mResources.phone + "/") : string.Empty) + mResources.email;
			tfPass.name = mResources.password;
			tfUser.isPaintCarret = false;
			tfPass.isPaintCarret = false;
			tfUser.update();
			tfPass.update();
		}
		else
		{
			tfUser.name = ((mResources.language != 2) ? (mResources.phone + "/") : string.Empty) + mResources.email;
			tfPass.name = mResources.password;
			tfUser.update();
			tfPass.update();
		}
		if (TouchScreenKeyboard.visible)
		{
			mGraphics.addYWhenOpenKeyBoard = 50;
		}
		for (int i = 0; i < Effect2.vEffect2.size(); i++)
		{
			Effect2 effect = (Effect2)Effect2.vEffect2.elementAt(i);
			effect.update();
		}
		if (isUpdateAll && !isUpdateData && !isUpdateItem && !isUpdateMap && !isUpdateSkill)
		{
			isUpdateAll = false;
			mSystem.gcc();
			Service.gI().finishUpdate();
		}
		GameScr.cmx++;
		if (GameScr.cmx > GameCanvas.w * 3 + 100)
		{
			GameScr.cmx = 100;
		}
		if (ChatPopup.currChatPopup != null)
		{
			return;
		}
		GameCanvas.debug("LGU1", 0);
		GameCanvas.debug("LGU2", 0);
		GameCanvas.debug("LGU3", 0);
		updateLogo();
		GameCanvas.debug("LGU4", 0);
		GameCanvas.debug("LGU5", 0);
		if (g >= 0)
		{
			ylogo += dir * g;
			g += dir * v;
			if (g <= 0)
			{
				dir *= -1;
			}
			if (ylogo > 0)
			{
				dir *= -1;
				g -= 2 * v;
			}
		}
		GameCanvas.debug("LGU6", 0);
		if (tipid >= 0 && GameCanvas.gameTick % 100 == 0)
		{
			doChangeTip();
		}
		if (isLogin2 && !isRes)
		{
			tfUser.isPaintCarret = false;
			tfPass.isPaintCarret = false;
			tfUser.update();
			tfPass.update();
		}
		else
		{
			tfUser.name = ((mResources.language != 2) ? (mResources.phone + "/") : string.Empty) + mResources.email;
			tfPass.name = mResources.password;
			tfUser.update();
			tfPass.update();
		}
		if (GameCanvas.isTouch)
		{
			if (isRes)
			{
				center = cmdRes;
				left = cmdBackFromRegister;
			}
			else
			{
				center = cmdOK;
				left = cmdFogetPass;
			}
		}
		else if (isRes)
		{
			center = cmdRes;
			left = cmdBackFromRegister;
		}
		else
		{
			center = cmdOK;
			left = cmdFogetPass;
		}
		if (!Main.isPC && !TouchScreenKeyboard.visible && !Main.isMiniApp && !Main.isWindowsPhone)
		{
			string text = tfUser.getText().ToLower().Trim();
			string text2 = tfPass.getText().ToLower().Trim();
			if (!text.Equals(string.Empty) && !text2.Equals(string.Empty))
			{
				doLogin();
			}
			Main.isMiniApp = true;
		}
		updateTfWhenOpenKb();
	}

	private void doChangeTip()
	{
		tipid++;
		if (tipid >= mResources.tips.Length)
		{
			tipid = 0;
		}
		if (GameCanvas.currentDialog == GameCanvas.msgdlg && GameCanvas.msgdlg.isWait)
		{
			GameCanvas.msgdlg.setInfo(mResources.tips[tipid]);
		}
	}

	public void updateLogo()
	{
		if (defYL != yL)
		{
			yL += defYL - yL >> 1;
		}
	}

	public override void keyPress(int keyCode)
	{
		if (tfUser.isFocus)
		{
			tfUser.keyPressed(keyCode);
		}
		else if (tfPass.isFocus)
		{
			tfPass.keyPressed(keyCode);
		}
		base.keyPress(keyCode);
	}

	public override void unLoad()
	{
		base.unLoad();
	}

	public override void paint(mGraphics g)
	{
		GameCanvas.debug("PLG1", 1);
		GameCanvas.paintBGGameScr(g);
		GameCanvas.debug("PLG2", 2);
		int num = tfUser.y - 50;
		if (GameCanvas.h <= 220)
		{
			num += 5;
		}
		mFont.tahoma_7_white.drawString(g, "v" + GameMidlet.VERSION, GameCanvas.w - 2, 17, 1, mFont.tahoma_7_grey);
		CustomServerAddress.PaintLabel(g, 38);
		if (mSystem.clientType == 1 && !GameCanvas.isTouch)
		{
			mFont.tahoma_7_white.drawString(g, ServerListScreen.linkweb, GameCanvas.w - 2, GameCanvas.h - 15, 1, mFont.tahoma_7_grey);
		}
		else
		{
			mFont.tahoma_7_white.drawString(g, ServerListScreen.linkweb, GameCanvas.w - 2, 2, 1, mFont.tahoma_7_grey);
		}
		if (ChatPopup.currChatPopup != null || ChatPopup.serverChatPopUp != null)
		{
			return;
		}
		if (GameCanvas.currentDialog == null)
		{
			int h = 105;
			int w = ((GameCanvas.w < 200) ? 160 : 180);
			PopUp.paintPopUp(g, xLog, yLog - 10, w, h, -1, true);
			if (GameCanvas.h > 160 && DoHoa.imgLogo != null)
			{
				g.drawImage(DoHoa.imgLogo, GameCanvas.hw, num, 3);
			}
			GameCanvas.debug("PLG4", 1);
			int num2 = 4;
			int num3 = num2 * 32 + 23 + 33;
			if (num3 >= GameCanvas.w)
			{
				num2--;
				num3 = num2 * 32 + 23 + 33;
			}
			xLog = GameCanvas.w / 2 - num3 / 2;
			tfUser.x = xLog + 10;
			tfUser.y = yLog + 20;
			tfPass.x = xLog + 10;
			tfPass.y = yLog + 55;
			tfUser.paint(g);
			tfPass.paint(g);
			g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
			cmdTogglePassword.x = tfPass.x + tfPass.width + 2;
			cmdTogglePassword.y = tfPass.y;
			cmdTogglePassword.paint(g);
			int num4 = 0;
			if (GameCanvas.w >= 176)
			{
				num4 = 50;
			}
			else
			{
				mFont.tahoma_7b_green2.drawString(g, mResources.acc + ":", tfUser.x - 35, tfUser.y + 7, 0);
				mFont.tahoma_7b_green2.drawString(g, mResources.pwd + ":", tfPass.x - 35, tfPass.y + 7, 0);
				mFont.tahoma_7b_green2.drawString(g, mResources.server + ":" + serverName, GameCanvas.w / 2, tfPass.y + 32, 2);
				num4 = 0;
			}
		}
		base.paint(g);
	}

	public override void updateKey()
	{
		if (GameCanvas.isTouch)
		{
			if (cmdCallHotline != null && cmdCallHotline.isPointerPressInside())
			{
				cmdCallHotline.performAction();
			}
		}
		else if (mSystem.clientType == 1 && GameCanvas.keyPressed[13])
		{
			GameCanvas.keyPressed[13] = false;
			cmdCallHotline.performAction();
		}
		if (isContinueToLogin || isLoggingIn || GameCanvas.currentDialog != null)
		{
			return;
		}
		if ((!isLogin2 || isRes) && cmdTogglePassword.isPointerPressInside())
		{
			cmdTogglePassword.performAction();
			return;
		}
		if (!GameCanvas.isTouch)
		{
			if (tfUser.isFocus)
			{
				right = tfUser.cmdClear;
			}
			else
			{
				right = tfPass.cmdClear;
			}
		}
		if (GameCanvas.keyPressed[(!Main.isPC) ? 2 : 21])
		{
			focus--;
			if (focus < 0)
			{
				focus = 1;
			}
		}
		else if (GameCanvas.keyPressed[(!Main.isPC) ? 8 : 22] || GameCanvas.keyPressed[16])
		{
			focus++;
			if (focus > 1)
			{
				focus = 0;
			}
		}
		if (GameCanvas.keyPressed[(!Main.isPC) ? 2 : 21] || GameCanvas.keyPressed[(!Main.isPC) ? 8 : 22] || GameCanvas.keyPressed[16])
		{
			GameCanvas.clearKeyPressed();
			if (!isLogin2 || isRes)
			{
				if (focus == 1)
				{
					tfUser.isFocus = false;
					tfPass.isFocus = true;
				}
				else if (focus == 0)
				{
					tfUser.isFocus = true;
					tfPass.isFocus = false;
				}
				else
				{
					tfUser.isFocus = false;
					tfPass.isFocus = false;
				}
			}
		}
		if (GameCanvas.isTouch)
		{
			if (isRes)
			{
				center = cmdRes;
				left = cmdBackFromRegister;
			}
			else
			{
				center = cmdOK;
				left = cmdFogetPass;
			}
		}
		else if (isRes)
		{
			center = cmdRes;
			left = cmdBackFromRegister;
		}
		else
		{
			center = cmdOK;
			left = cmdFogetPass;
		}
		if (GameCanvas.isPointerJustRelease && (!isLogin2 || isRes))
		{
			if (GameCanvas.isPointerHoldIn(tfUser.x, tfUser.y, tfUser.width, tfUser.height))
			{
				focus = 0;
			}
			else if (GameCanvas.isPointerHoldIn(tfPass.x, tfPass.y, tfPass.width, tfPass.height))
			{
				focus = 1;
			}
		}
		if (Main.isPC && GameCanvas.keyPressed[(!Main.isPC) ? 5 : 25] && right != null)
		{
			right.performAction();
		}
		base.updateKey();
		GameCanvas.clearKeyPressed();
	}

	public void resetLogo()
	{
		yL = -50;
	}

	public void perform(int idAction, object p)
	{
		if (isLoggingIn && (idAction == 2008 || idAction == 2102)) return;
		switch (idAction)
		{
		case 13:
			switch (mSystem.clientType)
			{
			case 1:
				mSystem.callHotlineJava();
				break;
			case 3:
			case 5:
				mSystem.callHotlineIphone();
				break;
			case 6:
				mSystem.callHotlineWindowsPhone();
				break;
			case 4:
				mSystem.callHotlinePC();
				break;
			case 2:
				break;
			}
			break;
		case 1000:
			try
			{
				GameMidlet.instance.platformRequest((string)p);
			}
			catch (Exception)
			{
			}
			GameCanvas.endDlg();
			break;
		case 1001:
			GameCanvas.endDlg();
			isRes = false;
			break;
		case 1002:
		{
			string text = Rms.loadRMSString("userAo" + ServerListScreen.ipSelect);
			doGuestLogin(text);
			break;
		}
		case 1004:
			ServerListScreen.doUpdateServer();
			GameCanvas.serverScreen.switchToMe();
			break;
		case 10021:
			actRegisterLeft();
			break;
		case 1003:
			GameCanvas.startOKDlg(mResources.goToWebForPassword);
			break;
		case 1005:
			try
			{
				GameMidlet.instance.platformRequest("http://abc.com");
				break;
			}
			catch (Exception)
			{
				break;
			}
		case 10041:
			Rms.saveRMSInt("lowGraphic", 0);
			GameCanvas.startOK(mResources.plsRestartGame, 8885, null);
			break;
		case 10042:
			Rms.saveRMSInt("lowGraphic", 1);
			GameCanvas.startOK(mResources.plsRestartGame, 8885, null);
			break;
		case 2001:
			if (isCheck)
			{
				isCheck = false;
			}
			else
			{
				isCheck = true;
			}
			break;
		case 2002:
			doRegister();
			break;
		case 2003:
			doMenu();
			break;
		case 2004:
			actRegister();
			break;
		case 2008:
			sessionReplaced = false;
			Rms.saveRMSString("acc", tfUser.getText().Trim());
			Rms.saveRMSString("pass", tfPass.getText().Trim());
			isLogin2 = false;
			doLogin();
			break;
		case 2101:
			focusLoginField(false);
			tfPass.setFocusWithKb(true);
			break;
		case 2102:
			if (isRes)
			{
				doRegister();
			}
			else
			{
				perform(2008, null);
			}
			break;
		case 2103:
			bool reopenKeyboard = TField.kb != null && TField.currentTField == tfPass;
			tfPass.revealPassword = !tfPass.revealPassword;
			cmdTogglePassword.caption = tfPass.revealPassword ? "Ẩn" : "Hiện";
			tfPass.setText(tfPass.getText());
			if (reopenKeyboard)
			{
				TField.kb.active = false;
				TField.kb = null;
				tfPass.setFocusWithKb(true);
			}
			break;
		case 4000:
			doRegister(tfUser.getText());
			break;
		}
	}

	public void actRegisterLeft()
	{
		if (isLogin2)
		{
			doLogin();
			return;
		}
		isRes = false;
		tfPass.isFocus = false;
		tfUser.isFocus = true;
		left = cmdMenu;
	}

	public void actRegister()
	{
		GameCanvas.endDlg();
		isRes = true;
		tfPass.isFocus = false;
		tfUser.isFocus = true;
	}

	public void backToRegister()
	{
		GameCanvas.timeBreakLoading = mSystem.currentTimeMillis() + 30000;
		ServerListScreen.countDieConnect = 0;
		if (GameCanvas.loginScr.isLogin2)
		{
			GameCanvas.startYesNoDlg(mResources.note, new Command(mResources.YES, GameCanvas.panel, 10019, null), new Command(mResources.NO, GameCanvas.panel, 10020, null));
			return;
		}
		if (Main.isWindowsPhone)
		{
			GameMidlet.isBackWindowsPhone = true;
		}
		GameCanvas.instance.resetToLoginScr = false;
		GameCanvas.instance.doResetToLoginScr(GameCanvas.loginScr);
	}
}
