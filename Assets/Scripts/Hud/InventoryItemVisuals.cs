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

        internal static Rect StarBounds(Rect cell, int index, int count)
        {
            if (count <= 0) return new Rect();
            float size = System.Math.Min(5, (cell.width - 2) / count);
            return new Rect(cell.center.x - count * size / 2 + index * size, cell.yMax + 3.5f, size, size);
        }
        internal const float LevelHeight = 12;
        internal static Rect LevelBounds(Rect cell, float width, bool bag)
        {
            width = bag ? System.Math.Min(width, cell.width - 4) : width;
            return new Rect(bag ? cell.x - 3 : cell.xMax - width + 3, bag ? cell.y - 3 : cell.yMax - 9, width, LevelHeight);
        }

        internal static Rect CompactLevelBounds(Rect cell,float width)
            => LevelBounds(cell,System.Math.Min(width,cell.width),false);

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
        internal static void PaintStars(Rect cell, Rect viewport, Item item, Rect? strip = null)
        {
            int count = InventoryFrame.StarSlots(item);
            if (strip.HasValue) count = Mathf.Min(count, 8);
            if (count == 0) return;
            int zoom = Mathf.Max(1, mGraphics.zoomLevel);
            Color previous = GUI.color;
            try {
                GUI.color = Color.white;
                GUI.BeginGroup(new Rect(viewport.x * zoom, viewport.y * zoom, viewport.width * zoom, viewport.height * zoom));
                try {
                    for (int i = 0; i < count; i++) {
                        int kind = InventoryFrame.StarKind(item, i);
                        Image image = kind == 1 ? Panel.imgStar : kind == 2 ? (Panel.imgStar8 ?? Panel.imgStar)
                            : kind == 3 ? (Panel.imgStarCuongHoa ?? Panel.imgMaxStar) : Panel.imgMaxStar;
                        if (image == null) continue;
                        Rect star;
                        if (strip is Rect row) {
                            float size = Mathf.Min(row.height, row.width / count);
                            star = new Rect(row.x + i * size, row.yMax - size, size, size);
                        } else star = StarBounds(cell, i, count);
                        GUI.DrawTexture(new Rect((star.x - viewport.x) * zoom, (star.y - viewport.y) * zoom,
                            star.width * zoom, star.height * zoom), image.texture);
                    }
                } finally { GUI.EndGroup(); }
            } finally { GUI.color = previous; }
        }
        private static readonly Dictionary<mFont, GUIStyle> compactStyles = new Dictionary<mFont, GUIStyle>();
        private sealed class CompactFit {
            internal GUIContent Content;internal float Width,Height;internal int BaseSize,FontSize,Zoom;internal Font Font;
        }
        private static readonly Dictionary<mFont,Dictionary<string,CompactFit>> compactFits=new Dictionary<mFont,Dictionary<string,CompactFit>>();
        // Keep the game's font, fitting the complete value instead of dropping digits with ellipses.
        internal static void PaintCompactText(mGraphics g,mFont font,string text,Rect bounds,Rect viewport,int color,bool center=false,float fontScale=1,bool topAligned=false)
        {
            Rect clip=InventoryFrame.Intersection(bounds,viewport);if(clip.width<=0||clip.height<=0)return;
            if(!compactStyles.TryGetValue(font,out GUIStyle style)) {
                style=new GUIStyle(GUI.skin.label){font=font.myFont,alignment=TextAnchor.MiddleLeft,
                    padding=new RectOffset(),margin=new RectOffset(),wordWrap=false,richText=false};
                compactStyles.Add(font,style);
            }
            style.font=font.myFont;
            style.alignment=center?(topAligned?TextAnchor.UpperCenter:TextAnchor.MiddleCenter):TextAnchor.MiddleLeft;
            int zoom=Mathf.Max(1,mGraphics.zoomLevel);
            if(!compactFits.TryGetValue(font,out Dictionary<string,CompactFit> fits)) {
                fits=new Dictionary<string,CompactFit>();compactFits.Add(font,fits);
            }
            if(!fits.TryGetValue(text,out CompactFit fit)) {
                if(fits.Count>=256)fits.Clear();

                fit=new CompactFit{Content=new GUIContent(text)};fits.Add(text,fit);
            }
            int baseSize=Mathf.Max(1,Mathf.RoundToInt(font.myFont.fontSize*fontScale));
            if(fit.Font!=font.myFont||fit.BaseSize!=baseSize||fit.Width!=bounds.width||fit.Height!=bounds.height||fit.Zoom!=zoom) {
                style.fontSize=baseSize;
                while(style.fontSize>1) {
                    Vector2 measured=style.CalcSize(fit.Content);
                    if(measured.x<=(bounds.width-2)*zoom&&measured.y<=bounds.height*zoom)break;
                    style.fontSize--;
                }
                fit.Font=font.myFont;fit.BaseSize=baseSize;fit.Width=bounds.width;fit.Height=bounds.height;fit.Zoom=zoom;fit.FontSize=style.fontSize;
            }
            style.fontSize=fit.FontSize;
            Color ink=new Color((color>>16&255)/255f,(color>>8&255)/255f,(color&255)/255f,1);
            style.normal.textColor=style.hover.textColor=style.active.textColor=style.focused.textColor=ink;
            style.onNormal.textColor=style.onHover.textColor=style.onActive.textColor=style.onFocused.textColor=ink;
            Color previous=GUI.color;
            try {
                GUI.color=Color.white;
                GUI.BeginGroup(new Rect(clip.x*zoom,clip.y*zoom,clip.width*zoom,clip.height*zoom));
                try {GUI.Label(new Rect((bounds.x-clip.x+1)*zoom,(bounds.y-clip.y)*zoom,(bounds.width-2)*zoom,bounds.height*zoom),text,style);}
                finally {GUI.EndGroup();}
            } finally {GUI.color=previous;}
    }
        // Fit native bitmap glyphs at one common size for every label in the row.
        internal sealed class FittedTextRow
        {
            private GUIStyle style;
            private readonly GUIContent[] content = new GUIContent[6];
            private readonly Vector2[] sizes = new Vector2[6];
            private readonly bool clipToViewport;
            private Font font;
            private int zoom, baseSize, count;
            private float textScale = 1;
            internal const float Padding = 4;

            internal FittedTextRow(bool clipToViewport = false) { this.clipToViewport = clipToViewport; }

            internal void Paint(mFont nativeFont, string[] labels, Rect[] bounds, int length,
                Rect viewport, int selected, Color normalColor, Color selectedColor)
            {
                if (style == null) style = new GUIStyle(GUI.skin.label) {
                    alignment = TextAnchor.MiddleCenter, padding = new RectOffset(), margin = new RectOffset(),
                    wordWrap = false, richText = false, fixedWidth = 0, fixedHeight = 0
                };
                if (length == 0 || viewport.width <= 0 || viewport.height <= 0) return;
                Font currentFont = nativeFont.myFont ?? GUI.skin.font;
                int scale = Mathf.Max(1, mGraphics.zoomLevel), size = Mathf.Max(1, currentFont?.fontSize ?? 12);
                bool dirty = font != currentFont || zoom != scale || baseSize != size || count != length;
                for (int i = 0; i < length; i++) {
                    Vector2 available = new Vector2(Mathf.Max(0, bounds[i].width * scale - 2 * Padding),
                        Mathf.Max(0, bounds[i].height * scale - 4));
                    if (content[i] == null || content[i].text != labels[i] || sizes[i] != available) dirty = true;
                    if (content[i] == null) content[i] = new GUIContent(labels[i]); else content[i].text = labels[i];
                    sizes[i] = available;
                }
                style.font = currentFont;
                if (dirty) {
                    style.fontSize = size;
                    // Bitmap fonts (notably x4) ignore GUIStyle.fontSize. Scale their native
                    // glyphs rather than changing fontSize; tabs share the smallest fit.
                    textScale = 1;
                    for (int i = 0; i < length; i++) {
                        Vector2 measured = style.CalcSize(content[i]);
                        float fit = 1;
                        if (measured.x > 0) fit = Mathf.Min(fit, Mathf.Max(0, sizes[i].x - 1) / measured.x);
                        if (measured.y > 0) fit = Mathf.Min(fit, Mathf.Max(0, sizes[i].y - 1) / measured.y);
                        textScale = Mathf.Min(textScale, Mathf.Max(.01f, fit));
                    }
                    font = currentFont; zoom = scale; baseSize = size; count = length;
                }
                Color previous = GUI.color;
                try {
                    GUI.color = Color.white;
                    for (int i = 0; i < length; i++) {
                        Rect inset = new Rect(bounds[i].x + Padding / scale, bounds[i].y + 2f / scale,
                            sizes[i].x / scale, sizes[i].y / scale);
                        Rect clip = InventoryFrame.Intersection(inset, viewport);
                        if (clip.width <= 0 || clip.height <= 0) continue;
                        Color ink = i == selected ? selectedColor : normalColor;
                        style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = ink;
                        style.onNormal.textColor = style.onHover.textColor = style.onActive.textColor = style.onFocused.textColor = ink;
                        Matrix4x4 matrix = GUI.matrix;
                        Vector2 center = inset.center * scale;
                        try {
                            GUIUtility.ScaleAroundPivot(new Vector2(textScale, textScale), center);
                            float pixelsPerUnit = scale / textScale;
                            float width = inset.width * pixelsPerUnit, height = inset.height * pixelsPerUnit;
                            Rect label = new Rect(center.x - width / 2, center.y - height / 2, width, height);
                            if (!clipToViewport || (clip.width >= inset.width - .01f && clip.height >= inset.height - .01f))
                                GUI.Label(label, content[i], style);
                            else {
                                // Convert the clip to the scaled label's coordinate space.
                                Rect group = new Rect(center.x + (clip.x - inset.center.x) * pixelsPerUnit,
                                    center.y + (clip.y - inset.center.y) * pixelsPerUnit,
                                    clip.width * pixelsPerUnit, clip.height * pixelsPerUnit);
                                GUI.BeginGroup(group);
                                try { GUI.Label(new Rect(label.x - group.x, label.y - group.y, label.width, label.height), content[i], style); }
                                finally { GUI.EndGroup(); }
                            }
                        }
                        finally { GUI.matrix = matrix; }
                    }
                } finally { GUI.color = previous; }
            }
        }
        // Match SmallImage.drawSmallImage: entries outside the 256 atlas are separate images.
        internal static bool TryAtlasIcon(int id,out Image image,out Rect uv)
        {
            image=null;uv=new Rect(0,0,1,1);
            if(id<0||SmallImage.imgbig==null||SmallImage.smallImg==null||id>=SmallImage.smallImg.Length)return false;
            int[] data=SmallImage.smallImg[id];
            if(data==null||data.Length<5||data[0]<0||data[0]>=SmallImage.imgbig.Length
                ||data[1]<0||data[2]<0||data[1]>=256||data[2]>=256||data[3]<=0||data[4]<=0||data[3]>=256||data[4]>=256)return false;
            image=SmallImage.imgbig[data[0]];if(image?.texture==null)return false;
            int zoom=Mathf.Max(1,mGraphics.zoomLevel);
            float x=data[1]*zoom,y=data[2]*zoom,w=data[3]*zoom,h=data[4]*zoom;
            if(x+w>image.texture.width||y+h>image.texture.height){image=null;return false;}
            uv=new Rect(x/image.texture.width,1-(y+h)/image.texture.height,w/image.texture.width,h/image.texture.height);
            return true;
        }
        internal static void PaintCompactIcon(mGraphics g,int id,Rect bounds,Rect viewport)
        {
            Rect clip=InventoryFrame.Intersection(bounds,viewport);if(clip.width<=0||clip.height<=0)return;
            Image image;Rect uv;
            if(!TryAtlasIcon(id,out image,out uv)) {
                if(id>=0&&SmallImage.imgNew!=null&&id<SmallImage.imgNew.Length) {
                    if(SmallImage.imgNew[id]==null)SmallImage.createImage(id);
                    image=SmallImage.imgNew[id]?.img;
                }
                uv=new Rect(0,0,1,1);
            }
            if(image?.texture==null||image==SmallImage.imgEmpty||image.texture.width<=1||image.texture.height<=1) {
                InventoryFrame.Clip(g,clip);
                InventoryFrame.Rounded(g,bounds,InventoryFrame.SlotBackground,255,InventoryFrame.Accent);
                PaintCompactText(g,mFont.tahoma_7b_dark,"?",bounds,viewport,InventoryFrame.Border,true);
                return;
            }
            float aspect=image.texture.width*uv.width/(image.texture.height*uv.height);
            float width=Mathf.Min(bounds.width,bounds.height*aspect),height=width/aspect;
            int zoom=Mathf.Max(1,mGraphics.zoomLevel);Color previous=GUI.color;
            try {
                GUI.color=Color.white;GUI.BeginGroup(new Rect(clip.x*zoom,clip.y*zoom,clip.width*zoom,clip.height*zoom));
                try {GUI.DrawTextureWithTexCoords(new Rect((bounds.center.x-width/2-clip.x)*zoom,(bounds.center.y-height/2-clip.y)*zoom,width*zoom,height*zoom),image.texture,uv);}
                finally {GUI.EndGroup();}
            } finally {GUI.color=previous;}
        }
        // The native skill tile has a 4-pixel rim on a 28-pixel cell.
        private const float SkillFrameInset = 4f / 28;
        internal static void PaintSkillIcon(mGraphics g, int id, Rect bounds, Rect viewport, bool selected = false, bool frameOnTop = false)
        {
            Rect clip = InventoryFrame.Intersection(bounds, viewport);
            if (clip.width <= 0 || clip.height <= 0) return;
            Texture2D frame = (selected ? GameScr.imgSkill2 : GameScr.imgSkill)?.texture;
            if (frame != null) PaintSkillFrame(frame, bounds, clip, false);
            else {
                InventoryFrame.Clip(g, clip);
                InventoryFrame.Rounded(g, bounds, InventoryFrame.SlotBackground, 255, selected ? 0x00C86A : 0xFF9900);
            }
            PaintCompactIcon(g, id, InventoryFrame.Inset(bounds, bounds.width * SkillFrameInset), viewport);
            if (frameOnTop && frame != null) PaintSkillFrame(frame, bounds, clip, true);
        }
        private static void PaintSkillFrame(Texture2D frame, Rect bounds, Rect clip, bool rimOnly)
        {
            int zoom = Mathf.Max(1, mGraphics.zoomLevel);
            Rect pixels = new Rect((bounds.x - clip.x) * zoom, (bounds.y - clip.y) * zoom, bounds.width * zoom, bounds.height * zoom);
            Color previous = GUI.color;
            try {
                GUI.color = Color.white;
                GUI.BeginGroup(new Rect(clip.x * zoom, clip.y * zoom, clip.width * zoom, clip.height * zoom));
                try {
                    if (!rimOnly) GUI.DrawTexture(pixels, frame);
                    else {
                        // The sprite's center is opaque. Draw only its rim above the icon.
                        const float rim = SkillFrameInset, middle = 1 - 2 * rim;
                        DrawRim(0, 0, 1, rim);
                        DrawRim(0, 1 - rim, 1, rim);
                        DrawRim(0, rim, rim, middle);
                        DrawRim(1 - rim, rim, rim, middle);
                    }
                } finally { GUI.EndGroup(); }
            } finally { GUI.color = previous; }

            void DrawRim(float x, float y, float width, float height)
            {
                GUI.DrawTextureWithTexCoords(
                    new Rect(pixels.x + x * pixels.width, pixels.y + y * pixels.height, width * pixels.width, height * pixels.height),
                    frame, new Rect(x, 1 - y - height, width, height));
            }
        }
        private static readonly Dictionary<Texture2D,Image> disabledButtons=new Dictionary<Texture2D,Image>();
        private static readonly Dictionary<Texture2D,Image> smoothButtons=new Dictionary<Texture2D,Image>();
        private static readonly Dictionary<Texture2D,Image> smoothDisabledButtons=new Dictionary<Texture2D,Image>();
        private static Image ButtonPart(Image source,bool disabled,bool smooth)
        {
            if(source?.texture==null||(!disabled&&!smooth))return source;
            var cache=smooth?(disabled?smoothDisabledButtons:smoothButtons):disabledButtons;
            if(!cache.TryGetValue(source.texture,out Image grey)) {
                grey=HudButtonSkin.CopyNative(source,(pixels,width,height)=>{
                    if(disabled)for(int i=0;i<pixels.Length;i++) {Color32 p=pixels[i];byte value=(byte)((p.r*30+p.g*59+p.b*11)/100);pixels[i]=new Color32(value,value,value,p.a);}
                },smooth?FilterMode.Bilinear:FilterMode.Point);cache.Add(source.texture,grey);
            }
            return grey;
        }
        private static readonly Texture2D[] nativeTabSources = new Texture2D[2];
        private static readonly Image[] nativeTabSkins = new Image[2];
        private static Image NativeTabSkin(bool selected)
        {
            Image source = selected ? PopUp.imgPopUp2 : PopUp.imgPopUp;
            if (source?.texture == null) { PopUp.loadBg(); source = selected ? PopUp.imgPopUp2 : PopUp.imgPopUp; }
            if (source?.texture == null) return null;
            int index = selected ? 1 : 0;
            if (nativeTabSources[index] != source.texture || nativeTabSkins[index]?.texture == null) {
                if (nativeTabSkins[index]?.texture != null) UnityEngine.Object.Destroy(nativeTabSkins[index].texture);
                // The imported atlas has mipmaps. Thin edge slices otherwise sample adjacent
                // atlas rows when reduced. CopyNative creates a Point texture without mipmaps.
                nativeTabSkins[index] = HudButtonSkin.CopyNative(source, null, FilterMode.Point);
                nativeTabSources[index] = source.texture;
            }
            return nativeTabSkins[index];
        }
        // The popup button atlas has eight 10-logical-pixel slices. Stretch only its edges;
        // tiling whole edge slices can overwrite the corners at narrow chip widths.
        internal static void PaintNativeTab(mGraphics g, Rect bounds, Rect viewport, bool selected)
        {
            Image skin = NativeTabSkin(selected);
            Rect clip = InventoryFrame.Intersection(bounds, viewport);
            if (clip.width <= 0 || clip.height <= 0 || skin?.texture == null) return;
            int zoom = Mathf.Max(1, mGraphics.zoomLevel);
            float cap = Mathf.Min(10, Mathf.Min(bounds.width, bounds.height) / 2);
            InventoryFrame.Clip(g, clip);
            Color previous = GUI.color;
            try {
                GUI.color = Color.white;
                GUI.BeginGroup(new Rect(clip.x * zoom, clip.y * zoom, clip.width * zoom, clip.height * zoom));
                try {
                    int fill = selected ? 12052656 : 16770503;
                    GUI.color = new Color((fill >> 16 & 255) / 255f, (fill >> 8 & 255) / 255f, (fill & 255) / 255f, 1);
                    GUI.DrawTexture(Pixels(cap, cap, bounds.width - 2 * cap, bounds.height - 2 * cap), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    Draw(0, 0, 0, cap, cap); Draw(10, cap, 0, bounds.width - 2 * cap, cap);
                    Draw(20, bounds.width - cap, 0, cap, cap);
                    Draw(30, 0, cap, cap, bounds.height - 2 * cap);
                    Draw(40, bounds.width - cap, cap, cap, bounds.height - 2 * cap);
                    Draw(50, 0, bounds.height - cap, cap, cap);
                    Draw(60, cap, bounds.height - cap, bounds.width - 2 * cap, cap);
                    Draw(70, bounds.width - cap, bounds.height - cap, cap, cap);
                } finally { GUI.EndGroup(); }
            } finally { GUI.color = previous; }
            Rect Pixels(float x, float y, float width, float height)
            {
                float left = Mathf.Round((bounds.x + x) * zoom), top = Mathf.Round((bounds.y + y) * zoom);
                float right = Mathf.Round((bounds.x + x + width) * zoom), bottom = Mathf.Round((bounds.y + y + height) * zoom);
                return new Rect(left - clip.x * zoom, top - clip.y * zoom, right - left, bottom - top);
            }
            void Draw(int sourceY, float x, float y, float width, float height)
            {
                if (width <= 0 || height <= 0) return;
                GUI.DrawTextureWithTexCoords(Pixels(x, y, width, height), skin.texture,
                    new Rect(0, 1f - (sourceY + 10f) * zoom / skin.texture.height,
                        10f * zoom / skin.texture.width, 10f * zoom / skin.texture.height));
            }
        }
        internal static void PaintNativeButton(mGraphics g,Rect bounds,bool enabled,bool selected,Rect viewport)
        {
            Rect clip=InventoryFrame.Intersection(bounds,viewport);if(clip.width<=0||clip.height<=0)return;
            int zoom=Mathf.Max(1,mGraphics.zoomLevel);
            GUI.BeginGroup(new Rect(clip.x*zoom,clip.y*zoom,clip.width*zoom,clip.height*zoom));
            // Scroll-card buttons shrink the native caps. Smooth a cached private copy
            // instead of dropping edge pixels with the shared sprite's Point filter.
            try {PaintNativeButton(g,new Rect(bounds.x-clip.x,bounds.y-clip.y,bounds.width,bounds.height),enabled,selected,true);}
            finally {GUI.EndGroup();}
        }
        internal static void PaintNativeButton(mGraphics g,Rect bounds,bool enabled,bool selected,bool smooth=false)
        {
            Image left=ButtonPart(selected?Command.btn1left:Command.btn0left,!enabled,smooth);
            Image middle=ButtonPart(selected?Command.btn1mid:Command.btn0mid,!enabled,smooth);
            Image right=ButtonPart(selected?Command.btn1right:Command.btn0right,!enabled,smooth);
            if(left?.texture==null||middle?.texture==null||right?.texture==null)return;
            // Fit all three native pieces to the hit rectangle, including their complete rounded caps.
            int zoom=Mathf.Max(1,mGraphics.zoomLevel);
            float cap=Mathf.Min(bounds.width/2,bounds.height*left.texture.width/left.texture.height);
            Color previous=GUI.color;
            try {
                GUI.color=Color.white;
                GUI.DrawTexture(new Rect(bounds.x*zoom,bounds.y*zoom,cap*zoom,bounds.height*zoom),left.texture);
                GUI.DrawTexture(new Rect((bounds.x+cap)*zoom,bounds.y*zoom,Mathf.Max(0,bounds.width-2*cap)*zoom,bounds.height*zoom),middle.texture);
                GUI.DrawTexture(new Rect((bounds.xMax-cap)*zoom,bounds.y*zoom,cap*zoom,bounds.height*zoom),right.texture);
            } finally {GUI.color=previous;}
        }
        internal static void PaintLevel(mGraphics g, Rect cell, Rect viewport, int level, bool bag, bool compact = false)
        {
            mFont font = PrepareLevelFont();
            Rect badge = compact ? CompactLevelBounds(cell,levelWidth) : LevelBounds(cell, levelWidth, bag);
            InventoryFrame.Clip(g, viewport);
            InventoryFrame.Rounded(g, badge, InventoryFrame.Accent);
            PaintLevelLabel(g, badge, viewport, level, font, badge.width, compact);
        }
        internal static void PaintDiscipleLevelLabel(mGraphics g, Rect badge, Rect viewport, int level)
        {
            // Match the bag's text size even though this badge has different geometry.
            mFont font = PrepareLevelFont();
            float bagWidth = Mathf.Min(levelWidth, InventoryFrame.BagSize - 4);
            PaintLevelLabel(g, badge, viewport, level, font, bagWidth, false);
        }
        private static mFont PrepareLevelFont()
        {
            mFont font = mFont.tahoma_7;
            int zoom = Mathf.Max(1, mGraphics.zoomLevel);
            if (levelFont != font.myFont || levelZoom != zoom || levelWidth <= 0) {
                levelFont = font.myFont; levelZoom = zoom; levelWidth = font.getWidth("Lv7") + 6; levelStyles.Clear();
            }
            return font;
        }
        private static void PaintLevelLabel(mGraphics g, Rect badge, Rect viewport, int level, mFont font, float fitWidth, bool compact)
        {
            int zoom = levelZoom;
            string label = "Lv" + Mathf.Clamp(level, 0, 7);
            if (zoom == 1) {
                font.drawString(g, label, (int)badge.center.x, (int)(badge.center.y - font.getHeight() / 2f), mFont.CENTER);
                return;
            }
            int key = Mathf.RoundToInt(fitWidth * zoom) * 2 + (compact ? 1 : 0);
            if (!levelStyles.TryGetValue(key, out GUIStyle style)) {
                style = new GUIStyle(GUI.skin.label) {
                    font = font.myFont, fontSize = Mathf.Max(1, font.myFont.fontSize), alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0),
                    wordWrap = false, richText = false, fixedWidth = 0, fixedHeight = 0
                };
                while (style.fontSize > 1 && (style.CalcSize(new GUIContent("Lv7")).x > fitWidth * zoom - 4
                    || style.CalcSize(new GUIContent("Lv7")).y > LevelHeight * zoom - 2)) style.fontSize--;
                style.normal.textColor = style.hover.textColor = Color.black;
                style.active.textColor = style.focused.textColor = Color.black;
                style.onNormal.textColor = style.onHover.textColor = style.onActive.textColor = style.onFocused.textColor = Color.black;
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
