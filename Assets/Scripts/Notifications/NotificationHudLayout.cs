using Mod.CuongLe;
using UnityEngine;

namespace Notifications
{
    public static class NotificationHudLayout
    {
        private static Rect cachedBell;
        private static int cachedKey;
        private static bool hasLayout;

        // Convert physical dp into the game's scaled logical coordinates.
        public static int TouchSize
        {
            get
            {
                float density = Screen.dpi > 0 ? Mathf.Max(1, Screen.dpi / 160f) : 1;
                float scale = Application.platform == RuntimePlatform.Android
                    ? ScaleGUI.WIDTH / Mathf.Max(1, ScaleGUI.Viewport.width) : 1;
                return Mathf.Max(24, Mathf.CeilToInt(48 * density * scale / Mathf.Max(1, mGraphics.zoomLevel)));
            }
        }

        public static Rect BellBounds()
        {
            int size = TouchSize;
            InfoMe info = GameScr.info2;
            int key = GameCanvas.w * 397 ^ GameCanvas.h ^ size * 31 ^ Char.vItemTime.size() * 71 ^
                GameScr.textTime.size() * 97 ^ (DoHoa.HienThiLogo ? 1 : 0) ^
                (GameScr.gI().popUpYesNo != null ? 13 : 0) ^ info.cmx * 17 ^ info.cmy * 19;
            if (hasLayout && key == cachedKey && cachedBell.width > 0 && IsFree(cachedBell)) return cachedBell;
            hasLayout = true; cachedKey = key;
            int start = GameScr.imgPanel.getWidth() + DoHoa.imgSetting.getWidth() + DoHoa.imgBoxOpenItem.getWidth() + 20;
            // Prefer the existing mod button cluster; search around occupied HUD rectangles if needed.
            for (int y = 3; y + size < GameCanvas.h - 55; y += 4)
                for (int x = start; x + size < GameCanvas.w - 4; x += 4)
                    if (IsFree(new Rect(x, y, size, size))) return cachedBell = new Rect(x, y, size, size);
            for (int y = 3; y + size < GameCanvas.h - 55; y += 4)
                for (int x = 4; x + size < GameCanvas.w - 4; x += 4)
                    if (IsFree(new Rect(x, y, size, size))) return cachedBell = new Rect(x, y, size, size);
            return cachedBell = new Rect(); // Never steal the hitbox of an existing control on an unsupported tiny viewport.
        }

        private static bool IsFree(Rect box)
        {
            int panel = GameScr.imgPanel.getWidth();
            int controls = panel + DoHoa.imgSetting.getWidth() + DoHoa.imgBoxOpenItem.getWidth() + 20;
            if (box.Overlaps(new Rect(0, 0, controls, Mathf.Max(45, GameScr.imgPanel.getHeight() + 4)))) return false;
            if (box.Overlaps(new Rect(GameCanvas.w - 155, 0, 155, 40))) return false;
            if (DoHoa.HienThiLogo && DoHoa.imgLogo != null && box.Overlaps(new Rect(
                GameCanvas.w / 2 - DoHoa.imgLogo.getWidth() / 2 - 3, 20 - DoHoa.imgLogo.getHeight() / 2 - 3,
                DoHoa.imgLogo.getWidth() + 6, DoHoa.imgLogo.getHeight() + 6))) return false;
            if (Char.vItemTime.size() > 0 && box.Overlaps(new Rect(16, 39, Char.vItemTime.size() * 24 + 32, 43))) return false;
            int textY = Char.vItemTime.size() == 0 ? 45 : 90;
            if (GameScr.textTime.size() > 0 && box.Overlaps(new Rect(0, textY, panel + 50, GameScr.textTime.size() * 12 + 14))) return false;
            if (box.Overlaps(new Rect(0, GameCanvas.h - 170, Mathf.Min(190, GameCanvas.w / 2), 170))) return false;
            if (box.Overlaps(new Rect(0, GameCanvas.h - 100, 120, 100)) ||
                box.Overlaps(new Rect(GameCanvas.w - 100, GameCanvas.h - 100, 100, 100))) return false;
            InfoMe info = GameScr.info2;
            if (info.info.info != null && info.info.info.charInfo != null && box.Overlaps(new Rect(
                info.cmx - info.info.W / 2 - 20, info.cmy - info.info.H - 10, info.info.W + 40, info.info.H + 30))) return false;
            if (GameScr.gI().popUpYesNo != null && box.Overlaps(new Rect(GameCanvas.w - 210, 0, 210, 85))) return false;
            return true;
        }
    }
}
