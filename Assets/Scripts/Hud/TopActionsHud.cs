using UnityEngine;

namespace Hud
{
    internal static class TopActionsHud
    {
        internal const string Hub = "top-actions.hub";
        internal const string MapListSwitch = "top-actions.map-list-switch";
        internal const int ButtonGap = 4;
        private const int SlotSize = 34, HubOrder = HudLayout.ChatOrder, SwitchSlot = 3;
        private static string pressed;
        private static bool captured, keepMovement;
        private static Image mapItemsGlyph, mapCombatGlyph;
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
            HudLayout.Register(HudLayout.ChatButton, () => ButtonBounds(0), HudLayout.ChatOrder, () => GameScr.IsChatButtonVisible);
            HudLayout.Register(HudLayout.Bell, () => ButtonBounds(1), HudLayout.BellOrder, () => Notifications.NotificationCenter.IsBellVisible);
            HudLayout.Register(Hub, () => ButtonBounds(2), HubOrder, () => IsRowVisible);
            HudLayout.Register(MapListSwitch, () => ButtonBounds(SwitchSlot), HubOrder, () => IsRowVisible);
            ResourceRequest items = Resources.LoadAsync<Texture2D>("res/map-list-items");
            ResourceRequest combat = Resources.LoadAsync<Texture2D>("res/map-list-combat");
            items.completed += _ => mapItemsGlyph = CreateMapListGlyph(items.asset as Texture2D);
            combat.completed += _ => mapCombatGlyph = CreateMapListGlyph(combat.asset as Texture2D);
        }

        private static Rect ButtonBounds(int slot)
        {
            Image skin = GameScr.imgChatPC;
            if (skin == null) return new Rect();

            bool compact = GameCanvas.w <= 240;
            int width = skin.getWidth(), height = skin.getHeight();
            int right = (compact ? 1 : 6) + (SlotSize - width + 1) / 2;
            int top = (compact ? 5 : 2) + (SlotSize - height + 1) / 2 + mGraphics.addYWhenOpenKeyBoard;
            // Keep the native button size and existing slot spacing, tightening only on tiny viewports.
            int stride = Mathf.Clamp((GameCanvas.w - right - width) / SwitchSlot, width, SlotSize + ButtonGap);
            return HudLayout.Anchor(HorizontalAnchor.Right, VerticalAnchor.Top,
                new Vector2(width, height), new Vector2(right + slot * stride, top));
        }

        private static Image CreateMapListGlyph(Texture2D source)
        {
            if (source == null) return null;
            // The supplied icon's alpha defines its silhouette on the shared button skin.
            int size = SlotSize / 2 * mGraphics.zoomLevel;
            return HudButtonSkin.CopyNative(source, (pixels, _, __) => {
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(255, 255, 255, pixels[i].a);
            }, size);
        }

        // Runs after invitation capture, before notification/forms and gameplay pointer dispatch.
        internal static void UpdateInput()
        {
            consumedTick = -1;
            if (!CanInput) { pressed = null; captured = false; return; }
            string hit = HudLayout.HitTest(new Vector2(GameCanvas.px, GameCanvas.py));
            bool action = hit == Hub || hit == MapListSwitch || hit == HudLayout.ChatButton || hit == HudLayout.Bell;
            if (GameCanvas.isPointerJustDown) {
                captured = action;
                pressed = action ? hit : null;
                keepMovement = hit == MapListSwitch || (hit == Hub && MapNavigation.Active);
            }
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
                else if (activate == MapListSwitch) {
                    MapListsHud.ToggleMode();
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
            if (!keepMovement) {
                MapNavigation.Cancel();
                Char.myCharz().currentMovePoint = null;
            }
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
                    bool highlighted = hover == name || pressed == name;
                    if (name == HudLayout.ChatButton) {
                        Image skin = highlighted ? GameScr.imgChatsPC2 : GameScr.imgChatPC;
                        g.drawImage(skin, (int)rect.x, (int)rect.y);
                    }
                    else if (name == MapListSwitch || name == Hub) {
                        HudButtonSkin.PaintNativeBackground(g, rect, highlighted);
                        if (name == MapListSwitch) {
                            Image glyph = MapListsHud.ShowsCombatLists ? mapCombatGlyph : mapItemsGlyph;
                            g.drawImage(glyph, rect.center.x, rect.center.y, mGraphics.HCENTER | mGraphics.VCENTER);
                            return;
                        }
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
