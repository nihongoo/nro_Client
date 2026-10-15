using UnityEngine;

namespace Hud
{
    internal static class TopInfoHud
    {
        private const string Root = "top-info.bounds", Avatar = "top-info.avatar", Hp = "top-info.hp",
            Ki = "top-info.ki", Exp = "top-info.exp", Fps = "top-info.fps";
        private const int Order = 5;
        private static bool pressed, captured;
        private static Rect avatar, hp, ki, exp, fps;
        private static string lastOverflow;
        private static Image expSkin;
        private static int skinZoom;
        private static Texture2D kiSource, kiSmooth, expSource, expSmooth;
        private static Image ExpSkin {
            get {
                if (expSkin == null || skinZoom != mGraphics.zoomLevel) {
                    expSkin = GameCanvas.loadImage("/mainImage/SP.png");
                    skinZoom = mGraphics.zoomLevel;
                }
                return expSkin;
            }
        }
        private static int DesignUnit(float pixels) => Mathf.RoundToInt(HudLayout.PixelsToLogical(pixels));
        private static bool Visible => GameScr.instance != null && GameCanvas.currentScreen == GameScr.instance
            && GameScr.isPaint && !GameScr.isPaintOther && GameScr.isPaintRada == 1 && !GameCanvas.panel.isShow;

        internal static void Register()
        {
            // Only ResolveBounds computes placement. All consumers use slices of this cached provider.
            HudLayout.Register(Root, ResolveBounds, Order, () => false, false);
            HudLayout.Register(Avatar, () => PartBounds(Avatar), Order, () => Visible);
            HudLayout.Register(Hp, () => PartBounds(Hp), Order, () => Visible, false);
            HudLayout.Register(Ki, () => PartBounds(Ki), Order, () => Visible, false);
            HudLayout.Register(Exp, () => PartBounds(Exp), Order, () => Visible, false);
            HudLayout.Register(Fps, () => PartBounds(Fps), Order, () => Visible, false);
        }

        private static Rect ResolveBounds()
        {
            int gap = Mathf.Max(1, DesignUnit(6)), rowGap = Mathf.Max(1, DesignUnit(1));
            int rowHeight = Mathf.Max(Mathf.CeilToInt(HudLayout.PixelsToLogical(11)), mFont.tahoma_7_white.getHeight());
            int diameter = rowHeight * 3 + rowGap * 2;
            avatar = HudLayout.Anchor(HorizontalAnchor.Left, VerticalAnchor.Top, new Vector2(diameter, diameter),
                new Vector2(DesignUnit(HudLayout.IsMobileLayout ? 6 : 12), DesignUnit(6)));
            float left = avatar.xMax + gap;
            // Gear/chest keep their existing placement. Reserve the earliest fixed button edge.
            float right = Mathf.Min(GameCanvas.w - gap, GameScr.LegacyInfoPanelWidth + 5 - gap);
            Rect chat = GameScr.ChatButtonBounds;
            if (chat.yMin < avatar.yMax && chat.yMax > avatar.yMin) right = Mathf.Min(right, chat.xMin - gap);
            int width = Mathf.FloorToInt(Mathf.Min(DesignUnit(HudLayout.IsMobileLayout ? 210 : 250), right - left));
            int fpsWidth = mFont.tahoma_7_white.getWidth("FPS: 999") + 2;
            if (width < fpsWidth + gap + 3) {
                avatar = hp = ki = exp = fps = new Rect();
                return new Rect(); // An unsupported tiny viewport must not draw underneath fixed buttons.
            }
            hp = new Rect(left, avatar.y, width, rowHeight);
            ki = new Rect(left, hp.yMax + rowGap, width, rowHeight);
            fps = new Rect(hp.xMax - fpsWidth, ki.yMax + rowGap, fpsWidth, rowHeight);
            exp = new Rect(left, fps.y, width - fpsWidth - gap, rowHeight);
            return Rect.MinMaxRect(avatar.x, avatar.y, hp.xMax, avatar.yMax);
        }

        private static Rect PartBounds(string name)
        {
            HudLayout.GetRect(Root);
            switch (name) {
                case Avatar: return avatar;
                case Hp: return hp;
                case Ki: return ki;
                case Exp: return exp;
                default: return fps;
            }
        }

        // Called before movement/touch dispatch, independently of legacy isPC/isTouch flags.
        internal static bool UpdateInput()
        {
            if (!Visible || GameScr.instance.popUpYesNo != null || GameScr.instance.isPaintPopup()) {
                pressed = captured = false;
                return false;
            }
            bool over = HudLayout.IsTopmost(Avatar, new Vector2(GameCanvas.px, GameCanvas.py));
            if (HudLayout.HitTest(new Vector2(GameCanvas.px, GameCanvas.py)) == "legacy.overlay") {
                pressed = captured = false;
                return false;
            }
            if (GameCanvas.isPointerJustDown) pressed = captured = over;
            if (!over) pressed = false;
            if (GameCanvas.isPointerJustRelease) {
                bool activate = pressed && over && GameCanvas.isPointerClick;
                bool consume = captured;
                pressed = captured = false;
                if (!consume) return false;
                GameCanvas.clearAllPointerEvent();
                if (!activate) return true;
                Char.myCharz().currentMovePoint = null;
                GameScr.instance.cmdMenu?.performAction();
                SoundMn.gI().buttonClick();
                return true;
            }
            if (!captured || !(GameCanvas.isPointerDown || GameCanvas.isPointerJustDown)) return false;
            GameCanvas.isPointerJustDown = false;
            GameScr.instance.isPointerDowning = false;
            return true;
        }

        internal static void Paint(mGraphics g)
        {
            if (!Visible) return;
            Char c = Char.myCharz();
            int tx = g.getTranslateX(), ty = g.getTranslateY() - mGraphics.addYWhenOpenKeyBoard;
            g.translate(-tx, -ty);
            try {
                HudLayout.ForEachBackToFront((name, rect) => {
                    if (name == Avatar) PaintAvatar(rect, c);
                    else if (name == Hp) PaintBar(g, rect, GameScr.imgHP?.texture, Fraction(c.cHP, c.cHPFull), "HP " + NinjaUtil.getMoneys(c.cHP));
                    else if (name == Ki) PaintBar(g, rect, SmoothTexture(GameScr.imgMP, ref kiSource, ref kiSmooth), Fraction(c.cMP, c.cMPFull), "KI " + NinjaUtil.getMoneys(c.cMP));
                    else if (name == Exp) {
                        string power = NinjaUtil.getMoneys(c.cPower);
                        string overflow = mFont.tahoma_7_white.getWidth(power) > rect.width - 2 ? rect.width.ToString() : null;
                        if (overflow != null && overflow != lastOverflow)
                            Debug.LogWarning("HUD: so suc manh vuot chieu rong thanh EXP (" + power + " @ " + overflow + "). Giu nguyen font; can user duyet do rong.");
                        lastOverflow = overflow;
                        PaintBar(g, rect, SmoothTexture(ExpSkin, ref expSource, ref expSmooth), Fraction(c.cLevelPercent, 10000), power);
                    }
                    else if (name == Fps) {
                        g.setClip((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
                        mFont.tahoma_7_white.drawString(g, "FPS: " + Mod.CuongLe.MainMod.CalculateFPS(), (int)rect.xMax - 1,
                            (int)rect.y + ((int)rect.height - mFont.tahoma_7_white.getHeight()) / 2, mFont.RIGHT);
                        g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
                    }
                });
            }
            finally {
                g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
                g.translate(tx, ty);
            }
        }

        private static float Fraction(long value, long max) => max <= 0 ? 0 : Mathf.Clamp01((float)((double)value / max));

        // Convert the registry rect through the active safe-area matrix, then snap its actual screen edges.
        // This is rendering conversion only; no second layout or pointer bounds are constructed here.
        private static Rect ScreenPixels(Rect rect)
        {
            Vector3 min = GUI.matrix.MultiplyPoint3x4(new Vector3(rect.x * mGraphics.zoomLevel, rect.y * mGraphics.zoomLevel, 0));
            Vector3 max = GUI.matrix.MultiplyPoint3x4(new Vector3(rect.xMax * mGraphics.zoomLevel, rect.yMax * mGraphics.zoomLevel, 0));
            return Rect.MinMaxRect(Mathf.Round(min.x), Mathf.Round(min.y), Mathf.Round(max.x), Mathf.Round(max.y));
        }

        private static void Solid(Rect pixels, Color color)
        {
            if (pixels.width <= 0 || pixels.height <= 0) return;
            GUI.color = color;
            GUI.DrawTexture(pixels, Texture2D.whiteTexture);
        }

        // Three slices retain the original rounded caps/highlight; only the middle expands in width.
        private static void Capsule(Rect pixels, Texture2D texture, Color tint)
        {
            if (texture == null || pixels.width <= 0 || pixels.height <= 0) return;
            float sourceCap = Mathf.Min(texture.height / 2f, texture.width / 2f);
            float cap = Mathf.Min(Mathf.Round(pixels.height * sourceCap / texture.height), Mathf.Floor(pixels.width / 2));
            float capUv = sourceCap / texture.width;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(new Rect(pixels.x, pixels.y, cap, pixels.height), texture, new Rect(0, 0, capUv, 1));
            if (pixels.width > cap * 2)
                GUI.DrawTextureWithTexCoords(new Rect(pixels.x + cap, pixels.y, pixels.width - cap * 2, pixels.height),
                    texture, new Rect(capUv, 0, 1 - capUv * 2, 1));
            GUI.DrawTextureWithTexCoords(new Rect(pixels.xMax - cap, pixels.y, cap, pixels.height), texture, new Rect(1 - capUv, 0, capUv, 1));
        }

        // The lower bars shrink different source heights to the same row. Filter private copies only;
        // shared game textures (including HP) retain their existing Point sampling.
        private static Texture2D SmoothTexture(Image skin, ref Texture2D source, ref Texture2D copy)
        {
            Texture2D current = skin?.texture;
            if (source == current && copy != null) return copy;
            if (copy != null) UnityEngine.Object.Destroy(copy);
            source = current;
            copy = current == null ? null : UnityEngine.Object.Instantiate(current);
            if (copy != null) {
                copy.name = current.name + " (top-info smooth)";
                copy.hideFlags = HideFlags.DontSave;
                copy.filterMode = FilterMode.Bilinear;
                copy.wrapMode = TextureWrapMode.Clamp;
            }
            return copy;
        }

        private static void PaintBar(mGraphics g, Rect rect, Texture2D skin, float fraction, string text)
        {
            Rect pixels = ScreenPixels(rect);
            Matrix4x4 matrix = GUI.matrix;
            Color color = GUI.color;
            try {
                GUI.matrix = Matrix4x4.identity;
                if (skin != null) {
                    Capsule(pixels, skin, new Color(0.25f, 0.25f, 0.25f, 1));
                    Rect filled = pixels;
                    filled.width = Mathf.Round(pixels.width * fraction);
                    Capsule(filled, skin, Color.white);
                }
            }
            finally { GUI.matrix = matrix; GUI.color = color; }
            g.setClip((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            mFont.tahoma_7_white.drawString(g, text, (int)rect.center.x,
                (int)rect.y + ((int)rect.height - mFont.tahoma_7_white.getHeight()) / 2, mFont.CENTER);
            g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
        }

        // Reuse the same atlas/cache/request lifecycle as SmallImage, without resampling its pixels.
        private static bool TryHeadSprite(int id, out Texture2D texture, out Rect source)
        {
            texture = null; source = new Rect();
            if (SmallImage.imgbig != null && SmallImage.smallImg != null && id >= 0 && id < SmallImage.smallImg.Length) {
                int[] region = SmallImage.smallImg[id];
                if (region != null && region.Length >= 5 && region[1] < 256 && region[2] < 256 && region[3] < 256 && region[4] < 256
                    && region[0] >= 0 && region[0] < SmallImage.imgbig.Length && SmallImage.imgbig[region[0]] != null) {
                    texture = SmallImage.imgbig[region[0]].texture;
                    source = new Rect(region[1] * mGraphics.zoomLevel, region[2] * mGraphics.zoomLevel,
                        region[3] * mGraphics.zoomLevel, region[4] * mGraphics.zoomLevel);
                    return texture != null && source.width > 0 && source.height > 0;
                }
            }
            if (SmallImage.imgNew == null || id < 0 || id >= SmallImage.imgNew.Length) return false;
            var small = SmallImage.imgNew[id];
            if (small == null) { SmallImage.createImage(id); return false; }
            if (small.img == null || small.img.texture == null) return false;
            if (GameCanvas.gameTick % 1000 == 0) { small.timePaint++; small.timeUpdate = small.timePaint; }
            texture = small.img.texture;
            source = new Rect(0, 0, small.img.getRealImageWidth(), small.img.getRealImageHeight());
            return source.width > 1 && source.height > 1;
        }

        // Sample the R button's original border/background, excluding its central chat glyph.
        private static void ChatBackground(Rect pixels, bool hover)
        {
            Texture2D texture = (hover ? GameScr.imgChatsPC2 : GameScr.imgChatPC)?.texture;
            if (texture == null) return;
            GUI.color = Color.white;
            for (int row = 0; row < (int)pixels.height; row++) {
                float v = 1 - (row + 1f) / pixels.height;
                float heightUv = 1f / pixels.height;
                Rect line = new Rect(pixels.x, pixels.y + row, pixels.width, 1);
                float sourceY = (row + 0.5f) / pixels.height;
                if (sourceY < 0.28f || sourceY > 0.82f) {
                    GUI.DrawTextureWithTexCoords(line, texture, new Rect(0, v, 1, heightUv));
                    continue;
                }
                float edge = Mathf.Round(pixels.width * 0.23f);
                GUI.DrawTextureWithTexCoords(new Rect(line.x, line.y, edge, 1), texture, new Rect(0, v, 0.23f, heightUv));
                GUI.DrawTextureWithTexCoords(new Rect(line.x + edge, line.y, line.width - edge * 2, 1), texture,
                    new Rect(0.19f, v, 1f / texture.width, heightUv));
                GUI.DrawTextureWithTexCoords(new Rect(line.xMax - edge, line.y, edge, 1), texture, new Rect(0.77f, v, 0.23f, heightUv));
            }
        }

        private static void PaintAvatar(Rect rect, Char c)
        {
            Rect pixels = ScreenPixels(rect);
            int diameter = (int)Mathf.Min(pixels.width, pixels.height);
            pixels = new Rect(Mathf.Round(pixels.center.x - diameter / 2f), Mathf.Round(pixels.center.y - diameter / 2f), diameter, diameter);
            Part head = GameScr.parts[c.head];
            bool hasHead = TryHeadSprite(head.pi[Char.CharInfo[0][0][0]].id, out Texture2D texture, out Rect source);
            int scale = hasHead ? Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(diameter / source.width, diameter / source.height))) : 1;
            Rect portrait = hasHead ? new Rect(Mathf.Round(pixels.center.x - source.width * scale / 2),
                Mathf.Round(pixels.center.y - source.height * scale / 2), source.width * scale, source.height * scale) : new Rect();
            float radius = diameter / 2f;
            bool hover = HudLayout.IsTopmost(Avatar, new Vector2(GameCanvas.pxMouse, GameCanvas.pyMouse));
            Matrix4x4 matrix = GUI.matrix;
            Color color = GUI.color;
            try {
                GUI.matrix = Matrix4x4.identity;
                ChatBackground(pixels, hover);
                for (int row = 0; row < diameter; row++) {
                    float dy = row + 0.5f - radius;
                    float half = Mathf.Sqrt(Mathf.Max(0, radius * radius - dy * dy));
                    float left = Mathf.Ceil(pixels.center.x - half), right = Mathf.Floor(pixels.center.x + half);
                    Rect line = new Rect(left, pixels.y + row, right - left, 1);
                    if (!hasHead || line.y < portrait.y || line.y >= portrait.yMax) continue;
                    float start = Mathf.Max(line.x, portrait.x), end = Mathf.Min(line.xMax, portrait.xMax);
                    if (end <= start) continue;
                    Rect uv = new Rect((source.x + (start - portrait.x) / scale) / texture.width,
                        (texture.height - source.y - (line.y - portrait.y + 1) / scale) / texture.height,
                        (end - start) / scale / texture.width, 1f / scale / texture.height);
                    GUI.color = Color.white;
                    GUI.DrawTextureWithTexCoords(new Rect(start, line.y, end - start, 1), texture, uv);
                }
                if (GameScr.isNewClanMessage) {
                    float x = pixels.xMax - 6, y = pixels.y + 1;
                    Solid(new Rect(x + 1, y, 3, 1), Color.red);
                    Solid(new Rect(x, y + 1, 5, 3), Color.red);
                    Solid(new Rect(x + 1, y + 4, 3, 1), Color.red);
                }
            }
            finally { GUI.matrix = matrix; GUI.color = color; }
        }
    }
}
