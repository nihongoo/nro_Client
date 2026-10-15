using UnityEngine;

namespace Notifications
{
    public static class NotificationHudLayout
    {
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

        // Temporary messages never relocate the control. Hide only while their real bounds cover it.
        public static bool IsBellObscured
        {
            get
            {
                Rect bell = Hud.HudLayout.GetRect(Hud.HudLayout.Bell);
                PopUpYesNo popup = GameScr.gI().popUpYesNo;
                if (popup != null) return popup.VisualBounds.Overlaps(bell);
                InfoMe info = GameScr.info2;
                return info.isUpdate && !GameScr.gI().isVS() && info.info.info != null && info.info.info.charInfo != null &&
                    bell.Overlaps(new Rect(info.cmx - info.info.W / 2 - 20,
                        info.cmy - info.info.H - 10, info.info.W + 40, info.info.H + 30));
            }
        }

    }
}
