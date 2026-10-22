using System.Collections.Generic;
using UnityEngine;

namespace Hud
{
    internal static class CombatMobileHud
    {
        internal const string Attack = "combat.attack", Joystick = "combat.joystick", JoystickTouch = "combat.joystick-touch";
        private const string ContextBounds = "combat.context.bounds";
        private static string Context => GameScr.isPickNgocRong ? "combat.context.dragon-ball" :
            GameScr.isudungCapsun4 ? "combat.context.capsule4" : GameScr.isudungCapsun3 ? "combat.context.capsule3" : null;
        internal static string SkillName(int slot) => "combat.skill." + slot;
        internal static string ItemName(int slot) => "combat.item." + slot;
        // 844x390 is a design reference, not a fixed number of device pixels. Scale the
        // complete combat group by height, then anchor it; MotherCanvas zoom stays unchanged.
        internal static float DesignUnit(float pixels) => HudLayout.PixelsToLogical(pixels)
            * (GameCanvas.h * Mathf.Max(1,mGraphics.zoomLevel) / 390f);
        private static Vector2 DesignUnits(Vector2 pixels) => new Vector2(DesignUnit(pixels.x),DesignUnit(pixels.y));
        internal static bool Visible => HudLayout.IsMobileLayout && GameCanvas.currentScreen == GameScr.instance
            && GameScr.instance != null && GameScr.isPaint && !GameScr.isPaintOther && GameScr.isPaintRada == 1
            && GameScr.instance.mobCapcha == null
            && !HudLayout.IsPlayerDead && Char.myCharz().taskMaint != null && Char.myCharz().taskMaint.taskId != 0;
        internal static bool CanInput => Visible && !GameScr.instance.isNotPaintTouchControl()
            && !GameScr.instance.isLockKey && !Char.isLockKey && !Controller.isStopReadMessage && !InfoDlg.isLock
            && !Notifications.NotificationCenter.IsOpen && !Mod.CuongLe.DoHoa.isShowMenuVIP && ChatPopup.serverChatPopUp == null
            && !Char.isLoadingMap
            && !Char.myCharz().isTeleport && !Char.myCharz().isPaintNewSkill
            && GameScr.instance.mobCapcha == null;

        internal static void Register()
        {
            // Broad acquisition zone comes first: visible buttons win if they overlap it.
            HudLayout.Register(JoystickTouch, () => {
                Rect visible = HudLayout.GetRect(Joystick);
                return Rect.MinMaxRect(0,Mathf.Min(GameCanvas.hh >> 1,visible.yMin),
                    Mathf.Max(GameCanvas.w > 380 ? GameCanvas.hw / 4 * 3 - 20 : GameCanvas.hw,visible.xMax),
                    Mathf.Max(GameCanvas.w > 380 ? GameCanvas.h : GameCanvas.h - 80,visible.yMax));
            },
                HudLayout.ControlsOrder, () => Visible && GameScr.isAnalog != 0);
            Design(Joystick, 36, 244, 112, HorizontalAnchor.Left, false);
            Design(Attack, 750, 282, 80);
            int[,] positions = { {685,306}, {697,255}, {738,222}, {790,220}, {632,318}, {645,239}, {699,181}, {777,163} };
            for (int i = 0; i < 8; i++) Design(SkillName(i), positions[i,0], positions[i,1], 46);
            HudLayout.Register("combat.items", () => {
                Rect neighbor = HudLayout.GetRect(SkillName(4));
                Rect group = HudLayout.Anchor(HorizontalAnchor.Center,VerticalAnchor.Bottom,
                    DesignUnits(new Vector2(132,40)),DesignUnits(new Vector2(124,0)));
                // Share the neighboring bottom skill's centerline on every viewport.
                group.y = neighbor.center.y-group.height/2;
                // Keep all three items clear of the right-anchored outer skill.
                group.x = Mathf.Min(group.x,neighbor.xMin-DesignUnit(6)-group.width);
                return group;
            },HudLayout.ControlsOrder,() => false,false);
            for (int i = 0; i < 3; i++) {
                int slot = i;
                HudLayout.Register(ItemName(i), () => {
                    Rect group = HudLayout.GetRect("combat.items");
                    return new Rect(group.x+DesignUnit(46*slot),group.y,DesignUnit(40),group.height);
                },HudLayout.ControlsOrder,() => Visible);
            }
            // Context items keep their own decorative bounds; no target-switch entry/hitbox.
            HudLayout.Register(ContextBounds, () => HudLayout.Anchor(HorizontalAnchor.Right,VerticalAnchor.Bottom,
                DesignUnits(new Vector2(40,40)),DesignUnits(new Vector2(234,100))),
                HudLayout.ControlsOrder,() => false,false);
            foreach(string context in new[] { "combat.context.dragon-ball", "combat.context.capsule4", "combat.context.capsule3" }) {
                string name = context;
                HudLayout.Register(name,() => HudLayout.GetRect(ContextBounds),HudLayout.ControlsOrder,() => Visible && Context == name);
            }
        }
        private static void Design(string name, int left, int top, int size,
            HorizontalAnchor anchor = HorizontalAnchor.Right, bool blocks = true)
        {
            float margin = anchor == HorizontalAnchor.Right ? 844 - left - size :
                anchor == HorizontalAnchor.Center ? left + size / 2f - 422 : left;
            HudLayout.Register(name, () => HudLayout.Anchor(anchor,VerticalAnchor.Bottom,DesignUnits(new Vector2(size,size)),
                DesignUnits(new Vector2(margin,390-top-size))), HudLayout.ControlsOrder,
                () => Visible && (name != Joystick || GameScr.isAnalog != 0), blocks);
        }
        internal static bool Owns(string name) => name != null && name.StartsWith("combat.");
        internal static void Activate(string name, Skill skill, int itemId)
        {
            if (!CanInput) return;
            if (name == Attack) GameCanvas.keyPressed[Main.isPC ? 25 : 5] = true;
            else if (name == Context) GameScr.instance.UseMobileHudContext();
            else if (name.StartsWith("combat.skill.")) {
                if (GameScr.isHaveSelectSkill && !GameScr.instance.isCharging() && skill != null)
                    GameScr.instance.SelectMobileHudSkill(int.Parse(name.Substring("combat.skill.".Length)), skill);
            }
            else if (name.StartsWith("combat.item.")) MobileQuickItems.Use(int.Parse(name.Substring("combat.item.".Length)), itemId);
        }
        internal static Skill SlotSkill(int index) => GameScr.onScreenSkill != null && index < GameScr.onScreenSkill.Length
            ? GameScr.onScreenSkill[index] : null;

        internal static void Paint(mGraphics g)
        {
            if (!Visible) return;
            Rect attack = HudLayout.GetRect(Attack);
            PaintAttack(attack);
            for (int i = 0; i < 8; i++) {
                Rect rect = HudLayout.GetRect(SkillName(i));
                Skill skill = SlotSkill(i);
                bool selected = skill != null && skill == Char.myCharz().myskill;
                Button(rect, MobileCombatPointers.Held(SkillName(i)));
                if (skill == null) { PaintEmptySlot(g,rect,i); continue; }
                if (!GameScr.isHaveSelectSkill) continue;
                CircularCombatIcon.PaintSkill(g,rect,skill);
                if (selected) PaintSelectedSkill(rect);
                long remaining = skill.coolDown - mSystem.currentTimeMillis() + skill.lastTimeUseThisSkill;
                mFont.tahoma_7b_yellow.drawString(g, remaining > 0 ? string.Concat(remaining/1000) : string.Empty,
                    (int)rect.center.x, (int)rect.center.y - 5, mFont.CENTER, mFont.tahoma_7_red);
            }
            for (int i = 0; i < 3; i++) {
                Rect rect = HudLayout.GetRect(ItemName(i));
                Button(rect, MobileCombatPointers.Held(ItemName(i)));
                Item item = MobileQuickItems.Get(i);
                if (item == null) { PaintEmptySlot(g,rect,i); continue; }
                CircularCombatIcon.PaintFloatingItem(g,rect,item.template.iconID);
                long remaining = MobileQuickItems.Remaining(item);
                if (remaining > 0) {
                    CircularCombatIcon.PaintItemCooldown(rect,remaining,MobileQuickItems.CooldownMillis);
                    mFont.tahoma_7b_yellow.drawString(g,string.Concat((remaining+999)/1000),
                        (int)rect.center.x,(int)rect.center.y-mFont.tahoma_7b_yellow.getHeight()/2,mFont.CENTER,mFont.tahoma_7_red);
                }
                PaintItemBadge(g,rect,item.quantity);
            }
            if (Char.myCharz().statusMe != 14) {
                if (GameScr.isAnalog != 0) GameScr.gamePad.paint(g);
                if (Context != null) {
                    Rect rect = HudLayout.GetRect(ContextBounds);
                    Button(rect,MobileCombatPointers.Held(Context));
                    if (Context == "combat.context.dragon-ball") CircularCombatIcon.PaintImage(rect,GameScr.imgNR1);
                    else CircularCombatIcon.PaintFloatingItem(g,rect,Context == "combat.context.capsule4" ? 1088 : 1087);
                }
            }
        }
        private static void PaintEmptySlot(mGraphics g, Rect rect, int slot)
        {
            mFont.tahoma_7b_white.drawString(g,(slot+1).ToString(),(int)rect.center.x,
                (int)rect.center.y-mFont.tahoma_7b_white.getHeight()/2,mFont.CENTER,mFont.tahoma_7b_dark);
        }
        private static void Button(Rect rect, bool held) => Circle(rect, new Color32(40,24,12,158),
            held ? new Color32(255,226,168,255) : new Color32(242,180,90,255), 2);

        private static void PaintSelectedSkill(Rect rect)
        {
            // Persistent selection, separate from press feedback. Paint after the icon/
            // cooldown so neither can hide it; do not resize the skill or its hitbox.
            float outside = DesignUnit(3);
            Rect halo = new Rect(rect.x-outside,rect.y-outside,rect.width+outside*2,rect.height+outside*2);
            Circle(halo,new Color32(0,0,0,0),new Color32(255,243,107,255),2);
            Circle(rect,new Color32(0,0,0,0),new Color32(255,243,107,255),4);
        }

        private static readonly Texture2D[] attackSkins = new Texture2D[2], attackSources = new Texture2D[2];
        private static void PaintAttack(Rect rect)
        {
            int state = MobileCombatPointers.Held(Attack) ? 1 : 0;
            Image image = state == 0 ? GameScr.imgFire0 : GameScr.imgFire1;
            if (image?.texture == null) return;
            if (attackSkins[state] == null || attackSources[state] != image.texture) {
                if (attackSkins[state] != null) Object.Destroy(attackSkins[state]);
                // Smooth only this private HUD copy. Shared assets/PC sampling stay intact.
                attackSkins[state] = HudButtonSkin.CopyNative(image,filter: FilterMode.Bilinear).texture;
                attackSources[state] = image.texture;
            }
            Rect pixels = CirclePixels(rect);
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            try { GUI.matrix = Matrix4x4.identity; GUI.color = Color.white; GUI.DrawTexture(pixels,attackSkins[state]); }
            finally { GUI.matrix = matrix; GUI.color = color; }
        }

        private static GUIStyle badgeText;
        internal static void PaintItemBadge(mGraphics g, Rect slot, int quantity, Vector2? outsideLogical = null)
        {
            string text = quantity > 99 ? "99+" : string.Concat(quantity);
            mFont font = mFont.tahoma_7_white;
            int zoom = Mathf.Max(1,mGraphics.zoomLevel);
            Vector2 textSize;
            if (zoom == 1) textSize = new Vector2(font.getWidth(text),font.getHeight());
            else {
                if (badgeText == null || badgeText.font != font.myFont) {
                    badgeText = new GUIStyle(GUI.skin.label) {
                        font = font.myFont, alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset(0,0,0,0), margin = new RectOffset(0,0,0,0),
                        wordWrap = false, richText = false, fixedWidth = 0, fixedHeight = 0
                    };
                    badgeText.normal.textColor = badgeText.hover.textColor = Color.white;
                    badgeText.active.textColor = badgeText.focused.textColor = Color.white;
                }
                textSize = badgeText.CalcSize(new GUIContent(text));
            }
            int padding = Mathf.Max(3,zoom*2);
            int height = Mathf.CeilToInt(textSize.y)+2*padding;
            int width = Mathf.Max(height,Mathf.CeilToInt(textSize.x)+2*padding);
            // The same base-pixel rect positions skin and text under the safe-area matrix.
            float outsideX = (outsideLogical?.x ?? DesignUnit(6))*zoom, outsideY = (outsideLogical?.y ?? DesignUnit(4))*zoom;
            Rect badge = new Rect(Mathf.Round(slot.xMax*zoom+outsideX)-width,
                Mathf.Round(slot.yMax*zoom+outsideY)-height,width,height);
            Image skin = Notifications.NotificationSkin.BadgeIcon;
            Color color = GUI.color;
            try {
                GUI.color = Color.white;
                // Preserve round end caps; widen only the middle for 99/99+.
                float cap = height/2f, uvCap = Mathf.Min(.5f,skin.texture.height/(2f*skin.texture.width));
                GUI.DrawTextureWithTexCoords(new Rect(badge.x,badge.y,cap,height),skin.texture,new Rect(0,0,uvCap,1));
                if (width > height) GUI.DrawTextureWithTexCoords(new Rect(badge.x+cap,badge.y,width-height,height),
                    skin.texture,new Rect(.5f-.5f/skin.texture.width,0,1f/skin.texture.width,1));
                GUI.DrawTextureWithTexCoords(new Rect(badge.xMax-cap,badge.y,cap,height),skin.texture,new Rect(1-uvCap,0,uvCap,1));
                if (zoom != 1) GUI.Label(badge,text,badgeText);
            }
            finally { GUI.color = color; }
            // Bitmap font has no legacy GUI-label x offset; center its measured glyph box.
            if (zoom == 1) font.drawString(g,text,Mathf.RoundToInt(badge.center.x-textSize.x/2),
                Mathf.RoundToInt(badge.center.y-textSize.y/2),mFont.LEFT);
        }

        internal static Rect CirclePixels(Rect rect)
        {
            Rect pixels = HudButtonSkin.ScreenPixels(rect);
            int size = Mathf.Max(1,Mathf.RoundToInt(Mathf.Min(pixels.width,pixels.height)));
            return new Rect(Mathf.Round(pixels.center.x-size/2f),Mathf.Round(pixels.center.y-size/2f),size,size);
        }
        internal static int BorderWidth(int designPixels)
        {
            if (designPixels == 0) return 0;
            Rect stroke = HudButtonSkin.ScreenPixels(new Rect(0,0,DesignUnit(designPixels),DesignUnit(designPixels)));
            return Mathf.Max(1,Mathf.RoundToInt(Mathf.Min(stroke.width,stroke.height)));
        }

        private static readonly Dictionary<string,Texture2D> circles = new Dictionary<string,Texture2D>();
        // Rasterize at the final physical diameter. No enlarged tiny circle texture.
        internal static void Circle(Rect rect, Color32 fill, Color32 border, int borderPixels)
        {
            Rect pixels = CirclePixels(rect);
            int size = (int)pixels.width;
            int borderWidth = BorderWidth(borderPixels);
            string key = size + ":" + fill + ":" + border + ":" + borderWidth;
            if (!circles.TryGetValue(key,out Texture2D texture)) {
                // Bound cache across resize/orientation changes.
                if (circles.Count >= 40) { foreach (Texture2D old in circles.Values) Object.Destroy(old); circles.Clear(); }
                texture = new Texture2D(size,size,TextureFormat.RGBA32,false);
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp; texture.hideFlags = HideFlags.DontSave;
                Color32[] data = new Color32[size*size];
                float radius = size/2f;
                for (int y=0;y<size;y++) for(int x=0;x<size;x++) {
                    float distance = new Vector2(x+.5f-radius,y+.5f-radius).magnitude;
                    data[y*size+x] = distance > radius ? new Color32(0,0,0,0) : distance >= radius-borderWidth ? border : fill;
                }
                texture.SetPixels32(data); texture.Apply(false,true); circles.Add(key,texture);
            }
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            try { GUI.matrix = Matrix4x4.identity; GUI.color = Color.white;
                GUI.DrawTexture(pixels,texture); }
            finally { GUI.matrix = matrix; GUI.color = color; }
        }
    }
}
