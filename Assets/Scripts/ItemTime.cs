public class ItemTime
{
    public short idIcon;

    public int second;

    public int minute;

    private long expiresAt;

    private bool isText;

    private bool dontClear;

    private string text;

    private bool isPaint_coolDownBar;

    public int time;

    public int coutTime;

    private int per = 100;

    public ItemTime()
    {
    }

    public ItemTime(short idIcon, int s)
    {
        this.idIcon = idIcon;
        minute = s / 60;
        second = s % 60;
        time = s;
        coutTime = s;
        expiresAt = Hud.BuffIconStore.Now + System.Math.Max(0, (long)coutTime) * 1000;
        isPaint_coolDownBar = idIcon == 14;
    }

    public void initTimeText(sbyte id, string text, int time)
    {
        if (time == -1)
        {
            dontClear = true;
        }
        else
        {
            dontClear = false;
        }
        isText = true;
        minute = time / 60;
        second = time % 60;
        idIcon = id;
        this.time = time;
        coutTime = time;
        this.text = text;
        expiresAt = Hud.BuffIconStore.Now + System.Math.Max(0, (long)coutTime) * 1000;
        isPaint_coolDownBar = idIcon == 14;
    }

    public void initTime(int time, bool isText)
    {
        minute = time / 60;
        second = time % 60;
        this.time = time;
        coutTime = time;
        this.isText = isText;
        expiresAt = Hud.BuffIconStore.Now + System.Math.Max(0, (long)coutTime) * 1000;
    }

    public static bool isExistItem(int id)
    {
        for (int i = 0; i < Char.vItemTime.size(); i++)
        {
            ItemTime itemTime = (ItemTime)Char.vItemTime.elementAt(i);
            if (itemTime.idIcon == id)
            {
                return true;
            }
        }
        return false;
    }

    public static ItemTime getMessageById(int id)
    {
        for (int i = 0; i < GameScr.textTime.size(); i++)
        {
            ItemTime itemTime = (ItemTime)GameScr.textTime.elementAt(i);
            if (itemTime.idIcon == id)
            {
                return itemTime;
            }
        }
        return null;
    }

    public static bool isExistMessage(int id)
    {
        for (int i = 0; i < GameScr.textTime.size(); i++)
        {
            ItemTime itemTime = (ItemTime)GameScr.textTime.elementAt(i);
            if (itemTime.idIcon == id)
            {
                return true;
            }
        }
        return false;
    }

    public static ItemTime getItemById(int id)
    {
        for (int i = 0; i < Char.vItemTime.size(); i++)
        {
            ItemTime itemTime = (ItemTime)Char.vItemTime.elementAt(i);
            if (itemTime.idIcon == id)
            {
                return itemTime;
            }
        }
        return null;
    }

    public void initTime(int time)
    {
        this.time = time;
        minute = time / 60;
        second = time % 60;
        coutTime = time;
        expiresAt = Hud.BuffIconStore.Now + System.Math.Max(0, (long)coutTime) * 1000;
    }

    public void paintText(mGraphics g, int x, int y)
    {
        if (isPaint_coolDownBar)
        {
            if (Char.myCharz() != null)
            {
                int num = 80;
                int x2 = GameCanvas.w / 2 - num / 2;
                int y2 = GameCanvas.h - 80;
                g.setColor(8421504);
                g.fillRect(x2, y2, num, 2);
                g.setColor(16777215);
                if (per > 0)
                {
                    g.fillRect(x2, y2, num * per / 100, 2);
                }
            }
            return;
        }
        string empty = string.Empty;
        empty = minute + "'";
        if (minute < 1)
        {
            empty = second + "s";
        }
        if (minute < 0)
        {
            empty = string.Empty;
        }
        if (dontClear)
        {
            empty = string.Empty;
        }
        if (!text.Contains("ngày"))
        {
            mFont.tahoma_7b_white.drawString(g, text + " " + empty, x, y, 0, mFont.tahoma_7b_dark);
        }
    }

    public static int getItemTimeInSeconds(int idIcon)
    {
        return getItemById(idIcon)?.coutTime ?? (0);
    }

    public void update()
    {
        if (dontClear) return;
        long remaining = System.Math.Max(0, expiresAt - Hud.BuffIconStore.Now);
        coutTime = (int)System.Math.Min(int.MaxValue, (remaining + 999) / 1000);
        minute = coutTime / 60;
        second = coutTime % 60;
        if (time > 0) per = (int)System.Math.Min(100, coutTime * 100L / time);
        if (coutTime == 0) {
            if (isText) GameScr.textTime.removeElement(this);
            else Char.vItemTime.removeElement(this);
        }
    }
}
