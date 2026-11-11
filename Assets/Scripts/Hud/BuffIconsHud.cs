using System.Collections.Generic;
using UnityEngine;

namespace Hud
{
    internal static class BuffIconsHud
    {
        internal const string Root = "buff-icons.bounds";
        private const string Tooltip = "buff-icons.tooltip";
        private const int Order = 15;
        private static readonly string[] slots = new string[BuffIconStore.MaxVisible];
        private static readonly List<BuffIconEntry> visible = new List<BuffIconEntry>(BuffIconStore.MaxEntries);
        private static BuffIconGrid grid;
        private static long checkedFrame = -1, checkedState = -1, storeRevision = -1, nextExpiry, layoutVersion;
        private static Rect infoRect, hpRect, targetRect, actionsRect;
        private static int layoutWidth, layoutHeight, layoutZoom, layoutCount = -1;
        private static bool layoutMobile;
        private static mFont layoutFont;
        private static Font nativeFont;
        private static int nativeSize, countdownDigits = 2;
        private static readonly BuffCountdownText countdown = new BuffCountdownText();
        private static Image normalCorner, goldCorner, tooltipCorner;
        private static Texture2D cornerSource;
        private static BuffIconEntry tooltipEntry;
        private static long tooltipLayout = -1, tooltipVersion;
        private static Rect tooltipBounds;
        private static int selected, pressed, consumedTick = -1;
        private static bool captured, moved, scrollPress;
        private static int first, dragFirst;
        private static float pressX;
        private static string[] effectLines = new string[0];
        private static readonly TooltipLine[] tooltipLines = {
            new TooltipLine(), new TooltipLine(), new TooltipLine(), new TooltipLine(),
            new TooltipLine(), new TooltipLine(), new TooltipLine()
        };
        private sealed class TooltipLine
        {
            private readonly GUIContent content = new GUIContent();
            private GUIStyle style;
            internal void Paint(mGraphics g, mFont font, string text, int x, int y)
            {
                if (font.myFont == null) { font.drawString(g, text, x, y, mFont.LEFT); return; }
                if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft };
                style.font = font.myFont; style.normal.textColor = font.color1;
                if (content.text != text) content.text = text;
                int offset = (font.id > 0 && font.id < 10) || font.id == 19 ? 1 : font.id >= 10 && font.id <= 18 ? 2 : 0;
                int zoom = Mathf.Max(1, mGraphics.zoomLevel);
                GUI.Label(new Rect(x * zoom, (y + mFont.yAddFont - offset) * zoom, GameCanvas.w * zoom, 100), content, style);
            }
        }
        internal static bool ConsumesInput => consumedTick == GameCanvas.gameTick;
        private static bool Visible => GameScr.instance != null && GameCanvas.currentScreen == GameScr.instance
            && GameScr.isPaint && !GameScr.isPaintOther && GameScr.isPaintRada == 1 && !GameCanvas.isLoading
            && !Char.ischangingMap && !GameCanvas.panel.isShow && !GameCanvas.menu.showMenu
            && GameCanvas.currentDialog == null && !ChatTextField.gI().isShow && ChatPopup.currChatPopup == null
            && ChatPopup.serverChatPopUp == null && !Notifications.NotificationCenter.IsOpen
            && !Mod.CuongLe.DoHoa.isShowMenuVIP && !GameScr.instance.isPaintPopup();

        internal static void Reset()
        {
            selected = pressed = first = 0; captured = moved = scrollPress = false; consumedTick = -1;
            checkedFrame = -1;
        }
        internal static void Register()
        {
            HudLayout.Register(Root, () => grid.Bounds, Order, () => Visible, cacheVersion: () => LayoutVersion);
            for (int i = 0; i < slots.Length; i++) {
                int index = i; slots[i] = "buff-icons.slot." + i;
                HudLayout.Register(slots[i], () => grid.Cell(index), Order, () => Visible, cacheVersion: () => LayoutVersion);
            }
            HudLayout.Register(Tooltip, () => tooltipBounds, HudLayout.MenuOrder + 11, () => Visible && Selection() != null,
                cacheVersion: () => TooltipVersion);
        }
        internal static Rect StackBounds => Visible ? HudLayout.GetRect(Root) : new Rect();
        private static long LayoutVersion { get { EnsureLayout(); return layoutVersion; } }
        private static bool Same(Rect a, Rect b) => a.x == b.x && a.y == b.y && a.width == b.width && a.height == b.height;
        private static void EnsureLayout()
        {
            long now = BuffIconStore.Now, frame = HudLayout.FrameId, state = HudLayout.StateGeneration;
            bool mobile = HudLayout.IsMobileLayout;
            mFont font = mobile ? mFont.tahoma_7b_white : mFont.tahoma_7_white;
            int size = font.myFont == null ? 0 : font.myFont.fontSize;
            bool fontChanged = layoutFont != font || nativeFont != font.myFont || nativeSize != size || countdown.Dirty;
            bool viewportChanged = layoutWidth != GameCanvas.w || layoutHeight != GameCanvas.h
                || layoutZoom != mGraphics.zoomLevel || layoutMobile != mobile;
            bool entriesChanged = storeRevision != BuffIconStore.Revision || now >= nextExpiry;
            if (!entriesChanged && !fontChanged && !viewportChanged && checkedFrame == frame && checkedState == state) return;
            checkedFrame = frame; checkedState = state;
            if (entriesChanged) {
                nextExpiry = BuffIconStore.CopyVisible(visible, now); storeRevision = BuffIconStore.Revision;
                countdownDigits = 2;
                for (int i = 0; i < visible.Count; i++) countdownDigits = Mathf.Max(countdownDigits, visible[i].Countdown(now).Length - 1);
            }
            HudLayout.TryGetRect("target-info.bounds", out Rect target);
            Rect info = HudLayout.GetRect("top-info.bounds"), hp = HudLayout.GetRect("top-info.hp"), actions = HudLayout.GetRect(TopActionsHud.Hub);
            bool geometryChanged = viewportChanged || fontChanged || layoutCount != visible.Count || !Same(infoRect, info) || !Same(hpRect, hp)
                || !Same(targetRect, target) || !Same(actionsRect, actions);
            if (geometryChanged) {
                infoRect = info; hpRect = hp; targetRect = target; actionsRect = actions;
                layoutWidth = GameCanvas.w; layoutHeight = GameCanvas.h; layoutZoom = mGraphics.zoomLevel; layoutMobile = mobile;
                layoutFont = font; nativeFont = font.myFont; nativeSize = size; layoutCount = visible.Count;
                grid = BuffIconGrid.Resolve(info, hp, target, actions, layoutWidth, layoutHeight, layoutZoom,
                    font.getHeight(), visible.Count, mobile);
            }
            if (geometryChanged || entriesChanged) {
                float oldHeight = grid.CaptionHeight;
                countdown.Configure(font, mobile ? grid.CaptionFontPixels : Mathf.Max(1, size > 0 ? size : font.getHeight()),
                    grid.Size, countdownDigits, layoutZoom);
                grid.FitCaption(countdown.Height, visible.Count, layoutHeight);
                if (geometryChanged || oldHeight != grid.CaptionHeight) layoutVersion++;
            }
            first = Mathf.Clamp(first, 0, Mathf.Max(0, visible.Count - grid.Count));
        }
        private static BuffIconEntry Selection()
        {
            if (selected == 0) return null;
            EnsureLayout();
            for (int i = 0; i < grid.Count; i++) if (visible[first + i].Id == selected) return visible[first + i];
            return null;
        }
        private static long TooltipVersion
        {
            get {
                BuffIconEntry entry = Selection();
                if (tooltipEntry != entry || tooltipLayout != layoutVersion) {
                    tooltipEntry = entry; tooltipLayout = layoutVersion; tooltipVersion++;
                    tooltipBounds = new Rect();
                    if (entry != null) {
                        float width = Mathf.Min(GameCanvas.w - 8, Mathf.Max(HudLayout.PixelsToLogical(280),
                            mFont.tahoma_7b_white.getWidth(entry.Name) + 12));
                        effectLines = mFont.tahoma_7_white.splitFontArray(entry.Effect, (int)width - 12);
                        float height = (Mathf.Min(effectLines.Length, 5) + 2) * mFont.tahoma_7_white.getHeight() + 12;
                        tooltipBounds = new Rect(Mathf.Clamp(grid.X, 4, GameCanvas.w - width - 4),
                            Mathf.Clamp(grid.Bounds.yMax + grid.Gap, 4, Mathf.Max(4, GameCanvas.h - height - 4)), width, height);
                    }
                }
                return tooltipVersion;
            }
        }
        internal static void UpdateInput()
        {
            consumedTick = -1;
            if (!Visible || InfoDlg.isLock) { Reset(); return; }
            EnsureLayout();
            if (Selection() == null) selected = 0;
            if (!captured && !GameCanvas.isPointerJustDown && !GameCanvas.isPointerJustRelease && GameCanvas.pXYScrollMouse == 0) return;
            string hit = HudLayout.HitTest(new Vector2(GameCanvas.px, GameCanvas.py));
            int id = 0;
            for (int i = 0; i < grid.Count; i++) if (hit == slots[i]) { id = visible[first + i].Id; break; }
            if (GameCanvas.isPointerJustDown) {
                captured = id != 0 || hit == Root || selected != 0;
                pressed = id; scrollPress = id != 0 || hit == Root; moved = false; pressX = GameCanvas.px; dragFirst = first;
            }
            if (captured && scrollPress && Mathf.Abs(GameCanvas.px - pressX) >= 4) {
                moved = true;
                SetFirst(dragFirst + Mathf.RoundToInt((pressX - GameCanvas.px) / (grid.Size + grid.Gap)));
            }
            int wheel = GameCanvas.pXYScrollMouse;
            if (wheel != 0 && grid.Bounds.Contains(new Vector2(GameCanvas.pxMouse, GameCanvas.pyMouse))) {
                SetFirst(first - wheel); GameCanvas.pXYScrollMouse = 0; Consume(false);
            }
            if (!captured) return;
            if (GameCanvas.isPointerJustRelease) {
                if (!moved && GameCanvas.isPointerClick && id == pressed) {
                    if (id != 0) selected = selected == id ? 0 : id;
                    else if (hit != Tooltip) selected = 0;
                }
                captured = false; pressed = 0;
                Consume(true); return;
            }
            if (GameCanvas.isPointerDown || GameCanvas.isPointerJustDown) Consume(false);
        }
        private static void SetFirst(int value)
        {
            int next = Mathf.Clamp(value, 0, Mathf.Max(0, visible.Count - grid.Count));
            if (next == first) return;
            first = next; selected = 0;
        }
        private static void Consume(bool released)
        {
            consumedTick = GameCanvas.gameTick;
            GameScr.instance.isPointerDowning = false;
            Char.myCharz().currentMovePoint = null;
            if (released) GameCanvas.clearAllPointerEvent();
            else GameCanvas.isPointerJustDown = false;
        }
        private static void EnsureFrames()
        {
            if (PopUp.goc == null) PopUp.loadBg();
            if (cornerSource == PopUp.goc.texture && normalCorner?.texture != null && goldCorner?.texture != null && tooltipCorner?.texture != null) return;
            normalCorner = InventoryFrameSkin.Corners(InventoryFrame.SlotBackground, InventoryFrame.Border, 255);
            goldCorner = InventoryFrameSkin.Corners(InventoryFrame.SlotBackground, 0xffd34e, 255);
            tooltipCorner = InventoryFrameSkin.Corners(InventoryFrame.PanelBackground, InventoryFrame.Border, 255);
            cornerSource = PopUp.goc.texture;
        }
        private static Color Ink(int rgb) => new Color((rgb >> 16 & 255) / 255f, (rgb >> 8 & 255) / 255f, (rgb & 255) / 255f, 1);
        private static void PaintFrame(Rect cell, int fill, int border, Image corner)
        {
            // Scale the original corner artwork only when the cell is smaller than
            // the native 14x12 logical corner pair (Android zoom 3/4). Never overlap caps.
            int x = (int)cell.x, y = (int)cell.y, w = (int)cell.width, h = (int)cell.height;
            float scale = Mathf.Min(1, Mathf.Min(cell.width / 14, cell.height / 12));
            int cw = Mathf.FloorToInt(7 * scale), ch = Mathf.FloorToInt(6 * scale);
            int zoom = Mathf.Max(1, mGraphics.zoomLevel); Color previous = GUI.color;
            try {
                // mGraphics.fillRect builds a string cache key for every fill. These
                // four fixed-color strips can use the shared white texture directly.
                GUI.color = Ink(border);
                GUI.DrawTexture(new Rect((x + cw) * zoom, y * zoom, (w - 2 * cw) * zoom, h * zoom), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x * zoom, (y + ch) * zoom, w * zoom, (h - 2 * ch) * zoom), Texture2D.whiteTexture);
                GUI.color = Ink(fill);
                GUI.DrawTexture(new Rect((x + cw) * zoom, (y + 1) * zoom, (w - 2 * cw) * zoom, (h - 2) * zoom), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect((x + 1) * zoom, (y + ch) * zoom, (w - 2) * zoom, (h - 2 * ch) * zoom), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.DrawTextureWithTexCoords(new Rect(x * zoom, y * zoom, cw * zoom, ch * zoom), corner.texture, new Rect(0, 0, 1, 1));
                GUI.DrawTextureWithTexCoords(new Rect((x + w - cw) * zoom, y * zoom, cw * zoom, ch * zoom), corner.texture, new Rect(1, 0, -1, 1));
                GUI.DrawTextureWithTexCoords(new Rect(x * zoom, (y + h - ch) * zoom, cw * zoom, ch * zoom), corner.texture, new Rect(0, 1, 1, -1));
                GUI.DrawTextureWithTexCoords(new Rect((x + w - cw) * zoom, (y + h - ch) * zoom, cw * zoom, ch * zoom), corner.texture, new Rect(1, 1, -1, -1));
            } finally { GUI.color = previous; }
        }
        internal static void Paint(mGraphics g)
        {
            if (!Visible) return;
            int tx = g.getTranslateX(), ty = g.getTranslateY() - mGraphics.addYWhenOpenKeyBoard;
            g.translate(-tx, -ty);
            try {
                HudLayout.GetRect(Root);
                if (grid.Count == 0) return;
                EnsureFrames();
                long now = BuffIconStore.Now;
                for (int i = 0; i < grid.Count; i++) {
                    BuffIconEntry entry = visible[first + i]; Rect cell = grid.Cell(i);
                    // Native slot colors/corners; the timed fusion alone has a gold outline.
                    PaintFrame(cell, InventoryFrame.SlotBackground, entry.Id == 1 ? 0xffd34e : InventoryFrame.Border,
                        entry.Id == 1 ? goldCorner : normalCorner);
                    // Kilis has no x1 asset in the existing pack; use the available Chibi icon there.
                    int icon = entry.Icon == 15359 && mGraphics.zoomLevel == 1 ? 433 : entry.Icon;
                    InventoryItemVisuals.PaintCompactIcon(g, icon, InventoryFrame.Inset(cell, 1), cell);
                    countdown.Paint(cell, entry.Countdown(now), grid.CaptionGap);
                }
            } finally {
                g.setClip(0, 0, GameCanvas.w, GameCanvas.h); g.translate(tx, ty);
            }
        }
        // Tooltips overlay neighboring list rows; paint/hit order both stay above those rows.
        internal static void PaintTooltip(mGraphics g)
        {
            if (!Visible) return;
            int tx = g.getTranslateX(), ty = g.getTranslateY() - mGraphics.addYWhenOpenKeyBoard;
            g.translate(-tx, -ty);
            try {
                BuffIconEntry focus = Selection();
                if (focus != null) {
                    Rect tooltip = HudLayout.GetRect(Tooltip);
                    EnsureFrames();
                    PaintFrame(tooltip, InventoryFrame.PanelBackground, InventoryFrame.Border, tooltipCorner);
                    int x = (int)tooltip.x + 6, y = (int)tooltip.y + 6, line = mFont.tahoma_7_white.getHeight();
                    tooltipLines[0].Paint(g, mFont.tahoma_7b_dark, focus.Name, x, y); y += line;
                    for (int i = 0; i < effectLines.Length && i < 5; i++, y += line)
                        tooltipLines[i + 1].Paint(g, mFont.tahoma_7, effectLines[i], x, y);
                    tooltipLines[6].Paint(g, mFont.tahoma_7, focus.TooltipCountdown(BuffIconStore.Now), x, y);
                }
            } finally {
                g.setClip(0, 0, GameCanvas.w, GameCanvas.h); g.translate(tx, ty);
            }
        }
    }
}
