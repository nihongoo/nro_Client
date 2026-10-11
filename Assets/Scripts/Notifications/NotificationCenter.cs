using System.Collections.Generic;
using Mod.CuongLe;
using UnityEngine;

namespace Notifications
{
    public static class NotificationCenter
    {
        private static readonly List<string[]> rows = new List<string[]>();
        private static Rect bell, window, closeButton, content;
        private static readonly Rect[] tabs = new Rect[2];
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
            UpdateLayout(size);
            if (!IsOpen)
            {
                if (bell.width > 0 && GameCanvas.isPointerJustRelease && GameCanvas.isPointerClick &&
                    bell.Contains(new Vector2(GameCanvas.px, GameCanvas.py)))
                {
                    IsOpen = true; tab = 0; scroll = 0; dragging = false;
                    UpdateLayout(size);
                    Consume();
                }
                backDown = escape;
                return;
            }
            consumedTick = GameCanvas.gameTick;
            if (escape && !backDown || GameCanvas.keyPressed[13]) { Close(); backDown = escape; return; }
            backDown = escape;
            Vector2 pointer = new Vector2(GameCanvas.px, GameCanvas.py);
            if (GameCanvas.isPointerJustRelease)
            {
                if (!window.Contains(pointer) || closeButton.Contains(pointer)) Close();
                else
                {
                    for (int i = 0; i < tabs.Length; i++)
                        if (tabs[i].Contains(pointer) && i != tab) { tab = i; scroll = 0; cachedTab = -1; }
                }
                dragging = false;
            }
            else if (GameCanvas.isPointerDown && content.Contains(pointer))
            {
                if (!dragging) { dragging = true; dragY = GameCanvas.py; dragScroll = scroll; }
                scroll = System.Math.Max(0, System.Math.Min(System.Math.Max(0, contentHeight - (int)content.height), dragScroll + dragY - GameCanvas.py));
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
            g.drawImage(NotificationSkin.BellIcon, x + size / 2 - 4, y + size - 12, 3);
            if (NotificationStore.UnreadCount > 0)
            {
                int badgeX = x + size - 12;
                g.drawImage(NotificationSkin.BadgeIcon, badgeX, y + 9, 3);
                mFont.tahoma_7b_white.drawString(g, NotificationStore.UnreadCount.ToString(),
                    badgeX, y + (18 - mFont.tahoma_7b_white.getHeight()) / 2, 2);
            }
        }

        private static void UpdateLayout(int touchSize)
        {
            const int padding = 10, gap = 6;
            // Keep the panel at the bell, on the right side, rather than covering the screen center.
            int width = System.Math.Min(GameCanvas.w - 12, System.Math.Min(320, System.Math.Max(180, GameCanvas.w * 45 / 100)));
            int x = System.Math.Max(6, System.Math.Min(GameCanvas.w - width - 6, (int)bell.xMax - width));
            int y = System.Math.Max(6, System.Math.Min(GameCanvas.h - 6, (int)bell.y));
            int maxHeight = System.Math.Min(300, GameCanvas.h - y - 6);
            window = new Rect(x, y, width, maxHeight);
            closeButton = new Rect(window.xMax - padding - touchSize, window.y + padding, touchSize, touchSize);
            int tabHeight = System.Math.Max(30, touchSize);
            int tabWidth = (width - padding * 2 - gap) / 2;
            float tabY = closeButton.yMax + gap;
            for (int i = 0; i < tabs.Length; i++)
                tabs[i] = new Rect(window.x + padding + i * (tabWidth + gap), tabY, tabWidth, tabHeight);
            float contentY = tabY + tabHeight + 8;
            content = new Rect(window.x + padding, contentY, width - padding * 2, 0);
            // Measure wrapped rows at the final width before sizing the panel; Server can still scroll.
            EnsureRows();
            int headerHeight = (int)(contentY - window.y) + padding;
            window.height = System.Math.Min(maxHeight, headerHeight + System.Math.Max(20, contentHeight + 3));
            content.height = System.Math.Max(0, (int)(window.yMax - padding - contentY));
            scroll = System.Math.Max(0, System.Math.Min(scroll, System.Math.Max(0, contentHeight - (int)content.height)));
        }

        public static void Paint(mGraphics g)
        {
            if (!IsOpen || !CanUse) return;
            UpdateLayout(NotificationHudLayout.TouchSize);
            int x = (int)window.x, y = (int)window.y, w = (int)window.width, h = (int)window.height;
            g.translate(-g.getTranslateX(), -g.getTranslateY()); g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
            NotificationSkin.PaintFrame(g, x, y, w, h, false);
            g.drawImage(NotificationSkin.CloseIcon, (int)closeButton.center.x, (int)closeButton.center.y, 3);
            mFont.tahoma_7b_dark.drawString(g, "Thông báo", (int)window.center.x,
                (int)closeButton.center.y - 6, 2);
            for (int i = 0; i < tabs.Length; i++)
            {
                Rect bounds = tabs[i];
                NotificationSkin.PaintFrame(g, (int)bounds.x, (int)bounds.y, (int)bounds.width, (int)bounds.height, i == tab);
                string label = i == 0 ? "Boss" : "Server" + (NotificationStore.UnreadCount > 0 ? " (" + NotificationStore.UnreadCount + ")" : "");
                (i == tab ? mFont.tahoma_7_green2 : mFont.tahoma_7_grey).drawString(g, label,
                    (int)bounds.center.x, (int)bounds.center.y - 6, 2);
            }
            g.setClip((int)content.x, (int)content.y, (int)content.width, (int)content.height);
            int rowY = (int)content.y + 3 - scroll;
            if (rows.Count == 0) mFont.tahoma_7b_dark.drawString(g,
                NotificationStore.HasSnapshot ? "Chưa có thông báo." : "Đang nhận thông báo...", (int)content.x + 4, rowY, 0);
            foreach (string[] row in rows)
            {
                foreach (string line in row) { mFont.tahoma_7b_dark.drawString(g, line, (int)content.x + 4, rowY, 0); rowY += 12; }
                // drawLine scales axis-aligned coordinates twice; keep this separator in logical pixels.
                g.setColor(13524492); g.fillRect((int)content.x + 4, rowY + 2, (int)content.width - 8, 1); rowY += 8;
            }
            g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
        }

        private static void EnsureRows()
        {
            long minute = NotificationStore.ServerNow / 60000;
            if (cachedRevision == NotificationStore.Revision && cachedWidth == (int)content.width && cachedTab == tab && cachedMinute == minute) return;
            cachedRevision = NotificationStore.Revision; cachedWidth = (int)content.width; cachedTab = tab; cachedMinute = minute;
            rows.Clear(); contentHeight = 0;
            IReadOnlyList<NotificationEntry> entries = tab == 0 ? NotificationStore.Bosses : NotificationStore.Server;
            foreach (NotificationEntry entry in entries)
            {
                long age = System.Math.Max(0, (NotificationStore.ServerNow - entry.UpdatedAt) / 60000);
                string text = tab == 0 ? entry.BossName + " - " + entry.MapName + " - " +
                    (entry.Killed ? "bị " + entry.Killer + " tiêu diệt - " : "") + age + " phút trước"
                    : "#" + entry.Id + " " + entry.Text;
                string[] lines = mFont.tahoma_7b_dark.splitFontArray(text, System.Math.Max(20, (int)content.width - 8));
                rows.Add(lines); contentHeight += lines.Length * 12 + 8;
            }
        }
    }
}
