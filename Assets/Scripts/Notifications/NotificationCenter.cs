using System.Collections.Generic;
using Mod.CuongLe;
using UnityEngine;

namespace Notifications
{
    public static class NotificationCenter
    {
        private static readonly List<string[]> rows = new List<string[]>();
        private static Rect bell, window, closeButton, content;
        private static readonly List<NotificationEntry> rowEntries = new List<NotificationEntry>();
        private static readonly Rect[] tabs = new Rect[3];
        private static int tab, scroll, contentHeight, cachedRevision = -1, cachedWidth, cachedTab = -1;
        private static long cachedMinute = -1;
        private static bool dragging, backDown;
        private static int dragY, dragScroll, consumedTick = -1;
        private static int ActionSize => System.Math.Max(32, NotificationHudLayout.TouchSize);
        // Match the "Chat" label in ChatTextField.paint, including its font size and color.
        private static mFont TitleFont => mFont.tahoma_7b_green2;
        public static bool IsOpen { get; private set; }
        public static bool ConsumesInput => IsOpen || consumedTick == GameCanvas.gameTick;

        private static bool CanUse => GameCanvas.currentScreen == GameScr.instance && GameScr.isPaint && !GameScr.isPaintOther && !GameCanvas.isLoading &&
            !GameCanvas.panel.isShow && !DoHoa.isShowMenuVIP && GameCanvas.currentDialog == null && !GameCanvas.menu.showMenu &&
            ChatPopup.currChatPopup == null && ChatPopup.serverChatPopUp == null &&
            !ChatTextField.gI().isShow && !Char.ischangingMap;

        internal static bool IsBellVisible => CanUse && !IsOpen && !NotificationHudLayout.IsBellObscured;

        public static void Close()
        {
            IsOpen = false; dragging = false; NotificationStore.ReadingServer = false; NotificationStore.ReadingInvitations = false;
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
            bell = Hud.HudLayout.GetRect(Hud.HudLayout.Bell);
            if (!IsOpen && NotificationHudLayout.IsBellObscured) { backDown = escape; return; }
            UpdateLayout();
            if (!IsOpen)
            {
                if (bell.width > 0 && GameCanvas.isPointerJustRelease && GameCanvas.isPointerClick &&
                    Hud.HudLayout.IsTopmost(Hud.HudLayout.Bell, new Vector2(GameCanvas.px, GameCanvas.py)))
                {
                    IsOpen = true; tab = 0; scroll = 0; dragging = false;
                    UpdateLayout();
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
                    bool switched = false;
                    for (int i = 0; i < tabs.Length; i++)
                        if (tabs[i].Contains(pointer) && i != tab) { tab = i; scroll = 0; cachedTab = -1; switched = true; }
                    if (!switched && tab == 2 && content.Contains(pointer) && (!dragging || System.Math.Abs(GameCanvas.py - dragY) < 6))
                        HandleInvitationAction(pointer);
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
            NotificationStore.ReadingInvitations = IsOpen && tab == 2;
            if (NotificationStore.ReadingInvitations) NotificationStore.MarkInvitationsRead();
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
            bell = Hud.HudLayout.GetRect(Hud.HudLayout.Bell);
            if (!IsBellVisible || bell.width <= 0) return;
            int x = (int)bell.x, y = (int)bell.y, size = (int)bell.width;
            int centerX = NotificationHudLayout.BellCenterX;
            g.drawImage(NotificationSkin.BellIcon, centerX, y + size / 2, 3);
            if (NotificationStore.UnreadCount > 0)
            {
                int badgeX = System.Math.Min(x + size - 12, centerX + 10);
                g.drawImage(NotificationSkin.BadgeIcon, badgeX, y + 9, 3);
                mFont.tahoma_7b_white.drawString(g, NotificationStore.UnreadCount.ToString(),
                    badgeX, y + (18 - mFont.tahoma_7b_white.getHeight()) / 2, 2);
            }
        }

        private static void UpdateLayout()
        {
            const int padding = 10, gap = 6;
            // Keep the panel at the bell, on the right side, rather than covering the screen center.
            int width = System.Math.Min(GameCanvas.w - 12, System.Math.Min(320, System.Math.Max(180, GameCanvas.w * 45 / 100)));
            int x = System.Math.Max(6, System.Math.Min(GameCanvas.w - width - 6, (int)bell.xMax - width));
            int y = System.Math.Max(6, System.Math.Min(GameCanvas.h - 6, (int)bell.y));
            int maxHeight = System.Math.Min(300, GameCanvas.h - y - 6);
            window = new Rect(x, y, width, maxHeight);
            int closeSize = System.Math.Max(NotificationSkin.CloseIcon.getWidth(), NotificationSkin.CloseIcon.getHeight());
            int titleRowHeight = System.Math.Max(TitleFont.getHeight(), closeSize);
            closeButton = new Rect(window.xMax - padding - closeSize,
                window.y + padding + (titleRowHeight - closeSize) / 2, closeSize, closeSize);
            // Use the standard Command button height instead of the bell's DPI-sized touch target.
            int tabHeight = mScreen.cmdH;
            int tabWidth = (width - padding * 2 - gap * 2) / 3;
            float tabY = window.y + padding + titleRowHeight + gap;
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
            UpdateLayout();
            int x = (int)window.x, y = (int)window.y, w = (int)window.width, h = (int)window.height;
            g.translate(-g.getTranslateX(), -g.getTranslateY()); g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
            NotificationSkin.PaintFrame(g, x, y, w, h, false);
            g.drawImage(NotificationSkin.CloseIcon, (int)closeButton.center.x, (int)closeButton.center.y, 3);
            TitleFont.drawString(g, "Thông báo", (int)content.x,
                (int)closeButton.center.y - TitleFont.getHeight() / 2, 0);
            for (int i = 0; i < tabs.Length; i++)
            {
                Rect bounds = tabs[i];
                NotificationSkin.PaintFrame(g, (int)bounds.x, (int)bounds.y, (int)bounds.width, (int)bounds.height, i == tab);
                int count = i == 1 ? NotificationStore.ServerUnreadCount : NotificationStore.InvitationUnreadCount;
                string label = i == 0 ? "Boss" : (i == 1 ? "Server" : "Lời mời") + (count > 0 ? " (" + count + ")" : "");
                (i == tab ? mFont.tahoma_7_green2 : mFont.tahoma_7_grey).drawString(g, label,
                    (int)bounds.center.x, (int)bounds.center.y - 6, 2);
            }
            g.setClip((int)content.x, (int)content.y, (int)content.width, (int)content.height);
            int rowY = (int)content.y + 3 - scroll;
            if (rows.Count == 0) mFont.tahoma_7b_dark.drawString(g,
                NotificationStore.HasSnapshot ? "Chưa có thông báo." : "Đang nhận thông báo...", (int)content.x + 4, rowY, 0);
            for (int index = 0; index < rows.Count; index++)
            {
                string[] row = rows[index];
                int bodyHeight = RowBodyHeight(row);
                int textY = rowY + (bodyHeight - row.Length * 12) / 2;
                foreach (string line in row) { mFont.tahoma_7b_dark.drawString(g, line, (int)content.x + 4, textY, 0); textY += 12; }
                if (tab == 2) {
                    NotificationEntry entry = rowEntries[index];
                    int actionY = rowY + (bodyHeight - ActionSize) / 2;
                    if (entry.Responding) mFont.tahoma_7_grey.drawString(g, "Đang trả lời...",
                        (int)ActionBounds(actionY, true).x, rowY + (bodyHeight - 12) / 2, 0);
                    else {
                        PopUpYesNo.PaintAction(g, ActionBounds(actionY, true), true);
                        PopUpYesNo.PaintAction(g, ActionBounds(actionY, false), false);
                    }
                }
                rowY += bodyHeight;
                // drawLine scales axis-aligned coordinates twice; keep this separator in logical pixels.
                g.setColor(13524492); g.fillRect((int)content.x + 4, rowY + 2, (int)content.width - 8, 1); rowY += 8;
            }
            g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
        }

        private static int RowBodyHeight(string[] row)
        {
            return tab == 2 ? System.Math.Max(row.Length * 12, ActionSize) : row.Length * 12;
        }

        private static Rect ActionBounds(int y, bool accept)
        {
            int size = ActionSize;
            return new Rect(content.xMax - (accept ? size * 2 + 10 : size + 4), y, size, size);
        }

        private static void HandleInvitationAction(Vector2 pointer)
        {
            EnsureRows();
            int y = (int)content.y + 3 - scroll;
            for (int index = 0; index < rows.Count; index++) {
                int bodyHeight = RowBodyHeight(rows[index]);
                int actionY = y + (bodyHeight - ActionSize) / 2;
                if (ActionBounds(actionY, true).Contains(pointer)) { NotificationStore.RespondToInvitation(rowEntries[index].Id, true); return; }
                if (ActionBounds(actionY, false).Contains(pointer)) { NotificationStore.RespondToInvitation(rowEntries[index].Id, false); return; }
                y += bodyHeight + 8;
            }
        }

        private static void EnsureRows()
        {
            long minute = NotificationStore.ServerNow / (tab == 2 ? 1000 : 60000);
            if (cachedRevision == NotificationStore.Revision && cachedWidth == (int)content.width && cachedTab == tab && cachedMinute == minute) return;
            cachedRevision = NotificationStore.Revision; cachedWidth = (int)content.width; cachedTab = tab; cachedMinute = minute;
            rows.Clear(); rowEntries.Clear(); contentHeight = 0;
            IReadOnlyList<NotificationEntry> entries = tab == 0 ? NotificationStore.Bosses : tab == 1 ? NotificationStore.Server : NotificationStore.Invitations;
            foreach (NotificationEntry entry in entries)
            {
                long age = System.Math.Max(0, (NotificationStore.ServerNow - entry.UpdatedAt) / 60000);
                string text = tab == 0 ? entry.BossName + " - " + entry.MapName + " - " +
                    (entry.Killed ? "bị " + entry.Killer + " tiêu diệt - " : "") + age + " phút trước"
                    : tab == 1 ? "#" + entry.Id + " " + entry.Text
                    : entry.Text + " (" + System.Math.Max(0, (entry.ExpiresAt - NotificationStore.ServerNow + 999) / 1000) + "s)";
                // Reserve the right column for actions; wrapped text and buttons share one row.
                int textWidth = tab == 2 ? (int)content.width - ActionSize * 2 - 20 : (int)content.width - 8;
                string[] lines = mFont.tahoma_7b_dark.splitFontArray(text, System.Math.Max(20, textWidth));
                rows.Add(lines); rowEntries.Add(entry); contentHeight += RowBodyHeight(lines) + 8;
            }
        }
    }
}
