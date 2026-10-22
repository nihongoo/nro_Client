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
            return new Rect(bag ? cell.x - 3 : cell.xMax - width + 3, bag ? cell.y - 3 : cell.yMax - 9, width, 12);
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
        private static readonly Dictionary<mFont, GUIStyle> compactStyles = new Dictionary<mFont, GUIStyle>();
        // Keep the game's font, fitting the complete value instead of dropping digits with ellipses.
        internal static void PaintCompactText(mGraphics g,mFont font,string text,Rect bounds,Rect viewport,int color,bool center=false)
        {
            Rect clip=InventoryFrame.Intersection(bounds,viewport);if(clip.width<=0||clip.height<=0)return;
            if(!compactStyles.TryGetValue(font,out GUIStyle style)) {
                style=new GUIStyle(GUI.skin.label){font=font.myFont,alignment=TextAnchor.MiddleLeft,
                    padding=new RectOffset(),margin=new RectOffset(),wordWrap=false,richText=false};
                compactStyles.Add(font,style);
            }
            style.alignment=center?TextAnchor.MiddleCenter:TextAnchor.MiddleLeft;
            int zoom=Mathf.Max(1,mGraphics.zoomLevel);
            style.fontSize=Mathf.Max(1,font.myFont.fontSize);
            while(style.fontSize>1&&(style.CalcSize(new GUIContent(text)).x>(bounds.width-2)*zoom||style.CalcSize(new GUIContent(text)).y>bounds.height*zoom))style.fontSize--;
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
        internal static void PaintSkillIcon(mGraphics g,int id,Rect bounds,Rect viewport,bool selected=false)
        {
            Rect clip=InventoryFrame.Intersection(bounds,viewport);if(clip.width<=0||clip.height<=0)return;
            // Restore the actual orange skill-frame sprite, rather than a recolored popup border.
            Image frame=selected?GameScr.imgSkill2:GameScr.imgSkill;
            if(frame?.texture!=null) {
                int zoom=Mathf.Max(1,mGraphics.zoomLevel);Color previous=GUI.color;
                try {
                    GUI.color=Color.white;GUI.BeginGroup(new Rect(clip.x*zoom,clip.y*zoom,clip.width*zoom,clip.height*zoom));
                    try {GUI.DrawTexture(new Rect((bounds.x-clip.x)*zoom,(bounds.y-clip.y)*zoom,bounds.width*zoom,bounds.height*zoom),frame.texture);}
                    finally {GUI.EndGroup();}
                } finally {GUI.color=previous;}
            }
            else {InventoryFrame.Clip(g,clip);InventoryFrame.Rounded(g,bounds,InventoryFrame.SlotBackground,255,selected?0x00C86A:0xFF9900);}
            // The source is a 28-logical-pixel frame with an inset of four pixels.
            PaintCompactIcon(g,id,InventoryFrame.Inset(bounds,bounds.width/7f),viewport);
        }
        private static readonly Dictionary<Texture2D,Image> disabledButtons=new Dictionary<Texture2D,Image>();
        private static Image ButtonPart(Image source,bool disabled)
        {
            if(source?.texture==null||!disabled)return source;
            if(!disabledButtons.TryGetValue(source.texture,out Image grey)) {
                grey=HudButtonSkin.CopyNative(source,(pixels,width,height)=>{
                    for(int i=0;i<pixels.Length;i++) {Color32 p=pixels[i];byte value=(byte)((p.r*30+p.g*59+p.b*11)/100);pixels[i]=new Color32(value,value,value,p.a);}
                });disabledButtons.Add(source.texture,grey);
            }
            return grey;
        }
        internal static void PaintNativeButton(mGraphics g,Rect bounds,bool enabled,bool selected)
        {
            Image left=ButtonPart(selected?Command.btn1left:Command.btn0left,!enabled);
            Image middle=ButtonPart(selected?Command.btn1mid:Command.btn0mid,!enabled);
            Image right=ButtonPart(selected?Command.btn1right:Command.btn0right,!enabled);
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
            mFont font = mFont.tahoma_7;
            int zoom = Mathf.Max(1, mGraphics.zoomLevel);
            if (levelFont != font.myFont || levelZoom != zoom || levelWidth <= 0) {
                levelFont = font.myFont; levelZoom = zoom; levelWidth = font.getWidth("Lv7") + 6; levelStyles.Clear();
            }
            Rect badge = compact ? CompactLevelBounds(cell,levelWidth) : LevelBounds(cell, levelWidth, bag);
            InventoryFrame.Clip(g, viewport);
            InventoryFrame.Rounded(g, badge, InventoryFrame.Accent);
            string label = "Lv" + Mathf.Clamp(level, 0, 7);
            if (zoom == 1) {
                font.drawString(g, label, (int)badge.center.x, (int)(badge.center.y - font.getHeight() / 2f), mFont.CENTER);
                return;
            }
            int key = Mathf.RoundToInt(badge.width * zoom) * 2 + (compact ? 1 : 0);
            if (!levelStyles.TryGetValue(key, out GUIStyle style)) {
                style = new GUIStyle(GUI.skin.label) {
                    font = font.myFont, fontSize = Mathf.Max(1, font.myFont.fontSize), alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0),
                    wordWrap = false, richText = false, fixedWidth = 0, fixedHeight = 0
                };
                while (style.fontSize > 1 && (style.CalcSize(new GUIContent("Lv7")).x > badge.width * zoom - 4
                    || style.CalcSize(new GUIContent("Lv7")).y > badge.height * zoom - 2)) style.fontSize--;
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
