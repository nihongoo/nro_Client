using UnityEngine;

namespace Hud
{
    // Suppress the legacy world stream for the entire press/drag/release of a PC button.
    // Keyboard events still run through Main/MyKeyMap/GameScr without alteration.
    internal static class CombatPcPointer
    {
        private static string target, pending;
        private static Skill skill, pendingSkill;
        private static int item = -1, pendingItem = -1, frame = -1, map = -1, character, zoom;
        private static bool cancelled, consumedFrame;
        private static Vector2 releasePoint, size;
        private static Rect viewport;
        private static mScreen screen;
        internal static bool Held(string name) => target == name && !cancelled;
        internal static void Cancel()
        {
            if (target != null) cancelled = true;
            pending = null;
        }
        internal static bool Poll(Vector2 raw, Vector2 logical)
        {
            if (HudLayout.IsMobileLayout) return false;
            if (frame == Time.frameCount) return consumedFrame;
            frame = Time.frameCount; consumedFrame = false;
            Vector2 currentSize = new Vector2(GameCanvas.w,GameCanvas.h);
            if (screen != GameCanvas.currentScreen || map != TileMap.mapID || character != Char.myCharz().charID
                || size != currentSize || zoom != mGraphics.zoomLevel || viewport != ScaleGUI.Viewport) {
                Cancel(); CircularCombatIcon.Clear(); CombatPcHud.ClearBoxes(); TargetInfoHud.ClearShapes();
                screen = GameCanvas.currentScreen; map = TileMap.mapID; character = Char.myCharz().charID;
                size = currentSize; zoom = mGraphics.zoomLevel; viewport = ScaleGUI.Viewport;
            }
            if (!CombatPcHud.CanInput) Cancel();
            bool inside = ScaleGUI.Viewport.Contains(raw);
            string hit = inside ? HudLayout.HitTest(logical) : null;
            if (CombatPcHud.CanInput && Input.GetMouseButtonDown(1) && hit != null && hit.StartsWith("pc-combat.item.")) {
                Cancel();
                MobileQuickItems.ShowSlotMenu(int.Parse(hit.Substring("pc-combat.item.".Length)));
                consumedFrame = true;
                return true;
            }
            if (Input.GetMouseButtonDown(0) && CombatPcHud.Owns(hit)) {
                target = hit; cancelled = !CombatPcHud.CanInput; skill = null; item = -1;
                if (hit.StartsWith("pc-combat.skill.")) skill = CombatPcHud.SlotSkill(int.Parse(hit.Substring("pc-combat.skill.".Length)));
                else if (hit.StartsWith("pc-combat.item.")) item = MobileQuickItems.Template(int.Parse(hit.Substring("pc-combat.item.".Length)));
                GameCanvas.clearAllPointerEvent();
            }
            if (target != null) {
                consumedFrame = true;
                if (Input.GetMouseButtonUp(0)) {
                    if (!cancelled && CombatPcHud.CanInput && hit == target) {
                        pending = target; pendingSkill = skill; pendingItem = item; releasePoint = logical;
                    }
                    target = null;
                }
                else if (!Input.GetMouseButton(0)) { target = null; pending = null; }
                GameCanvas.clearAllPointerEvent();
            }
            // A world press dragged onto a control must neither activate it nor click through.
            else if (Input.GetMouseButtonUp(0) && CombatPcHud.Owns(hit)) {
                consumedFrame = true; GameCanvas.clearAllPointerEvent();
            }
            return consumedFrame;
        }
        internal static void Dispatch()
        {
            string command = pending; pending = null;
            if (command == null || !CombatPcHud.CanInput || HudLayout.HitTest(releasePoint) != command) return;
            CombatPcHud.Activate(command,pendingSkill,pendingItem);
        }
    }
}
