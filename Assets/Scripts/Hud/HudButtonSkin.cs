using UnityEngine;

namespace Hud
{
    internal static class HudButtonSkin
    {
        private static Image normalBackground, hoverBackground;
        private static Texture2D normalSource, hoverSource;
        private static Texture2D avatarNormal, avatarHover, avatarNormalSource, avatarHoverSource;

        // Imported textures need not be CPU-readable. Copy once without altering the shared asset.
        internal static Image CopyNative(Image source, System.Action<Color32[], int, int> edit)
        {
            Texture2D original = source.texture;
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(original.width, original.height, 0, RenderTextureFormat.ARGB32);
            Texture2D copy = null;
            try {
                Graphics.Blit(original, temporary);
                RenderTexture.active = temporary;
                copy = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0, 0, original.width, original.height), 0, 0);
                Color32[] pixels = copy.GetPixels32();
                edit(pixels, original.width, original.height);
                copy.SetPixels32(pixels);
                copy.filterMode = FilterMode.Point;
                copy.wrapMode = TextureWrapMode.Clamp;
                copy.hideFlags = HideFlags.DontSave;
                copy.Apply(false, true);
                Image result = new Image();
                UnityEngine.Object.Destroy(result.texture);
                result.texture = copy; result.w = original.width; result.h = original.height;
                return result;
            }
            catch { if (copy != null) UnityEngine.Object.Destroy(copy); throw; }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(temporary); }
        }

        private static Image NativeBackground(bool hover)
        {
            Image skin = hover ? GameScr.imgChatsPC2 : GameScr.imgChatPC;
            if (skin == null) return null;
            Image cached = hover ? hoverBackground : normalBackground;
            Texture2D source = hover ? hoverSource : normalSource;
            if (cached != null && source == skin.texture) return cached;
            if (cached != null) UnityEngine.Object.Destroy(cached.texture);
            cached = CopyNative(skin, (pixels, width, height) => {
                // Pixel rows are bottom-up. Preserve the complete native frame and replace only
                // the chat-glyph rectangle with clean background pixels from the same source row.
                for (int y = 0; y < height; y++) {
                    float topY = 1 - (y + 0.5f) / height;
                    if (topY < 0.28f || topY > 0.82f) continue;
                    Color32 clean = pixels[y * width + Mathf.FloorToInt(width * 0.19f)];
                    for (int x = Mathf.FloorToInt(width * 0.23f); x < Mathf.CeilToInt(width * 0.77f); x++)
                        pixels[y * width + x] = clean;
                }
            });
            if (hover) { hoverSource = skin.texture; hoverBackground = cached; }
            else { normalSource = skin.texture; normalBackground = cached; }
            return cached;
        }
        // Convert the registry rect through the active safe-area matrix, then snap its actual screen edges.
        // This is rendering conversion only; no second layout or pointer bounds are constructed here.
        internal static Rect ScreenPixels(Rect rect)
        {
            Vector3 min = GUI.matrix.MultiplyPoint3x4(new Vector3(rect.x * mGraphics.zoomLevel, rect.y * mGraphics.zoomLevel, 0));
            Vector3 max = GUI.matrix.MultiplyPoint3x4(new Vector3(rect.xMax * mGraphics.zoomLevel, rect.yMax * mGraphics.zoomLevel, 0));
            return Rect.MinMaxRect(Mathf.Round(min.x), Mathf.Round(min.y), Mathf.Round(max.x), Mathf.Round(max.y));
        }

        // Avatar has a different diameter from the native R button. Smooth only its private
        // background copy when fitting it; the head sprite and action-row textures remain Point.
        internal static void PaintBackgroundPixels(Rect pixels, bool hover)
        {
            Image background = NativeBackground(hover);
            if (background == null) return;
            Texture2D source = background.texture;
            Texture2D cached = hover ? avatarHover : avatarNormal;
            Texture2D previous = hover ? avatarHoverSource : avatarNormalSource;
            if (cached == null || previous != source) {
                if (cached != null) UnityEngine.Object.Destroy(cached);
                cached = UnityEngine.Object.Instantiate(source);
                cached.name = source.name + " (avatar background)";
                cached.filterMode = FilterMode.Bilinear;
                cached.wrapMode = TextureWrapMode.Clamp;
                cached.hideFlags = HideFlags.DontSave;
                if (hover) { avatarHover = cached; avatarHoverSource = source; }
                else { avatarNormal = cached; avatarNormalSource = source; }
            }
            GUI.color = Color.white;
            GUI.DrawTexture(pixels, cached);
        }

        internal static void PaintNativeBackground(mGraphics g, Rect rect, bool hover)
        {
            // Identical native dimensions, centering and rendering path to R, inside the 34x34 hit rect.
            g.drawImage(NativeBackground(hover), (int)rect.center.x, (int)rect.center.y, mGraphics.HCENTER | mGraphics.VCENTER);
        }
    }
}
