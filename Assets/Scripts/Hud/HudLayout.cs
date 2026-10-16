using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hud
{
    public enum HorizontalAnchor { Left, Center, Right }
    public enum VerticalAnchor { Top, Center, Bottom }

    // All rectangles are game logical coordinates, not physical pixels. ScaleGUI already
    // applies the safe-area viewport to rendering/pointers; do not subtract its insets again.
    public static class HudLayout
    {
        public const string WorldBar = "world-chat.bar", ChatButton = "chat.button", Bell = "notification.bell";
        // Match the existing GameScr paint order. Higher order wins pointer arbitration.
        public const int BellOrder = 10, WorldBarOrder = 20, ControlsOrder = 30, ChatOrder = 40, MenuOrder = 50, OverlayOrder = 100;
        private sealed class Entry
        {
            public string Name;
            public Func<Rect> Resolve;
            public Func<bool> Visible;
            public int Order, Serial;
            public bool BlocksPointer, Resolving;
            public long Generation = -1;
            public Rect Bounds;
        }
        private static readonly Dictionary<string, Entry> byName = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private static readonly List<Entry> backToFront = new List<Entry>();
        private static bool legacyRegistered;
        private static long generation;
        private static int width = -1, height, zoom, keyboardY;
        private static RuntimePlatform platform;

        // Device.Application reports the simulated Android/iOS platform in Device Simulator.
        // Read-only, no Editor override, preferences, RMS flag or mod toggle.
        public static bool IsMobileLayout => UnityEngine.Device.Application.platform == RuntimePlatform.Android
            || UnityEngine.Device.Application.platform == RuntimePlatform.IPhonePlayer;
        public static Vector2 DesignReference => IsMobileLayout ? new Vector2(844, 390) : new Vector2(1280, 720);
        public static Rect ScreenBounds => new Rect(0, 0, GameCanvas.w, GameCanvas.h);

        // Exact physical-design-pixel conversion: 1280x720 / zoom 2 = 640x360.
        // This does not change MotherCanvas zoom or stretch a component to another aspect ratio.
        public static float PixelsToLogical(float pixels) => pixels / Mathf.Max(1, mGraphics.zoomLevel);
        public static Vector2 PixelsToLogical(Vector2 pixels) => pixels / Mathf.Max(1, mGraphics.zoomLevel);
        // Optional uniform fit for a complete reference design on a different logical viewport.
        // Kept separate from exact conversion so callers explicitly choose responsive scaling.
        public static Vector2 FitDesignSize(Vector2 pixels)
        {
            Vector2 reference = DesignReference;
            return pixels * Mathf.Min(GameCanvas.w / reference.x, GameCanvas.h / reference.y);
        }
        public static Rect Anchor(HorizontalAnchor horizontal, VerticalAnchor vertical, Vector2 size, Vector2 margin)
        {
            float x = horizontal == HorizontalAnchor.Left ? margin.x : horizontal == HorizontalAnchor.Right
                ? GameCanvas.w - size.x - margin.x : (GameCanvas.w - size.x) / 2f + margin.x;
            float y = vertical == VerticalAnchor.Top ? margin.y : vertical == VerticalAnchor.Bottom
                ? GameCanvas.h - size.y - margin.y : (GameCanvas.h - size.y) / 2f + margin.y;
            return new Rect(x, y, Mathf.Max(0, size.x), Mathf.Max(0, size.y));
        }
        public static Rect Inset(float left, float top, float right, float bottom) =>
            new Rect(left, top, Mathf.Max(0, GameCanvas.w - left - right), Mathf.Max(0, GameCanvas.h - top - bottom));

        // Register once when the component is introduced. Its provider is evaluated once per
        // frame/viewport generation; drawing, hit tests and obstacle collection share that result.
        public static void Register(string name, Func<Rect> resolve, int order, Func<bool> visible = null, bool blocksPointer = true)
        {
            if (string.IsNullOrEmpty(name) || resolve == null) throw new ArgumentException("HUD name/rect provider is required.");
            if (byName.ContainsKey(name)) throw new InvalidOperationException("HUD rect already registered: " + name);
            var entry = new Entry { Name = name, Resolve = resolve, Order = order, Serial = byName.Count, Visible = visible, BlocksPointer = blocksPointer };
            byName.Add(name, entry); backToFront.Add(entry);
            backToFront.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Serial.CompareTo(b.Serial));
        }
        public static void RegisterAnchored(string name, HorizontalAnchor horizontal, VerticalAnchor vertical,
            Vector2 logicalSize, Vector2 logicalMargin, int order, Func<bool> visible = null, bool blocksPointer = true) =>
            Register(name, () => Anchor(horizontal, vertical, logicalSize, logicalMargin), order, visible, blocksPointer);

        // Design-pixel size/margins are converted lazily, including after a zoom change.
        public static void RegisterDesignAnchored(string name, HorizontalAnchor horizontal, VerticalAnchor vertical,
            Vector2 pixelSize, Vector2 pixelMargin, int order, Func<bool> visible = null, bool blocksPointer = true) =>
            Register(name, () => Anchor(horizontal, vertical, PixelsToLogical(pixelSize), PixelsToLogical(pixelMargin)),
                order, visible, blocksPointer);

        public static void BeginFrame()
        {
            generation++;
            EnsureViewport();
        }
        // State can change between input and paint (modal open/close, tabs, scrolling).
        // Retain the same providers, but discard their pre-change cached rectangles.
        public static void Invalidate() => generation++;
        private static void EnsureViewport()
        {
            RuntimePlatform current = UnityEngine.Device.Application.platform;
            if (width != GameCanvas.w || height != GameCanvas.h || zoom != mGraphics.zoomLevel
                || keyboardY != mGraphics.addYWhenOpenKeyBoard || platform != current)
            {
                width = GameCanvas.w; height = GameCanvas.h; zoom = mGraphics.zoomLevel;
                keyboardY = mGraphics.addYWhenOpenKeyBoard; platform = current; generation++;
            }
            if (!legacyRegistered && GameScr.instance != null)
            {
                legacyRegistered = true; // Set first: a provider can refer to another registered rect.
                GameScr.RegisterHudRects();
            }
        }
        private static Rect Resolve(Entry entry)
        {
            if (entry.Generation != generation) {
                if (entry.Resolving) throw new InvalidOperationException("Circular HUD rect dependency: " + entry.Name);
                entry.Resolving = true;
                try { entry.Bounds = entry.Resolve(); entry.Generation = generation; }
                finally { entry.Resolving = false; }
            }
            return entry.Bounds;
        }
        private static bool IsVisible(Entry entry) => entry.Visible == null || entry.Visible();
        public static Rect GetRect(string name)
        {
            EnsureViewport();
            if (!byName.TryGetValue(name, out Entry entry)) throw new KeyNotFoundException("HUD rect not registered: " + name);
            return Resolve(entry);
        }
        public static bool TryGetRect(string name, out Rect bounds)
        {
            EnsureViewport();
            if (byName.TryGetValue(name, out Entry entry)) { bounds = Resolve(entry); return true; }
            bounds = new Rect(); return false;
        }
        // Future HUD painters use this traversal. Equal-order components draw in registration
        // order; HitTest reverses precisely the same list. Decorative entries can opt out of input.
        public static void ForEachBackToFront(Action<string, Rect> paint)
        {
            EnsureViewport();
            foreach (Entry entry in backToFront)
            {
                Rect bounds = Resolve(entry);
                if (bounds.width > 0 && bounds.height > 0 && IsVisible(entry)) paint(entry.Name, bounds);
            }
        }
        public static string HitTest(Vector2 point)
        {
            EnsureViewport();
            for (int i = backToFront.Count - 1; i >= 0; i--)
            {
                Entry entry = backToFront[i];
                if (entry.BlocksPointer && Resolve(entry).Contains(point) && IsVisible(entry)) return entry.Name;
            }
            return null;
        }
        public static bool IsTopmost(string name, Vector2 point) => HitTest(point) == name;
        // Read existing combat-control adapters without resolving the lists that are currently
        // computing their own bounds (and without constructing another set of hitboxes).
        public static void CopyObstaclesAtOrder(int order, List<Rect> output)
        {
            EnsureViewport(); output.Clear();
            foreach (Entry entry in backToFront) {
                if (entry.Order != order || !entry.BlocksPointer || !IsVisible(entry)) continue;
                Rect bounds = Resolve(entry);
                if (bounds.width > 0 && bounds.height > 0) output.Add(bounds);
            }
        }
        // List height follows painted controls, not broad joystick acquisition zones.
        // Keep the original obstacle API for chat/input; their policies are unchanged.
        internal static void CopyVisualControls(List<Rect> output)
        {
            EnsureViewport(); output.Clear();
            foreach (Entry entry in backToFront) {
                if (entry.Order != ControlsOrder || entry.Name == CombatMobileHud.JoystickTouch || !IsVisible(entry)) continue;
                Rect bounds = entry.Name == "pc-analog.touch"
                    ? !IsMobileLayout && CombatPcHud.Visible && GameScr.isAnalog != 0 && Char.myCharz().statusMe != 14
                        && GameScr.gamePad != null ? GameScr.gamePad.HudVisualBounds : new Rect()
                    : Resolve(entry);
                if (bounds.width > 0 && bounds.height > 0) output.Add(bounds);
            }
        }
        // This is the source behind GameScr.CollectChatObstacles, not a second obstacle system.
        public static void CopyObstaclesAbove(string name, List<Rect> output)
        {
            EnsureViewport(); output.Clear();
            if (!byName.TryGetValue(name, out Entry target)) return;
            int first = backToFront.IndexOf(target) + 1;
            for (int i = first; i < backToFront.Count; i++)
            {
                Entry entry = backToFront[i]; Rect bounds = Resolve(entry);
                if (entry.BlocksPointer && bounds.width > 0 && bounds.height > 0 && IsVisible(entry)) output.Add(bounds);
            }
        }
    }
}
