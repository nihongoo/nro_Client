using UnityEngine;

namespace Hud
{
    internal static class HudButtonSkin
    {
        private static Image normalBackground, hoverBackground;
        private static Texture2D normalSource, hoverSource;
        private static Texture2D avatarNormal, avatarHover, avatarNormalSource, avatarHoverSource;

        // Imported textures need not be CPU-readable. Copy once without altering the shared asset.
        internal static Image CopyNative(Image source, System.Action<Color32[], int, int> edit = null, FilterMode filter = FilterMode.Point)
            => CopyNative(source.texture, edit, 0, filter);

        internal static Image CopyNative(Texture2D source, System.Action<Color32[], int, int> edit,
            int squareSize, FilterMode filter = FilterMode.Point)
        {
            Texture2D copy = CopyNativeTexture(source, edit, filter, squareSize);
            Image result = new Image();
            UnityEngine.Object.Destroy(result.texture);
            result.texture = copy; result.w = copy.width; result.h = copy.height;
            return result;
        }

        internal static Texture2D CopyNativeTexture(Texture original, System.Action<Color32[], int, int> edit,
            FilterMode filter = FilterMode.Point, int squareSize = 0)
        {
            int width = squareSize > 0 ? squareSize : original.width;
            int height = squareSize > 0 ? squareSize : original.height;
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Texture2D copy = null;
            try {
                Graphics.Blit(original, temporary);
                RenderTexture.active = temporary;
                copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                if (edit != null) {
                    Color32[] pixels = copy.GetPixels32();
                    edit(pixels, width, height);
                    copy.SetPixels32(pixels);
                }
                copy.filterMode = filter;
                copy.wrapMode = TextureWrapMode.Clamp;
                copy.hideFlags = HideFlags.DontSave;
                copy.Apply(false, true);
                return copy;
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
                // The 31-unit chat glyph narrows to a tail near the lower rim. A wide mask
                // there would sample the rim and copy it across the button as a horizontal line.
                for (int topY = 9 * height / 31; topY < 25 * height / 31; topY++) {
                    bool tail = topY >= 21 * height / 31;
                    int left = (tail ? 15 : 6) * width / 31;
                    int right = (tail ? 23 : 25) * width / 31;
                    int row = (height - 1 - topY) * width; // Texture pixels are bottom-up.
                    Color32 before = pixels[row + left - 1], after = pixels[row + right];
                    for (int x = left; x < right; x++) {
                        float t = (float)(x - left + 1) / (right - left + 1);
                        pixels[row + x] = Color32.Lerp(before, after, t);
                    }
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
                // Explicit RGBA upload also works for the generated non-readable background.
                // Do not clone the platform-dependent GPU texture through Instantiate.
                cached = CopyNative(background, filter: FilterMode.Bilinear).texture;
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
            g.drawImage(NativeBackground(hover), (int)rect.x, (int)rect.y);
        }
    }
}
