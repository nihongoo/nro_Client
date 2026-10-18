using UnityEngine;

namespace Hud
{
    internal static class InventoryBagBadge
    {
        private static GUIStyle textStyle;
        private static Font sourceFont;
        private static float cachedWidth;
        private static int cachedZoom;
        private static float width, height, bitmapScale;
        private static readonly string[] maximumLabels = { "999", "999+" };

        internal static void Paint(mGraphics g, Rect cell, Rect viewport, int quantity)
        {
            mFont font = mFont.tahoma_7_white;
            int zoom = Mathf.Max(1, mGraphics.zoomLevel);
            float available = (cell.width + InventoryFrame.BagGap - 2) * zoom;
            if (textStyle == null || sourceFont != font.myFont || cachedWidth != available || cachedZoom != zoom) {
                sourceFont = font.myFont; cachedWidth = available; cachedZoom = zoom;
                textStyle = new GUIStyle(GUI.skin.label) {
                    font = font.myFont, alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0),
                    wordWrap = false, richText = false, fixedWidth = 0, fixedHeight = 0
                };
                textStyle.normal.textColor = textStyle.hover.textColor = Color.white;
                textStyle.active.textColor = textStyle.focused.textColor = Color.white;
                float maximum = 0;
                if (zoom == 1) {
                    foreach (string label in maximumLabels) maximum = Mathf.Max(maximum, font.getWidth(label));
                    bitmapScale = Mathf.Min(1, (available - 4) / Mathf.Max(1, maximum));
                    width = Mathf.Min(available, maximum + 4); height = font.getHeight() * bitmapScale + 4;
                } else {
                    textStyle.fontSize = Mathf.Max(1, font.myFont.fontSize);
                    do {
                        maximum = 0;
                        foreach (string label in maximumLabels)
                            maximum = Mathf.Max(maximum, textStyle.CalcSize(new GUIContent(label)).x);
                        if (maximum + 4 <= available || textStyle.fontSize <= 1) break;
                        textStyle.fontSize--;
                    } while (true);
                    width = Mathf.Min(available, Mathf.Ceil(maximum) + 4);
                    height = Mathf.Ceil(textStyle.CalcSize(new GUIContent("0123456789")).y) + 4;
                }
            }
            Rect badge = InventoryBag.BadgeBounds(cell, width / zoom, height / zoom);
            string text = InventoryBag.Quantity(quantity);
            Image skin = Notifications.NotificationSkin.BadgeIcon;
            Color previous = GUI.color;
            try {
                GUI.color = Color.white;
                GUI.BeginGroup(new Rect(viewport.x * zoom, viewport.y * zoom, viewport.width * zoom, viewport.height * zoom));
                try {
                    Rect pixels = new Rect((badge.x - viewport.x) * zoom, (badge.y - viewport.y) * zoom, width, height);
                    float cap = Mathf.Min(height / 2, width / 2), uvCap = Mathf.Min(.5f, skin.texture.height / (2f * skin.texture.width));
                    GUI.DrawTextureWithTexCoords(new Rect(pixels.x, pixels.y, cap, height), skin.texture, new Rect(0, 0, uvCap, 1));
                    if (width > cap * 2)
                        GUI.DrawTextureWithTexCoords(new Rect(pixels.x + cap, pixels.y, width - cap * 2, height), skin.texture,
                            new Rect(.5f - .5f / skin.texture.width, 0, 1f / skin.texture.width, 1));
                    GUI.DrawTextureWithTexCoords(new Rect(pixels.xMax - cap, pixels.y, cap, height), skin.texture, new Rect(1 - uvCap, 0, uvCap, 1));
                    if (zoom != 1) GUI.Label(pixels, text, textStyle);
                } finally { GUI.EndGroup(); }
                if (zoom == 1) {
                    Matrix4x4 matrix = GUI.matrix;
                    try {
                        GUIUtility.ScaleAroundPivot(new Vector2(bitmapScale, bitmapScale), badge.center);
                        font.drawString(g, text, (int)badge.center.x, (int)(badge.center.y - font.getHeight() / 2f), mFont.CENTER);
                    } finally { GUI.matrix = matrix; }
                }
            } finally { GUI.color = previous; }
        }
    }
}
