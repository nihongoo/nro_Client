public class Waypoint : IActionListener
{
	public short minX;

	public short minY;

	public short maxX;

	public short maxY;

	public bool isEnter;

	public bool isOffline;

	public PopUp popup;

	public Waypoint(short minX, short minY, short maxX, short maxY, bool isEnter, bool isOffline, string name)
	{
		this.minX = minX;
		this.minY = minY;
		this.maxX = maxX;
		this.maxY = maxY;
		name = Res.changeString(name);
		this.isEnter = isEnter;
		this.isOffline = isOffline;
		if (((TileMap.mapID == 21 || TileMap.mapID == 22 || TileMap.mapID == 23) && this.minX >= 0 && this.minX <= 24) || (((TileMap.mapID == 0 && Char.myCharz().cgender != 0) || (TileMap.mapID == 7 && Char.myCharz().cgender != 1) || (TileMap.mapID == 14 && Char.myCharz().cgender != 2)) && isOffline))
		{
			return;
		}
		if (TileMap.isInAirMap() || TileMap.mapID == 47)
		{
			if (minY <= 150 || !TileMap.isInAirMap())
			{
				popup = new PopUp(name, minX + (maxX - minX) / 2, maxY - ((minX <= 100) ? 48 : 24));
				popup.command = new Command(null, this, 1, this);
				popup.isWayPoint = true;
				popup.isPaint = false;
				PopUp.addPopUp(popup);
				TileMap.vGo.addElement(this);
			}
			return;
		}
		if (!isEnter && !isOffline)
		{
			popup = new PopUp(name, minX, minY - 24);
			popup.command = new Command(null, this, 1, this);
			popup.isWayPoint = true;
			popup.isPaint = false;
			PopUp.addPopUp(popup);
		}
		else
		{
			if (TileMap.isTrainingMap())
			{
				popup = new PopUp(name, minX, minY - 16);
			}
			else
			{
				int x = minX + (maxX - minX) / 2;
				popup = new PopUp(name, x, minY - ((minY == 0) ? (-32) : 16));
			}
			popup.command = new Command(null, this, 2, this);
			popup.isWayPoint = true;
			popup.isPaint = false;
			PopUp.addPopUp(popup);
		}
		TileMap.vGo.addElement(this);
	}

    internal bool TryGetEntryPoint(out int x, out int y)
    {
        int left = System.Math.Max(0, (int)minX), right = System.Math.Min(TileMap.pxw - 1, maxX);
        int top = System.Math.Max(1, (int)minY), bottom = System.Math.Min(TileMap.pxh - 1, maxY);
        int center = (left + right) / 2;
        x = y = -1;
        int bestScore = int.MaxValue;
        for (int row = bottom; row >= top; row--) {
            for (int column = left; column <= right; column++) {
                if (!TileMap.IsClearMapEntry(column, row)) continue;
                bool onFloor = TileMap.IsMapEntryGround(column, row);
                int score = (onFloor ? 0 : TileMap.pxw + TileMap.pxh)
                    + System.Math.Abs(center - column) + bottom - row;
                if (score >= bestScore) continue;
                bestScore = score;
                x = column;
                y = row;
            }
        }
        return x >= 0;
    }

	public void perform(int idAction, object p)
	{
        if (Char.ischangingMap || Char.isLoadingMap) return;
		switch (idAction)
		{
		case 1:
		{
            Char player = Char.myCharz();
            if (player.cx >= minX && player.cx <= maxX && player.cy >= minY && player.cy <= maxY) {
                if (Service.gI().SendWaypointPosition()) {
                    if (isOffline || TileMap.isTrainingMap()) Service.gI().getMapOffline();
                    else Service.gI().requestChangeMap();
                }
                break;
            }
            if (!TryGetEntryPoint(out int xEnd2, out int yEnd2)) break;
			GameScr.gI().auto = 0;
			Char.myCharz().currentMovePoint = new MovePoint(xEnd2, yEnd2);
			Char.myCharz().cdir = ((Char.myCharz().cx - Char.myCharz().currentMovePoint.xEnd <= 0) ? 1 : (-1));
			Service.gI().charMove();
			break;
		}
		case 2:
			GameScr.gI().auto = 0;
			if (Char.myCharz().isInEnterOfflinePoint() != null)
			{
				Service.gI().charMove();
				Service.gI().getMapOffline();
			}
			else if (Char.myCharz().isInEnterOnlinePoint() != null)
			{
				Service.gI().charMove();
				Service.gI().requestChangeMap();
			}
			else
			{
                if (!TryGetEntryPoint(out int xEnd, out int yEnd)) break;
				Char.myCharz().currentMovePoint = new MovePoint(xEnd, yEnd);
				Char.myCharz().cdir = ((Char.myCharz().cx - Char.myCharz().currentMovePoint.xEnd <= 0) ? 1 : (-1));
				Char.myCharz().endMovePointCommand = new Command(null, this, 2, null);
			}
			break;
		}
	}
}
