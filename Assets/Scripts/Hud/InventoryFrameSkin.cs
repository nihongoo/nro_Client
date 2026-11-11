using System.Collections.Generic;
using UnityEngine;

namespace Hud
{
    // Preserve the native artwork, but upload a complete frame instead of drawing
    // touching quads through the fractional safe-area matrix on the phone.
    internal static class NativeHudSkin
    {
        private static readonly Dictionary<(Texture2D, int, int, int, int, int, int), Texture2D> frames =
            new Dictionary<(Texture2D, int, int, int, int, int, int), Texture2D>();
        private static readonly Dictionary<Texture2D, Color32[]> sources = new Dictionary<Texture2D, Color32[]>();

        internal static void PaintTab(Rect bounds, Rect viewport, bool selected)
        {
            PopUp.loadBg();
            Paint(bounds, viewport, selected ? PopUp.imgPopUp2 : PopUp.imgPopUp,
                selected ? 12052656 : 16770503, -1);
        }
        internal static void PaintFrame(Rect bounds, Rect viewport, int fill, int border)
        {
            PopUp.loadBg();
            Paint(bounds, viewport, PopUp.goc, fill, border);
        }
        private static void Paint(Rect bounds, Rect viewport, Image image, int fill, int border)
        {
            Rect pixels = HudButtonSkin.ScreenPixels(bounds);
            Rect clip = InventoryFrame.Intersection(pixels, HudButtonSkin.ScreenPixels(viewport));
            if (clip.width <= 0 || clip.height <= 0 || image?.texture == null) return;
            bool tab = border < 0;
            float scaleX = pixels.width / bounds.width, scaleY = pixels.height / bounds.height;
            int width = (int)pixels.width, height = (int)pixels.height;
            int capX = Mathf.Clamp(Mathf.RoundToInt((tab ? 10 : 7) * scaleX), 1, width / 2);
            int capY = Mathf.Clamp(Mathf.RoundToInt((tab ? 10 : 6) * scaleY), 1, height / 2);
            var key = (image.texture, width, height, capX, capY, fill, border);
            if (!frames.TryGetValue(key, out Texture2D texture) || texture == null) {
                if (frames.Count >= 64) {
                    foreach (Texture2D old in frames.Values) Object.Destroy(old);
                    frames.Clear();
                }
                Color32[] source = SourcePixels(image);
                var output = new Color32[width * height];
                Color32 background = Rgb(fill), outline = Rgb(border);
                int edgeX = Mathf.Max(1, Mathf.RoundToInt(scaleX)), edgeY = Mathf.Max(1, Mathf.RoundToInt(scaleY));
                int tile = image.texture.height / 8;
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) {
                    bool left = x < capX, right = x >= width - capX;
                    bool top = y < capY, bottom = y >= height - capY;
                    Color32 color;
                    if (tab) {
                        if (!left && !right && !top && !bottom) color = background;
                        else {
                            int row = top ? (left ? 0 : right ? 2 : 1) : bottom ? (left ? 5 : right ? 7 : 6) : left ? 3 : 4;
                            float u = left ? (x + .5f) / capX : right ? (x - width + capX + .5f) / capX : (x - capX + .5f) / Mathf.Max(1, width - 2 * capX);
                            float v = top ? (y + .5f) / capY : bottom ? (y - height + capY + .5f) / capY : (y - capY + .5f) / Mathf.Max(1, height - 2 * capY);
                            int sx = Mathf.Min(image.texture.width - 1, Mathf.FloorToInt(u * tile));
                            int sy = image.texture.height - 1 - (row * tile + Mathf.Min(tile - 1, Mathf.FloorToInt(v * tile)));
                            color = source[sy * image.texture.width + sx];
                        }
                    } else if ((left || right) && (top || bottom)) {
                        int cx = right ? width - 1 - x : x, cy = bottom ? height - 1 - y : y;
                        int sx = Mathf.Min(image.texture.width - 1, Mathf.FloorToInt((cx + .5f) * image.texture.width / capX));
                        int sy = image.texture.height - 1 - Mathf.Min(image.texture.height - 1, Mathf.FloorToInt((cy + .5f) * image.texture.height / capY));
                        Color32 corner = source[sy * image.texture.width + sx];
                        color = new Color32((byte)((background.r * corner.r + outline.r * (255 - corner.r)) / 255),
                            (byte)((background.g * corner.r + outline.g * (255 - corner.r)) / 255),
                            (byte)((background.b * corner.r + outline.b * (255 - corner.r)) / 255), corner.a);
                    } else color = x < edgeX || x >= width - edgeX || y < edgeY || y >= height - edgeY ? outline : background;
                    output[(height - 1 - y) * width + x] = color;
                }
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false) {
                    name = "HUD native complete frame", filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave
                };
                texture.SetPixels32(output);
                texture.Apply(false, true);
                frames[key] = texture;
            }
            SmoothRoundedSkin.PaintTexture(pixels, clip, texture);
        }
        private static Color32[] SourcePixels(Image image)
        {
            if (!sources.TryGetValue(image.texture, out Color32[] pixels)) {
                if (sources.Count >= 8) sources.Clear();
                Image copy = HudButtonSkin.CopyNative(image, (data, width, height) => pixels = data);
                Object.Destroy(copy.texture);
                sources[image.texture] = pixels;
            }
            return pixels;
        }
        private static Color32 Rgb(int color) => new Color32((byte)(color >> 16), (byte)(color >> 8), (byte)color, 255);
    }

    internal static class InventoryFrameSkin
    {
        private static readonly Dictionary<long, Image> corners = new Dictionary<long, Image>();
        private static Texture2D source;

        internal static Image Corners(int fill, int border, byte opacity)
        {
            if (PopUp.goc == null) PopUp.loadBg();
            if (source != PopUp.goc.texture) {
                foreach (Image image in corners.Values) Object.Destroy(image.texture);
                corners.Clear(); source = PopUp.goc.texture;
            }
            long key = ((long)fill << 32) | ((long)border << 8) | opacity;
            if (!corners.TryGetValue(key, out Image result)) {
                if (corners.Count >= 64) {
                    foreach (Image image in corners.Values) Object.Destroy(image.texture);
                    corners.Clear();
                }
                result = HudButtonSkin.CopyNative(PopUp.goc,
                    (pixels, width, height) => TintCorners(pixels, fill, border, opacity));
                corners.Add(key, result);
            }
            return result;
        }

        // Keep the original sprite geometry/alpha, recoloring its white fill and black outline.
        internal static void TintCorners(Color32[] pixels, int fill, int border, byte opacity)
        {
            for (int i = 0; i < pixels.Length; i++) {
                Color32 source = pixels[i];
                int light = source.r;
                pixels[i] = new Color32(
                    (byte)(((fill >> 16 & 255) * light + (border >> 16 & 255) * (255 - light)) / 255),
                    (byte)(((fill >> 8 & 255) * light + (border >> 8 & 255) * (255 - light)) / 255),
                    (byte)(((fill & 255) * light + (border & 255) * (255 - light)) / 255),
                    (byte)(source.a * opacity / 255));
            }
        }
    }
}
