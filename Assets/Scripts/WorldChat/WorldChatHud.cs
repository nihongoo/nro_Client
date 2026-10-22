using UnityEngine;

namespace WorldChat
{
    public static class WorldChatHud
    {
        private static bool CanShow => GameScr.instance != null && GameCanvas.currentScreen == GameScr.instance
            && GameScr.isPaint && !GameScr.isPaintOther && !GameCanvas.isLoading && !Char.ischangingMap;

        public static void BeginSession()
        {
            WorldChatStore.BeginSession(); ChatTextField.ResetWorldSession();
        }
        public static void Open()
        {
            if (!CanShow || GameCanvas.currentDialog != null) return;
            Notifications.NotificationCenter.Close();
            if (GameCanvas.panel.isShow) GameCanvas.panel.hide();
            GameCanvas.clearAllPointerEvent(); GameCanvas.clearKeyHold(); GameCanvas.clearKeyPressed();
            ChatTextField.gI().OpenWorldChat();
            if (!WorldChatStore.Ready) WorldChatProtocol.RequestSnapshot();
        }
        public static void UpdateInput()
        {
            if (WorldChatStore.CheckTimeout()) WorldChatProtocol.RequestSnapshot();
            if (!CanShow) return;
            ChatTextField.gI().HandleWorldResult();
            // A hidden ticker must not finish while the player is typing/confirming.
            if (!ChatTextField.gI().isShow && GameCanvas.currentDialog == null)
                WorldChatStore.Ticker.Update(GameCanvas.w, mFont.tahoma_7b_yellow.getWidth);
            if (GameCanvas.currentDialog != null || GameCanvas.panel.isShow || Notifications.NotificationCenter.IsOpen
                || GameCanvas.menu.showMenu || ChatPopup.currChatPopup != null || ChatPopup.serverChatPopUp != null
                || ChatTextField.gI().isShow || Mod.CuongLe.DoHoa.isShowMenuVIP) return;
            if (WorldChatStore.Ticker.Current != null && GameCanvas.isPointerJustRelease && GameCanvas.isPointerClick
                && WorldChatLayout.CanTapBar(new Vector2(GameCanvas.px, GameCanvas.py))) Open();
        }
        internal static bool IsVisible => CanShow && WorldChatStore.Ticker.Current != null && !GameCanvas.panel.isShow
            && !Notifications.NotificationCenter.IsOpen && GameCanvas.currentDialog == null && ChatPopup.currChatPopup == null
            && ChatPopup.serverChatPopUp == null && !ChatTextField.gI().isShow;
        public static void Paint(mGraphics g)
        {
            if (!IsVisible) return;
            Rect bar = WorldChatLayout.BarBounds;
            g.translate(-g.getTranslateX(), -g.getTranslateY());
            // The stock 15px rectangle extended 2px off-screen. Its visible 13px area is unchanged.
            g.setClip((int)bar.x, (int)bar.y, (int)bar.width, (int)bar.height);
            g.fillRect((int)bar.x, (int)bar.y, (int)bar.width, (int)bar.height, 0, 90);
            string text = WorldChatStore.Ticker.Current;
            mFont.tahoma_7b_yellow.drawString(g, text, WorldChatStore.Ticker.X, (int)bar.y, 0, mFont.tahoma_7b_dark);
            g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
        }
    }
}
