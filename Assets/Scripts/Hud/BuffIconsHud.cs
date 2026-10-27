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
        private static readonly List<BuffIconEntry> visible = new List<BuffIconEntry>(BuffIconStore.MaxVisible);
        private static BuffIconGrid grid;
        private static int selected, pressed, consumedTick = -1;
        private static bool captured;
        private static string[] effectLines = new string[0];
        internal static bool ConsumesInput => consumedTick == GameCanvas.gameTick;
        private static bool Visible => GameScr.instance != null && GameCanvas.currentScreen == GameScr.instance
            && GameScr.isPaint && !GameScr.isPaintOther && GameScr.isPaintRada == 1 && !GameCanvas.isLoading
            && !Char.ischangingMap && !GameCanvas.panel.isShow && !GameCanvas.menu.showMenu
            && GameCanvas.currentDialog == null && !ChatTextField.gI().isShow && ChatPopup.currChatPopup == null
            && ChatPopup.serverChatPopUp == null && !Notifications.NotificationCenter.IsOpen
            && !Mod.CuongLe.DoHoa.isShowMenuVIP && !GameScr.instance.isPaintPopup();

        internal static void Reset()
        {
            selected = pressed = 0; captured = false; consumedTick = -1;
        }
        internal static void Register()
        {
            HudLayout.Register(Root, Resolve, Order, () => Visible, false);
            for (int i = 0; i < slots.Length; i++) {
                int index = i; slots[i] = "buff-icons.slot." + i;
                HudLayout.Register(slots[i], () => { HudLayout.GetRect(Root); return grid.Cell(index); },
                    Order, () => Visible);
            }
            HudLayout.Register(Tooltip, TooltipBounds, HudLayout.MenuOrder + 11, () => Visible && Selection() != null);
        }
        internal static Rect StackBounds => Visible ? HudLayout.GetRect(Root) : new Rect();
        private static Rect Resolve()
        {
            BuffIconStore.CopyVisible(visible, BuffIconStore.Now);
            HudLayout.TryGetRect("target-info.bounds", out Rect target);
            grid = BuffIconGrid.Resolve(HudLayout.GetRect("top-info.bounds"), HudLayout.GetRect("top-info.hp"),
                target, HudLayout.GetRect(TopActionsHud.Hub), GameCanvas.w, GameCanvas.h, mGraphics.zoomLevel,
                mFont.tahoma_7_white.getHeight(), visible.Count);
            return grid.Bounds;
        }
        private static BuffIconEntry Selection()
        {
            if (selected == 0) return null;
            HudLayout.GetRect(Root);
            for (int i = 0; i < grid.Count; i++) if (visible[i].Id == selected) return visible[i];
            return null;
        }
        private static Rect TooltipBounds()
        {
            BuffIconEntry entry = Selection();
            if (entry == null) return new Rect();
            float width = Mathf.Min(GameCanvas.w - 8, Mathf.Max(HudLayout.PixelsToLogical(280),
                mFont.tahoma_7b_white.getWidth(entry.Name) + 12));
            effectLines = mFont.tahoma_7_white.splitFontArray(entry.Effect, (int)width - 12);
            float height = (Mathf.Min(effectLines.Length, 5) + 2) * mFont.tahoma_7_white.getHeight() + 12;
            return new Rect(Mathf.Clamp(grid.X, 4, GameCanvas.w - width - 4),
                Mathf.Clamp(grid.Bounds.yMax + grid.Gap, 4, Mathf.Max(4, GameCanvas.h - height - 4)), width, height);
        }
        internal static void UpdateInput()
        {
            consumedTick = -1;
            if (!Visible || InfoDlg.isLock) { Reset(); return; }
            if (Selection() == null) selected = 0;
            string hit = HudLayout.HitTest(new Vector2(GameCanvas.px, GameCanvas.py));
            int id = 0;
            for (int i = 0; i < grid.Count; i++) if (hit == slots[i]) { id = visible[i].Id; break; }
            if (GameCanvas.isPointerJustDown) {
                captured = id != 0 || selected != 0;
                pressed = id;
            }
            if (!captured) return;
            if (GameCanvas.isPointerJustRelease) {
                if (GameCanvas.isPointerClick && id == pressed) {
                    if (id != 0) selected = selected == id ? 0 : id;
                    else if (hit != Tooltip) selected = 0;
                }
                captured = false; pressed = 0;
                HudLayout.Invalidate(); Consume(true); return;
            }
            if (GameCanvas.isPointerDown || GameCanvas.isPointerJustDown) Consume(false);
        }
        private static void Consume(bool released)
        {
            consumedTick = GameCanvas.gameTick;
            GameScr.instance.isPointerDowning = false;
            Char.myCharz().currentMovePoint = null;
            if (released) GameCanvas.clearAllPointerEvent();
            else GameCanvas.isPointerJustDown = false;
        }
        private static void PaintCountdown(mGraphics g, Rect cell, string text)
        {
            Rect label = new Rect(cell.x, cell.yMax + 1, cell.width, mFont.tahoma_7_white.getHeight());
            Rect viewport = HudLayout.ScreenBounds;
            float stroke = 1f / Mathf.Max(1, mGraphics.zoomLevel);
            // Fit long charm durations using the same game font; retain every minute digit.
            foreach (Vector2 offset in outline) {
                Rect shadow = label; shadow.x += offset.x * stroke; shadow.y += offset.y * stroke;
                InventoryItemVisuals.PaintCompactText(g, mFont.tahoma_7_white, text, shadow, viewport, 0x201000, true);
            }
            InventoryItemVisuals.PaintCompactText(g, mFont.tahoma_7_white, text, label, viewport, 0xffffff, true);
        }
        private static readonly Vector2[] outline = { new Vector2(-1,0), new Vector2(1,0), new Vector2(0,-1), new Vector2(0,1) };
        private static void PaintFrame(mGraphics g, Rect cell, int border)
        {
            // Scale the original corner artwork only when the 30px cell is smaller than
            // the native 14x12 logical corner pair (Android zoom 3/4). Never overlap caps.
            int x = (int)cell.x, y = (int)cell.y, w = (int)cell.width, h = (int)cell.height;
            float scale = Mathf.Min(1, cell.width / 14, cell.height / 12);
            int cw = Mathf.FloorToInt(7 * scale), ch = Mathf.FloorToInt(6 * scale);
            g.setColor(border); g.fillRect(x + cw, y, w - 2 * cw, h); g.fillRect(x, y + ch, w, h - 2 * ch);
            g.setColor(InventoryFrame.SlotBackground);
            g.fillRect(x + cw, y + 1, w - 2 * cw, h - 2); g.fillRect(x + 1, y + ch, w - 2, h - 2 * ch);
            Image corner = InventoryFrameSkin.Corners(InventoryFrame.SlotBackground, border, 255);
            int zoom = Mathf.Max(1, mGraphics.zoomLevel); Color previous = GUI.color;
            try {
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
                for (int i = 0; i < grid.Count; i++) {
                    BuffIconEntry entry = visible[i]; Rect cell = HudLayout.GetRect(slots[i]);
                    // Native slot colors/corners; the timed fusion alone has a gold outline.
                    PaintFrame(g, cell, entry.Id == 1 ? 0xffd34e : InventoryFrame.Border);
                    // Kilis has no x1 asset in the existing pack; use the available Chibi icon there.
                    int icon = entry.Icon == 15359 && mGraphics.zoomLevel == 1 ? 433 : entry.Icon;
                    InventoryItemVisuals.PaintCompactIcon(g, icon, InventoryFrame.Inset(cell, 1), cell);
                    g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
                    PaintCountdown(g, cell, BuffIconStore.FormatTime(BuffIconStore.Remaining(entry, BuffIconStore.Now)));
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
                    InventoryFrame.Rounded(g, tooltip, InventoryFrame.PanelBackground, 255, InventoryFrame.Border);
                    int x = (int)tooltip.x + 6, y = (int)tooltip.y + 6, line = mFont.tahoma_7_white.getHeight();
                    mFont.tahoma_7b_dark.drawString(g, focus.Name, x, y, mFont.LEFT); y += line;
                    for (int i = 0; i < effectLines.Length && i < 5; i++, y += line)
                        mFont.tahoma_7.drawString(g, effectLines[i], x, y, mFont.LEFT);
                    mFont.tahoma_7.drawString(g, "Còn " + BuffIconStore.FormatTime(BuffIconStore.Remaining(focus, BuffIconStore.Now)), x, y, mFont.LEFT);
                }
            } finally {
                g.setClip(0, 0, GameCanvas.w, GameCanvas.h); g.translate(tx, ty);
            }
        }
    }
}
