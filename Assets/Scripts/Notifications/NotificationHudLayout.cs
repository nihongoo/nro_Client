using UnityEngine;

namespace Notifications
{
    public static class NotificationHudLayout
    {
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
