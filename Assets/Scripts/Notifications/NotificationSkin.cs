using UnityEngine;

namespace Notifications
{
    internal static class NotificationSkin
    {
        private static Image bellIcon, bellGlyph, closeIcon, badgeIcon, smoothBadge;
        private static int assetZoom = -1;
        internal static Image BellGlyph {
            get {
                LoadIcons();
                if (bellGlyph == null) bellGlyph = Hud.HudButtonSkin.CopyNative(bellIcon, (pixels, width, height) => {
                    // Keep only the original cream bell symbol; its old red/orange disc is discarded.
                    for (int y = 0; y < height; y++)
                        for (int x = 0; x < width; x++) {
                            int i = y * width + x;
                            bool center = x >= width * 0.25f && x < width * 0.75f
                                && y >= height * 0.23f && y < height * 0.77f;
                            byte alpha = center ? (byte)Mathf.Clamp((pixels[i].b - 64) * 255 / 140, 0, 255) : (byte)0;
                            pixels[i] = new Color32(255, 248, 220, alpha);
                        }
                });
                return bellGlyph;
            }
        }
        internal static Image BadgeIcon {
            get {
                LoadIcons();
                if (smoothBadge == null) {
                    smoothBadge = Hud.HudButtonSkin.CopyNative(badgeIcon, SmoothBadgeEdges);
                    smoothBadge.texture.filterMode = FilterMode.Bilinear;
                }
                return smoothBadge;
            }
        }
        internal static Image CloseIcon { get { LoadIcons(); return closeIcon; } }

        private static void SmoothBadgeEdges(Color32[] pixels, int width, int height)
        {
            // The original border has hard, enlarged pixel steps. Reconstruct its rounded
            // contour at the native output resolution, keeping source colors and row gradient.
            int border = Mathf.Max(1, mGraphics.zoomLevel);
            Color outer = pixels[width / 2];
            Color rim = pixels[Mathf.Min(height - 1, border) * width + width / 2];
            Color32[] rows = new Color32[height];
            for (int y = 0; y < height; y++) rows[y] = pixels[y * width + width / 2];
            float radius = Mathf.Min(width, height) / 2f;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) {
                    float qx = Mathf.Abs(x + 0.5f - width / 2f) - (width / 2f - radius);
                    float qy = Mathf.Abs(y + 0.5f - height / 2f) - (height / 2f - radius);
                    float dx = Mathf.Max(qx, 0), dy = Mathf.Max(qy, 0);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
                    Color color = Color.Lerp(outer, rim, Mathf.Clamp01(-distance - border + 0.5f));
                    color = Color.Lerp(color, rows[y], Mathf.Clamp01(-distance - border * 2 + 0.5f));
                    color.a = Mathf.Clamp01(0.5f - distance);
                    pixels[y * width + x] = color;
                }
        }

        private static void LoadIcons()
        {
            if (assetZoom == mGraphics.zoomLevel && bellIcon != null && closeIcon != null && badgeIcon != null) return;
            if (bellGlyph != null) { UnityEngine.Object.Destroy(bellGlyph.texture); bellGlyph = null; }
            if (smoothBadge != null) { UnityEngine.Object.Destroy(smoothBadge.texture); smoothBadge = null; }
            bellIcon = GameCanvas.loadImage("/mainImage/notification-bell.png");
            closeIcon = GameCanvas.loadImage("/mainImage/myTexture2dbtX.png");
            badgeIcon = GameCanvas.loadImage("/mainImage/notification-badge.png");
            assetZoom = mGraphics.zoomLevel;
        }

        // Reuse the stock popup texture. Clamp the final edge tile so it cannot cover a corner.
        internal static void PaintFrame(mGraphics g, int x, int y, int w, int h, bool selected)
        {
            if (w < 20 || h < 20) return;
            Image skin = selected ? PopUp.imgPopUp2 : PopUp.imgPopUp;
            g.setColor(selected ? 12052656 : 16770503);
            g.fillRect(x + 10, y + 10, w - 20, h - 20);
            for (int offset = 10; offset < w - 10; offset += 10)
            {
                int length = System.Math.Min(10, w - 10 - offset);
                g.drawRegion(skin, 0, 10, length, 10, 0, x + offset, y, 0);
                g.drawRegion(skin, 0, 60, length, 10, 0, x + offset, y + h - 10, 0);
            }
            for (int offset = 10; offset < h - 10; offset += 10)
            {
                int length = System.Math.Min(10, h - 10 - offset);
                g.drawRegion(skin, 0, 30, 10, length, 0, x, y + offset, 0);
                g.drawRegion(skin, 0, 40, 10, length, 0, x + w - 10, y + offset, 0);
            }
            g.drawRegion(skin, 0, 0, 10, 10, 0, x, y, 0);
            g.drawRegion(skin, 0, 20, 10, 10, 0, x + w - 10, y, 0);
            g.drawRegion(skin, 0, 50, 10, 10, 0, x, y + h - 10, 0);
            g.drawRegion(skin, 0, 70, 10, 10, 0, x + w - 10, y + h - 10, 0);
        }
    }
}
