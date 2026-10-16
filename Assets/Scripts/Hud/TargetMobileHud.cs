using System.Collections.Generic;
using UnityEngine;

namespace Hud
{
    // Project the existing focus, loading missing power through the existing stats request.
    // Never selects a target, attacks, opens a panel or changes movement.
    internal static class TargetMobileHud
    {
        private const string Root = "target-mobile.bounds", Map = "target-mobile.map", Hp = "target-mobile.hp",
            Name = "target-mobile.name", Power = "target-mobile.power";
        private const int Order = 10;
        private static readonly Color32 Cream = new Color32(255,243,218,255);
        private static Rect mapRect, hpRect, nameRect, powerRect;
        private static float upperWidth, lowerWidth, stepY;
        private sealed class PowerRequest
        {
            internal Char Target, Owner;
            internal int Map, Zone, Attempts;
            internal long Sent;
        }
        private static readonly Dictionary<int,PowerRequest> powerRequests = new Dictionary<int,PowerRequest>();
        private static Char powerOwner;
        private static int powerMap = -1, powerZone = -1;
        private static long nextPowerRequest;
        private sealed class Marquee
        {
            internal string Value;
            internal int Available;
            internal long Started, LastPaint;
        }
        private static readonly Dictionary<string,Marquee> marquees = new Dictionary<string,Marquee>();
        private static object textTarget;
        private struct Target
        {
            internal string Name;
            internal long Hp, MaxHp, Power;
            internal bool HasHp, HasPower, PowerKnown;
        }
        private static bool Visible => HudLayout.IsMobileLayout && GameScr.instance != null
            && GameCanvas.currentScreen == GameScr.instance && GameScr.isPaint && !GameScr.isPaintOther
            && GameScr.isPaintRada == 1 && !GameCanvas.isLoading && !Char.ischangingMap
            && !GameCanvas.panel.isShow && !GameCanvas.menu.showMenu && GameCanvas.currentDialog == null
            && !ChatTextField.gI().isShow && ChatPopup.currChatPopup == null && ChatPopup.serverChatPopUp == null
            && !Notifications.NotificationCenter.IsOpen && !Mod.CuongLe.DoHoa.isShowMenuVIP;
        internal static bool Active => Visible && Read(out _,out _) && HudLayout.GetRect(Root).width > 0;
        private static float Unit(float pixels) => HudLayout.PixelsToLogical(pixels)
            * (GameCanvas.h*Mathf.Max(1,mGraphics.zoomLevel)/390f);
        internal static int CountdownY => Active ? Mathf.CeilToInt(HudLayout.GetRect(Root).yMax+Unit(3)) : 13;

        internal static void Update()
        {
            if (!HudLayout.IsMobileLayout) return;
            Char player = Char.ExistingLocalPlayer;
            if (player != powerOwner || powerMap != TileMap.mapID || powerZone != TileMap.zoneID
                || GameCanvas.currentScreen != GameScr.instance || Char.ischangingMap || GameCanvas.isLoading) {
                powerRequests.Clear(); nextPowerRequest = 0;
                powerOwner = player; powerMap = TileMap.mapID; powerZone = TileMap.zoneID;
            }
            if (!Active || player?.charFocus == null || !Read(out Target data,out _) || !data.HasPower || data.PowerKnown) return;
            Char target = player.charFocus;
            long now = mSystem.currentTimeMillis();
            if (now < nextPowerRequest) return;
            if (!powerRequests.TryGetValue(target.charID,out PowerRequest request) || request.Target != target) {
                if (powerRequests.Count >= 32) powerRequests.Clear();
                request = new PowerRequest { Target = target, Owner = player, Map = powerMap, Zone = powerZone };
                powerRequests[target.charID] = request;
            }
            if (request.Attempts >= 2 || request.Attempts > 0 && now-request.Sent < 5000) return;
            request.Sent = now; request.Attempts++;
            nextPowerRequest = now+750;
            Service.gI().getPlayerMenu(target.charID); // Existing -79 request; no panel or wait dialog.
        }
        internal static void ReceivePower(int id, long power, string level)
        {
            if (!HudLayout.IsMobileLayout || !powerRequests.TryGetValue(id,out PowerRequest request)
                || request.Owner != Char.ExistingLocalPlayer || request.Map != TileMap.mapID || request.Zone != TileMap.zoneID
                || GameCanvas.currentScreen != GameScr.instance || Char.ischangingMap || GameCanvas.isLoading || power < 0) return;
            // Reply A may arrive after selecting B. Only update the originally requested A.
            if (request.Target != Char.ExistingLocalPlayer?.charFocus && request.Target != GameScr.findCharInMap(id)) return;
            request.Target.cPower = power;
            request.Target.currStrLevel = level;
            HudLayout.Invalidate();
        }

        private static bool Read(out Target data, out object identity)
        {
            data = new Target(); identity = null;
            Char player = Char.ExistingLocalPlayer;
            if (player == null) return false;
            if (player.charFocus != null) {
                Char target = player.charFocus;
                if (target.cHP <= 0 || target.statusMe == 14 || target.statusMe == 5 || target.statusMe == 15 || target.isInvisiblez) return false;
                if (!ReferenceEquals(target,player) && !GameScr.vCharInMap.contains(target)) return false;
                identity = target;
                data = new Target { Name = target.cName, Hp = target.cHP, MaxHp = target.cHPFull,
                    Power = target.cPower, HasHp = true, HasPower = target.charID >= 0 || target.cPower > 0 || target.currStrLevel != null,
                    PowerKnown = target.cPower >= 0 && (target.cPower > 0 || target.currStrLevel != null || ReferenceEquals(target,player)) };
            }
            else if (player.mobFocus != null) {
                Mob target = player.mobFocus;
                if (target.hp <= 0 || target.status == 0 || target.status == 1) return false;
                if (!target.isMobMe && !GameScr.vMob.contains(target)) return false;
                identity = target;
                data = new Target { Name = target.getTemplate()?.name, Hp = target.hp, MaxHp = target.maxHp,
                    HasHp = true, HasPower = false, PowerKnown = false };
            }
            return identity != null && !string.IsNullOrEmpty(data.Name);
        }
        internal static void Register()
        {
            // One provider owns the complete layout. This information tab never captures input.
            HudLayout.Register(Root,Bounds,Order,() => Active,false);
        }
        private static float RowHeight => Mathf.Max(Unit(12),mFont.tahoma_7b_white.getHeight()+Unit(2));
        private static Rect Bounds()
        {
            mapRect = hpRect = nameRect = powerRect = new Rect();
            if (!HudLayout.IsMobileLayout || !Read(out Target target,out _)) return new Rect();
            float gap = Unit(6), center = GameCanvas.w/2f, wing = Unit(14);
            Rect info = HudLayout.GetRect("top-info.bounds"), hub = HudLayout.GetRect(TopActionsHud.Hub);
            float available = Mathf.Max(0,2*Mathf.Min(center-info.xMax-gap,hub.xMin-gap-center));
            // HP keeps its existing painter, height and preferred width. Only narrow screens shrink it.
            upperWidth = Mathf.Min(Unit(212),available-2*wing);
            if (upperWidth < Unit(180)) return new Rect(); // HP minimum 160 + two 10px margins.
            float power = target.HasPower ? mFont.tahoma_7_white.getWidth(PowerValue(target))+Unit(8) : 0;
            float content = mFont.tahoma_7b_white.getWidth(target.Name)+(target.HasPower ? power+Unit(4) : 0);
            float lowerLimit = Mathf.Max(upperWidth-Unit(52),Mathf.Min(Unit(140),upperWidth-Unit(40)));
            lowerWidth = Mathf.Min(lowerLimit,Mathf.Max(Unit(140),content+Unit(20)));
            float width = upperWidth+2*wing;
            float mapHeight = Mathf.Max(Unit(12),mFont.tahoma_7_white.getHeight());
            float hpHeight = Mathf.Max(Unit(14),RowHeight);
            float row = Mathf.Max(Unit(12),mFont.tahoma_7b_white.getHeight());
            float hpY = Unit(1)+mapHeight+Unit(2), nameY = hpY+hpHeight+Unit(3.5f);
            // Native fonts set a hard lower bound on very low-resolution viewports.
            float height = Mathf.Max(Unit(58),nameY+row+Unit(4));
            Rect root = HudLayout.Anchor(HorizontalAnchor.Center,VerticalAnchor.Top,new Vector2(width,height),Vector2.zero);
            mapRect = new Rect(center-upperWidth/2+Unit(10),root.y+Unit(1),upperWidth-Unit(20),mapHeight);
            hpRect = new Rect(center-upperWidth/2+Unit(10),root.y+hpY,upperWidth-Unit(20),hpHeight);
            stepY = hpY+hpHeight+Unit(1.75f);
            Rect line = new Rect(center-lowerWidth/2+Unit(10),root.y+nameY,lowerWidth-Unit(20),row);
            if (target.HasPower) {
                power = Mathf.Min(power,line.width*.55f);
                powerRect = new Rect(line.xMax-power,line.y+Unit(1),power,line.height-Unit(2));
                line.width = Mathf.Max(0,line.width-power-Unit(4));
            }
            nameRect = line;
            return root;
        }
        internal static void Paint(mGraphics g)
        {
            if (!Active || !Read(out Target target,out object identity)) return;
            if (!ReferenceEquals(textTarget,identity)) {
                marquees.Clear(); textTarget = identity;
            }
            Rect root = HudLayout.GetRect(Root);
            if (root.width <= 0) return;
            int tx = g.getTranslateX(), ty = g.getTranslateY()-mGraphics.addYWhenOpenKeyBoard;
            g.translate(-tx,-ty);
            try {
                PaintPanel(root);
                string separator = mGraphics.zoomLevel == 1 ? " - " : " · ";
                Text(g,Map,mapRect,TileMap.mapName+separator+"Khu "+TileMap.zoneID,mFont.tahoma_7_white,new Color32(255,243,218,185),true);
                Rect hp = hpRect;
                if (target.HasHp) {
                    Rounded(hp,new Color32(80,51,28,255),hp.height/2);
                    float fraction = target.MaxHp <= 0 ? 0 : Mathf.Clamp01((float)((double)target.Hp/target.MaxHp));
                    if (fraction > 0) RoundedFill(hp,fraction,new Color32(229,70,48,255));
                    Text(g,Hp,hp,"HP "+NinjaUtil.getMoneys(System.Math.Max(0,target.Hp)),mFont.tahoma_7_white,Cream,true);
                }
                Text(g,Name,nameRect,target.Name,mFont.tahoma_7b_white,Cream,false);
                if (target.HasPower) {
                    Rect power = powerRect;
                    Rounded(power,new Color32(242,205,74,255),power.height/2);
                    Text(g,Power,power,PowerValue(target),mFont.tahoma_7_white,new Color32(40,24,12,255),true);
                }
            }
            finally { g.setClip(0,0,GameCanvas.w,GameCanvas.h); g.translate(tx,ty); }
        }
        private static void Text(mGraphics g, string key, Rect rect, string text, mFont font, Color32 color, bool center)
        {
            if (rect.width <= 0 || rect.height <= 0) return;
            float padding = Unit(4);
            string value = text ?? string.Empty;
            int available = Mathf.FloorToInt(rect.width-2*padding);
            if (available <= 0) return;
            int width = font.getWidth(value);
            float offset = 0;
            long now = mSystem.currentTimeMillis();
            if (!marquees.TryGetValue(key,out Marquee marquee)) {
                marquee = new Marquee(); marquees.Add(key,marquee);
            }
            if (marquee.Value != value || marquee.Available != available || now-marquee.LastPaint > 250) {
                marquee.Value = value; marquee.Available = available; marquee.Started = now;
            }
            marquee.LastPaint = now;
            if (width > available) {
                // Read both ends: pause, scroll left, pause, scroll back. Native font, no truncation.
                float overflow = width-available, travel = overflow/Mathf.Max(.01f,Unit(22));
                const float pause = 1f;
                float phase = Mathf.Max(0,(now-marquee.Started)/1000f) % (2*(pause+travel));
                if (phase < pause) offset = 0;
                else if (phase < pause+travel) offset = (phase-pause)/travel*overflow;
                else if (phase < 2*pause+travel) offset = overflow;
                else offset = (1-(phase-2*pause-travel)/travel)*overflow;
            }
            int clipX = Mathf.CeilToInt(rect.x+padding);
            int x = width > available ? clipX-Mathf.RoundToInt(offset)
                : Mathf.RoundToInt(center ? rect.center.x-width/2f : rect.x+padding);
            int y = Mathf.RoundToInt(rect.center.y-font.getHeight()/2f);
            g.setClip(clipX,Mathf.FloorToInt(rect.y),available,Mathf.CeilToInt(rect.height));
            font.drawTintedString(g,value,x,y,color);
            g.setClip(0,0,GameCanvas.w,GameCanvas.h);
        }
        private static readonly Dictionary<string,Texture2D> shapes = new Dictionary<string,Texture2D>();
        private static string PowerValue(Target target) => target.PowerKnown ? NinjaUtil.getMoneys(target.Power) : "SM --";
        private static Texture2D panelShape;
        private static Vector4 panelSize, panelDetail;
        private static float panelStroke;
        private static int panelZoom;
        private static readonly List<Vector2> outlinePoints = new List<Vector2>();
        private static void PaintPanel(Rect root)
        {
            Rect pixels = HudButtonSkin.ScreenPixels(root);
            float sx = pixels.width/root.width, sy = pixels.height/root.height;
            int upper = Mathf.RoundToInt(upperWidth*sx), lower = Mathf.RoundToInt(lowerWidth*sx);
            int step = Mathf.RoundToInt(stepY*sy), radius = Mathf.Max(1,Mathf.RoundToInt(Unit(7)*Mathf.Min(sx,sy)));
            int wing = Mathf.RoundToInt(Unit(14)*sx);
            float stroke = Mathf.Max(1,Unit(1.5f)*Mathf.Min(sx,sy));
            Vector4 size = new Vector4(pixels.width,pixels.height,upper,lower);
            Vector4 detail = new Vector4(step,radius,wing,0);
            if (panelShape == null || panelSize != size || panelDetail != detail || panelStroke != stroke || panelZoom != mGraphics.zoomLevel) {
                if (panelShape != null) Object.Destroy(panelShape);
                panelShape = null;
                panelShape = BuildPanel((int)pixels.width,(int)pixels.height,upper,lower,step,radius,wing,stroke);
                panelSize = size; panelDetail = detail; panelStroke = stroke; panelZoom = mGraphics.zoomLevel;
            }
            Draw(pixels,panelShape,new Rect(0,0,1,1),Color.white);
        }
        private static void Curve(Vector2 control, Vector2 end)
        {
            Vector2 start = outlinePoints[outlinePoints.Count-1];
            int segments = Mathf.Max(4,Mathf.CeilToInt((Vector2.Distance(start,control)+Vector2.Distance(control,end))/.75f));
            for (int i=1;i<=segments;i++) {
                float t = (float)i/segments, u = 1-t;
                outlinePoints.Add(u*u*start+2*u*t*control+t*t*end);
            }
        }
        private static void Corner(Vector2 previous, Vector2 vertex, Vector2 next, float radius)
        {
            float cut = Mathf.Min(radius,Mathf.Min(Vector2.Distance(previous,vertex),Vector2.Distance(vertex,next))*.45f);
            outlinePoints.Add(vertex+(previous-vertex).normalized*cut);
            Curve(vertex,vertex+(next-vertex).normalized*cut);
        }
        private static Texture2D BuildPanel(int width, int height, int upper, int lower, int step, int radius, int wing, float stroke)
        {
            float ul = (width-upper)/2f, ur = width-ul, ll = (width-lower)/2f, lr = width-ll;
            // Short diagonal ledge between tiers. Round every join, keep the flared shoulders concave.
            float ledge = Mathf.Min(radius*.45f,Mathf.Max(1,(height-step)*.2f));
            Vector2[] vertices = {
                new Vector2(ur,wing),new Vector2(ur,step-ledge),new Vector2(lr,step+ledge),
                new Vector2(lr,height),new Vector2(ll,height),new Vector2(ll,step+ledge),
                new Vector2(ul,step-ledge),new Vector2(ul,wing)
            };
            outlinePoints.Clear(); outlinePoints.Add(Vector2.zero); outlinePoints.Add(new Vector2(width,0));
            Curve(new Vector2(ur,0),vertices[0]);
            for (int i=1;i<vertices.Length-1;i++) Corner(vertices[i-1],vertices[i],vertices[i+1],radius);
            outlinePoints.Add(vertices[vertices.Length-1]); Curve(new Vector2(ul,0),Vector2.zero);
            Texture2D texture = new Texture2D(width,height,TextureFormat.RGBA32,false) {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave
            };
            Color32[] data = new Color32[width*height];
            for (int y=0;y<height;y++) for (int x=0;x<width;x++) {
                Vector2 point = new Vector2(x+.5f,height-y-.5f);
                bool inside = false; float distance = float.MaxValue;
                for (int i=1;i<outlinePoints.Count;i++) {
                    Vector2 a = outlinePoints[i-1], b = outlinePoints[i], edge = b-a;
                    if ((a.y>point.y)!=(b.y>point.y) && point.x < (b.x-a.x)*(point.y-a.y)/(b.y-a.y)+a.x) inside = !inside;
                    // The top edge closes the fill, but has no stroke against the screen edge.
                    if (a.y == 0 && b.y == 0) continue;
                    float t = edge.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point-a,edge)/edge.sqrMagnitude) : 0;
                    distance = Mathf.Min(distance,Vector2.Distance(point,a+t*edge));
                }
                float signed = inside ? distance : -distance;
                float coverage = Mathf.Clamp01(signed+.5f);
                float border = coverage*Mathf.Clamp01(stroke+.5f-signed);
                float alpha = coverage*.88f+border*(1-.88f);
                float blend = alpha > 0 ? border/alpha : 0;
                data[y*width+x] = new Color32((byte)Mathf.RoundToInt(Mathf.Lerp(43,242,blend)),
                    (byte)Mathf.RoundToInt(Mathf.Lerp(26,180,blend)),(byte)Mathf.RoundToInt(Mathf.Lerp(10,90,blend)),
                    (byte)Mathf.RoundToInt(alpha*255));
            }
            texture.SetPixels32(data); texture.Apply(false,true);
            return texture;
        }
        private static Texture2D Shape(Rect rect, float radius)
        {
            int width = Mathf.Max(1,(int)rect.width), height = Mathf.Max(1,(int)rect.height);
            int r = Mathf.Clamp(Mathf.RoundToInt(radius),1,Mathf.Min(width,height)/2);
            string key = width+":"+height+":"+r;
            if (shapes.TryGetValue(key,out Texture2D texture)) return texture;
            if (shapes.Count >= 24) ClearShapes();
            texture = new Texture2D(width,height,TextureFormat.RGBA32,false) {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave
            };
            Color32[] data = new Color32[width*height];
            for (int y=0;y<height;y++) for (int x=0;x<width;x++) {
                float dx = Mathf.Max(Mathf.Abs(x+.5f-width/2f)-(width/2f-r),0);
                float dy = Mathf.Max(Mathf.Abs(y+.5f-height/2f)-(height/2f-r),0);
                if (dx*dx+dy*dy > r*r) continue;
                data[y*width+x] = new Color32(255,255,255,255);
            }
            texture.SetPixels32(data); texture.Apply(false,true); shapes.Add(key,texture);
            return texture;
        }
        private static void Rounded(Rect logical, Color32 color, float radius)
        {
            Rect pixels = HudButtonSkin.ScreenPixels(logical);
            float scale = logical.height > 0 ? pixels.height/logical.height : 1;
            Draw(pixels,Shape(pixels,radius*scale),new Rect(0,0,1,1),color);
        }
        private static void RoundedFill(Rect logical, float fraction, Color32 color)
        {
            Rect pixels = HudButtonSkin.ScreenPixels(logical);
            Draw(new Rect(pixels.x,pixels.y,Mathf.Round(pixels.width*fraction),pixels.height),
                Shape(pixels,pixels.height/2),new Rect(0,0,fraction,1),color);
        }
        private static void Draw(Rect rect, Texture2D texture, Rect uv, Color color)
        {
            Matrix4x4 matrix = GUI.matrix; Color previous = GUI.color;
            try { GUI.matrix = Matrix4x4.identity; GUI.color = color; GUI.DrawTextureWithTexCoords(rect,texture,uv); }
            finally { GUI.matrix = matrix; GUI.color = previous; }
        }
        internal static void ClearShapes()
        {
            foreach (Texture2D texture in shapes.Values) Object.Destroy(texture);
            shapes.Clear();
            if (panelShape != null) Object.Destroy(panelShape);
            panelShape = null;
        }
    }
}
