using System.Collections.Generic;
using UnityEngine;

namespace Hud
{
    // Scoped sink for the existing Skill.paint/SmallImage command stream. The skill
    // still decides cooldown geometry, colour and state; this class only masks it.
    internal sealed class CircularCombatIcon
    {
        private struct Key : System.IEquatable<Key>
        {
            public int Source, X, Y, Width, Height, Diameter;
            public bool Equals(Key other) => Source == other.Source && X == other.X && Y == other.Y
                && Width == other.Width && Height == other.Height && Diameter == other.Diameter;
            public override bool Equals(object other) => other is Key key && Equals(key);
            public override int GetHashCode()
            {
                unchecked { int hash = Source; hash = hash*31+X; hash = hash*31+Y;
                    hash = hash*31+Width; hash = hash*31+Height; return hash*31+Diameter; }
            }
        }
        private sealed class Cached
        {
            public Texture2D Texture;
            public long Used;
        }
        private const int Capacity = 32;
        private static readonly Dictionary<Key,Cached> icons = new Dictionary<Key,Cached>();
        private static readonly Dictionary<int,Texture2D> masks = new Dictionary<int,Texture2D>();
        private static readonly CircularCombatIcon painter = new CircularCombatIcon();
        private static long useSerial;
        private Rect target;
        private int centerY;
        private bool floatingItem;

        internal static void PaintSkill(mGraphics graphics, Rect rect, Skill skill)
        {
            Begin(rect);
            CircularCombatIcon previous = graphics.HudIconPainter;
            graphics.HudIconPainter = painter;
            try { skill.paint((int)rect.center.x,(int)rect.center.y,graphics); }
            finally { graphics.HudIconPainter = previous; }
        }
        internal static void PaintFloatingItem(mGraphics graphics, Rect rect, int iconId)
        {
            Begin(rect,true);
            CircularCombatIcon previous = graphics.HudIconPainter;
            graphics.HudIconPainter = painter;
            try { SmallImage.drawSmallImage(graphics,iconId,(int)rect.center.x,(int)rect.center.y,0,StaticObj.VCENTER_HCENTER); }
            finally { graphics.HudIconPainter = previous; }
        }
        internal static void PaintImage(Rect rect, Image image)
        {
            if (image == null) return;
            Begin(rect);
            painter.DrawRegion(image.texture,new Rect(0,0,1,1));
        }
        private static void Begin(Rect rect, bool floating = false)
        {
            Rect outer = CombatMobileHud.CirclePixels(rect);
            int border = CombatMobileHud.BorderWidth(2);
            painter.floatingItem = floating;
            painter.target = floating ? outer : new Rect(outer.x+border,outer.y+border,
                Mathf.Max(1,outer.width-2*border),Mathf.Max(1,outer.height-2*border));
            painter.centerY = (int)rect.center.y;
        }
        internal void DrawRegion(Texture2D source, Rect uv)
        {
            if (source == null) return;
            if (floatingItem) {
                // The item sits ABOVE the circle/outline. Preserve the complete sprite,
                // native pixels when they fit, and aspect ratio; never crop by a circle.
                float width = source.width*uv.width, height = source.height*uv.height;
                if (width <= 0 || height <= 0) return;
                float fit = Mathf.Min(1,Mathf.Min(target.width/width,target.height/height));
                width = Mathf.Max(1,Mathf.Round(width*fit)); height = Mathf.Max(1,Mathf.Round(height*fit));
                Draw(new Rect(Mathf.Round(target.center.x-width/2),Mathf.Round(target.center.y-height/2),width,height),
                    source,uv,Color.white);
                return;
            }
            Key key = new Key {
                Source = source.GetInstanceID(), X = Mathf.RoundToInt(uv.x*source.width),
                Y = Mathf.RoundToInt(uv.y*source.height), Width = Mathf.RoundToInt(uv.width*source.width),
                Height = Mathf.RoundToInt(uv.height*source.height), Diameter = (int)target.width
            };
            if (key.Width <= 0 || key.Height <= 0 || key.X < 0 || key.Y < 0
                || key.X+key.Width > source.width || key.Y+key.Height > source.height) return;
            if (!icons.TryGetValue(key,out Cached cached)) {
                if (icons.Count >= Capacity) EvictOldest();
                cached = new Cached { Texture = Cover(source,key) };
                icons.Add(key,cached);
            }
            cached.Used = ++useSerial;
            Draw(target,cached.Texture,new Rect(0,0,1,1),Color.white);
        }
        private static Texture2D Cover(Texture2D source, Key key)
        {
            // Imported/server textures may be unreadable. Read just this atlas region,
            // then release every temporary GPU/CPU copy; only the round result is cached.
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);
            Texture2D readable = null, result = null;
            try {
                Graphics.Blit(source,temporary); RenderTexture.active = temporary;
                readable = new Texture2D(key.Width,key.Height,TextureFormat.RGBA32,false);
                readable.ReadPixels(new Rect(key.X,key.Y,key.Width,key.Height),0,0);
                Color32[] input = readable.GetPixels32();
                int diameter = key.Diameter;
                // Minimal cover: NEVER round scale upwards to 2x/3x and crop the excess.
                // Resample once at the final physical diameter; square icons retain the
                // entire source extent before applying just the requested circular mask.
                float scale = (float)diameter/Mathf.Min(key.Width,key.Height);
                Color32[] output = new Color32[diameter*diameter];
                float radius = diameter/2f;
                for (int y=0;y<diameter;y++) for(int x=0;x<diameter;x++) {
                    float dx = x+.5f-radius, dy = y+.5f-radius;
                    if (dx*dx+dy*dy > radius*radius) continue;
                    float sx = Mathf.Clamp(key.Width/2f+dx/scale-.5f,0,key.Width-1);
                    float sy = Mathf.Clamp(key.Height/2f+dy/scale-.5f,0,key.Height-1);
                    int x0 = Mathf.FloorToInt(sx), y0 = Mathf.FloorToInt(sy);
                    int x1 = Mathf.Min(x0+1,key.Width-1), y1 = Mathf.Min(y0+1,key.Height-1);
                    Color lower = Color.Lerp(input[y0*key.Width+x0],input[y0*key.Width+x1],sx-x0);
                    Color upper = Color.Lerp(input[y1*key.Width+x0],input[y1*key.Width+x1],sx-x0);
                    output[y*diameter+x] = Color.Lerp(lower,upper,sy-y0);
                }
                result = new Texture2D(diameter,diameter,TextureFormat.RGBA32,false);
                result.filterMode = FilterMode.Point; result.wrapMode = TextureWrapMode.Clamp; result.hideFlags = HideFlags.DontSave;
                result.SetPixels32(output); result.Apply(false,true);
                return result;
            }
            catch { if (result != null) Object.Destroy(result); throw; }
            finally {
                if (readable != null) Object.Destroy(readable);
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(temporary);
            }
        }
        internal void DrawCooldown(Rect nativeFill, Color tint)
        {
            // Map the actual 20x20 fill emitted by Skill.paint onto the round icon.
            // No second timer, cooldown formula, or skill-state mutation here.
            float top = Mathf.Clamp01((nativeFill.y-centerY+10)/20f);
            float bottom = Mathf.Clamp01((nativeFill.yMax-centerY+10)/20f);
            DrawCooldownMask(top,bottom,tint);
        }
        internal static void PaintItemCooldown(Rect rect, long remaining, long duration)
        {
            if (remaining <= 0 || duration <= 0) return;
            Begin(rect);
            painter.DrawCooldownMask(1-Mathf.Clamp01((float)remaining/duration),1,new Color(0,0,0,.65f));
        }
        private void DrawCooldownMask(float top, float bottom, Color tint)
        {
            if (bottom <= top) return;
            int diameter = (int)target.width;
            if (!masks.TryGetValue(diameter,out Texture2D mask)) {
                if (masks.Count >= 8) { foreach(Texture2D old in masks.Values) Object.Destroy(old); masks.Clear(); }
                mask = new Texture2D(diameter,diameter,TextureFormat.RGBA32,false);
                mask.filterMode = FilterMode.Point; mask.wrapMode = TextureWrapMode.Clamp; mask.hideFlags = HideFlags.DontSave;
                Color32[] data = new Color32[diameter*diameter];
                float radius = diameter/2f;
                for(int y=0;y<diameter;y++) for(int x=0;x<diameter;x++) {
                    float dx=x+.5f-radius,dy=y+.5f-radius;
                    if(dx*dx+dy*dy<=radius*radius) data[y*diameter+x]=new Color32(255,255,255,255);
                }
                mask.SetPixels32(data); mask.Apply(false,true); masks.Add(diameter,mask);
            }
            Draw(new Rect(target.x,target.y+top*diameter,diameter,(bottom-top)*diameter),mask,
                new Rect(0,1-bottom,1,bottom-top),tint);
        }
        private static void Draw(Rect destination, Texture2D texture, Rect uv, Color tint)
        {
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            try { GUI.matrix = Matrix4x4.identity; GUI.color = tint; GUI.DrawTextureWithTexCoords(destination,texture,uv,true); }
            finally { GUI.matrix = matrix; GUI.color = color; }
        }
        private static void EvictOldest()
        {
            Key oldest = new Key(); long serial = long.MaxValue;
            foreach(KeyValuePair<Key,Cached> entry in icons) if(entry.Value.Used < serial) { oldest=entry.Key; serial=entry.Value.Used; }
            Object.Destroy(icons[oldest].Texture); icons.Remove(oldest);
        }
        internal static void Clear()
        {
            foreach(Cached cached in icons.Values) Object.Destroy(cached.Texture);
            foreach(Texture2D mask in masks.Values) Object.Destroy(mask);
            icons.Clear(); masks.Clear();
        }
    }
}
