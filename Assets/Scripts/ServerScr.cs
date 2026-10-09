public class ServerScr : mScreen, IActionListener
{
	private int mainSelect;

	private MyVector vecServer = new MyVector();

	private Command cmdCheck;

	public const int icmd = 100;

	private int wc;

	private int hc;

	private int w2c;

	private int numw;

	private int numh;

	public ServerScr()
	{
		TileMap.bgID = (byte)(mSystem.currentTimeMillis() % 9);
		if (TileMap.bgID == 5 || TileMap.bgID == 6)
		{
			TileMap.bgID = 4;
		}
		GameScr.loadCamera(true, -1, -1);
		GameScr.cmx = 100;
		GameScr.cmy = 200;
	}

	public override void switchToMe()
	{
		SoundMn.gI().stopAll();
		base.switchToMe();
		vecServer = new MyVector();
		Command server = new Command("NRO", this, 100, null);
		server.setType();
		vecServer.addElement(server);
		Command custom = new Command("Nhập IP server", this, 96, null);
		custom.setType();
		vecServer.addElement(custom);
		sort();
	}

	private void sort()
	{
		mainSelect = ServerListScreen.ipSelect;
		w2c = 5;
		wc = 160;
		hc = mScreen.cmdH;
		numw = 1;
        numh = vecServer.size() / numw + ((vecServer.size() % numw != 0) ? 1 : 0);
		for (int i = 0; i < vecServer.size(); i++)
		{
			Command command = (Command)vecServer.elementAt(i);
			if (command != null)
			{
				int num = GameCanvas.hw - numw * (wc + w2c) / 2;
				int x = num + i % numw * (wc + w2c);
				int num2 = GameCanvas.hh - numh * (hc + w2c) / 2;
				int y = num2 + i / numw * (hc + w2c);
				command.x = x;
				command.y = y;
			}
		}
	}

	public override void update()
	{
		if (GameCanvas.currentDialog != null) return;
		GameScr.cmx++;
		if (GameScr.cmx > GameCanvas.w * 3 + 100)
		{
			GameScr.cmx = 100;
		}
		for (int i = 0; i < vecServer.size(); i++)
		{
			Command command = (Command)vecServer.elementAt(i);
			if (!GameCanvas.isTouch)
			{
				if (i == mainSelect)
				{
					if (GameCanvas.gameTick % 10 < 4)
					{
						command.isFocus = true;
					}
					else
					{
						command.isFocus = false;
					}
					cmdCheck = new Command(mResources.SELECT, this, command.idAction, null);
					center = cmdCheck;
				}
				else
				{
					command.isFocus = false;
				}
			}
			else if (command != null && command.isPointerPressInside())
			{
				command.performAction();
			}
		}
	}

	public override void paint(mGraphics g)
	{
		GameCanvas.paintBGGameScr(g);
		for (int i = 0; i < vecServer.size(); i++)
		{
			if (vecServer.elementAt(i) != null)
			{
				((Command)vecServer.elementAt(i)).paint(g);
			}
		}
		base.paint(g);
		CustomServerAddress.PaintLabel(g, 38);
	}

	public override void updateKey()
	{
		if (GameCanvas.currentDialog != null) return;
		base.updateKey();
		int num = mainSelect % numw;
		int num2 = mainSelect / numw;
		if (GameCanvas.keyPressed[4])
		{
			if (num > 0)
			{
				mainSelect--;
			}
			GameCanvas.keyPressed[4] = false;
		}
		else if (GameCanvas.keyPressed[6])
		{
			if (num < numw - 1)
			{
				mainSelect++;
			}
			GameCanvas.keyPressed[6] = false;
		}
		else if (GameCanvas.keyPressed[2])
		{
			if (num2 > 0)
			{
				mainSelect -= numw;
			}
			GameCanvas.keyPressed[2] = false;
		}
		else if (GameCanvas.keyPressed[8])
		{
			if (num2 < numh - 1)
			{
				mainSelect += numw;
			}
			GameCanvas.keyPressed[8] = false;
		}
		if (mainSelect < 0)
		{
			mainSelect = 0;
		}
		if (mainSelect >= vecServer.size())
		{
			mainSelect = vecServer.size() - 1;
		}
		if (GameCanvas.keyPressed[5])
		{
			((Command)vecServer.elementAt(mainSelect)).performAction();
			GameCanvas.keyPressed[5] = false;
		}
		GameCanvas.clearKeyPressed();
	}

	public void perform(int idAction, object p)
	{
		switch (idAction)
		{
		case 96:
			if (!LoginScr.isLoggingIn) new CustomServerDialog().show();
			break;
		case 99:
			Session_ME.gI().clearSendingMessage();
			ServerListScreen.ipSelect = mainSelect;
			GameCanvas.serverScreen.selectServer();
			GameCanvas.serverScreen.switchToMe();
			break;
		default:
			Session_ME.gI().clearSendingMessage();
			ServerListScreen.ipSelect = idAction - 100;
			Res.outz("Default:    ServerListScreen.ipSelect " + ServerListScreen.ipSelect);
			GameCanvas.serverScreen.selectServer();
			GameCanvas.serverScreen.switchToMe();
			break;
		}
	}
}
