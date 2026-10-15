using UnityEngine;

namespace Hud
{
    internal static class TopActionsHud
    {
        internal const string Hub = "top-actions.hub";
        internal const int Size = 34, ButtonGap = 4, HubOrder = HudLayout.ChatOrder;
        private static string pressed;
        private static bool captured;
        private static int consumedTick = -1;
        internal static bool ConsumesInput => consumedTick == GameCanvas.gameTick;
        internal static bool IsRowVisible => GameScr.instance != null && GameCanvas.currentScreen == GameScr.instance
            && GameScr.isPaint && !GameScr.isPaintOther && !GameCanvas.isLoading && !Char.ischangingMap;
        private static bool CanInput => IsRowVisible && GameCanvas.currentDialog == null && !GameCanvas.panel.isShow
            && !GameCanvas.menu.showMenu && !ChatTextField.gI().isShow && ChatPopup.currChatPopup == null
            && ChatPopup.serverChatPopUp == null && !Notifications.NotificationCenter.ConsumesInput
            && !Mod.CuongLe.DoHoa.isShowMenuVIP
            && !GameScr.instance.isPaintPopup() && !InfoDlg.isLock;

        internal static bool IsHovered(string name) => CanInput
            && (pressed == name || !HudLayout.IsMobileLayout
                && HudLayout.IsTopmost(name, new Vector2(GameCanvas.pxMouse, GameCanvas.pyMouse)));

        internal static void Register()
        {
            // Both platform layouts retain the same 34 logical units and original R margins.
            // No legacy isPC/isTouch flag is used to derive geometry.
            HudLayout.Register(HudLayout.ChatButton, ChatBounds, HudLayout.ChatOrder, () => GameScr.IsChatButtonVisible);
            HudLayout.Register(HudLayout.Bell, BellBounds, HudLayout.BellOrder, () => Notifications.NotificationCenter.IsBellVisible);
            HudLayout.Register(Hub, HubBounds, HubOrder, () => IsRowVisible);
        }

        private static Rect ChatBounds() => HudLayout.Anchor(HorizontalAnchor.Right, VerticalAnchor.Top,
            new Vector2(Size, Size), new Vector2(GameCanvas.w <= 240 ? 1 : 6,
                (GameCanvas.w <= 240 ? 5 : 2) + mGraphics.addYWhenOpenKeyBoard));
        private static Rect BellBounds()
        {
            Rect chat = HudLayout.GetRect(HudLayout.ChatButton);
            return new Rect(chat.x - Size - ButtonGap, chat.y, Size, Size);
        }
        private static Rect HubBounds()
        {
            Rect bell = HudLayout.GetRect(HudLayout.Bell);
            return new Rect(bell.x - Size - ButtonGap, bell.y, Size, Size);
        }

        // Runs after invitation capture, before notification/forms and gameplay pointer dispatch.
        internal static void UpdateInput()
        {
            consumedTick = -1;
            if (!CanInput) { pressed = null; captured = false; return; }
            string hit = HudLayout.HitTest(new Vector2(GameCanvas.px, GameCanvas.py));
            bool action = hit == Hub || hit == HudLayout.ChatButton || hit == HudLayout.Bell;
            if (GameCanvas.isPointerJustDown) { captured = action; pressed = action ? hit : null; }
            if (captured && hit != pressed) pressed = null; // Dragging out cancels activation.
            if (!captured) return;
            if (GameCanvas.isPointerJustRelease) {
                string activate = GameCanvas.isPointerClick && hit == pressed ? pressed : null;
                pressed = null; captured = false;
                Consume(true);
                if (activate == HudLayout.Bell) Notifications.NotificationCenter.TryOpenBell();
                else if (activate == HudLayout.ChatButton) {
                    ChatTextField.gI().startChat(GameScr.instance, string.Empty);
                    SoundMn.gI().buttonClick();
                }
                else if (activate == Hub) {
                    Mod.CuongLe.MainMod.ShowMenu();
                    SoundMn.gI().buttonClick();
                }
                return;
            }
            if (GameCanvas.isPointerDown || GameCanvas.isPointerJustDown) Consume(false);
        }

        private static void Consume(bool released)
        {
            consumedTick = GameCanvas.gameTick;
            GameScr.instance.isPointerDowning = false;
            Char.myCharz().currentMovePoint = null;
            // pointerReleased does not restore isPointerClick: retain it until the release.
            if (released) GameCanvas.clearAllPointerEvent();
            else GameCanvas.isPointerJustDown = false;
        }

        // Bell is painted at its existing early order 10; these controls keep order 40.
        internal static void PaintControls(mGraphics g)
        {
            string hover = CanInput && !HudLayout.IsMobileLayout
                ? HudLayout.HitTest(new Vector2(GameCanvas.pxMouse, GameCanvas.pyMouse)) : null;
            int tx = g.getTranslateX(), ty = g.getTranslateY() - mGraphics.addYWhenOpenKeyBoard;
            g.translate(-tx, -ty);
            try {
                HudLayout.ForEachBackToFront((name, rect) => {
                    if (name == HudLayout.ChatButton) {
                        // R keeps the existing R-labelled game asset on PC and mobile.
                        Image skin = hover == name || pressed == name ? GameScr.imgChatsPC2 : GameScr.imgChatPC;
                        g.drawImage(skin, (int)rect.center.x, (int)rect.center.y, mGraphics.HCENTER | mGraphics.VCENTER);
                    }
                    else if (name == Hub) {
                        HudButtonSkin.PaintNativeBackground(g, rect, hover == name || pressed == name);
                        g.setColor(0xffffff);
                        const int cell = 3, gap = 2, total = cell * 3 + gap * 2;
                        int x = (int)rect.center.x - total / 2, y = (int)rect.center.y - total / 2;
                        for (int row = 0; row < 3; row++)
                            for (int column = 0; column < 3; column++)
                                g.fillRect(x + column * (cell + gap), y + row * (cell + gap), cell, cell);
                    }
                });
            }
            finally { g.translate(tx, ty); }
        }
    }
}
