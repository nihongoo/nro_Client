using System;
using Hud;
using UnityEngine;

namespace Hud
{
    internal static class InventoryFrame
    {
        internal const string Root = "inventory.frame", Close = "inventory.close", Tabs = "inventory.tabs",
            Bag = "inventory.bag", Money = "inventory.money", Equipment = "inventory.equipment.bounds",
            Detail = "inventory.detail", DetailClose = "inventory.detail.close", DetailUpper = "inventory.detail.upper", DetailLower = "inventory.detail.lower";
        private static readonly string[] bagCells = new string[256];
        internal static string BagCell(int index) => bagCells[index];
        internal static string Filter(int index) => "inventory.filter." + index;
        // Original Panel.paintInventory / paintBottomMoneyInfo colors.
        internal const int Border = 6702080, Accent = 16383818,
            EquipmentBackground = 15196114, SlotBackground = 9993045, MoneyBackground = 11837316;
        internal const float MoneyIconHeight = 10, MoneyGap = 3;
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
            for (int i = 0; i < bagCells.Length; i++) bagCells[i] = "inventory.bag.cell." + i;
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
                HudLayout.Register(Slot(i), () => Intersection(EquipmentBounds(slot), HudLayout.GetRect(Equipment)), HudLayout.OverlayOrder + 12,
                    () => Visible && GameCanvas.panel.currentTabIndex == 1);
            }
            HudLayout.Register(Equipment, () => HudLayout.GetRect(Bag), HudLayout.OverlayOrder + 11,
                () => Visible && GameCanvas.panel.currentTabIndex == 1);
            HudLayout.Register(Money, () => {
                Rect panel = HudLayout.GetRect(Root);
                return new Rect(1, panel.yMax - 20, panel.width - 2, 19);
            }, HudLayout.OverlayOrder + 11, () => Visible);
            HudLayout.Register(Bag, () => {
                Rect panel = HudLayout.GetRect(Root), money = HudLayout.GetRect(Money);
                return new Rect(3, 73, panel.width - 6, Mathf.Max(0, money.y - 75));
            }, HudLayout.OverlayOrder + 11, () => Visible && GameCanvas.panel.currentTabIndex == 1);
            for (int i = 0; i < bagCells.Length; i++) {
                int slot = i;
                HudLayout.Register(BagCell(i), () => {
                    Rect cell = BagCellBounds(slot);
                    float left = Mathf.Max(0, BagGap - 4);
                    return Intersection(new Rect(cell.x - left, cell.y, cell.width + left + 2, cell.height + 2), HudLayout.GetRect(Bag));
                },
                    HudLayout.OverlayOrder + 12, () => Visible && GameCanvas.panel.currentTabIndex == 1
                        && slot < GameCanvas.panel.InventoryBagCellCount);
            }
            for (int i = 0; i < 4; i++) {
                int filter = i;
                HudLayout.Register(Filter(i), () => Intersection(FilterBounds(filter), new Rect(3, 73, HudLayout.GetRect(Root).width - 6, Mathf.Max(0, HudLayout.GetRect(Money).y - 75))),
                    HudLayout.OverlayOrder + 13, () => Visible && GameCanvas.panel.currentTabIndex == 1);
            }
            HudLayout.Register(Detail, () => HudLayout.Anchor(HorizontalAnchor.Right, VerticalAnchor.Top,
                new Vector2(Mathf.Min(Unit(HudLayout.IsMobileLayout ? 250 : 320), Mathf.Max(0, GameCanvas.w - HudLayout.GetRect(Root).width - 8)), GameCanvas.h), Vector2.zero),
                HudLayout.OverlayOrder + 14, () => Visible && GameCanvas.panel.InventoryBagDetailOpen);
            HudLayout.Register(DetailClose, () => new Rect(HudLayout.GetRect(Detail).xMax - 21, 3, 18, 18),
                HudLayout.OverlayOrder + 15, () => Visible && GameCanvas.panel.InventoryBagDetailOpen);
            HudLayout.Register(DetailUpper, () => GameCanvas.panel.InventoryDetailUpperBounds, HudLayout.OverlayOrder + 15,
                () => Visible && GameCanvas.panel.InventoryBagDetailOpen);
            HudLayout.Register(DetailLower, () => GameCanvas.panel.InventoryDetailLowerBounds, HudLayout.OverlayOrder + 15,
                () => Visible && GameCanvas.panel.InventoryBagDetailOpen);
            for (int i = 0; i < 12; i++) {
                int button = i;
                HudLayout.Register("inventory.detail.button." + i, () => GameCanvas.panel.InventoryDetailButtonBounds(button), HudLayout.OverlayOrder + 16,
                    () => Visible && GameCanvas.panel.InventoryBagDetailOpen && button < GameCanvas.panel.InventoryDetailButtonCount);
            }
        }
        internal static Rect EquipmentBounds(int slot)
        {
            float size = Unit(HudLayout.IsMobileLayout ? 40 : 64);
            float gap = Mathf.Max(4, Unit(12));
            float left = (HudLayout.GetRect(Root).width - size * 3 - gap * 2) / 2;
            return new Rect(left + slot % 3 * (size + gap), 86 + slot / 3 * (size + 9) - (GameCanvas.panel?.InventoryFrameScroll ?? 0), size, size);
        }
        internal static float EquipmentBottom => EquipmentBounds(8).yMax + 10;
        internal static Rect Intersection(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin),
            Mathf.Max(a.yMin, b.yMin), Mathf.Max(Mathf.Max(a.xMin, b.xMin), Mathf.Min(a.xMax, b.xMax)),
            Mathf.Max(Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.yMax, b.yMax)));
        internal static float BagSize => Unit(HudLayout.IsMobileLayout ? 44 : 56);
        internal static float BagGap => Mathf.Max(6, Unit(12));
        internal static Rect FilterBounds(int index)
        {
            Rect panel = HudLayout.GetRect(Root);
            return new Rect(4 + index * (panel.width - 8) / 4, EquipmentBottom + 16, (panel.width - 8) / 4 - 1, 22);
        }
        internal static Rect BagCellBounds(int index)
        {
            float left = (HudLayout.GetRect(Root).width - BagSize * InventoryBag.Columns - BagGap * (InventoryBag.Columns - 1)) / 2;
            return new Rect(left + index % InventoryBag.Columns * (BagSize + BagGap),
                FilterBounds(0).yMax + 6 + index / InventoryBag.Columns * (BagSize + BagGap), BagSize, BagSize);
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
        internal static void Rounded(mGraphics g, Rect rect, int color, byte opacity = 255, int borderColor = 0)
        {
            // Sprite regions use Graphics.DrawTexture and do not inherit GUI.color.
            // Pass a cached recolored copy; GUI.color controls opacity of the center fills only.
            Color previous = GUI.color;
            try {
                GUI.color = new Color32(255, 255, 255, opacity);
                PopUp.paintPopUp(g, (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height,
                    color, false, InventoryFrameSkin.Corners(color, borderColor, opacity), borderColor);
            } finally { GUI.color = previous; }
        }
        internal static void Clip(mGraphics g, Rect rect) => g.setClip((int)rect.x, (int)rect.y,
            Mathf.Max(0, (int)rect.width), Mathf.Max(0, (int)rect.height));
        // Transparent padding of the unchanged x1/x2/x3/x4 currency sprites.
        // UVs are verified against PNG alpha bounds by Tests/Inventory/run.py.
        internal static Rect MoneySpriteUv(bool gold, int zoom)
        {
            if (gold && zoom == 2) return new Rect(1f/26, 1f/20, 24f/26, 18f/20);
            if (gold && zoom == 3) return new Rect(1f/39, 1f/30, 37f/39, 28f/30);
            if (gold && zoom == 4) return new Rect(2f/52, 2f/40, 48f/52, 36f/40);
            if (!gold && zoom == 4) return new Rect(1f/48, 0, 46f/48, 1);
            return new Rect(0, 0, 1, 1);
        }
        internal static Rect MoneyIconBounds(Rect row, float textLeft, float aspect)
            => new Rect(textLeft - MoneyGap - MoneyIconHeight * aspect,
                row.center.y - MoneyIconHeight / 2, MoneyIconHeight * aspect, MoneyIconHeight);
        internal static Rect MoneyTextBounds(Rect row, int width, bool gold, float goldAspect)
            => new Rect(gold ? row.x + 3 + MoneyIconHeight * goldAspect + MoneyGap : row.xMax - 3 - width,
                row.y, width, row.height);
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
    private bool inventoryAppearanceFromSettings;
    internal int InventoryFrameScroll => InventoryFrameActive && currentTabIndex == 1 ? cmy : 0;

    private void OpenInventoryAppearanceSettings()
    {
        setTypeMain();
        inventoryAppearanceFromSettings = true;
        SelectInventoryFrameTab(4);
        cmx = cmtoX = 0;
    }

    private void CloseInventoryFrame()
    {
        CloseInventoryBagDetail();
        if (currentTabName.Length >= 7 && currentTabIndex == 4 && inventoryAppearanceFromSettings) {
            lastTabIndex[0] = 6;
            setTypeOption();
            HudLayout.Invalidate();
        } else if (currentTabIndex == 4 || currentTabIndex == 5) SelectInventoryFrameTab(6);
        else hide();
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
            SyncInventoryBag();
            currentListLength = (Char.myCharz().arrItemBody?.Length ?? 0) + (Char.myCharz().arrItemBag?.Length ?? 0);
            int last = InventoryBag.Rows(inventoryBagMap.Count) * InventoryBag.Columns - 1;
            cmyLim = Mathf.Max(0, (int)(InventoryFrame.BagCellBounds(last).yMax + cmy + 5 - content.yMax));
            int previousScroll = cmy;
            cmy = Mathf.Clamp(cmy, 0, cmyLim); cmtoY = Mathf.Clamp(cmtoY, 0, cmyLim);
            if (cmy != previousScroll) HudLayout.Invalidate();
        } else if (currentTabIndex == 4 || currentTabIndex == 5 || currentTabIndex == 6) {
            cmyLim = Mathf.Max(0, currentListLength * ITEM_HEIGHT - hScroll);
            cmy = Mathf.Clamp(cmy, 0, cmyLim); cmtoY = Mathf.Clamp(cmtoY, 0, cmyLim);
        }
    }

    private void SelectInventoryFrameTab(int id)
    {
        if (id >= currentTabName.Length) return;
        CloseInventoryBagDetail();
        currentTabIndex = id; lastTabIndex[type] = id;
        selected = -1; cp = null; cmy = cmtoY = 0;
        switch (id) {
            case 0: setTabTask(); break;
            case 1: setTabInventory(true); break;
            case 2: setTabSkill(); break;
            case 3: setTabClans(); break;
            case 4: setTabMod(); break;
            case 6: setTabTool(); break;
        }
        HudLayout.Invalidate(); SyncInventoryFrame();
    }

    private bool UpdateInventoryFrameInput()
    {
        SyncInventoryFrame();
        EnsureInventoryDetailLayout();
        if (isClose || cmtoX > 0) return true;
        if (!isShow || (chatTField != null && chatTField.isShow) || InfoDlg.isShow
            || tabIcon != null && tabIcon.isShow || GameCanvas.currentDialog != null || GameCanvas.menu.showMenu) return false;
        string hit = HudLayout.HitTest(new Vector2(GameCanvas.px, GameCanvas.py));
        if (GameCanvas.isPointerJustDown) {
            inventoryPressed = hit;
            if (hit == null || !hit.StartsWith("inventory.bag.cell.")) inventoryBagDoubleTap.Reset();
            if (hit != InventoryFrame.Slot(7)) inventoryEquipmentDoubleTap.Reset();
            inventoryDragY = GameCanvas.py; inventoryDragX = GameCanvas.px; inventoryDragScroll = cmy; inventoryDragging = false;
        }
        UpdateInventoryBagDetailDrag();
        bool frameGesture = currentTabIndex == 1 || inventoryPressed == InventoryFrame.Close
            || inventoryPressed != null && inventoryPressed.StartsWith("inventory.tab.");
        // The legacy pointer stream keeps JustDown set until a handler consumes it.
        // Capture the origin once, including when panel.updateKey runs twice per tick.
        // Other tabs still need their original pointer stream for legacy list handlers.
        if (frameGesture) GameCanvas.isPointerJustDown = false;
        if (IsInventoryBagDragTarget(inventoryPressed) && (GameCanvas.isPointerDown || GameCanvas.isPointerJustRelease)) {
            int delta = GameCanvas.py - inventoryDragY;
            if (System.Math.Abs(delta) > 4 || System.Math.Abs(GameCanvas.px - inventoryDragX) > 4) {
                inventoryDragging = true; inventoryBagDoubleTap.Reset();
                inventoryEquipmentDoubleTap.Reset();
            }
            if (inventoryDragging) {
                cmy = cmtoY = Mathf.Clamp(inventoryDragScroll - delta, 0, cmyLim); HudLayout.Invalidate();
            }
        }
        if (GameCanvas.isPointerJustRelease) {
            bool click = GameCanvas.isPointerClick && !inventoryDragging && hit == inventoryPressed;
            inventoryPressed = null;
            if (click && HandleInventoryBagClick(hit)) { GameCanvas.clearAllPointerEvent(); return true; }
            if (click && hit == InventoryFrame.Close) { CloseInventoryFrame(); GameCanvas.clearAllPointerEvent(); return true; }
            if (click && hit != null && hit.StartsWith("inventory.tab.")) {
                SelectInventoryFrameTab(InventoryFrameTabIds[int.Parse(hit.Substring("inventory.tab.".Length))]);
                GameCanvas.clearAllPointerEvent(); return true;
            }
            if (currentTabIndex == 1 && click) {
                if (hit != null && hit.StartsWith("inventory.equipment.")) {
                    int slot = int.Parse(hit.Substring("inventory.equipment.".Length));
                    HandleInventoryEquipmentClick(slot);
                    GameCanvas.clearAllPointerEvent(); return true;
                }
            }
            if (frameGesture) GameCanvas.clearAllPointerEvent();
        }
        int previousTab = Main.isPC ? 23 : 4, nextTab = Main.isPC ? 24 : 6;
        if (GameCanvas.keyPressed[previousTab] || GameCanvas.keyPressed[nextTab]) {
            int[] tabs = InventoryFrameTabIds;
            int index = Array.IndexOf(tabs, currentTabName.Length >= 7 && currentTabIndex == 4 ? 6 : currentTabIndex);
            int direction = GameCanvas.keyPressed[nextTab] ? 1 : -1;
            SelectInventoryFrameTab(tabs[(index + direction + tabs.Length) % tabs.Length]);
            GameCanvas.keyPressed[previousTab] = GameCanvas.keyPressed[nextTab] = false; return true;
        }
        if (GameCanvas.keyPressed[13]) {
            GameCanvas.keyPressed[13] = false;
            if (InventoryBagDetailOpen) CloseInventoryBagDetail(); else CloseInventoryFrame();
            return true;
        }
        if (currentTabIndex != 1) return false;
        int up = Main.isPC ? 21 : 2, down = Main.isPC ? 22 : 8, fire = Main.isPC ? 25 : 5;
        if (GameCanvas.keyPressed[up] || GameCanvas.keyPressed[down]) {
            MoveInventoryBagSelection(GameCanvas.keyPressed[down] ? 1 : -1);
            if (selected >= (Char.myCharz().arrItemBody?.Length ?? 0)) {
                int visual = inventoryBagMap.IndexOf(selected - (Char.myCharz().arrItemBody?.Length ?? 0));
                if (visual >= 0) ScrollInventoryBagIntoView(InventoryFrame.BagCellBounds(visual));
            }
            else {
                int slot = Array.IndexOf(inventoryBodySlots, selected);
                if (slot >= 0) ScrollInventoryBagIntoView(InventoryFrame.EquipmentBounds(slot));
            }
            GameCanvas.keyPressed[up] = GameCanvas.keyPressed[down] = false;
        }
        if (GameCanvas.keyPressed[12] || GameCanvas.keyPressed[fire]) {
            GameCanvas.keyPressed[12] = GameCanvas.keyPressed[fire] = false;
            inventoryBagDoubleTap.Reset();
            inventoryEquipmentDoubleTap.Reset();
            if (selected >= 0 && selected < currentListLength) {
                Item[] body = Char.myCharz().arrItemBody, bag = Char.myCharz().arrItemBag;
                Item item = selected < (body?.Length ?? 0) ? body[selected] : bag[selected - (body?.Length ?? 0)];
                if (item != null) OpenInventoryItemDetail(item);
            }
        }
        return true;
    }

    private void PaintInventoryFrame(mGraphics g)
    {
        SyncInventoryFrame(); GameScr.resetTranslate(g);
        g.setColor(0, .25f); g.fillRect(0, 0, GameCanvas.w, GameCanvas.h);
        Rect root = HudLayout.GetRect(InventoryFrame.Root);
        GameCanvas.paintz.paintFrameSimple(0, 0, W, H, g);
        g.setColor(InventoryFrame.SlotBackground); g.fillRect(1, 1, W - 2, 46);
        InventoryFrame.Clip(g, new Rect(2, 1, W - 4, 46));
        SmallImage.drawSmallImage(g, Char.myCharz().avatarz(), 23, 44, 0, 33);
        Char character = Char.myCharz();
        InventoryFrame.Clip(g, new Rect(46, 1, W - 49, 46));
        mFont.tahoma_7_yellow.drawString(g, mResources.HP + ": " + character.cHP + "/" + character.cHPFull, 46, 1, 0);
        mFont.tahoma_7_yellow.drawString(g, mResources.KI + ": " + character.cMP + "/" + character.cMPFull, 46, 12, 0);
        mFont.tahoma_7_yellow.drawString(g, mResources.hit_point + ": " + character.cDamFull + "  " + mResources.critical + ": " + character.cCriticalFull + "%", 46, 23, 0);
        mFont.tahoma_7_yellow.drawString(g, mResources.giamsatthuong + ": " + character.cGiamST + "%  " + mResources.critdame + ": " + character.cCritDameFull + "%", 46, 34, 0);
        Rect close = HudLayout.GetRect(InventoryFrame.Close);
        InventoryFrame.Clip(g, root);
        InventoryFrame.Rounded(g, close, InventoryFrame.EquipmentBackground);
        g.drawImage(imgX, (int)close.center.x, (int)close.center.y, mGraphics.HCENTER | mGraphics.VCENTER);
        int[] ids = InventoryFrameTabIds;
        int activeTab = currentTabName.Length >= 7 && (currentTabIndex == 4 || currentTabIndex == 5) ? 6 : currentTabIndex;
        for (int i = 0; i < ids.Length; i++) {
            Rect tab = HudLayout.GetRect(InventoryFrame.Tab(i));
            InventoryFrame.Clip(g, tab);
            PopUp.paintPopUp(g, (int)tab.x, (int)tab.y, (int)tab.width - 1, (int)tab.height,
                activeTab == ids[i] ? 1 : 0, true);
            string[] words = currentTabName[ids[i]];
            for (int line = 0; line < words.Length; line++)
                (activeTab == ids[i] ? mFont.tahoma_7_green2 : mFont.tahoma_7_grey).drawString(g,
                    words[line], (int)tab.center.x, (int)tab.y + 1 + line * 11, mFont.CENTER);
        }
        GameScr.resetTranslate(g); InventoryFrame.Clip(g, new Rect(1, 73, W - 2, H - 94));
        switch (currentTabIndex) {
            case 0: paintTask(g); break;
            case 1:
                InventoryFrame.Rounded(g, new Rect(3, 73 - cmy, W - 6, InventoryFrame.EquipmentBottom + cmy - 73), InventoryFrame.EquipmentBackground);
                mFont.tahoma_7b_dark.drawString(g, "Trang bị", W / 2, 73 - cmy, mFont.CENTER);
                for (int slot = 0; slot < 9; slot++) PaintInventoryEquipment(g, slot);
                PaintInventoryBag(g);
                break;
            case 2: paintSkill(g); break;
            case 3: paintClans(g); break;
            case 4: paintModTabs(g); break;
            case 5: paintBox(g); break;
            case 6: paintTools(g); break;
        }
        GameScr.resetTranslate(g); PaintInventoryMoney(g);
        if (InventoryBagDetailOpen) PaintInventoryBagDetail(g); else paintDetail(g);
        if (tabIcon != null && tabIcon.isShow) tabIcon.paint(g);
        GameScr.resetTranslate(g);
    }

    private void PaintInventoryEquipment(mGraphics g, int slot)
    {
        Rect rect = InventoryFrame.EquipmentBounds(slot), viewport = HudLayout.GetRect(InventoryFrame.Equipment);
        if (rect.yMax + 9 < viewport.y || rect.y > viewport.yMax) return;
        InventoryFrame.Clip(g, viewport);
        int index = inventoryBodySlots[slot]; Item[] body = Char.myCharz().arrItemBody;
        Item item = body != null && index >= 0 && index < body.Length ? body[index] : null;
        int color = InventoryFrame.Border;
        if (slot < 5 && item != null) {
            color = InventoryFrame.SetColor(item);
        }
        bool chosen = index >= 0 && index == selected;
        int border = chosen ? InventoryFrame.Accent : color == InventoryFrame.Border ? 0 : color;
        InventoryFrame.Rounded(g, rect, InventoryFrame.SlotBackground, 255, border);
        if (slot < 5 && item != null && color != InventoryFrame.Border)
            InventoryItemVisuals.PaintAura(rect, viewport, color);
        if (item == null) {
            InventoryFrame.Clip(g, InventoryFrame.Intersection(rect, viewport));
            mFont.tahoma_7_white.drawString(g, inventorySlotLabels[slot], (int)rect.center.x, (int)rect.center.y - 5, mFont.CENTER);
        } else {
            InventoryFrame.Clip(g, InventoryFrame.Intersection(rect, viewport));
            SmallImage.drawSmallImage(g, item.template.iconID, (int)rect.center.x, (int)rect.center.y, 0, 3);
        }
        InventoryFrame.Clip(g, viewport);
        if (slot < 6) {
            int level = Mathf.Clamp(InventoryFrame.Option(item, Item.OPT_LVITEM), 0, 7);
            int stars = Mathf.Clamp(InventoryFrame.Option(item, Item.OPT_STARSLOT), 0, 6);
            if (item != null) InventoryItemVisuals.PaintLevel(g, rect, viewport, level, false);
            InventoryItemVisuals.PaintStars(rect, viewport, stars);
        }
    }

    private void PaintInventoryMoney(mGraphics g)
    {
        Rect row = HudLayout.GetRect(InventoryFrame.Money);
        InventoryFrame.Clip(g, row); InventoryFrame.Rounded(g, row, InventoryFrame.MoneyBackground);
        // Reserve the maximum text widths independently; a value never moves its neighbor.
        // getWidth truncates physical font metrics to logical integers; keep the last pixel.
        int gemWidth = mFont.tahoma_7_yellow.getWidth("1.000.000") + 1;
        int goldWidth = mFont.tahoma_7_yellow.getWidth("99.999.999.999") + 1;
        Rect goldUv = InventoryFrame.MoneySpriteUv(true, mGraphics.zoomLevel);
        Rect gemUv = InventoryFrame.MoneySpriteUv(false, mGraphics.zoomLevel);
        float goldAspect = imgXu.texture.width * goldUv.width / (imgXu.texture.height * goldUv.height);
        float gemAspect = imgLuong.texture.width * gemUv.width / (imgLuong.texture.height * gemUv.height);
        Rect goldText = InventoryFrame.MoneyTextBounds(row, goldWidth, true, goldAspect);
        Rect gemText = InventoryFrame.MoneyTextBounds(row, gemWidth, false, goldAspect);
        PaintInventoryMoneyIcon(imgXu, goldUv, InventoryFrame.MoneyIconBounds(row, goldText.x, goldAspect));
        PaintInventoryMoneyIcon(imgLuong, gemUv, InventoryFrame.MoneyIconBounds(row, gemText.x, gemAspect));
        PaintInventoryMoneyText(g, goldText, Char.myCharz().xu);
        PaintInventoryMoneyText(g, gemText, Char.myCharz().luong);
        InventoryFrame.Clip(g, HudLayout.ScreenBounds);
    }

    private static void PaintInventoryMoneyIcon(Image image, Rect uv, Rect bounds)
    {
        int zoom = mGraphics.zoomLevel;
        Color previous = GUI.color;
        try {
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(new Rect(bounds.x * zoom, bounds.y * zoom,
                bounds.width * zoom, bounds.height * zoom), image.texture, uv);
        } finally { GUI.color = previous; }
    }

    private static GUIStyle inventoryMoneyTextStyle;
    private static void PaintInventoryMoneyText(mGraphics g, Rect bounds, long value)
    {
        string text = value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.');
        mFont font = mFont.tahoma_7_yellow;
        if (mGraphics.zoomLevel == 1) {
            InventoryFrame.Clip(g, bounds);
            font.drawString(g, text, (int)bounds.x, (int)(bounds.center.y - font.getHeight() / 2f), mFont.LEFT);
            return;
        }
        if (inventoryMoneyTextStyle == null || inventoryMoneyTextStyle.font != font.myFont)
            inventoryMoneyTextStyle = new GUIStyle(GUI.skin.label) {
                font = font.myFont, alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0),
                wordWrap = false, richText = false, fixedWidth = 0, fixedHeight = 0
            };
        inventoryMoneyTextStyle.normal.textColor = inventoryMoneyTextStyle.hover.textColor = font.color1;
        inventoryMoneyTextStyle.active.textColor = inventoryMoneyTextStyle.focused.textColor = font.color1;
        int zoom = mGraphics.zoomLevel;
        Color previous = GUI.color;
        try {
            GUI.color = Color.white;
            GUI.BeginGroup(new Rect(bounds.x * zoom, bounds.y * zoom, bounds.width * zoom, bounds.height * zoom));
            try { GUI.Label(new Rect(0, 0, bounds.width * zoom, bounds.height * zoom), text, inventoryMoneyTextStyle); }
            finally { GUI.EndGroup(); }
        } finally { GUI.color = previous; }
    }
}
