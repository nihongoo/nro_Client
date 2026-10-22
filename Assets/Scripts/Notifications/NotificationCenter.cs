using System.Collections.Generic;
using Mod.CuongLe;
using UnityEngine;
using Hud;

namespace Notifications
{
    public static class NotificationCenter
    {
        private const string Dim = "notifications.dim", Panel = "notifications.panel",
            CloseButton = "notifications.close", Content = "notifications.content";
        private const int ModalOrder = HudLayout.OverlayOrder + 10, Padding = 8, Gap = 4,
            ActionSize = 32, LineHeight = 12, SlideMilliseconds = 180;
        private static readonly string[] TabKeys = { "notifications.boss", "notifications.server", "notifications.invites" };
        private static readonly string[] TabLabels = { "Boss", "Server", "Lời mời" };
        private sealed class Row
        {
            internal NotificationEntry Entry;
            internal string Text;
            internal int TextWidth;
            internal string[] ExpandedLines;
            internal string Age;
            internal int AgeWidth;
            internal int Top, Height;
        }
        // Slot rects are registered once and reused as data changes; the pool grows only to
        // the largest list encountered. Every action uses its registry rect for paint and input.
        private sealed class Slot
        {
            internal string Body, Text, Age, Yes, No, ServerToggle;
        }
        private static readonly List<Row> rows = new List<Row>();
        private static readonly List<Slot> slots = new List<Slot>();
        private static readonly HashSet<long> expandedServer = new HashSet<long>();
        private static Rect bell;
        private static int tab, scroll, contentHeight, cachedRevision = -1, cachedWidth, cachedTab = -1;
        private static long cachedTime = -1, openedAt, marqueeStartedAt, pressedInvitation, pressedServer;
        private static int layoutWidth = -1, layoutHeight, layoutZoom, ageZoom = -1, ageColumnWidth;
        private static bool layoutMobile;
        private static float panelWidth;
        private static long AnimationNow => System.Diagnostics.Stopwatch.GetTimestamp() * 1000 / System.Diagnostics.Stopwatch.Frequency;
        private static bool backDown, captured, moved, contentPress;
        private static string pressed;
        private static Vector2 pressPoint;
        private static int dragScroll, consumedTick = -1;
        private static mFont TitleFont => mFont.tahoma_7b_green2;
        public static bool IsOpen { get; private set; }
        public static bool ConsumesInput => IsOpen || consumedTick == GameCanvas.gameTick;

        private static bool CanUse => GameCanvas.currentScreen == GameScr.instance && GameScr.isPaint && !GameScr.isPaintOther && !GameCanvas.isLoading &&
            !GameCanvas.panel.isShow && !DoHoa.isShowMenuVIP && GameCanvas.currentDialog == null && !GameCanvas.menu.showMenu &&
            ChatPopup.currChatPopup == null && ChatPopup.serverChatPopUp == null &&
            !ChatTextField.gI().isShow && !Char.ischangingMap;
        internal static bool IsBellVisible => TopActionsHud.IsRowVisible && !NotificationHudLayout.IsBellObscured;

        internal static void RegisterHudRects()
        {
            System.Func<bool> visible = () => IsOpen && CanUse;
            HudLayout.Register(Dim, () => HudLayout.ScreenBounds, ModalOrder, visible);
            HudLayout.Register(Panel, PanelBounds, ModalOrder + 1, visible);
            HudLayout.Register(CloseButton, () => {
                Rect panel = HudLayout.GetRect(Panel);
                return new Rect(panel.xMax - Padding - ActionSize, panel.y + Padding, ActionSize, ActionSize);
            }, ModalOrder + 2, visible);
            for (int i = 0; i < TabKeys.Length; i++) {
                int index = i;
                HudLayout.Register(TabKeys[index], () => {
                    Rect panel = HudLayout.GetRect(Panel);
                    float width = Mathf.Floor((panel.width - Padding * 2 - Gap * 2) / 3);
                    return new Rect(panel.x + Padding + index * (width + Gap),
                        panel.y + Padding + ActionSize + Gap, width, mScreen.cmdH);
                }, ModalOrder + 2, visible);
            }
            HudLayout.Register(Content, () => {
                Rect panel = HudLayout.GetRect(Panel), tabs = HudLayout.GetRect(TabKeys[0]);
                return new Rect(panel.x + Padding, tabs.yMax + Gap, panel.width - Padding * 2,
                    Mathf.Max(0, panel.yMax - Padding - tabs.yMax - Gap));
            }, ModalOrder + 2, visible);
        }

        private static Rect PanelBounds()
        {
            // Freeze geometry for this opening. Data, minute changes and tab switches must
            // never move the panel or change a running text viewport. Resize/zoom may relayout.
            if (layoutWidth != GameCanvas.w || layoutHeight != GameCanvas.h || layoutZoom != mGraphics.zoomLevel
                || layoutMobile != HudLayout.IsMobileLayout) {
                layoutWidth = GameCanvas.w; layoutHeight = GameCanvas.h; layoutZoom = mGraphics.zoomLevel;
                layoutMobile = HudLayout.IsMobileLayout;
                panelWidth = layoutMobile ? Mathf.Floor(GameCanvas.w * 0.4f)
                    : Mathf.Floor(Mathf.Min(GameCanvas.w * 0.9f,
                        Mathf.Max(HudLayout.PixelsToLogical(420), MeasureTextWidth() + Padding * 2 + Gap * 2 + 2)));
            }
            float width = panelWidth;
            Rect bounds = HudLayout.Anchor(HorizontalAnchor.Right, VerticalAnchor.Top,
                new Vector2(width, GameCanvas.h), Vector2.zero);
            float progress = IsOpen ? Mathf.Clamp01((AnimationNow - openedAt) / (float)SlideMilliseconds) : 0;
            bounds.x += Mathf.Round(width * (1 - progress));
            return bounds;
        }

        private static IReadOnlyList<NotificationEntry> Entries => tab == 0 ? NotificationStore.BossHistory
            : tab == 1 ? NotificationStore.Server : NotificationStore.Invitations;

        private static string RowText(NotificationEntry entry) => (tab == 0
            ? entry.BossName + (entry.Killed ? " bị " + entry.Killer + " tiêu diệt tại " : " đã xuất hiện tại ")
                + entry.MapName
            : tab == 1 ? "#" + entry.Id + " " + entry.Text
            : entry.Text + " (" + System.Math.Max(0, (entry.ExpiresAt - NotificationStore.ServerNow + 999) / 1000) + "s)")
                .Replace('\n', ' ').Replace('\b', ' ').Replace('\r', ' ');

        private static string BossAgeText(NotificationEntry entry) =>
            System.Math.Max(0, (NotificationStore.ServerNow - entry.UpdatedAt) / 60000) + " phút trước";

        private static float MeasureTextWidth()
        {
            float textWidth = 0;
            foreach (NotificationEntry entry in Entries) {
                int ageWidth = tab == 0 ? AgeColumnWidth + Gap * 2 : 0;
                textWidth = Mathf.Max(textWidth, mFont.tahoma_7b_dark.getWidth(RowText(entry)) + ageWidth);
            }
            return textWidth;
        }

        private static int AgeColumnWidth
        {
            get {
                if (ageZoom == mGraphics.zoomLevel) return ageColumnWidth;
                ageZoom = mGraphics.zoomLevel; ageColumnWidth = 0;
                // Reserve five digits using the widest game-font digit, including the outline.
                // 1 -> 2 or 9 -> 10 minutes cannot resize the adjacent scrolling text column.
                for (int digit = 0; digit <= 9; digit++)
                    ageColumnWidth = Mathf.Max(ageColumnWidth,
                        mFont.tahoma_7b_dark.getWidth(new string((char)('0' + digit), 5) + " phút trước") + 2);
                return ageColumnWidth;
            }
        }

        public static void Close()
        {
            IsOpen = false; captured = false; pressed = null;
            NotificationStore.ReadingServer = false; NotificationStore.ReadingInvitations = false;
            HudLayout.Invalidate(); Consume();
        }

        public static void BeginSession()
        {
            IsOpen = false; captured = false; pressed = null; tab = 0; scroll = 0; consumedTick = -1;
            cachedRevision = -1;
            expandedServer.Clear();
            layoutWidth = -1;
            NotificationStore.BeginSession();
            HudLayout.Invalidate();
        }

        public static bool HandleKey(int key)
        {
            if (!IsOpen) return false;
            if (key == -7 || key == -22 || key == 27) Close();
            return true;
        }

        internal static bool TryOpenBell()
        {
            if (!CanUse || IsOpen || !HudLayout.IsTopmost(HudLayout.Bell, new Vector2(GameCanvas.px, GameCanvas.py))) return false;
            IsOpen = true; tab = 0; scroll = 0; captured = false; pressed = null;
            expandedServer.Clear();
            openedAt = marqueeStartedAt = AnimationNow;
            layoutWidth = -1;
            HudLayout.Invalidate();
            PrepareRows(); Consume();
            return true;
        }

        public static void UpdateInput()
        {
            bool escape = Input.GetKey(KeyCode.Escape);
            if (!CanUse) { if (IsOpen) Close(); backDown = escape; return; }
            if (!IsOpen) { backDown = escape; return; }
            if (escape && !backDown || GameCanvas.keyPressed[13]) { Close(); backDown = escape; return; }
            backDown = escape;
            PrepareRows();
            Vector2 pointer = new Vector2(GameCanvas.px, GameCanvas.py);
            string hit = HudLayout.HitTest(pointer);
            if (GameCanvas.isPointerJustDown) {
                captured = true; moved = false; pressed = hit; pressPoint = pointer; dragScroll = scroll;
                contentPress = hit == Content || FindServerRow(hit, out pressedServer) || FindAction(hit, out pressedInvitation, out _);
            }
            if (captured) {
                if (Mathf.Abs(pointer.y - pressPoint.y) >= 6 || Mathf.Abs(pointer.x - pressPoint.x) >= 6) moved = true;
                if (contentPress && moved) SetScroll(dragScroll + Mathf.RoundToInt(pressPoint.y - pointer.y));
                if (GameCanvas.isPointerJustRelease) {
                    string activate = !moved && hit == pressed ? pressed : null;
                    captured = false; pressed = null;
                    Activate(activate);
                }
            }
            if (IsOpen) {
                if (GameCanvas.pXYScrollMouse != 0 && HudLayout.GetRect(Content).Contains(new Vector2(GameCanvas.pxMouse, GameCanvas.pyMouse)))
                    SetScroll(scroll - GameCanvas.pXYScrollMouse * 24);
                NotificationStore.ReadingServer = tab == 1;
                NotificationStore.ReadingInvitations = tab == 2;
                if (NotificationStore.ReadingServer) NotificationStore.MarkServerRead();
                if (NotificationStore.ReadingInvitations) NotificationStore.MarkInvitationsRead();
            }
            Consume();
        }

        private static void Activate(string name)
        {
            if (name == null) return;
            if (name == Dim || name == CloseButton) { Close(); return; }
            for (int i = 0; i < TabKeys.Length; i++)
                if (name == TabKeys[i]) {
                    tab = i; scroll = 0; cachedTab = -1; marqueeStartedAt = AnimationNow;
                    HudLayout.Invalidate(); PrepareRows(); return;
                }
            if (FindServerRow(name, out long serverId) && serverId == pressedServer) {
                if (!expandedServer.Remove(serverId)) expandedServer.Add(serverId);
                cachedRevision = -1; HudLayout.Invalidate(); PrepareRows(); return;
            }
            if (FindAction(name, out long id, out bool accept) && id == pressedInvitation)
                NotificationStore.RespondToInvitation(id, accept);
        }

        private static bool FindServerRow(string name, out long id)
        {
            id = 0;
            if (tab != 1) return false;
            for (int i = 0; i < rows.Count; i++)
                if (name == slots[i].ServerToggle) { id = rows[i].Entry.Id; return true; }
            return false;
        }

        private static bool FindAction(string name, out long id, out bool accept)
        {
            id = 0; accept = false;
            if (tab != 2) return false;
            for (int i = 0; i < rows.Count; i++) {
                if (name != slots[i].Yes && name != slots[i].No) continue;
                id = rows[i].Entry.Id; accept = name == slots[i].Yes;
                return !rows[i].Entry.Responding;
            }
            return false;
        }

        private static void Consume()
        {
            consumedTick = GameCanvas.gameTick;
            // Keep our own press owner: clearing global flags each frame must not lose a held
            // button, nor require isPointerClick (pointerReleased does not restore that flag).
            GameCanvas.clearAllPointerEvent(); GameCanvas.clearKeyHold(); GameCanvas.clearKeyPressed();
            GameCanvas.keyAsciiPress = 0; GameCanvas.pXYScrollMouse = 0;
            if (GameScr.instance != null) Char.myCharz().currentMovePoint = null;
        }

        public static void PaintBell(mGraphics g)
        {
            bell = Hud.HudLayout.GetRect(Hud.HudLayout.Bell);
            if (!IsBellVisible || bell.width <= 0) return;
            int x = (int)bell.x, y = (int)bell.y, size = (int)bell.width;
            int centerX = (int)bell.center.x;
            int tx = g.getTranslateX(), ty = g.getTranslateY() - mGraphics.addYWhenOpenKeyBoard;
            g.translate(-tx, -ty);
            try {
                Hud.HudButtonSkin.PaintNativeBackground(g, bell, Hud.TopActionsHud.IsHovered(Hud.HudLayout.Bell));
                g.drawImage(NotificationSkin.BellGlyph, centerX, y + size / 2, 3);
                if (NotificationStore.UnreadCount > 0)
                {
                    int badgeX = System.Math.Min(x + size - 12, centerX + 10);
                    g.drawImage(NotificationSkin.BadgeIcon, badgeX, y + 9, 3);
                    mFont.tahoma_7b_white.drawString(g, NotificationStore.UnreadCount.ToString(),
                        badgeX, y + (18 - mFont.tahoma_7b_white.getHeight()) / 2, 2);
                }
            }
            finally { g.translate(tx, ty); }
        }

        private static void SetScroll(int value)
        {
            int next = Mathf.Clamp(value, 0, Mathf.Max(0, contentHeight - (int)HudLayout.GetRect(Content).height));
            if (next == scroll) return;
            scroll = next; HudLayout.Invalidate();
        }

        private static Rect RowBounds(int index)
        {
            if (index >= rows.Count) return new Rect();
            Rect content = HudLayout.GetRect(Content);
            Row row = rows[index];
            return new Rect(content.x, content.y + row.Top - scroll, content.width, row.Height);
        }

        private static Rect ActionBounds(int index, bool accept)
        {
            if (tab != 2 || index >= rows.Count || rows[index].Entry.Responding) return new Rect();
            Rect row = HudLayout.GetRect(slots[index].Body), content = HudLayout.GetRect(Content);
            bool stacked = row.width < ActionSize * 2 + Gap;
            Rect action = new Rect(stacked ? row.center.x - ActionSize / 2 : row.xMax - (accept ? ActionSize * 2 + Gap : ActionSize),
                row.y + LineHeight + Gap + (stacked && !accept ? ActionSize + Gap : 0), ActionSize, ActionSize);
            // A partially clipped button is neither drawn nor clickable. It becomes available
            // after scrolling the row into view; no second, larger hitbox is synthesized.
            return action.xMin < content.xMin || action.xMax > content.xMax || action.yMin < content.yMin || action.yMax > content.yMax ? new Rect() : action;
        }

        private static Rect BossAgeBounds(int index)
        {
            if (tab != 0 || index >= rows.Count) return new Rect();
            Rect row = HudLayout.GetRect(slots[index].Body);
            return new Rect(row.xMax - Gap - rows[index].AgeWidth, row.y, rows[index].AgeWidth, LineHeight);
        }

        private static Rect RowTextBounds(int index)
        {
            if (index >= rows.Count) return new Rect();
            Rect row = HudLayout.GetRect(slots[index].Body);
            float right = tab == 0 ? HudLayout.GetRect(slots[index].Age).x - Gap * 2 : row.xMax - Gap;
            // Include a one-unit gutter for the game font's outline on each side.
            return new Rect(row.x + Gap - 1, row.y, Mathf.Max(0, right - row.x - Gap + 1),
                rows[index].ExpandedLines == null ? LineHeight : row.height);
        }

        private static void PrepareRows()
        {
            Rect content = HudLayout.GetRect(Content);
            long time = tab == 1 ? 0 : NotificationStore.ServerNow / 1000;
            if (cachedRevision != NotificationStore.Revision || cachedWidth != (int)content.width || cachedTab != tab || cachedTime != time) {
                cachedRevision = NotificationStore.Revision; cachedWidth = (int)content.width; cachedTab = tab; cachedTime = time;
                rows.Clear(); contentHeight = 0;
                if (tab == 1) {
                    var liveIds = new HashSet<long>();
                    foreach (NotificationEntry entry in Entries) liveIds.Add(entry.Id);
                    expandedServer.RemoveWhere(id => !liveIds.Contains(id));
                }
                foreach (NotificationEntry entry in Entries) {
                    if (tab == 0 && rows.Count == NotificationStore.BossHistoryLimit) break;
                    string age = tab == 0 ? BossAgeText(entry) : null;
                    int ageWidth = age == null ? 0 : AgeColumnWidth;
                    string text = RowText(entry);
                    string[] expandedLines = tab == 1 && expandedServer.Contains(entry.Id)
                        ? mFont.tahoma_7b_dark.splitFontArray("#" + entry.Id + " " + entry.Text.Replace("\r", ""),
                            Mathf.Max(1, (int)content.width - Gap * 2 - 2)) : null;
                    int textHeight = expandedLines == null ? LineHeight : Mathf.Max(LineHeight, expandedLines.Length * LineHeight);
                    int actionsHeight = content.width < ActionSize * 2 + Gap ? ActionSize * 2 + Gap : ActionSize;
                    var row = new Row { Entry = entry, Text = text, TextWidth = mFont.tahoma_7b_dark.getWidth(text), ExpandedLines = expandedLines, Age = age, AgeWidth = ageWidth, Top = contentHeight,
                        Height = textHeight + (tab == 2 ? actionsHeight + Gap : 0) };
                    rows.Add(row); contentHeight += row.Height + Padding;
                }
                EnsureSlots(); HudLayout.Invalidate();
            }
            SetScroll(scroll);
        }

        private static void EnsureSlots()
        {
            while (slots.Count < rows.Count) {
                int index = slots.Count;
                var slot = new Slot { Body = "notifications.row." + index, Text = "notifications.text." + index, Age = "notifications.age." + index, Yes = "notifications.yes." + index, No = "notifications.no." + index, ServerToggle = "notifications.server-toggle." + index };
                slots.Add(slot);
                System.Func<bool> visible = () => IsOpen && CanUse && index < rows.Count;
                HudLayout.Register(slot.Body, () => RowBounds(index), ModalOrder + 3, visible, false);
                HudLayout.Register(slot.Age, () => BossAgeBounds(index), ModalOrder + 3, visible, false);
                HudLayout.Register(slot.Text, () => RowTextBounds(index), ModalOrder + 3, visible, false);
                // Only the actually painted row portion inside content can capture a click.
                HudLayout.Register(slot.ServerToggle, () => Intersection(HudLayout.GetRect(slot.Body), HudLayout.GetRect(Content)),
                    ModalOrder + 3, () => visible() && tab == 1);
                HudLayout.Register(slot.Yes, () => ActionBounds(index, true), ModalOrder + 4, visible);
                HudLayout.Register(slot.No, () => ActionBounds(index, false), ModalOrder + 4, visible);
            }
        }

        private static bool Hovered(string name) => captured && !moved && pressed == name
            || !HudLayout.IsMobileLayout && HudLayout.IsTopmost(name, new Vector2(GameCanvas.pxMouse, GameCanvas.pyMouse));

        private static void Clip(mGraphics g, Rect bounds) =>
            g.setClip((int)bounds.x, (int)bounds.y, (int)bounds.width, (int)bounds.height);

        private static void PaintRowText(mGraphics g, int index, Rect content, Rect screen)
        {
            Rect text = HudLayout.GetRect(slots[index].Text);
            if (text.width <= 2) return;
            Row row = rows[index];
            Clip(g, Intersection(text, Intersection(content, screen)));
            if (row.ExpandedLines != null) {
                int y = (int)text.y;
                foreach (string line in row.ExpandedLines) {
                    mFont.tahoma_7b_dark.drawString(g, line, (int)text.x + 1, y, 0); y += LineHeight;
                }
                return;
            }
            int available = (int)text.width - 2, offset = 0;
            if (row.TextWidth > available) {
                const int speed = 24;
                int distance = row.TextWidth + Gap * 6;
                // Use elapsed time, not paint calls or the rebuilt per-second row cache.
                // Repaint rate and changing age/countdown cannot restart or speed up motion.
                long elapsed = System.Math.Max(0, AnimationNow - marqueeStartedAt);
                offset = -(int)(elapsed * speed / 1000 % distance);
                // A following copy enters from the right while the first exits on the left;
                // no pause or empty-viewport phase at the loop boundary.
                mFont.tahoma_7b_dark.drawString(g, row.Text, (int)text.x + 1 + offset + distance, (int)text.y, 0);
            }
            mFont.tahoma_7b_dark.drawString(g, row.Text, (int)text.x + 1 + offset, (int)text.y, 0);
        }

        public static void Paint(mGraphics g)
        {
            if (!IsOpen || !CanUse) return;
            PrepareRows();
            Rect screen = HudLayout.GetRect(Dim), panel = HudLayout.GetRect(Panel), close = HudLayout.GetRect(CloseButton);
            Rect content = HudLayout.GetRect(Content);
            int tx = g.getTranslateX(), ty = g.getTranslateY() - mGraphics.addYWhenOpenKeyBoard;
            g.translate(-tx, -ty); Clip(g, screen);
            try {
                g.setColor(0, 0.55f); g.fillRect((int)screen.x, (int)screen.y, (int)screen.width, (int)screen.height);
                NotificationSkin.PaintFrame(g, (int)panel.x, (int)panel.y, (int)panel.width, (int)panel.height, false);
                // Intersect clips with ScreenBounds during the slide, so text/buttons never
                // render in the adjacent safe-area margin outside the game's viewport.
                Clip(g, Intersection(panel, screen));
                if (Hovered(CloseButton)) { g.setColor(12052656); g.fillRect((int)close.x, (int)close.y, (int)close.width, (int)close.height); }
                g.drawImage(NotificationSkin.CloseIcon, (int)close.center.x, (int)close.center.y, 3);
                // The game font paints an outline at x-1. Leave a gutter before its first
                // glyph instead of starting the clip exactly at the text origin.
                Clip(g, Intersection(new Rect(panel.x + Gap, close.y, Mathf.Max(0, close.x - panel.x - Gap * 2), close.height), screen));
                TitleFont.drawString(g, "Thông báo", (int)content.x + Gap, (int)close.center.y - TitleFont.getHeight() / 2, 0);
                for (int i = 0; i < TabKeys.Length; i++) {
                    Rect bounds = HudLayout.GetRect(TabKeys[i]);
                    Clip(g, Intersection(panel, screen));
                    NotificationSkin.PaintFrame(g, (int)bounds.x, (int)bounds.y, (int)bounds.width, (int)bounds.height, i == tab || Hovered(TabKeys[i]));
                    Clip(g, Intersection(bounds, screen));
                    (i == tab ? mFont.tahoma_7_green2 : mFont.tahoma_7_grey).drawString(g, TabLabels[i],
                        (int)bounds.center.x, (int)bounds.center.y - mFont.tahoma_7_grey.getHeight() / 2, 2);
                }
                Clip(g, Intersection(content, screen));
                if (rows.Count == 0) {
                    string text = NotificationStore.HasSnapshot ? "Chưa có thông báo." : "Đang nhận thông báo...";
                    int y = (int)content.y;
                    foreach (string line in mFont.tahoma_7b_dark.splitFontArray(text, Mathf.Max(1, (int)content.width - Gap * 2))) {
                        mFont.tahoma_7b_dark.drawString(g, line, (int)content.x + Gap, y, 0); y += LineHeight;
                    }
                }
                for (int i = 0; i < rows.Count; i++) {
                    Rect bounds = HudLayout.GetRect(slots[i].Body);
                    if (bounds.yMax < content.yMin || bounds.yMin > content.yMax) continue;
                    int y = (int)bounds.y + LineHeight;
                    Rect age = HudLayout.GetRect(slots[i].Age);
                    PaintRowText(g, i, content, screen);
                    if (tab == 0) {
                        Clip(g, Intersection(age, Intersection(content, screen)));
                        mFont.tahoma_7b_dark.drawString(g, rows[i].Age, (int)age.xMax - 1, (int)age.y, 1);
                    }
                    Clip(g, Intersection(content, screen));
                    if (tab == 2) {
                        if (rows[i].Entry.Responding) mFont.tahoma_7_grey.drawString(g, "Đang trả lời...", (int)bounds.x + Gap, y + Gap, 0);
                        else {
                            Rect yes = HudLayout.GetRect(slots[i].Yes), no = HudLayout.GetRect(slots[i].No);
                            if (yes.width > 0) PopUpYesNo.PaintAction(g, yes, true);
                            if (no.width > 0) PopUpYesNo.PaintAction(g, no, false);
                        }
                    }
                    g.setColor(13524492); g.fillRect((int)bounds.x + Gap, (int)bounds.yMax + Gap, (int)bounds.width - Gap * 2, 1);
                }
            }
            finally { Clip(g, screen); g.translate(tx, ty); }
        }

        private static Rect Intersection(Rect a, Rect b) => Rect.MinMaxRect(
            Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin),
            Mathf.Max(Mathf.Max(a.xMin, b.xMin), Mathf.Min(a.xMax, b.xMax)),
            Mathf.Max(Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.yMax, b.yMax)));
    }
}
