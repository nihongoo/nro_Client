using System.Collections.Generic;
using UnityEngine;

namespace Hud
{
    internal static class CombatPcHud
    {
        internal const string Attack = "pc-combat.attack";
        private const string KeyLabelsRms = "hud-pc-key-labels";
        private static bool keyLabelsLoaded, showKeyLabels;
        internal static bool ShowKeyLabels
        {
            get {
                if (!keyLabelsLoaded) {
                    showKeyLabels = Rms.loadRMSInt(KeyLabelsRms) != 0; // Unset (-1): keep labels visible.
                    keyLabelsLoaded = true;
                }
                return showKeyLabels;
            }
        }
        internal static void ToggleKeyLabels()
        {
            if (HudLayout.IsMobileLayout) return;
            bool enabled = !ShowKeyLabels;
            Rms.saveRMSInt(KeyLabelsRms,enabled ? 1 : 0);
            showKeyLabels = enabled;
        }
        private const string ContextBounds = "pc-combat.context.bounds";
        internal static string SkillName(int slot) => "pc-combat.skill." + slot;
        internal static string ItemName(int slot) => "pc-combat.item." + slot;
        private static string Context => GameScr.isPickNgocRong ? "pc-combat.context.dragon-ball" :
            GameScr.isudungCapsun4 ? "pc-combat.context.capsule4" : GameScr.isudungCapsun3 ? "pc-combat.context.capsule3" : null;
        internal static bool Visible => !HudLayout.IsMobileLayout && GameScr.instance != null
            && GameCanvas.currentScreen == GameScr.instance && GameScr.isPaint && !GameScr.isPaintOther
            && GameScr.isPaintRada == 1 && GameScr.instance.mobCapcha == null
            && Char.myCharz().taskMaint != null && Char.myCharz().taskMaint.taskId != 0;
        internal static bool CanInput => Visible && GameCanvas.currentDialog == null && !GameCanvas.panel.isShow
            && !GameCanvas.menu.showMenu && !ChatTextField.gI().isShow && !InfoDlg.isShow && !InfoDlg.isLock
            && ChatPopup.currChatPopup == null && ChatPopup.serverChatPopUp == null
            && !Notifications.NotificationCenter.IsOpen && !Mod.CuongLe.DoHoa.isShowMenuVIP
            && !GameScr.instance.isPaintPopup() && !GameScr.instance.isOpenUI()
            && !GameScr.instance.isLockKey && !Char.isLockKey && !Controller.isStopReadMessage
            && !Char.isLoadingMap && !GameCanvas.isLoading && !Char.myCharz().isTeleport
            && !Char.myCharz().isPaintNewSkill && Char.myCharz().statusMe != 14;

        internal static void Register()
        {
            Design(Attack,288,622,64);
            for (int i=0;i<8;i++) Design(SkillName(i),366+58*i,628,52);
            for (int i=0;i<3;i++) Design(ItemName(i),848+50*i,632,44);
            // Preserve the conditional dragon-ball/capsule action, separate from assigned items.
            Design(ContextBounds,948,578,44,false);
            foreach (string name in new[] { "pc-combat.context.dragon-ball", "pc-combat.context.capsule4", "pc-combat.context.capsule3" }) {
                string entry = name;
                HudLayout.Register(entry,() => HudLayout.GetRect(ContextBounds),HudLayout.ControlsOrder,
                    () => Visible && Context == entry);
            }
        }
        private static void Design(string name, int left, int top, int size, bool blocks = true)
        {
            HudLayout.RegisterDesignAnchored(name,HorizontalAnchor.Center,VerticalAnchor.Bottom,
                new Vector2(size,size),new Vector2(left+size/2f-640,720-top-size),
                HudLayout.ControlsOrder,() => Visible && blocks,blocks);
        }
        internal static bool Owns(string name) => name != null && name.StartsWith("pc-combat.");
        internal static Skill SlotSkill(int slot)
        {
            Skill[] skills = Main.isPC || !GameCanvas.isTouch ? GameScr.keySkill : GameScr.onScreenSkill;
            return skills != null && slot >= 0 && slot < skills.Length ? skills[slot] : null;
        }
        internal static void Activate(string target, Skill capturedSkill, int itemId)
        {
            if (!CanInput) return;
            if (target == Attack) GameCanvas.keyPressed[Main.isPC ? 25 : 5] = true;
            else if (target == Context) GameScr.instance.UseMobileHudContext();
            else if (target.StartsWith("pc-combat.skill.")) {
                int slot = int.Parse(target.Substring("pc-combat.skill.".Length));
                if (GameScr.isHaveSelectSkill && !GameScr.instance.isCharging()
                    && capturedSkill != null && capturedSkill == SlotSkill(slot))
                    GameScr.instance.SelectMobileHudSkill(slot,capturedSkill);
            }
            else if (target.StartsWith("pc-combat.item."))
                MobileQuickItems.Use(int.Parse(target.Substring("pc-combat.item.".Length)),itemId);
        }

        internal static void Paint(mGraphics g)
        {
            if (!Visible) return;
            Rect attack = HudLayout.GetRect(Attack);
            PaintAttack(attack);
            KeyLabel(g,attack,Main.isPC ? "ENT" : null);
            for (int i=0;i<8;i++) {
                Rect rect = HudLayout.GetRect(SkillName(i));
                Skill skill = SlotSkill(i);
                Button(rect,SkillName(i));
                if (skill == null) PaintEmptySkill(g,rect,i);
                if (skill != null && GameScr.isHaveSelectSkill) {
                    CircularCombatIcon.PaintPcSkill(g,rect,skill);
                    if (skill == Char.myCharz().myskill)
                        PaintSelectedSkill(rect);
                    long remaining = skill.coolDown-mSystem.currentTimeMillis()+skill.lastTimeUseThisSkill;
                    if (remaining > 0) Timer(g,rect,remaining/1000);
                }
                KeyLabel(g,rect,SkillKeyLabel(i));
            }
            for (int i=0;i<3;i++) {
                Rect rect = HudLayout.GetRect(ItemName(i));
                Button(rect,ItemName(i));
                Item item = MobileQuickItems.Get(i);
                if (item == null) { PaintEmptySkill(g,rect,i); continue; }
                CircularCombatIcon.PaintPcItem(g,rect,item.template.iconID);
                long remaining = MobileQuickItems.Remaining(item);
                if (remaining > 0) {
                    CircularCombatIcon.PaintPcItemCooldown(rect,remaining,MobileQuickItems.CooldownMillis);
                    Timer(g,rect,(remaining+999)/1000);
                }
                // Stop at the next slot's left edge; lower the badge to expose more icon.
                CombatMobileHud.PaintItemBadge(g,rect,item.quantity,HudLayout.PixelsToLogical(new Vector2(6,12)));
            }
            if (Context != null) {
                Rect rect = HudLayout.GetRect(ContextBounds);
                Button(rect,Context);
                if (Context == "pc-combat.context.dragon-ball")
                    CircularCombatIcon.PaintPcImage(rect,GameScr.imgNR1);
                else CircularCombatIcon.PaintPcItem(g,rect,Context == "pc-combat.context.capsule4" ? 1088 : 1087);
            }
            // Old PC only drew this when the saved analog mode was enabled; keep that policy.
            if (GameScr.isAnalog != 0 && Char.myCharz().statusMe != 14) GameScr.gamePad?.paint(g);
            // Tutorial compatibility only; these fields no longer place or hit-test controls.
            Rect hint = HudLayout.GetRect(ItemName(0));
            GameScr.xHP = (int)hint.x; GameScr.yHP = (int)hint.y;
        }
        private static void PaintEmptySkill(mGraphics g, Rect rect, int slot)
        {
            // Slot identity, independent of the optional keyboard hint in the corner.
            mFont.tahoma_7b_white.drawString(g,(slot+1).ToString(),(int)rect.center.x,
                (int)rect.center.y-mFont.tahoma_7b_white.getHeight()/2,mFont.CENTER,mFont.tahoma_7b_dark);
        }
        private static void Timer(mGraphics g, Rect rect, long seconds) => mFont.tahoma_7b_yellow.drawString(g,
            seconds.ToString(),(int)rect.center.x,(int)rect.center.y-mFont.tahoma_7b_yellow.getHeight()/2,mFont.CENTER,mFont.tahoma_7_red);
        private static void PaintSelectedSkill(Rect rect)
        {
            // Persistent, opaque selection above the icon/cooldown. The dark edge keeps
            // the bright double outline legible on both the map and light skill icons.
            float outside = HudLayout.PixelsToLogical(3);
            Rect halo = new Rect(rect.x-outside,rect.y-outside,rect.width+2*outside,rect.height+2*outside);
            Box(halo,15,new Color32(0,0,0,0),new Color32(40,24,12,255),3);
            outside = HudLayout.PixelsToLogical(2);
            halo = new Rect(rect.x-outside,rect.y-outside,rect.width+2*outside,rect.height+2*outside);
            Box(halo,14,new Color32(0,0,0,0),new Color32(255,230,0,255),2);
            Box(rect,12,new Color32(0,0,0,0),new Color32(255,243,218,255),4);
        }
        private static readonly Texture2D[] attackSkins = new Texture2D[2], attackSources = new Texture2D[2];
        private static void PaintAttack(Rect rect)
        {
            int state = CombatPcPointer.Held(Attack) || Hover(Attack) ? 1 : 0;
            Image image = state == 0 ? GameScr.imgFire0 : GameScr.imgFire1;
            if (image?.texture == null) return;
            if (attackSkins[state] == null || attackSources[state] != image.texture) {
                if (attackSkins[state] != null) Object.Destroy(attackSkins[state]);
                attackSkins[state] = HudButtonSkin.CopyNative(image,filter: FilterMode.Bilinear).texture;
                attackSources[state] = image.texture;
            }
            Rect pixels = HudButtonSkin.ScreenPixels(rect);
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            try { GUI.matrix = Matrix4x4.identity; GUI.color = Color.white; GUI.DrawTexture(pixels,attackSkins[state]); }
            finally { GUI.matrix = matrix; GUI.color = color; }
        }
        private static string SkillKeyLabel(int slot)
        {
            if (!Main.isPC || !GameCanvas.isMoveNumberPad) return null;
            if (TField.isQwerty) return (slot+1).ToString();
            // The non-QWERTY branch only safely maps 7/8 to slots 0/1 on PC.
            // Its old key 9 lookup uses index 21; leave that unrelated keyboard bug untouched.
            return slot < 2 ? (slot+7).ToString() : null;
        }
        private static bool Hover(string name) => CanInput && HudLayout.HitTest(new Vector2(GameCanvas.pxMouse,GameCanvas.pyMouse)) == name;
        private static void Button(Rect rect, string name) => Box(rect,12,new Color32(40,24,12,158),
            Hover(name) || CombatPcPointer.Held(name) ? new Color32(255,226,168,255) : new Color32(242,180,90,255),2);
        private static GUIStyle keyText;
        // Match MapListsHud.PanelColor (#28180c), fully opaque for legible glyphs.
        private static readonly Color32 KeyLabelColor = new Color32(40,24,12,255);
        private static void KeyLabel(mGraphics g, Rect rect, string text)
        {
            if (!ShowKeyLabels || string.IsNullOrEmpty(text)) return;
            mFont font = mFont.tahoma_7_whiteSmall;
            bool native = mGraphics.zoomLevel != 1 && font.myFont != null;
            if (native && (keyText == null || keyText.font != font.myFont)) {
                keyText = new GUIStyle(GUI.skin.label) {
                    font = font.myFont, fontSize = 0, alignment = TextAnchor.UpperLeft,
                    padding = new RectOffset(0,0,0,0), margin = new RectOffset(0,0,0,0),
                    fixedWidth = 0, fixedHeight = 0, wordWrap = false, richText = false
                };
                keyText.normal.textColor = keyText.hover.textColor = KeyLabelColor;
                keyText.active.textColor = keyText.focused.textColor = keyText.normal.textColor;
            }
            Vector2 textSize = native ? keyText.CalcSize(new GUIContent(text))
                : new Vector2(font.getWidth(text),font.getHeight());
            Rect buttonPixels = HudButtonSkin.ScreenPixels(rect);
            Rect label = new Rect(buttonPixels.x+PhysicalUnit(2),buttonPixels.y+PhysicalUnit(1),
                Mathf.Ceil(textSize.x)+1,Mathf.Ceil(textSize.y)+1);
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            try {
                // Align the glyph box itself to the corner, without the old chip padding
                // or centered-box inset. All labels share the same physical origin offsets.
                GUI.matrix = Matrix4x4.identity; GUI.color = Color.white;
                if (native) GUI.Label(label,text,keyText);
            }
            finally { GUI.matrix = matrix; GUI.color = color; }
            if (!native) font.drawTintedString(g,text,
                Mathf.RoundToInt(rect.x+HudLayout.PixelsToLogical(2)),
                Mathf.RoundToInt(rect.y+HudLayout.PixelsToLogical(1)),KeyLabelColor);
        }
        internal static int PhysicalUnit(int designPixels)
        {
            Rect pixels = HudButtonSkin.ScreenPixels(new Rect(0,0,HudLayout.PixelsToLogical(designPixels),HudLayout.PixelsToLogical(designPixels)));
            return Mathf.Max(1,Mathf.RoundToInt(Mathf.Min(pixels.width,pixels.height)));
        }
        private static readonly Dictionary<string,Texture2D> boxes = new Dictionary<string,Texture2D>();
        private static void Box(Rect rect, int radius, Color32 fill, Color32 border, int stroke)
        {
            Rect pixels = HudButtonSkin.ScreenPixels(rect);
            int size = Mathf.Max(1,Mathf.RoundToInt(Mathf.Min(pixels.width,pixels.height)));
            int r = PhysicalUnit(radius), b = PhysicalUnit(stroke);
            string key = size+":"+r+":"+b+":"+fill+":"+border;
            if (!boxes.TryGetValue(key,out Texture2D texture)) {
                if (boxes.Count >= 32) ClearBoxes();
                texture = new Texture2D(size,size,TextureFormat.RGBA32,false) {
                    filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave
                };
                Color32[] data = new Color32[size*size];
                for (int y=0;y<size;y++) for (int x=0;x<size;x++) {
                    if (!CircularCombatIcon.InsideRounded(x+.5f,y+.5f,size,r)) continue;
                    bool inner = x>=b && y>=b && x<size-b && y<size-b
                        && CircularCombatIcon.InsideRounded(x-b+.5f,y-b+.5f,size-b*2,Mathf.Max(0,r-b));
                    data[y*size+x] = inner ? fill : border;
                }
                texture.SetPixels32(data); texture.Apply(false,true); boxes.Add(key,texture);
            }
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            try { GUI.matrix = Matrix4x4.identity; GUI.color = Color.white;
                GUI.DrawTexture(new Rect(Mathf.Round(pixels.center.x-size/2f),Mathf.Round(pixels.center.y-size/2f),size,size),texture); }
            finally { GUI.matrix = matrix; GUI.color = color; }
        }
        internal static void ClearBoxes()
        {
            foreach (Texture2D texture in boxes.Values) Object.Destroy(texture);
            boxes.Clear();
            for (int i=0;i<attackSkins.Length;i++) {
                if (attackSkins[i] != null) Object.Destroy(attackSkins[i]);
                attackSkins[i] = attackSources[i] = null;
            }
        }
    }
}
