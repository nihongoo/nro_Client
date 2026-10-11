using System;

namespace Mod.CuongLe
{
    public class Boss
    {
    	public string NameBoss;

    	public string MapName;

    	public int MapId;

    	public DateTime AppearTime;

    	public Boss()
    	{
    	}

    	public Boss(string chatVip)
    	{
            try 
            {
                // Loại bỏ mã màu |c...| và |r
                string cleanChat = "";
                for (int i = 0; i < chatVip.Length; i++)
                {
                    if (chatVip[i] == '|' && i + 1 < chatVip.Length && (chatVip[i + 1] == 'c' || chatVip[i + 1] == 'r'))
                    {
                        if (chatVip[i + 1] == 'c')
                        {
                            i += 2;
                            while (i < chatVip.Length && chatVip[i] != '|') i++;
                        }
                        else if (chatVip[i + 1] == 'r')
                        {
                            i += 1;
                        }
                    }
                    else
                    {
                        cleanChat += chatVip[i];
                    }
                }

                string lowerChat = cleanChat.ToLower();
                int idxSplit = lowerChat.IndexOf(" vừa xuất hiện tại ");
                if (idxSplit == -1) idxSplit = lowerChat.IndexOf(" appear at ");

                if (idxSplit != -1)
                {
                    NameBoss = cleanChat.Substring(0, idxSplit).Replace("BOSS ", "").Replace("Boss ", "").Replace("boss ", "").Trim();
                    if (lowerChat.Contains(" vừa xuất hiện tại "))
                        MapName = cleanChat.Substring(idxSplit + " vừa xuất hiện tại ".Length).Trim();
                    else 
                        MapName = cleanChat.Substring(idxSplit + " appear at ".Length).Trim();
                }
                else 
                {
                    NameBoss = cleanChat;
                    MapName = "Không xác định";
                }
                MapId = GetMapID(MapName);
                AppearTime = DateTime.Now;
            }
            catch (Exception ex) 
            {
                NameBoss = "Lỗi phân tách";
                MapName = chatVip;
                AppearTime = DateTime.Now;
                Res.outz("Lỗi Boss parsing: " + ex.ToString());
            }
    	}

    	public int GetMapID(string mapName)
    	{
    		for (int i = 0; i < TileMap.mapNames.Length; i++)
    		{
    			if (TileMap.mapNames[i].ToLower().Trim().Replace("  ", " ")
    				.Equals(mapName.ToLower().Trim().Replace("  ", " ")))
    			{
    				if (NameBoss.ToLower().Trim().Replace("  ", " ")
    					.StartsWith("tiểu đội trưởng") || NameBoss.ToLower().Trim().Replace("  ", " ")
    					.StartsWith("số"))
    				{
    					return 25;
    				}
    				return i;
    			}
    		}
    		return -1;
    	}

    	public void Paint(mGraphics g, int x, int y, int align)
    	{
    		TimeSpan timeSpan = DateTime.Now.Subtract(AppearTime);
    		int num = (int)timeSpan.TotalSeconds;
    		_ = mFont.tahoma_7_yellow;
    		if (TileMap.mapID == MapId)
    		{
    			_ = mFont.tahoma_7_red;
    			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
    			{
    				if (((Char)GameScr.vCharInMap.elementAt(i)).cName.Equals(NameBoss))
    				{
    					_ = mFont.tahoma_7b_red;
    					break;
    				}
    			}
    		}
    		if (GetMapID(MapName) != TileMap.mapID)
    		{
    			mFont.tahoma_7b_yellow.drawString(g, NameBoss + " - " + MapName + " - " + ((num < 60) ? (num + "s") : (timeSpan.Minutes + "ph")) + " trước", x, y, align, mFont.tahoma_7_grey);
    		}
    		else
    		{
    			mFont.tahoma_7b_yellow.drawString(g, "Đang trong map có Boss " + NameBoss + " - " + ((num < 60) ? (num + "s") : (timeSpan.Minutes + "ph")) + " trước", x, y, align, mFont.tahoma_7_greySmall);
    		}
    	}
    }
}
