namespace Mod.CuongLe
{
    public class MenuGiaoDien
    {
        public static string[] menuMod = new string[6] {"Danh sách nhân vật", "Địa hình lưới", "Danh sách SKH", "Thông tin up vàng", "Thông tin sư phụ", "Auto Giải Capcha"};

    	public static bool[] getArrMod()
    	{
            return new bool[6]
    		{
    			DoHoa.isShowCharsInMap,
    			DoHoa.MapLuoi,
    			ModProCuongLe.hienThiDoKH,
    			MainMod.infoTrainGold,
    			ModProCuongLe.charw,
    			MainMod.AutoCapCha
    		};
    	}
    }
}
