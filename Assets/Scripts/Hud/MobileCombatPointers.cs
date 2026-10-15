using System.Collections.Generic;
using UnityEngine;

namespace Hud
{
    internal static class MobileCombatPointers
    {
        private sealed class Capture
        {
            public string Target;
            public Vector2 First, Point;
            public Skill Skill;
            public int ItemId = -1;
            public bool Cancelled;
            public float Started;
        }
        private sealed class Click { public Capture Capture; public Vector2 Point; }
        private static readonly Dictionary<int,Capture> pointers = new Dictionary<int,Capture>();
        private static readonly Queue<Click> clicks = new Queue<Click>();
        private static int inputFrame = -1;
        private static int? worldPointer, stickPointer;
        private static bool stickWasActive, worldSuppressed;
        private static Vector2 viewportSize;
        private static Rect viewport;
        private static int zoom;
        private static GameScr screen;
        private static mScreen activeScreen;
        private static GamePad pad;
        private static int map = -1, character;
        private static readonly HashSet<int> seen = new HashSet<int>();
        private static readonly List<int> missing = new List<int>();

        internal static bool Held(string target)
        {
            foreach(Capture capture in pointers.Values) if(!capture.Cancelled && capture.Target == target) return true;
            return false;
        }
        private static Vector2 Logical(Vector2 position)
        {
            Vector2 point = ScaleGUI.ToGuiPoint(new Vector2(position.x,Screen.height-position.y)) / Mathf.Max(1,mGraphics.zoomLevel);
            point.y += mGraphics.addYWhenOpenKeyBoard;
            return point;
        }
        // Main.OnGUI can run several times per Unity frame. Poll native touches exactly once.
        // The non-combat owner still uses the original singleton pointer stream; secondary
        // world/UI fingers are quarantined rather than retargeting the first pointer.
        internal static bool Poll()
        {
            if (!HudLayout.IsMobileLayout) return false;
            if (inputFrame == Time.frameCount) return true;
            inputFrame = Time.frameCount;
            Vector2 size = new Vector2(GameCanvas.w,GameCanvas.h);
            if (viewportSize != size || viewport != ScaleGUI.Viewport || zoom != mGraphics.zoomLevel) {
                Cancel(); viewportSize = size; viewport = ScaleGUI.Viewport; zoom = mGraphics.zoomLevel;
            }
            seen.Clear();
            if (Input.touchCount > 0) {
                for(int i=0;i<Input.touchCount;i++) {
                    Touch touch = Input.GetTouch(i);
                    seen.Add(touch.fingerId);
                    bool end = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                    Feed(touch.fingerId,Logical(touch.position),touch.phase == TouchPhase.Began,end,
                        touch.phase == TouchPhase.Canceled,ScaleGUI.Viewport.Contains(new Vector2(touch.position.x,Screen.height-touch.position.y)));
                }
            }
            else { // Device Simulator/editor mouse uses the same capture protocol.
                Vector2 mouse = new Vector2(Input.mousePosition.x,Input.mousePosition.y);
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
                {
                    seen.Add(-1);
                    Feed(-1,Logical(mouse),Input.GetMouseButtonDown(0),Input.GetMouseButtonUp(0),false,
                        ScaleGUI.Viewport.Contains(new Vector2(mouse.x,Screen.height-mouse.y)));
                }
            }
            missing.Clear();
            foreach(int id in pointers.Keys) if(!seen.Contains(id)) missing.Add(id);
            foreach(int id in missing) Feed(id,pointers[id].Point,false,true,true,false);
            return true; // Suppress Unity's emulated mouse for native touch, including Ended.
        }
        private static void Feed(int id, Vector2 point, bool began, bool ended, bool cancelled, bool inside)
        {
            if (began) {
                string target = inside ? HudLayout.HitTest(point) : null;
                var capture = new Capture { Target = target, First = point, Point = point, Cancelled = !inside, Started = Time.realtimeSinceStartup };
                if (CombatMobileHud.Owns(target)) {
                    if (!CombatMobileHud.CanInput) capture.Cancelled = true;
                    if (target.StartsWith("combat.skill.")) capture.Skill = CombatMobileHud.SlotSkill(int.Parse(target.Substring("combat.skill.".Length)));
                    if (target.StartsWith("combat.item.")) capture.ItemId = MobileQuickItems.Template(int.Parse(target.Substring("combat.item.".Length)));
                    if (target == CombatMobileHud.JoystickTouch) {
                        if (stickPointer.HasValue) capture.Cancelled = true;
                        else stickPointer = id;
                    }
                }
                else if (inside && !worldPointer.HasValue) {
                    worldPointer = id; worldSuppressed = false;
                    GameMidlet.gameCanvas.pointerPressed((int)point.x,(int)point.y);
                }
                else capture.Cancelled = true;
                pointers[id] = capture;
            }
            if (!pointers.TryGetValue(id,out Capture current)) return;
            current.Point = point;
            if (cancelled) current.Cancelled = true;
            if (CombatMobileHud.Owns(current.Target) && !CombatMobileHud.CanInput) current.Cancelled = true;
            if (!ended && !current.Cancelled && current.Target != null && current.Target.StartsWith("combat.item.")
                && Time.realtimeSinceStartup-current.Started >= .6f
                && (point-current.First).sqrMagnitude <= Mathf.Pow(CombatMobileHud.DesignUnit(8),2)
                && HudLayout.HitTest(point) == current.Target) {
                int slot = int.Parse(current.Target.Substring("combat.item.".Length));
                if (current.ItemId >= 0 && MobileQuickItems.Template(slot) == current.ItemId) {
                    Cancel(); // Release after the long press cannot also use the item.
                    MobileQuickItems.ShowSlotMenu(slot);
                }
            }
            if (worldPointer == id) {
                // Do not let a world drag enter a combat button or acquire a modal that opened mid-gesture.
                string now = HudLayout.HitTest(point);
                if (cancelled || now != current.Target && (CombatMobileHud.Owns(now) || now == "legacy.overlay"
                    || now != null && now.StartsWith("notifications."))) worldSuppressed = true;
                if (ended && now != current.Target) worldSuppressed = true;
                if (worldSuppressed) GameCanvas.clearAllPointerEvent();
                else if(ended) GameMidlet.gameCanvas.pointerReleased((int)point.x,(int)point.y);
                else if(!began) GameMidlet.gameCanvas.pointerDragged((int)point.x,(int)point.y);
            }
            if (!ended) return;
            if (!current.Cancelled && CombatMobileHud.Owns(current.Target) && current.Target != CombatMobileHud.JoystickTouch
                && HudLayout.HitTest(point) == current.Target) clicks.Enqueue(new Click { Capture=current,Point=point });
            if(worldPointer == id) worldPointer = null;
            if(stickPointer == id) stickPointer = null;
            pointers.Remove(id);
        }
        internal static void Cancel()
        {
            CircularCombatIcon.Clear();
            foreach(Capture capture in pointers.Values) capture.Cancelled = true;
            clicks.Clear(); worldSuppressed = true;
            if(worldPointer.HasValue) GameCanvas.clearAllPointerEvent();
        }
        // Called on every game tick, including while a modal prevents GameScr.updateKey.
        internal static void UpdateJoystick()
        {
            if (!HudLayout.IsMobileLayout) return;
            if (screen != GameScr.instance || pad != GameScr.gamePad || map != TileMap.mapID || character != Char.myCharz().charID
                || activeScreen != GameCanvas.currentScreen) {
                Cancel(); screen = GameScr.instance; pad = GameScr.gamePad; map = TileMap.mapID; character = Char.myCharz().charID;
                activeScreen = GameCanvas.currentScreen;
            }
            Capture stick = null;
            if(stickPointer.HasValue) pointers.TryGetValue(stickPointer.Value,out stick);
            bool active = CombatMobileHud.CanInput && GameScr.isAnalog != 0 && stick != null && !stick.Cancelled;
            if(!active && stick != null) stick.Cancelled = true;
            if(GameScr.gamePad != null && (active || stickWasActive)) {
                GameScr.gamePad.UpdateMobilePointer(active,active ? stick.First : Vector2.zero,active ? stick.Point : Vector2.zero);
            }
            stickWasActive = active;
            if(!CombatMobileHud.CanInput) {
                foreach(Capture capture in pointers.Values) if(CombatMobileHud.Owns(capture.Target)) capture.Cancelled = true;
                clicks.Clear();
            }
        }
        internal static void Dispatch()
        {
            while(clicks.Count > 0) {
                Click click = clicks.Dequeue(); Capture capture = click.Capture;
                if(!CombatMobileHud.CanInput || HudLayout.HitTest(click.Point) != capture.Target) continue;
                // A changed skill assignment while held cannot fire a different slot.
                if(capture.Target.StartsWith("combat.skill.") && capture.Skill != CombatMobileHud.SlotSkill(int.Parse(capture.Target.Substring("combat.skill.".Length)))) continue;
                CombatMobileHud.Activate(capture.Target,capture.Skill,capture.ItemId);
            }
        }
    }
}
