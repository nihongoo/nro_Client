using UnityEngine;

namespace Hud
{
    // Countdown-only glyph cache. Draw the existing game font's ink, without the
    // GUI label's ascender padding or a different fitted size for each duration.
    internal sealed class BuffCountdownText
    {
        private const string Alphabet = "0123456789ps";
        private static readonly Color Shadow = new Color(32 / 255f, 16 / 255f, 0, 1);
        private struct Glyph
        {
            internal float Left, Top, Width, Height, Advance;
            internal Rect Uv;
            internal bool Rotated;
        }
        private readonly Glyph[] glyphs = new Glyph[Alphabet.Length];
        private mFont gameFont;
        private Font font;
        private Texture texture;
        private int size, zoom, requested, digits;
        private float width, scale, top, bottom, widestDigit, widestSuffix;
        internal bool Dirty { get; private set; } = true;
        internal float Height => Mathf.Ceil((top - bottom) * scale) / zoom;

        internal BuffCountdownText() { Font.textureRebuilt += OnTextureRebuilt; }
        private void OnTextureRebuilt(Font changed) { if (changed == font) Dirty = true; }

        internal void Configure(mFont value, int pixels, float cellWidth, int maxDigits, int newZoom)
        {
            int nativeSize = value.myFont == null ? value.getHeight() : Mathf.Max(1, value.myFont.fontSize);
            bool reload = Dirty || gameFont != value || font != value.myFont || size != nativeSize || zoom != newZoom;
            if (!reload && requested == pixels && width == cellWidth && digits == maxDigits) return;
            gameFont = value; font = value.myFont; size = nativeSize; zoom = Mathf.Max(1, newZoom);
            requested = pixels; width = cellWidth; digits = Mathf.Max(2, maxDigits);
            if (reload) {
                top = float.MinValue; bottom = float.MaxValue; widestDigit = widestSuffix = 0;
                if (font != null) LoadFont(); else LoadBitmap();
                // RequestCharactersInTexture may synchronously rebuild this font.
                Dirty = false;
            }
            float widest = widestDigit * digits + widestSuffix;
            scale = Mathf.Min(pixels / (float)Mathf.Max(1, size), Mathf.Max(1, cellWidth * zoom - 2) / Mathf.Max(1, widest));
        }
        private void LoadFont()
        {
            if (font.dynamic) font.RequestCharactersInTexture(Alphabet, size, FontStyle.Normal);
            for (int i = 0; i < Alphabet.Length; i++) {
                font.GetCharacterInfo(Alphabet[i], out CharacterInfo info, size, FontStyle.Normal);
                bool rotated = info.uvBottomLeft.y != info.uvBottomRight.y;
                Rect uv = rotated
                    ? new Rect(info.uvTopLeft.x, info.uvTopLeft.y, info.uvBottomLeft.x - info.uvTopLeft.x, info.uvTopRight.y - info.uvTopLeft.y)
                    : new Rect(info.uvBottomLeft.x, info.uvBottomLeft.y, info.uvBottomRight.x - info.uvBottomLeft.x, info.uvTopLeft.y - info.uvBottomLeft.y);
                Store(i, new Glyph { Left = info.minX, Top = info.maxY, Width = info.maxX - info.minX,
                    Height = info.maxY - info.minY, Advance = info.advance, Uv = uv, Rotated = rotated });
            }
            texture = font.material.mainTexture;
        }
        private void LoadBitmap()
        {
            // Zoom 1 uses the same bitmap/table as mFont.tahoma_7_white.
            Image image = GameCanvas.loadImage("/myfont/tahoma_7_white.png");
            texture = image.texture;
            Color32[] pixels = image.texture.GetPixels32();
            DataInputStream input = MyStream.readFile("/myfont/tahoma_7");
            try {
                int count = input.readShort();
                for (int i = 0; i < count; i++) {
                    int x = input.readShort(), y = input.readShort(), w = input.readShort(), h = input.readShort();
                    int index = i < mFont.str.Length ? Alphabet.IndexOf(mFont.str[i]) : -1;
                    if (index < 0) continue;
                    if (y + h > texture.height) { y -= texture.height; x = texture.width / 2; }
                    int left = w, right = 0, upper = h, lower = 0;
                    for (int row = 0; row < h; row++) for (int col = 0; col < w; col++) {
                        if (pixels[(texture.height - 1 - y - row) * texture.width + x + col].a == 0) continue;
                        left = Mathf.Min(left, col); right = Mathf.Max(right, col + 1);
                        upper = Mathf.Min(upper, row); lower = Mathf.Max(lower, row + 1);
                    }
                    Store(index, new Glyph { Left = left, Top = -upper, Width = right - left, Height = lower - upper, Advance = w,
                        Uv = new Rect((x + left) / (float)texture.width, 1 - (y + lower) / (float)texture.height,
                            (right - left) / (float)texture.width, (lower - upper) / (float)texture.height) });
                }
            } finally { input.close(); }
        }
        private void Store(int index, Glyph glyph)
        {
            glyphs[index] = glyph;
            top = Mathf.Max(top, glyph.Top); bottom = Mathf.Min(bottom, glyph.Top - glyph.Height);
            if (index < 10) widestDigit = Mathf.Max(widestDigit, Mathf.Max(glyph.Advance, glyph.Left + glyph.Width));
            else widestSuffix = Mathf.Max(widestSuffix, Mathf.Max(glyph.Advance, glyph.Left + glyph.Width));
        }
        internal void Paint(Rect cell, string text, float gap)
        {
            float advance = 0, left = float.MaxValue, right = float.MinValue;
            for (int i = 0; i < text.Length; i++) {
                Glyph glyph = glyphs[Alphabet.IndexOf(text[i])];
                left = Mathf.Min(left, advance + glyph.Left); right = Mathf.Max(right, advance + glyph.Left + glyph.Width);
                advance += glyph.Advance;
            }
            float x = Mathf.Round(cell.center.x * zoom - (right + left) * scale / 2);
            float baseline = Mathf.Ceil((cell.yMax + gap) * zoom) + top * scale;
            Color previous = GUI.color;
            try {
                // One shadow pass keeps the ink readable without four more draws per glyph.
                for (int dy = 1; dy >= 0; dy--) {
                    GUI.color = dy == 0 ? Color.white : Shadow;
                    float pen = x;
                    for (int i = 0; i < text.Length; i++) {
                        Glyph glyph = glyphs[Alphabet.IndexOf(text[i])];
                        Rect ink = new Rect(pen + glyph.Left * scale, baseline - glyph.Top * scale + dy,
                            glyph.Width * scale, glyph.Height * scale);
                        DrawGlyph(ink, glyph); pen += glyph.Advance * scale;
                    }
                }
            } finally { GUI.color = previous; }
        }
        private void DrawGlyph(Rect ink, Glyph glyph)
        {
            if (!glyph.Rotated) { GUI.DrawTextureWithTexCoords(ink, texture, glyph.Uv); return; }
            Matrix4x4 previous = GUI.matrix;
            try {
                GUIUtility.RotateAroundPivot(90, ink.center);
                GUI.DrawTextureWithTexCoords(new Rect(ink.center.x - ink.height / 2, ink.center.y - ink.width / 2,
                    ink.height, ink.width), texture, glyph.Uv);
            } finally { GUI.matrix = previous; }
        }
    }
}
