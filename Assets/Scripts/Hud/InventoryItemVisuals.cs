using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hud
{
    internal static class InventoryItemVisuals
    {
        private static readonly Dictionary<int, Texture2D> auras = new Dictionary<int, Texture2D>();
        private static Texture2D cornerSource;
        private static Color32[] cornerPixels;
        private static int cornerWidth, cornerHeight;
        private static readonly Dictionary<int, GUIStyle> levelStyles = new Dictionary<int, GUIStyle>();
        private static Font levelFont;
        private static int levelZoom;
        private static float levelWidth;

        internal static Rect StarBounds(Rect cell, int index)
        {
            float size = System.Math.Min(5, (cell.width - 2) / 6);
            return new Rect(cell.center.x - 3 * size + index * size, cell.yMax + 3.5f, size, size);
        }
        internal static Rect LevelBounds(Rect cell, float width, bool bag)
        {
            width = bag ? System.Math.Min(width, cell.width - 4) : width;
            return new Rect(bag ? cell.x + 2 : cell.xMax - width + 3, bag ? cell.y - 2 : cell.yMax - 9, width, 12);
        }

        // Rounded alpha comes from the native corner sprite. The center stays transparent.
        internal static Color32[] AuraPixels(int color, Color32[] corner, int cw, int ch, int size)
        {
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
                // Keep hue in transparent pixels too, avoiding a dark fringe with bilinear sampling.
                pixels[y * size + x] = new Color32((byte)(color >> 16), (byte)(color >> 8), (byte)color, 0);
                float dx = System.Math.Min(x + .5f, size - x - .5f);
                float dy = System.Math.Min(y + .5f, size - y - .5f);
                float edge = System.Math.Min(dx, dy);
                if (edge <= 1 || edge >= 15) continue;
                float mask = 1;
                if (dx < 8 && dy < 8) {
                    int sx = System.Math.Min(cw - 1, (int)(dx * cw / 8));
                    int sy = ch - 1 - System.Math.Min(ch - 1, (int)(dy * ch / 8));
                    mask = corner[sy * cw + sx].a / 255f;
                }
                float fade = (float)System.Math.Pow(1 - (edge - 1) / 14, 1.8);
                pixels[y * size + x] = new Color32((byte)(color >> 16), (byte)(color >> 8), (byte)color,
                    (byte)System.Math.Round(190 * fade * mask));
            }
            return pixels;
        }
        private static Texture2D Aura(int color)
        {
            if (PopUp.goc == null) PopUp.loadBg();
            if (cornerSource != PopUp.goc.texture) {
                foreach (Texture2D texture in auras.Values) UnityEngine.Object.Destroy(texture);
                auras.Clear(); cornerSource = PopUp.goc.texture;
                Image readback = HudButtonSkin.CopyNative(PopUp.goc, (pixels, width, height) => {
                    cornerPixels = (Color32[])pixels.Clone(); cornerWidth = width; cornerHeight = height;
                });
                UnityEngine.Object.Destroy(readback.texture);
            }
            if (!auras.TryGetValue(color, out Texture2D aura)) {
                aura = new Texture2D(64, 64, TextureFormat.RGBA32, false) {
                    filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave
                };
                aura.SetPixels32(AuraPixels(color, cornerPixels, cornerWidth, cornerHeight, 64));
                aura.Apply(false, true); auras.Add(color, aura);
            }
            return aura;
        }
        internal static void PaintAura(Rect cell, Rect viewport, int color)
        {
            Texture2D aura = Aura(color);
            Color previous = GUI.color;
            int zoom = Mathf.Max(1, mGraphics.zoomLevel);
            try {
                GUI.color = Color.white;
                GUI.BeginGroup(new Rect(viewport.x * zoom, viewport.y * zoom, viewport.width * zoom, viewport.height * zoom));
                try { GUI.DrawTexture(new Rect((cell.x - viewport.x) * zoom, (cell.y - viewport.y) * zoom,
                    cell.width * zoom, cell.height * zoom), aura); }
                finally { GUI.EndGroup(); }
            } finally { GUI.color = previous; }
        }
        internal static void PaintStars(Rect cell, Rect viewport, int filled)
        {
            int zoom = Mathf.Max(1, mGraphics.zoomLevel);
            Color previous = GUI.color;
            try {
                GUI.color = Color.white;
                GUI.BeginGroup(new Rect(viewport.x * zoom, viewport.y * zoom, viewport.width * zoom, viewport.height * zoom));
                try {
                    for (int i = 0; i < 6; i++) {
                        Image image = i < filled ? Panel.imgStar : Panel.imgMaxStar;
                        if (image == null) continue;
                        Rect star = StarBounds(cell, i);
                        GUI.DrawTexture(new Rect((star.x - viewport.x) * zoom, (star.y - viewport.y) * zoom,
                            star.width * zoom, star.height * zoom), image.texture);
                    }
                } finally { GUI.EndGroup(); }
            } finally { GUI.color = previous; }
        }
        internal static void PaintLevel(mGraphics g, Rect cell, Rect viewport, int level, bool bag)
        {
            mFont font = mFont.tahoma_7;
            int zoom = Mathf.Max(1, mGraphics.zoomLevel);
            if (levelFont != font.myFont || levelZoom != zoom || levelWidth <= 0) {
                levelFont = font.myFont; levelZoom = zoom; levelWidth = font.getWidth("Lv7") + 6; levelStyles.Clear();
            }
            Rect badge = LevelBounds(cell, levelWidth, bag);
            InventoryFrame.Clip(g, viewport);
            InventoryFrame.Rounded(g, badge, InventoryFrame.Accent);
            string label = "Lv" + Mathf.Clamp(level, 0, 7);
            if (zoom == 1) {
                font.drawString(g, label, (int)badge.center.x, (int)(badge.center.y - font.getHeight() / 2f), mFont.CENTER);
                return;
            }
            int key = Mathf.RoundToInt(badge.width * zoom);
            if (!levelStyles.TryGetValue(key, out GUIStyle style)) {
                style = new GUIStyle(GUI.skin.label) {
                    font = font.myFont, fontSize = Mathf.Max(1, font.myFont.fontSize), alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0),
                    wordWrap = false, richText = false, fixedWidth = 0, fixedHeight = 0
                };
                while (style.fontSize > 1 && (style.CalcSize(new GUIContent("Lv7")).x > key - 4
                    || style.CalcSize(new GUIContent("Lv7")).y > badge.height * zoom - 2)) style.fontSize--;
                style.normal.textColor = style.hover.textColor = Color.black;
                style.active.textColor = style.focused.textColor = Color.black;
                if (levelStyles.Count >= 8) levelStyles.Clear();
                levelStyles.Add(key, style);
            }
            Color previous = GUI.color;
            try {
                GUI.color = Color.white;
                GUI.BeginGroup(new Rect(viewport.x * zoom, viewport.y * zoom, viewport.width * zoom, viewport.height * zoom));
                try { GUI.Label(new Rect((badge.x - viewport.x) * zoom, (badge.y - viewport.y) * zoom,
                    badge.width * zoom, badge.height * zoom), label, style); }
                finally { GUI.EndGroup(); }
            } finally { GUI.color = previous; }
        }
    }
}
