using System;
using Hud;
using UnityEngine;

namespace Hud
{
    internal static class InventoryFrame
    {
        internal const string Root = "inventory.frame", Close = "inventory.close", Tabs = "inventory.tabs",
            Bag = "inventory.legacy-bag", Money = "inventory.money";
        internal const int Background = 0x3b2515, Border = 0x6b4426, Dark = 0x27170c,
            Accent = 0xf2b45a, Red = 0xd93a2b;
        internal static bool Visible => GameScr.instance != null && GameCanvas.currentScreen == GameScr.instance
            && GameCanvas.panel != null && GameCanvas.panel.isShow
            && GameCanvas.panel.InventoryFrameActive && GameCanvas.panel.InventoryFrameTabIds.Length > 0
            && GameCanvas.currentDialog == null
            && !GameCanvas.menu.showMenu && !Notifications.NotificationCenter.IsOpen;
        // Mockup pixels are reference-design pixels, as in CombatMobileHud. Font and
        // native game icons remain logical-sized; MotherCanvas zoom is not changed.
        internal static float Unit(float pixels) => HudLayout.PixelsToLogical(pixels)
            * (GameCanvas.h * Mathf.Max(1, mGraphics.zoomLevel) / HudLayout.DesignReference.y);
        internal static string Slot(int i) => "inventory.equipment." + i;
        internal static string Tab(int i) => "inventory.tab." + i;
        internal static void Register()
        {
            HudLayout.Register(Root, () => HudLayout.Anchor(HorizontalAnchor.Left, VerticalAnchor.Top,
                new Vector2(Mathf.Max(Unit(HudLayout.IsMobileLayout ? 300 : 380),
                    mFont.tahoma_7_yellow.getWidth("99.999.999.999") + mFont.tahoma_7_yellow.getWidth("1.000.000") + 42), GameCanvas.h), Vector2.zero),
                HudLayout.OverlayOrder + 10, () => Visible);
            HudLayout.Register(Close, () => new Rect(HudLayout.GetRect(Root).xMax - 21, 3, 18, 18),
                HudLayout.OverlayOrder + 12, () => Visible);
            HudLayout.Register(Tabs, () => new Rect(2, 48, HudLayout.GetRect(Root).width - 4, 24),
                HudLayout.OverlayOrder + 11, () => false, false);
            // Keep old internal tab IDs: the remapping never changes gameplay handlers.
            for (int i = 0; i < 5; i++) {
                int slot = i;
                HudLayout.Register(Tab(i), () => {
                    int count = GameCanvas.panel?.InventoryFrameTabIds.Length ?? 0;
                    if (count == 0) return new Rect();
                    Rect tabs = HudLayout.GetRect(Tabs);
                    return new Rect(tabs.x + slot * tabs.width / count, tabs.y, tabs.width / count, tabs.height);
                }, HudLayout.OverlayOrder + 12,
                    () => Visible && slot < GameCanvas.panel.InventoryFrameTabIds.Length);
            }
            for (int i = 0; i < 9; i++) {
                int slot = i;
                HudLayout.Register(Slot(i), () => EquipmentBounds(slot), HudLayout.OverlayOrder + 12,
                    () => Visible && GameCanvas.panel.currentTabIndex == 1);
            }
            HudLayout.Register(Money, () => {
                Rect panel = HudLayout.GetRect(Root);
                return new Rect(1, panel.yMax - 20, panel.width - 2, 19);
            }, HudLayout.OverlayOrder + 11, () => Visible);
            HudLayout.Register(Bag, () => {
                Rect panel = HudLayout.GetRect(Root), last = EquipmentBounds(8), money = HudLayout.GetRect(Money);
                float y = last.yMax + 13;
                return new Rect(3, y, panel.width - 6, Mathf.Max(0, money.y - y - 2));
            }, HudLayout.OverlayOrder + 11, () => Visible && GameCanvas.panel.currentTabIndex == 1);
        }
        internal static Rect EquipmentBounds(int slot)
        {
            float size = Unit(HudLayout.IsMobileLayout ? 40 : 64);
            float gap = Mathf.Max(4, Unit(12));
            float left = (HudLayout.GetRect(Root).width - size * 3 - gap * 2) / 2;
            return new Rect(left + slot % 3 * (size + gap), 86 + slot / 3 * (size + 9), size, size);
        }
        internal static int Option(Item item, int id)
        {
            if (item?.itemOption == null) return 0;
            foreach (ItemOption option in item.itemOption)
                if (option?.optionTemplate != null && option.optionTemplate.id == id) return option.param;
            return 0;
        }
        // Set identities are item-option IDs from nro1.sql, independent of rarity 34/35/36.
        internal static int SetColor(Item item)
        {
            if (item?.itemOption == null) return Border;
            foreach (ItemOption option in item.itemOption) {
                if (option?.optionTemplate == null) continue;
                switch (option.optionTemplate.id) {
                    case 127: return 0xffd166; // Thên Xin Hăng
                    case 128: return 0x62d98b; // Kirin
                    case 129: return 0xff914d; // Sôngôku
                    case 130: return 0x4dd9cb; // Picolo
                    case 131: return 0xb8e866; // Ốc tiêu
                    case 132: return 0xc58aff; // Pikkoro Daimao
                    case 133: return 0xff667d; // Kakarot
                    case 134: return 0x66aaff; // Ca Đíc
                    case 135: return 0xd8bc96; // Nappa
                    case 233: return 0xf48bd4; // Gohan
                    case 237: return 0x82e6f7; // Nail chiến binh Namếc
                    case 241: return 0xa5a0ff; // Cađic M
                    case 245: return 0xf5f0c5; // Thần Vũ Trụ Kaio
                }
            }
            return Border;
        }
        internal static void Fill(mGraphics g, Rect rect, int color)
        {
            g.setColor(color); g.fillRect((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        }
        internal static void Clip(mGraphics g, Rect rect) => g.setClip((int)rect.x, (int)rect.y,
            Mathf.Max(0, (int)rect.width), Mathf.Max(0, (int)rect.height));
        internal static void Text(mGraphics g, mFont font, string text, int x, int y, int align, int rgb = 0xfff1d0)
        {
            if (mGraphics.zoomLevel == 1) { font.drawString(g, text, x, y, align); return; }
            Color previous = font.color1;
            try {
                font.color1 = new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
                font._drawString(g, text, x, y, align);
            } finally { font.color1 = previous; }
        }
    }
}

public partial class Panel
{
    internal bool InventoryFrameActive => type == 0 && Equals(GameCanvas.panel);
    internal int[] InventoryFrameTabIds => currentTabName == null ? Array.Empty<int>() : currentTabName.Length >= 7
        ? inventoryMainTabs : currentTabName.Length == 4 ? inventoryFourTabs : inventoryFiveTabs;
    private static readonly int[] inventoryMainTabs = new[] { 0, 1, 2, 3, 6 };
    private static readonly int[] inventoryFourTabs = new[] { 0, 1, 2, 3 };
    private static readonly int[] inventoryFiveTabs = new[] { 0, 1, 2, 3, 4 };
    private static readonly int[] inventoryBodySlots = { 0, 1, 2, 3, 4, 6, 5, 7, -1 };
    private static readonly string[] inventorySlotLabels = { "Áo", "Quần", "Găng", "Giày", "Rada", "GLT", "Skin", "Tai", "Pet" };
    private string inventoryPressed;
    private int inventoryDragY, inventoryDragScroll;
    private bool inventoryDragging;

    private void OpenInventoryAppearanceSettings()
    {
        setTypeMain();
        SelectInventoryFrameTab(4);
        cmx = cmtoX = 0;
    }

    private void CloseInventoryFrame()
    {
        if (currentTabName.Length >= 7 && currentTabIndex == 4) {
            lastTabIndex[0] = 6;
            setTypeOption();
            HudLayout.Invalidate();
        } else hide();
    }

    private void SyncInventoryFrame()
    {
        Rect panel = HudLayout.GetRect(InventoryFrame.Root);
        X = Y = 0; W = (int)panel.width; H = (int)panel.height;
        if (!isClose && cmtoX <= 0) cmx = 0;
        Rect content = currentTabIndex == 1 ? HudLayout.GetRect(InventoryFrame.Bag)
            : new Rect(3, 76, W - 6, Mathf.Max(0, H - 98));
        xScroll = (int)content.x; yScroll = (int)content.y;
        wScroll = (int)content.width; hScroll = (int)content.height;
        Rect close = HudLayout.GetRect(InventoryFrame.Close);
        cmdClose.x = (int)close.x; cmdClose.y = (int)close.y;
        cmdClose.w = (int)close.width; cmdClose.h = (int)close.height;
        if (currentTabIndex == 1) {
            isnewInventory = false; newSelected = 0; size_tab = 0; ITEM_HEIGHT = 24;
            currentListLength = Char.myCharz().arrItemBody.Length + Char.myCharz().arrItemBag.Length;
            cmyLim = Mathf.Max(0, Char.myCharz().arrItemBag.Length * ITEM_HEIGHT - hScroll);
            cmy = Mathf.Clamp(cmy, 0, cmyLim); cmtoY = Mathf.Clamp(cmtoY, 0, cmyLim);
        }
    }

    private void SelectInventoryFrameTab(int id)
    {
        if (id >= currentTabName.Length) return;
        currentTabIndex = id; lastTabIndex[type] = id;
        selected = -1; cp = null; cmy = cmtoY = 0;
        switch (id) {
            case 0: setTabTask(); break;
            case 1: setTabInventory(true); break;
            case 2: setTabSkill(); break;
            case 3: setTabClans(); break;
            case 4: setTabMod(); break;
            case 5: setTabBox(); break;
            case 6: setTabTool(); break;
        }
        HudLayout.Invalidate(); SyncInventoryFrame();
    }

    private bool UpdateInventoryFrameInput()
    {
        SyncInventoryFrame();
        if (isClose || cmtoX > 0) return true;
        if (!isShow || (chatTField != null && chatTField.isShow) || InfoDlg.isShow
            || tabIcon != null && tabIcon.isShow || GameCanvas.currentDialog != null || GameCanvas.menu.showMenu) return false;
        string hit = HudLayout.HitTest(new Vector2(GameCanvas.px, GameCanvas.py));
        if (GameCanvas.isPointerJustDown) {
            inventoryPressed = hit;
            inventoryDragY = GameCanvas.py; inventoryDragScroll = cmy; inventoryDragging = false;
        }
        if (inventoryPressed == InventoryFrame.Bag && GameCanvas.isPointerDown) {
            int delta = GameCanvas.py - inventoryDragY;
            if (System.Math.Abs(delta) > 4) inventoryDragging = true;
            if (inventoryDragging) cmy = cmtoY = Mathf.Clamp(inventoryDragScroll - delta, 0, cmyLim);
        }
        if (GameCanvas.isPointerJustRelease) {
            bool click = GameCanvas.isPointerClick && !inventoryDragging && hit == inventoryPressed;
            inventoryPressed = null;
            if (click && hit == InventoryFrame.Close) { CloseInventoryFrame(); GameCanvas.clearAllPointerEvent(); return true; }
            if (click && hit != null && hit.StartsWith("inventory.tab.")) {
                SelectInventoryFrameTab(InventoryFrameTabIds[int.Parse(hit.Substring("inventory.tab.".Length))]);
                GameCanvas.clearAllPointerEvent(); return true;
            }
            if (currentTabIndex == 1 && click) {
                if (hit != null && hit.StartsWith("inventory.equipment.")) {
                    int slot = int.Parse(hit.Substring("inventory.equipment.".Length));
                    selected = inventoryBodySlots[slot];
                    if (selected >= Char.myCharz().arrItemBody.Length) selected = -1;
                    if (selected >= 0) doFireInventory();
                    GameCanvas.clearAllPointerEvent(); return true;
                }
                if (hit == InventoryFrame.Bag && hScroll > 0) {
                    int row = (GameCanvas.py - yScroll + cmy) / ITEM_HEIGHT;
                    if (row >= 0 && row < Char.myCharz().arrItemBag.Length) {
                        selected = Char.myCharz().arrItemBody.Length + row; doFireInventory();
                    }
                    GameCanvas.clearAllPointerEvent(); return true;
                }
            }
        }
        int previousTab = Main.isPC ? 23 : 4, nextTab = Main.isPC ? 24 : 6;
        if (GameCanvas.keyPressed[previousTab] || GameCanvas.keyPressed[nextTab]) {
            int[] tabs = InventoryFrameTabIds;
            int index = Array.IndexOf(tabs, currentTabName.Length >= 7 && currentTabIndex == 4 ? 6 : currentTabIndex);
            int direction = GameCanvas.keyPressed[nextTab] ? 1 : -1;
            SelectInventoryFrameTab(tabs[(index + direction + tabs.Length) % tabs.Length]);
            GameCanvas.keyPressed[previousTab] = GameCanvas.keyPressed[nextTab] = false; return true;
        }
        if (GameCanvas.keyPressed[13]) { GameCanvas.keyPressed[13] = false; CloseInventoryFrame(); return true; }
        if (currentTabIndex != 1) return false;
        int up = Main.isPC ? 21 : 2, down = Main.isPC ? 22 : 8, fire = Main.isPC ? 25 : 5;
        if (GameCanvas.keyPressed[up] || GameCanvas.keyPressed[down]) {
            int count = currentListLength;
            if (count > 0) selected = (selected + (GameCanvas.keyPressed[down] ? 1 : -1) + count) % count;
            if (selected >= Char.myCharz().arrItemBody.Length) {
                int row = selected - Char.myCharz().arrItemBody.Length;
                cmy = cmtoY = Mathf.Clamp(row * ITEM_HEIGHT, 0, cmyLim);
            }
            GameCanvas.keyPressed[up] = GameCanvas.keyPressed[down] = false;
        }
        if (GameCanvas.keyPressed[12] || GameCanvas.keyPressed[fire]) {
            GameCanvas.keyPressed[12] = GameCanvas.keyPressed[fire] = false;
            if (selected >= 0 && selected < currentListLength) doFireInventory();
        }
        return true;
    }

    private void PaintInventoryFrame(mGraphics g)
    {
        SyncInventoryFrame(); GameScr.resetTranslate(g);
        g.setColor(0, .25f); g.fillRect(0, 0, GameCanvas.w, GameCanvas.h);
        Rect root = HudLayout.GetRect(InventoryFrame.Root);
        InventoryFrame.Fill(g, root, InventoryFrame.Background);
        g.setColor(InventoryFrame.Border); g.drawRect(0, 0, W - 1, H - 1);
        InventoryFrame.Clip(g, new Rect(2, 1, W - 4, 46));
        SmallImage.drawSmallImage(g, Char.myCharz().avatarz(), 23, 44, 0, 33);
        Char character = Char.myCharz();
        InventoryFrame.Clip(g, new Rect(46, 1, W - 49, 46));
        InventoryFrame.Text(g, mFont.tahoma_7, mResources.HP + ": " + character.cHP + "/" + character.cHPFull, 46, 1, 0);
        InventoryFrame.Text(g, mFont.tahoma_7, mResources.KI + ": " + character.cMP + "/" + character.cMPFull, 46, 12, 0);
        InventoryFrame.Text(g, mFont.tahoma_7, mResources.hit_point + ": " + character.cDamFull + "  " + mResources.critical + ": " + character.cCriticalFull + "%", 46, 23, 0);
        InventoryFrame.Text(g, mFont.tahoma_7, mResources.giamsatthuong + ": " + character.cGiamST + "%  " + mResources.critdame + ": " + character.cCritDameFull + "%", 46, 34, 0);
        Rect close = HudLayout.GetRect(InventoryFrame.Close);
        InventoryFrame.Clip(g, root); InventoryFrame.Fill(g, close, InventoryFrame.Red);
        mFont.tahoma_7b_white.drawString(g, "X", (int)close.center.x, (int)close.y + 3, mFont.CENTER);
        int[] ids = InventoryFrameTabIds;
        int activeTab = currentTabName.Length >= 7 && currentTabIndex == 4 ? 6 : currentTabIndex;
        for (int i = 0; i < ids.Length; i++) {
            Rect tab = HudLayout.GetRect(InventoryFrame.Tab(i));
            InventoryFrame.Fill(g, tab, activeTab == ids[i] ? InventoryFrame.Border : InventoryFrame.Dark);
            InventoryFrame.Clip(g, tab);
            string[] words = currentTabName[ids[i]];
            for (int line = 0; line < words.Length; line++)
                InventoryFrame.Text(g, mFont.tahoma_7b_white, words[line], (int)tab.center.x, (int)tab.y + 1 + line * 11,
                    mFont.CENTER, activeTab == ids[i] ? 0xfff1d0 : 0xc9a77c);
        }
        GameScr.resetTranslate(g); InventoryFrame.Clip(g, new Rect(1, 73, W - 2, H - 94));
        switch (currentTabIndex) {
            case 0: paintTask(g); break;
            case 1:
                InventoryFrame.Text(g, mFont.tahoma_7b_yellow, "Trang bị", W / 2, 73, mFont.CENTER, InventoryFrame.Accent);
                for (int slot = 0; slot < 9; slot++) PaintInventoryEquipment(g, slot);
                if (hScroll > 0) paintInventory(g, true);
                break;
            case 2: paintSkill(g); break;
            case 3: paintClans(g); break;
            case 4: paintModTabs(g); break;
            case 5: paintBox(g); break;
            case 6: paintTools(g); break;
        }
        GameScr.resetTranslate(g); PaintInventoryMoney(g);
        paintDetail(g);
        if (tabIcon != null && tabIcon.isShow) tabIcon.paint(g);
        GameScr.resetTranslate(g);
    }

    private void PaintInventoryEquipment(mGraphics g, int slot)
    {
        Rect rect = HudLayout.GetRect(InventoryFrame.Slot(slot));
        int index = inventoryBodySlots[slot]; Item[] body = Char.myCharz().arrItemBody;
        Item item = index >= 0 && index < body.Length ? body[index] : null;
        int color = InventoryFrame.Border;
        if (slot < 5 && item != null) {
            color = InventoryFrame.SetColor(item);
        }
        bool chosen = index >= 0 && index == selected;
        if (chosen) {
            g.setColor(InventoryFrame.Accent, .25f);
            g.fillRect((int)rect.x - 2, (int)rect.y - 2, (int)rect.width + 4, (int)rect.height + 4);
        }
        InventoryFrame.Fill(g, rect, InventoryFrame.Dark);
        g.setColor(chosen ? InventoryFrame.Accent : color);
        if (item == null) {
            for (int x = (int)rect.x; x < rect.xMax; x += 5) {
                g.drawLine(x, (int)rect.y, Mathf.Min(x + 2, (int)rect.xMax), (int)rect.y);
                g.drawLine(x, (int)rect.yMax, Mathf.Min(x + 2, (int)rect.xMax), (int)rect.yMax);
            }
            for (int y = (int)rect.y; y < rect.yMax; y += 5) {
                g.drawLine((int)rect.x, y, (int)rect.x, Mathf.Min(y + 2, (int)rect.yMax));
                g.drawLine((int)rect.xMax, y, (int)rect.xMax, Mathf.Min(y + 2, (int)rect.yMax));
            }
            InventoryFrame.Clip(g, rect);
            InventoryFrame.Text(g, mFont.tahoma_7_white, inventorySlotLabels[slot], (int)rect.center.x, (int)rect.center.y - 5, mFont.CENTER, 0xc9a77c);
        } else {
            g.drawRect((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            InventoryFrame.Clip(g, rect);
            SmallImage.drawSmallImage(g, item.template.iconID, (int)rect.center.x, (int)rect.center.y, 0, 3);
        }
        InventoryFrame.Clip(g, new Rect(1, 73, W - 2, H - 94));
        if (slot < 6) {
            int level = Mathf.Clamp(InventoryFrame.Option(item, Item.OPT_LVITEM), 0, 7);
            int stars = Mathf.Clamp(InventoryFrame.Option(item, Item.OPT_STARSLOT), 0, 6);
            if (item != null) {
                string label = "Lv" + level;
                int width = mFont.tahoma_7.getWidth(label) + 4;
                g.setColor(InventoryFrame.Accent);
                g.fillRoundRect((int)rect.xMax - width + 3, (int)rect.yMax - 9, width, 11, 5, 5);
                InventoryFrame.Text(g, mFont.tahoma_7, label, (int)rect.xMax - width + 5, (int)rect.yMax - 9, 0, InventoryFrame.Dark);
            }
            for (int i = 0; i < 6; i++) {
                g.setColor(i < stars ? InventoryFrame.Accent : InventoryFrame.Border);
                g.fillRoundRect((int)rect.center.x - 11 + i * 4, (int)rect.yMax + 4, 3, 3, 3, 3);
            }
        }
    }

    private void PaintInventoryMoney(mGraphics g)
    {
        Rect row = HudLayout.GetRect(InventoryFrame.Money);
        InventoryFrame.Clip(g, row); InventoryFrame.Fill(g, row, InventoryFrame.Dark);
        // Reserve the maximum text widths independently; a value never moves its neighbor.
        int gemWidth = mFont.tahoma_7_yellow.getWidth("1.000.000");
        int goldWidth = mFont.tahoma_7_yellow.getWidth("99.999.999.999");
        int gemRight = W - 5, gemLeft = gemRight - gemWidth;
        g.drawImage(imgXu, 10, (int)row.center.y, 3);
        InventoryFrame.Clip(g, new Rect(20, row.y, goldWidth, row.height));
        InventoryFrame.Text(g, mFont.tahoma_7_yellow, Char.myCharz().xu.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.'),
            20, (int)row.y + 4, 0);
        InventoryFrame.Clip(g, row); g.drawImage(imgLuong, gemLeft - 9, (int)row.center.y, 3);
        InventoryFrame.Clip(g, new Rect(gemLeft, row.y, gemWidth, row.height));
        InventoryFrame.Text(g, mFont.tahoma_7_yellow, Char.myCharz().luong.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.'),
            gemRight, (int)row.y + 4, mFont.RIGHT);
        InventoryFrame.Clip(g, HudLayout.ScreenBounds);
    }
}
