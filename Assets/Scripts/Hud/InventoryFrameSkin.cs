using System.Collections.Generic;
using UnityEngine;

namespace Hud
{
    internal static class InventoryFrameSkin
    {
        private static readonly Dictionary<long, Image> corners = new Dictionary<long, Image>();
        private static Texture2D source;
        internal static int DiagnosticCacheCount => corners.Count;

        internal static Image Corners(int fill, int border, byte opacity)
        {
            if (PopUp.goc == null) PopUp.loadBg();
            if (source != PopUp.goc.texture) {
                SkillDiagnostics.CacheCleared(false);
                foreach (Image image in corners.Values) Object.Destroy(image.texture);
                corners.Clear(); source = PopUp.goc.texture;
            }
            long key = ((long)fill << 32) | ((long)border << 8) | opacity;
            if (!corners.TryGetValue(key, out Image result)) {
                if (corners.Count >= 64) {
                    SkillDiagnostics.CacheCleared(true);
                    foreach (Image image in corners.Values) Object.Destroy(image.texture);
                    corners.Clear();
                }
                result = HudButtonSkin.CopyNative(PopUp.goc,
                    (pixels, width, height) => TintCorners(pixels, fill, border, opacity));
                corners.Add(key, result);
                SkillDiagnostics.CacheEntry(key,fill,border,opacity,source.GetInstanceID());
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
