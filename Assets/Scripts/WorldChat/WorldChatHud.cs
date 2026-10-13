using UnityEngine;

namespace WorldChat
{
    public static class WorldChatHud
    {
        private static long latestId;
        private static int offset, pause, ticks, lastWidth;
        private static string latestText;
        private static bool CanShow => GameScr.instance != null && GameCanvas.currentScreen == GameScr.instance
            && GameScr.isPaint && !GameScr.isPaintOther && !GameCanvas.isLoading && !Char.ischangingMap;

        public static void BeginSession()
        {
            WorldChatStore.BeginSession(); latestId = 0; latestText = null; offset = 0; pause = 60; ticks = 0;
        }
        public static void Open()
        {
            if (!CanShow || GameCanvas.currentDialog != null) return;
            if (WorldChatLayout.HistoryBounds().width == 0) { GameCanvas.startOKDlg("Màn hình quá nhỏ để mở chat thế giới."); return; }
            Notifications.NotificationCenter.Close();
            if (GameCanvas.panel.isShow) GameCanvas.panel.hide();
            ChatTextField.gI().isShow = false;
            GameCanvas.clearAllPointerEvent(); GameCanvas.clearKeyHold(); GameCanvas.clearKeyPressed();
            new WorldChatDialog().show();
            if (!WorldChatStore.Ready) WorldChatProtocol.RequestSnapshot();
        }
        public static void UpdateInput()
        {
            if (WorldChatStore.CheckTimeout()) WorldChatProtocol.RequestSnapshot();
            if (!CanShow) return;
            if (WorldChatStore.Entries.Count > 0)
            {
                WorldChatEntry latest = WorldChatStore.Entries[WorldChatStore.Entries.Count - 1];
                if (latest.Id != latestId || lastWidth != GameCanvas.w)
                {
                    latestId = latest.Id; latestText = latest.Display; offset = 0; pause = 60; ticks = 0; lastWidth = GameCanvas.w;
                }
                if (++ticks % 2 == 0 && mFont.tahoma_7b_yellow.getWidth(latestText) > GameCanvas.w)
                {
                    if (pause > 0) pause--;
                    else if (++offset > mFont.tahoma_7b_yellow.getWidth(latestText) + 24) { offset = 0; pause = 60; }
                }
            }
            if (GameCanvas.currentDialog != null || GameCanvas.panel.isShow || Notifications.NotificationCenter.IsOpen
                || GameCanvas.menu.showMenu || ChatPopup.currChatPopup != null || ChatPopup.serverChatPopUp != null
                || ChatTextField.gI().isShow || Mod.CuongLe.DoHoa.isShowMenuVIP) return;
            if (GameCanvas.isPointerJustRelease && GameCanvas.isPointerClick
                && WorldChatLayout.CanTapBar(new Vector2(GameCanvas.px, GameCanvas.py))) Open();
        }
        public static void Paint(mGraphics g)
        {
            if (!CanShow || GameCanvas.panel.isShow || Notifications.NotificationCenter.IsOpen || GameCanvas.currentDialog != null
                || ChatPopup.currChatPopup != null || ChatPopup.serverChatPopUp != null || ChatTextField.gI().isShow) return;
            g.translate(-g.getTranslateX(), -g.getTranslateY());
            // Match origin/develop's paintChatVip exactly (the last two pixels are off-screen).
            g.setClip(0, GameCanvas.h - 13, GameCanvas.w, 15);
            g.fillRect(0, GameCanvas.h - 13, GameCanvas.w, 15, 0, 90);
            string text = latestText ?? "Chat thế giới - bấm để xem và gửi tin";
            mFont.tahoma_7b_yellow.drawString(g, text, -offset, GameCanvas.h - 13, 0, mFont.tahoma_7b_dark);
            g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
        }
    }
}
