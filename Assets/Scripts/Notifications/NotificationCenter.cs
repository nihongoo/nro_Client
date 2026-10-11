using System.Collections.Generic;
using Mod.CuongLe;
using UnityEngine;

namespace Notifications
{
    public static class NotificationCenter
    {
        private static readonly List<string[]> rows = new List<string[]>();
        private static Rect bell, window;
        private static int tab, scroll, contentHeight, cachedRevision = -1, cachedWidth, cachedTab = -1;
        private static long cachedMinute = -1;
        private static bool dragging, backDown;
        private static int dragY, dragScroll, consumedTick = -1;
        public static bool IsOpen { get; private set; }
        public static bool ConsumesInput => IsOpen || consumedTick == GameCanvas.gameTick;

        private static bool CanUse => GameCanvas.currentScreen == GameScr.instance && GameScr.isPaint && !GameScr.isPaintOther && !GameCanvas.isLoading &&
            !GameCanvas.panel.isShow && !DoHoa.isShowMenuVIP && GameCanvas.currentDialog == null && !GameCanvas.menu.showMenu &&
            ChatPopup.currChatPopup == null && ChatPopup.serverChatPopUp == null &&
            !ChatTextField.gI().isShow && !Char.ischangingMap;

        public static void Close()
        {
            IsOpen = false; dragging = false; NotificationStore.ReadingServer = false;
            consumedTick = GameCanvas.gameTick;
            GameCanvas.clearAllPointerEvent(); GameCanvas.clearKeyHold(); GameCanvas.clearKeyPressed();
            GameCanvas.keyAsciiPress = 0;
        }

        public static void BeginSession()
        {
            // Login can precede GameScr construction; reset data without touching gameplay UI.
            IsOpen = false; dragging = false; tab = 0; scroll = 0; consumedTick = -1;
            NotificationStore.BeginSession();
        }

        public static bool HandleKey(int key)
        {
            if (!IsOpen) return false;
            if (key == -7 || key == -22 || key == 27) Close();
            return true;
        }

        public static void UpdateInput()
        {
            bool escape = Input.GetKey(KeyCode.Escape);
            if (!CanUse) { if (IsOpen) Close(); backDown = escape; return; }
            bell = NotificationHudLayout.BellBounds();
            int size = NotificationHudLayout.TouchSize;
            int width = System.Math.Min(380, GameCanvas.w - 12), height = System.Math.Min(300, GameCanvas.h - 12);
            window = new Rect((GameCanvas.w - width) / 2, (GameCanvas.h - height) / 2, width, height);
            if (!IsOpen)
            {
                if (bell.width > 0 && GameCanvas.isPointerJustRelease && GameCanvas.isPointerClick &&
                    bell.Contains(new Vector2(GameCanvas.px, GameCanvas.py)))
                {
                    IsOpen = true; tab = 0; scroll = 0; dragging = false;
                    Consume();
                }
                backDown = escape;
                return;
            }
            consumedTick = GameCanvas.gameTick;
            if (escape && !backDown || GameCanvas.keyPressed[13]) { Close(); backDown = escape; return; }
            backDown = escape;
            EnsureRows();
            Vector2 pointer = new Vector2(GameCanvas.px, GameCanvas.py);
            if (GameCanvas.isPointerJustRelease)
            {
                if (!window.Contains(pointer)) Close();
                else if (pointer.y < window.y + size)
                {
                    int selected = pointer.x < window.center.x ? 0 : 1;
                    if (selected != tab) { tab = selected; scroll = 0; cachedTab = -1; }
                }
                dragging = false;
            }
            else if (GameCanvas.isPointerDown && pointer.y >= window.y + size && window.Contains(pointer))
            {
                if (!dragging) { dragging = true; dragY = GameCanvas.py; dragScroll = scroll; }
                scroll = System.Math.Max(0, System.Math.Min(System.Math.Max(0, contentHeight - (int)window.height + size + 8), dragScroll + dragY - GameCanvas.py));
            }
            NotificationStore.ReadingServer = IsOpen && tab == 1;
            if (NotificationStore.ReadingServer) NotificationStore.MarkServerRead();
            Consume();
        }

        private static void Consume()
        {
            consumedTick = GameCanvas.gameTick;
            GameCanvas.clearAllPointerEvent(); GameCanvas.clearKeyHold(); GameCanvas.clearKeyPressed();
            GameCanvas.keyAsciiPress = 0;
        }

        public static void PaintBell(mGraphics g)
        {
            if (!CanUse || IsOpen || bell.width <= 0) return;
            int x = (int)bell.x, y = (int)bell.y, size = (int)bell.width;
            PaintBellIcon(g, x + size / 2 - 11, y + size / 2 - 12);
            if (NotificationStore.UnreadCount > 0)
            {
                PopUp.paintPopUp(g, x + size - 16, y, 16, 14, 0xb5271e, false);
                mFont.tahoma_7_white.drawString(g, NotificationStore.UnreadCount.ToString(), x + size - 8, y + 1, 2);
            }
        }

        // Logical pixel art, like the neighboring HUD icons; the touch rectangle remains independent.
        private static void PaintBellIcon(mGraphics g, int x, int y)
        {
            g.setColor(0x54331c);
            g.fillRect(x + 9, y, 4, 2);
            g.fillRect(x + 8, y + 2, 6, 2);
            g.fillRect(x + 6, y + 4, 10, 2);
            g.fillRect(x + 4, y + 6, 14, 3);
            g.fillRect(x + 3, y + 9, 16, 6);
            g.fillRect(x + 2, y + 15, 18, 2);
            g.fillRect(x + 1, y + 17, 20, 3);
            g.fillRect(x + 3, y + 20, 16, 1);
            g.fillRect(x + 8, y + 21, 6, 2);
            g.fillRect(x + 9, y + 23, 4, 1);
            g.setColor(0xb76b20);
            g.fillRect(x + 10, y + 1, 2, 2);
            g.fillRect(x + 7, y + 5, 8, 2);
            g.fillRect(x + 5, y + 7, 12, 3);
            g.fillRect(x + 4, y + 10, 14, 6);
            g.fillRect(x + 3, y + 16, 16, 2);
            g.fillRect(x + 2, y + 18, 18, 1);
            g.setColor(0xefb83e);
            g.fillRect(x + 7, y + 6, 7, 2);
            g.fillRect(x + 5, y + 8, 10, 3);
            g.fillRect(x + 4, y + 11, 11, 5);
            g.fillRect(x + 3, y + 16, 13, 2);
            g.fillRect(x + 9, y + 21, 4, 1);
            g.setColor(0xffe59a);
            g.fillRect(x + 8, y + 6, 4, 1);
            g.fillRect(x + 6, y + 8, 2, 3);
            g.fillRect(x + 5, y + 11, 2, 4);
            g.fillRect(x + 3, y + 17, 14, 1);
        }

        public static void Paint(mGraphics g)
        {
            if (!IsOpen || !CanUse) return;
            EnsureRows();
            int x = (int)window.x, y = (int)window.y, w = (int)window.width, h = (int)window.height;
            int header = NotificationHudLayout.TouchSize;
            g.translate(-g.getTranslateX(), -g.getTranslateY()); g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
            // Match the stock Panel frame; ornamental corner sprites use a different outline.
            GameCanvas.paintz.paintFrameSimple(x, y, w, h, g);
            GameCanvas.paintz.paintFrameInside(x + 5, y + header + 2, w - 10, h - header - 7, g);
            // Use the same tab textures and selected/unselected fonts as Panel.paintTab.
            int tabWidth = (w - 16) / 2;
            for (int i = 0; i < 2; i++)
                PopUp.paintPopUp(g, x + 8 + i * tabWidth, y + 3, tabWidth - 1, header - 3, i == tab ? 1 : 0, true);
            (tab == 0 ? mFont.tahoma_7_green2 : mFont.tahoma_7_grey).drawString(g, "Boss", x + w / 4, y + header / 2 - 6, 2);
            string label = "Server" + (NotificationStore.UnreadCount > 0 ? " (" + NotificationStore.UnreadCount + ")" : "");
            (tab == 1 ? mFont.tahoma_7_green2 : mFont.tahoma_7_grey).drawString(g, label, x + w * 3 / 4, y + header / 2 - 6, 2);
            g.setClip(x + 5, y + header + 2, w - 10, System.Math.Max(0, h - header - 7));
            int rowY = y + header + 5 - scroll;
            if (rows.Count == 0) mFont.tahoma_7b_dark.drawString(g,
                NotificationStore.HasSnapshot ? "Chưa có thông báo." : "Đang nhận thông báo...", x + 10, rowY, 0);
            foreach (string[] row in rows)
            {
                foreach (string line in row) { mFont.tahoma_7b_dark.drawString(g, line, x + 10, rowY, 0); rowY += 12; }
                // drawLine scales axis-aligned coordinates twice; keep this separator in logical pixels.
                g.setColor(13524492); g.fillRect(x + 8, rowY + 2, w - 16, 1); rowY += 8;
            }
            g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
        }

        private static void EnsureRows()
        {
            long minute = NotificationStore.ServerNow / 60000;
            if (cachedRevision == NotificationStore.Revision && cachedWidth == (int)window.width && cachedTab == tab && cachedMinute == minute) return;
            cachedRevision = NotificationStore.Revision; cachedWidth = (int)window.width; cachedTab = tab; cachedMinute = minute;
            rows.Clear(); contentHeight = 0;
            IReadOnlyList<NotificationEntry> entries = tab == 0 ? NotificationStore.Bosses : NotificationStore.Server;
            foreach (NotificationEntry entry in entries)
            {
                long age = System.Math.Max(0, (NotificationStore.ServerNow - entry.UpdatedAt) / 60000);
                string text = tab == 0 ? entry.BossName + " - " + entry.MapName + " - " +
                    (entry.Killed ? "bị " + entry.Killer + " tiêu diệt - " : "") + age + " phút trước"
                    : "#" + entry.Id + " " + entry.Text;
                string[] lines = mFont.tahoma_7b_dark.splitFontArray(text, System.Math.Max(20, (int)window.width - 24));
                rows.Add(lines); contentHeight += lines.Length * 12 + 8;
            }
            scroll = System.Math.Max(0, System.Math.Min(scroll, System.Math.Max(0, contentHeight - (int)window.height + NotificationHudLayout.TouchSize + 8)));
        }
    }
}
