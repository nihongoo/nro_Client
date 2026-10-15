using System;

public class GamePad
{
	private int xC;

	private int yC;

	private int xM;

	private int yM;

	private int xMLast;

	private int yMLast;

	private int R;

	private int r;

	private int d;

	private int xTemp;

	private int yTemp;

	private int deltaX;

	private int deltaY;

	private int delta;

	private int angle;

	public int xZone;

	public int yZone;

	public int wZone;

	public int hZone;

	private bool isGamePad;
	private readonly Position[] mobileHistory = new Position[4];
	private int mobileHistoryIndex;
	private bool mobilePointerActive;

	// Feed only the captured joystick finger through the original movement algorithm.
	// Snapshot the singleton world pointer so another finger can still operate map/UI.
	internal void UpdateMobilePointer(bool active, UnityEngine.Vector2 first, UnityEngine.Vector2 point)
	{
		int px = GameCanvas.px, py = GameCanvas.py, fx = GameCanvas.pxFirst, fy = GameCanvas.pyFirst;
		bool down = GameCanvas.isPointerDown, release = GameCanvas.isPointerJustRelease;
		Position[] history = GameCanvas.arrPos;
		int previousR = R, previousX = xZone, previousY = yZone, previousW = wZone, previousH = hZone;
		try {
			UnityEngine.Rect touch = Hud.HudLayout.GetRect(Hud.CombatMobileHud.JoystickTouch);
			xZone = (int)touch.x; yZone = (int)touch.y; wZone = (int)touch.xMax; hZone = (int)touch.yMax;
			if (active && !mobilePointerActive) {
				for (int i=0;i<mobileHistory.Length;i++) mobileHistory[i] = new Position((int)first.x,(int)first.y);
				mobileHistoryIndex = 0;
			}
			if (active) {
				mobileHistory[mobileHistoryIndex] = new Position((int)point.x,(int)point.y);
				mobileHistoryIndex = (mobileHistoryIndex+1)%mobileHistory.Length;
			}
			GameCanvas.arrPos = mobileHistory;
			GameCanvas.pxFirst = (int)first.x; GameCanvas.pyFirst = (int)first.y;
			GameCanvas.px = (int)point.x; GameCanvas.py = (int)point.y;
			GameCanvas.isPointerDown = active; GameCanvas.isPointerJustRelease = !active;
			R = UnityEngine.Mathf.Max(1,(int)(Hud.HudLayout.GetRect(Hud.CombatMobileHud.Joystick).width/2));
			update();
		}
		finally {
			GameCanvas.px = px; GameCanvas.py = py; GameCanvas.pxFirst = fx; GameCanvas.pyFirst = fy;
			GameCanvas.isPointerDown = down; GameCanvas.isPointerJustRelease = release;
			GameCanvas.arrPos = history; mobilePointerActive = active;
			R = previousR; xZone = previousX; yZone = previousY; wZone = previousW; hZone = previousH;
		}
	}

	public bool isSmallGamePad;

	public bool isMediumGamePad;

	public bool isLargeGamePad;

	public GamePad()
	{
		R = 28;
		if (GameCanvas.w < 300)
		{
			isSmallGamePad = true;
			isMediumGamePad = false;
			isLargeGamePad = false;
		}
		if (GameCanvas.w >= 300 && GameCanvas.w <= 380)
		{
			isSmallGamePad = false;
			isMediumGamePad = true;
			isLargeGamePad = false;
		}
		if (GameCanvas.w > 380)
		{
			isSmallGamePad = false;
			isMediumGamePad = false;
			isLargeGamePad = true;
		}
		if (!isLargeGamePad)
		{
			xZone = 0;
			wZone = GameCanvas.hw;
			yZone = GameCanvas.hh >> 1;
			hZone = GameCanvas.h - 80;
		}
		else
		{
			xZone = 0;
			wZone = GameCanvas.hw / 4 * 3 - 20;
			yZone = GameCanvas.hh >> 1;
			hZone = GameCanvas.h;
		}
	}

	public void update()
	{
		try
		{
			if (GameScr.isAnalog == 0)
			{
				return;
			}
			if (GameCanvas.isPointerDown && !GameCanvas.isPointerJustRelease)
			{
				xTemp = GameCanvas.pxFirst;
				yTemp = GameCanvas.pyFirst;
				if (xTemp < xZone || xTemp > wZone || yTemp < yZone || yTemp > hZone)
				{
					return;
				}
				if (!isGamePad)
				{
					xC = (xM = xTemp);
					yC = (yM = yTemp);
				}
				isGamePad = true;
				deltaX = GameCanvas.px - xC;
				deltaY = GameCanvas.py - yC;
				delta = Math.pow(deltaX, 2) + Math.pow(deltaY, 2);
				d = Res.sqrt(delta);
				if (Math.abs(deltaX) <= 4 && Math.abs(deltaY) <= 4)
				{
					return;
				}
				angle = Res.angle(deltaX, deltaY);
				if (!GameCanvas.isPointerHoldIn(xC - R, yC - R, 2 * R, 2 * R))
				{
					if (d != 0)
					{
						yM = deltaY * R / d;
						xM = deltaX * R / d;
						xM += xC;
						yM += yC;
						if (!Res.inRect(xC - R, yC - R, 2 * R, 2 * R, xM, yM))
						{
							xM = xMLast;
							yM = yMLast;
						}
						else
						{
							xMLast = xM;
							yMLast = yM;
						}
					}
					else
					{
						xM = xMLast;
						yM = yMLast;
					}
				}
				else
				{
					xM = GameCanvas.px;
					yM = GameCanvas.py;
				}
				resetHold();
				if (checkPointerMove(2))
				{
					if ((angle <= 360 && angle >= 340) || (angle >= 0 && angle <= 20))
					{
						GameCanvas.keyHold[(!Main.isPC) ? 6 : 24] = true;
						GameCanvas.keyPressed[(!Main.isPC) ? 6 : 24] = true;
					}
					else if (angle > 40 && angle < 70)
					{
						GameCanvas.keyHold[(!Main.isPC) ? 6 : 24] = true;
						GameCanvas.keyPressed[(!Main.isPC) ? 6 : 24] = true;
					}
					else if (angle >= 70 && angle <= 110)
					{
						GameCanvas.keyHold[(!Main.isPC) ? 8 : 22] = true;
						GameCanvas.keyPressed[(!Main.isPC) ? 8 : 22] = true;
					}
					else if (angle > 110 && angle < 120)
					{
						GameCanvas.keyHold[(!Main.isPC) ? 4 : 23] = true;
						GameCanvas.keyPressed[(!Main.isPC) ? 4 : 23] = true;
					}
					else if (angle >= 120 && angle <= 200)
					{
						GameCanvas.keyHold[(!Main.isPC) ? 4 : 23] = true;
						GameCanvas.keyPressed[(!Main.isPC) ? 4 : 23] = true;
					}
					else if (angle > 200 && angle < 250)
					{
						GameCanvas.keyHold[(!Main.isPC) ? 2 : 21] = true;
						GameCanvas.keyPressed[(!Main.isPC) ? 2 : 21] = true;
						GameCanvas.keyHold[(!Main.isPC) ? 4 : 23] = true;
						GameCanvas.keyPressed[(!Main.isPC) ? 4 : 23] = true;
					}
					else if (angle >= 250 && angle <= 290)
					{
						GameCanvas.keyHold[(!Main.isPC) ? 2 : 21] = true;
						GameCanvas.keyPressed[(!Main.isPC) ? 2 : 21] = true;
					}
					else if (angle > 290 && angle < 340)
					{
						GameCanvas.keyHold[(!Main.isPC) ? 2 : 21] = true;
						GameCanvas.keyPressed[(!Main.isPC) ? 2 : 21] = true;
						GameCanvas.keyHold[(!Main.isPC) ? 6 : 24] = true;
						GameCanvas.keyPressed[(!Main.isPC) ? 6 : 24] = true;
					}
				}
				else
				{
					resetHold();
				}
			}
			else
			{
				if (Hud.HudLayout.IsMobileLayout) {
					UnityEngine.Rect rect = Hud.HudLayout.GetRect(Hud.CombatMobileHud.Joystick);
					xM = xC = (int)rect.center.x; yM = yC = (int)rect.center.y;
				}
				else {
				xM = (xC = 45);
				if (!isLargeGamePad)
				{
					yM = (yC = GameCanvas.h - 90);
				}
				else
				{
					yM = (yC = GameCanvas.h - 45);
				}
				}
				isGamePad = false;
				resetHold();
			}
		}
		catch (Exception)
		{
		}
	}

	private bool checkPointerMove(int distance)
	{
		if (GameScr.isAnalog == 0)
		{
			return false;
		}
		if (Char.myCharz().statusMe == 3)
		{
			return true;
		}
		try
		{
			for (int num = 2; num > 0; num--)
			{
				int i = GameCanvas.arrPos[num].x - GameCanvas.arrPos[num - 1].x;
				int i2 = GameCanvas.arrPos[num].y - GameCanvas.arrPos[num - 1].y;
				if (Res.abs(i) > distance && Res.abs(i2) > distance)
				{
					return false;
				}
			}
		}
		catch (Exception)
		{
		}
		return true;
	}

	private void resetHold()
	{
		GameCanvas.clearKeyHold();
	}

	public void paint(mGraphics g)
	{
		if (Hud.HudLayout.IsMobileLayout) {
			if (GameScr.isAnalog == 0) return;
			UnityEngine.Rect rect = Hud.HudLayout.GetRect(Hud.CombatMobileHud.Joystick);
			Hud.CombatMobileHud.Circle(rect,new UnityEngine.Color32(40,24,12,64),new UnityEngine.Color32(255,243,218,153),2);
			float diameter = Hud.CombatMobileHud.DesignUnit(44);
			UnityEngine.Vector2 center = rect.center;
			if (mobilePointerActive) center += new UnityEngine.Vector2(xM-xC,yM-yC);
			Hud.CombatMobileHud.Circle(new UnityEngine.Rect(center.x-diameter/2,center.y-diameter/2,diameter,diameter),
				new UnityEngine.Color32(255,243,218,255),new UnityEngine.Color32(255,243,218,255),0);
			return;
		}
		if (GameScr.isAnalog != 0)
		{
			g.drawImage(GameScr.imgAnalog1, xC, yC, mGraphics.HCENTER | mGraphics.VCENTER);
			g.drawImage(GameScr.imgAnalog2, xM, yM, mGraphics.HCENTER | mGraphics.VCENTER);
		}
	}

	public bool disableCheckDrag()
	{
		if (GameScr.isAnalog == 0)
		{
			return false;
		}
		return isGamePad;
	}

	public bool disableClickMove()
	{
		try
		{
			if (GameScr.isAnalog == 0)
			{
				return false;
			}
			bool flag = false;
			if ((GameCanvas.px >= xZone && GameCanvas.px <= wZone && GameCanvas.py >= yZone && GameCanvas.py <= hZone) || GameCanvas.px >= GameCanvas.w - 50)
			{
				return true;
			}
			return false;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
