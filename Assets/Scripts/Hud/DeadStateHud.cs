using UnityEngine;

namespace Hud
{
    internal static class DeadStateHud
    {
        internal const string Button = "dead-state.button", MenuBounds = "dead-state.menu";
        private static MyVector menuItems;

        internal static bool IsDead => HudLayout.IsPlayerDead;

        internal static bool Visible => IsDead && GameScr.instance != null
            && GameCanvas.currentScreen == GameScr.instance && GameScr.isPaint && !GameScr.isPaintOther
            && GameScr.isPaintRada == 1 && !GameCanvas.panel.isShow && GameCanvas.currentDialog == null
            && !GameCanvas.menu.showMenu && !ChatTextField.gI().isShow
            && ChatPopup.currChatPopup == null && ChatPopup.serverChatPopUp == null
            && !Notifications.NotificationCenter.IsOpen && !Mod.CuongLe.DoHoa.isShowMenuVIP;

        internal static void Register()
        {
            HudLayout.Register(Button, ButtonBounds, HudLayout.ControlsOrder, () => Visible);
            // The existing Menu owns modal input; this entry provides its shared geometry.
            HudLayout.Register(MenuBounds, () => HudLayout.Anchor(HorizontalAnchor.Center, VerticalAnchor.Center,
                new Vector2(Mathf.Min(GameCanvas.w - 2, GameCanvas.menu.menuW * 3), GameCanvas.menu.menuH), Vector2.zero),
                HudLayout.OverlayOrder, () => false, false);
        }

        private static Rect ButtonBounds()
        {
            // Reuse the hidden skill row's provider, so the action stays above that row
            // on either platform without changing the living combat layout.
            Rect row = HudLayout.GetRect(HudLayout.IsMobileLayout
                ? CombatMobileHud.SkillName(4) : CombatPcHud.SkillName(0));
            Rect button = HudLayout.Anchor(HorizontalAnchor.Center, VerticalAnchor.Bottom,
                new Vector2(76, 24), Vector2.zero); // Native Command.paint skin size.
            button.y = Mathf.Max(0, Mathf.Min(row.yMin - button.height - 8, GameCanvas.h - button.height - 24));
            return button;
        }

        internal static void SyncCommand()
        {
            GameScr screen = GameScr.instance;
            if (screen == null) return;
            if (!Visible) {
                if (screen.cmdDead != null && screen.center == screen.cmdDead) screen.center = null;
                return;
            }
            if (screen.cmdDead == null) screen.cmdDead = new Command(mResources.DIES[0], 11038);
            Rect bounds = HudLayout.GetRect(Button);
            screen.cmdDead.x = (int)bounds.x; screen.cmdDead.y = (int)bounds.y;
            screen.cmdDead.w = (int)bounds.width; screen.cmdDead.h = (int)bounds.height;
            screen.cmdDead.isFocus = GameCanvas.isPointerDown
                && HudLayout.IsTopmost(Button, new Vector2(GameCanvas.px, GameCanvas.py));
            screen.center = screen.cmdDead;
        }

        internal static bool IsButtonPressed() => Visible && GameCanvas.isPointerClick
            && GameCanvas.isPointerJustRelease
            && HudLayout.IsTopmost(Button, new Vector2(GameCanvas.px, GameCanvas.py));

        internal static void OpenMenu(MyVector choices)
        {
            if (!IsDead || GameCanvas.menu.showMenu) return;
            GameCanvas.menu.startAt(choices, 0);
            menuItems = choices;
            HudLayout.Invalidate();
            SyncMenu(GameCanvas.menu, true);
            SyncCommand();
        }

        internal static void SyncMenu(Menu menu, bool force = false)
        {
            if (menuItems == null || menu.menuItems != menuItems) return;
            if (!IsDead || !menu.showMenu) {
                menu.showMenu = false;
                menuItems = null;
                HudLayout.Invalidate();
                return;
            }
            Rect bounds = HudLayout.GetRect(MenuBounds);
            bool moved = menu.menuX != (int)bounds.x || menu.menuY != (int)bounds.y;
            menu.menuX = (int)bounds.x; menu.menuY = (int)bounds.y; menu.w = (int)bounds.width - 1;
            if (force || moved) {
                for (int i = 0; i < Menu.menuTemY.Length; i++) Menu.menuTemY[i] = menu.menuY;
            }
        }
    }
}
