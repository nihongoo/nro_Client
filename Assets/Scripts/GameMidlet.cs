using UnityEngine;

public class GameMidlet
{
	// Default LAN test endpoint. Change these two constants together when needed.
	public const string DefaultServerHost = "192.168.1.7";
	public const int DefaultServerPort = 14445;
	// Set these URLs when the account website is available. Empty URLs hide the actions.
	public const string RegistrationUrl = "";
	public const string PasswordRecoveryUrl = "";
	public const string AccountWebsiteUrl = "";
	public static string IP = DefaultServerHost;

	public static int PORT = DefaultServerPort;

	public static string IP2;

	public static int PORT2;

	public static sbyte PROVIDER;

	public static int LANGUAGE;

	public static string VERSION = "2.3.6";

	public static GameCanvas gameCanvas;

	public static GameMidlet instance;

	public static bool isConnect2;

	public static bool isBackWindowsPhone;

	public GameMidlet()
	{
		initGame();
	}

	public void initGame()
	{
		CustomServerAddress.Load();
		instance = this;
		MotherCanvas.instance = new MotherCanvas();
		Session_ME.gI().setHandler(Controller.gI());
		Session_ME2.gI().setHandler(Controller.gI());
		Session_ME2.isMainSession = false;
		instance = this;
		gameCanvas = new GameCanvas();
		gameCanvas.start();
		SplashScr.loadImg();
		SplashScr.loadSplashScr();
		GameCanvas.currentScreen = new SplashScr();
	}

	public void exit()
	{
		if (Main.typeClient == 6)
		{
			mSystem.exitWP();
			return;
		}
		GameCanvas.bRun = false;
		mSystem.gcc();
		notifyDestroyed();
	}

	public static void sendSMS(string data, string to, Command successAction, Command failAction)
	{
		Cout.println("SEND SMS");
	}

	public static void flatForm(string url)
	{
		Cout.LogWarning("PLATFORM REQUEST: " + url);
		Application.OpenURL(url);
	}

	public void notifyDestroyed()
	{
		Main.exit();
	}

	public void platformRequest(string url)
	{
		Cout.LogWarning("PLATFORM REQUEST: " + url);
		Application.OpenURL(url);
	}

	public static bool HasAccountWebPage(string url)
	{
		System.Uri address;
		return System.Uri.TryCreate(url, System.UriKind.Absolute, out address)
			&& (address.Scheme == System.Uri.UriSchemeHttp || address.Scheme == System.Uri.UriSchemeHttps)
			&& !string.IsNullOrEmpty(address.Host);
	}

	public static void OpenAccountWebPage(string url)
	{
		if (!HasAccountWebPage(url))
		{
			GameCanvas.startOKDlg("Trang tài khoản chưa được cấu hình. Vui lòng thử lại sau.");
			return;
		}
		try
		{
			// Never append game credentials to the website URL.
			Application.OpenURL(url);
		}
		catch (System.Exception)
		{
			GameCanvas.startOKDlg("Không mở được trang tài khoản. Vui lòng thử lại sau.");
		}
	}
}
