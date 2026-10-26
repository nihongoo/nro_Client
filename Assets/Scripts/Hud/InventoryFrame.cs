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
            EquipmentBackground = 15196114, SlotBackground = 9993045, MoneyBackground = 11837316, PanelBackground = 14338484;
        internal const int Padding=6, Gap=4, FrameInset=3, FrameBorder=0xC78A36;
        internal const float MoneyIconHeight = 10, MoneyGap = 3;
        internal static Rect Inset(Rect rect,float padding=Padding) => new Rect(rect.x+padding,rect.y+padding,Mathf.Max(0,rect.width-2*padding),Mathf.Max(0,rect.height-2*padding));
        internal static void PaintSection(mGraphics g,Rect rect,int background) => Rounded(g,rect,background,255,FrameBorder);
        internal static bool Visible => GameScr.instance != null && GameCanvas.currentScreen == GameScr.instance
            && GameCanvas.panel != null && GameCanvas.panel.isShow
            && GameCanvas.panel.InventoryFrameActive && GameCanvas.panel.InventoryFrameTabIds.Length > 0
            && GameCanvas.currentDialog == null
            && !GameCanvas.menu.showMenu && !Notifications.NotificationCenter.IsOpen;
        // Cache the fixed frame geometry; scrolling is applied only by row providers.
        private struct FrameMetrics
        {
            internal int Width, Height, Zoom;
            internal bool Mobile;
            internal mFont Font;
            internal float FitScale, EquipmentSize, EquipmentGap, EquipmentLeft, BagSize, BagGap, BagLeft;
            internal Rect Root, Close, Tabs, Money, Bag, Detail;
        }
        private static FrameMetrics metrics;
        private static bool metricsReady;
        private static long metricsFrame=-1, metricsState=-1, metricsVersion;
        internal static long MetricsVersion {get{EnsureMetrics();return metricsVersion;}}
        private static float Unit(float pixels,int zoom,float fitScale) => pixels / Mathf.Max(1,zoom) * fitScale;
        private static void EnsureMetrics()
        {
            long tick=HudLayout.FrameId,state=HudLayout.StateGeneration;
            if(metricsReady&&metricsFrame==tick&&metricsState==state&&metrics.Width==GameCanvas.w
                &&metrics.Height==GameCanvas.h&&metrics.Zoom==mGraphics.zoomLevel&&ReferenceEquals(metrics.Font,mFont.tahoma_7_yellow))return;

                bool mobile=HudLayout.IsMobileLayout;
                metricsFrame=tick;metricsState=state;
                if(metricsReady&&metrics.Width==GameCanvas.w&&metrics.Height==GameCanvas.h&&metrics.Zoom==mGraphics.zoomLevel
                    &&metrics.Mobile==mobile&&ReferenceEquals(metrics.Font,mFont.tahoma_7_yellow))return;

                var next=new FrameMetrics {Width=GameCanvas.w,Height=GameCanvas.h,Zoom=mGraphics.zoomLevel,Mobile=mobile,Font=mFont.tahoma_7_yellow};
                next.FitScale=GameCanvas.h * Mathf.Max(1,mGraphics.zoomLevel) / (mobile?390f:720f);
                float width=Mathf.Max(Unit(mobile?300:380,next.Zoom,next.FitScale),
                    next.Font.getWidth("99.999.999.999")+next.Font.getWidth("1.000.000")+42);
                next.Root=HudLayout.Anchor(HorizontalAnchor.Left,VerticalAnchor.Top,new Vector2(width,GameCanvas.h),Vector2.zero);
                next.Close=new Rect(next.Root.xMax-21,3,18,18);
                next.Tabs=new Rect(2,48,Mathf.Floor(next.Root.width)-4,24);
                next.Money=new Rect(1,next.Root.yMax-20,next.Root.width-2,19);
                next.Bag=new Rect(3,73,next.Root.width-6,Mathf.Max(0,next.Money.y-75));
                next.Detail=HudLayout.Anchor(HorizontalAnchor.Right,VerticalAnchor.Top,
                    new Vector2(Mathf.Min(Unit(mobile?250:320,next.Zoom,next.FitScale),Mathf.Max(0,GameCanvas.w-next.Root.width-8)),GameCanvas.h),Vector2.zero);
                next.EquipmentSize=Unit(mobile?40:64,next.Zoom,next.FitScale);
                next.EquipmentGap=Mathf.Max(4,Unit(12,next.Zoom,next.FitScale));
                next.EquipmentLeft=(next.Root.width-next.EquipmentSize*3-next.EquipmentGap*2)/2;
                next.BagSize=Unit(mobile?44:56,next.Zoom,next.FitScale);
                next.BagGap=Mathf.Max(6,Unit(12,next.Zoom,next.FitScale));
                next.BagLeft=(next.Root.width-next.BagSize*InventoryBag.Columns-next.BagGap*(InventoryBag.Columns-1))/2;
                metrics=next;metricsReady=true;metricsVersion++;
        }
        // Preserve the original operation order, including division before multiplication.
        internal static float Unit(float pixels) {EnsureMetrics();return Unit(pixels,metrics.Zoom,metrics.FitScale);}
        private static Rect RootBounds {get{EnsureMetrics();return metrics.Root;}}
        private static Rect CloseBounds {get{EnsureMetrics();return metrics.Close;}}
        private static Rect TabsBounds {get{EnsureMetrics();return metrics.Tabs;}}
        private static Rect MoneyBounds {get{EnsureMetrics();return metrics.Money;}}
        private static Rect BagBounds {get{EnsureMetrics();return metrics.Bag;}}
        private static Rect DetailBounds {get{EnsureMetrics();return metrics.Detail;}}
        internal static string Slot(int i) => "inventory.equipment." + i;
        internal static string Tab(int i) => "inventory.tab." + i;
        internal static void Register()
        {
            for (int i = 0; i < bagCells.Length; i++) bagCells[i] = "inventory.bag.cell." + i;
            HudLayout.Register(Root,()=>RootBounds,HudLayout.OverlayOrder+10,()=>Visible,cacheVersion:()=>MetricsVersion);
            HudLayout.Register(Close,()=>CloseBounds,HudLayout.OverlayOrder+12,()=>Visible,cacheVersion:()=>MetricsVersion);
            HudLayout.Register(Tabs,()=>TabsBounds,HudLayout.OverlayOrder+11,()=>false,false,()=>MetricsVersion);
            // Keep old internal tab IDs: the remapping never changes gameplay handlers.
            for (int i = 0; i < 6; i++) {
                int slot = i;
                HudLayout.Register(Tab(i), () => {
                    int count = GameCanvas.panel?.InventoryFrameTabIds.Length ?? 0;
                    if (count == 0) return new Rect();
                    Rect tabs = HudLayout.GetRect(Tabs);
                    float left = Mathf.Floor(tabs.x + slot * tabs.width / count);
                    float right = Mathf.Floor(tabs.x + (slot + 1) * tabs.width / count);
                    return new Rect(left, tabs.y, right - left - 1, tabs.height);
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
            HudLayout.Register(Money,()=>MoneyBounds,HudLayout.OverlayOrder+11,()=>Visible,cacheVersion:()=>MetricsVersion);
            HudLayout.Register(Bag,()=>BagBounds,HudLayout.OverlayOrder+11,()=>Visible&&GameCanvas.panel.currentTabIndex==1,cacheVersion:()=>MetricsVersion);
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
            HudLayout.Register(Detail,()=>DetailBounds,HudLayout.OverlayOrder+14,
                ()=>Visible&&GameCanvas.panel.InventoryBagDetailOpen,cacheVersion:()=>MetricsVersion);
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
            EnsureMetrics();
            float size=metrics.EquipmentSize,gap=metrics.EquipmentGap,left=metrics.EquipmentLeft;
            return new Rect(left + slot % 3 * (size + gap), 73 + Padding + 12 + Gap + slot / 3 * (size + 9) - (GameCanvas.panel?.InventoryFrameScroll ?? 0), size, size);
        }
        internal static float EquipmentBottom => EquipmentBounds(8).yMax + 10;
        internal static Rect Intersection(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin),
            Mathf.Max(a.yMin, b.yMin), Mathf.Max(Mathf.Max(a.xMin, b.xMin), Mathf.Min(a.xMax, b.xMax)),
            Mathf.Max(Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.yMax, b.yMax)));
        internal static float BagSize {get{EnsureMetrics();return metrics.BagSize;}}
        internal static float BagGap {get{EnsureMetrics();return metrics.BagGap;}}
        internal static Rect FilterBounds(int index)
        {
            Rect panel = HudLayout.GetRect(Root);
            return new Rect(4 + index * (panel.width - 8) / 4, EquipmentBottom + 16, (panel.width - 8) / 4 - 1, 22);
        }
        internal static Rect BagCellBounds(int index)
        {
            EnsureMetrics();float left=metrics.BagLeft;
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
        internal static int StarSlots(Item item) => Mathf.Max(0, Option(item, Item.OPT_MAXSTARSLOT));
        internal static int FilledStars(Item item) => Mathf.Min(StarSlots(item), Mathf.Max(0, Option(item, Item.OPT_STARSLOT)));
        // Match the legacy item popup: ordinary, special filled, and reinforced empty slots.
        internal static int StarKind(Item item, int index)
        {
            if (index < 0 || index >= StarSlots(item)) return -1;
            if (index < FilledStars(item)) return index < 7 ? 1 : 2;
            return index >= 7 && index < Option(item, 228) ? 3 : 0;
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
        ? (Char.myCharz().havePet ? inventoryMainTabs : inventoryMainTabsWithoutDisciple) : currentTabName.Length == 4 ? inventoryFourTabs : inventoryFiveTabs;
    private static readonly int[] inventoryMainTabs = new[] { 0, 1, 2, 3, 7, 6 };
    private static readonly int[] inventoryMainTabsWithoutDisciple = new[] { 0, 1, 2, 3, 6 };
    private static readonly int[] inventoryFourTabs = new[] { 0, 1, 2, 3 };
    private static readonly int[] inventoryFiveTabs = new[] { 0, 1, 2, 3, 4 };
    private static readonly int[] inventoryBodySlots = { 0, 1, 2, 3, 4, 6, 5, 7, -1 };
    private static readonly string[] inventorySlotLabels = { "Áo", "Quần", "Găng", "Giày", "Rada", "GLT", "Skin", "Tai", "Pet" };
    private readonly InventoryItemVisuals.FittedTextRow inventoryTabText = new InventoryItemVisuals.FittedTextRow();
    private readonly Rect[] inventoryTabTextBounds = new Rect[6];
    private readonly string[] inventoryTabTextLabels = new string[6];
    private static readonly string[] inventoryMainTabLabels = { "Nhiệm\nVụ", "Hành\nTrang", "Kỹ\nNăng", "Bang\nHội", "Đệ\nTử", "Chức\nNăng" };
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
        ClosePotentialEditor();
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
        if (currentTabIndex == DiscipleTab && !Char.myCharz().havePet) { SelectInventoryFrameTab(1); return; }
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
            newSelected = 0; size_tab = 0; ITEM_HEIGHT = 24;
            SyncInventoryBag();
            currentListLength = (Char.myCharz().arrItemBody?.Length ?? 0) + (Char.myCharz().arrItemBag?.Length ?? 0);
            int last = InventoryBag.DisplaySlots(inventoryBagMap.Count, inventoryBagFilter) - 1;
            cmyLim = Mathf.Max(0, (int)(InventoryFrame.BagCellBounds(last).yMax + cmy + 5 - content.yMax));
            int previousScroll = cmy;
            cmy = Mathf.Clamp(cmy, 0, cmyLim); cmtoY = Mathf.Clamp(cmtoY, 0, cmyLim);
            if (cmy != previousScroll) HudLayout.Invalidate();
        } else if (DisciplePanelActive) {
            SyncDisciplePanel();
        } else if (currentTabIndex == 2) {
            SyncSkillPanel();
        } else if (currentTabIndex == 4 || currentTabIndex == 5 || currentTabIndex == 6) {
            cmyLim = Mathf.Max(0, currentListLength * ITEM_HEIGHT - hScroll);
            cmy = Mathf.Clamp(cmy, 0, cmyLim); cmtoY = Mathf.Clamp(cmtoY, 0, cmyLim);
        }
    }

    private void SelectInventoryFrameTab(int id)
    {
        if (id == DiscipleTab && !Char.myCharz().havePet) return;
        if (id >= currentTabName.Length && id != DiscipleTab) return;
        ClosePotentialEditor();
        CloseInventoryBagDetail(); CloseDiscipleDetail();
        currentTabIndex = id; lastTabIndex[type] = id;
        selected = -1; cp = null; cmy = cmtoY = 0;
        switch (id) {
            case 0: setTabTask(); break;
            case 1: setTabInventory(true); break;
            case 2: setTabSkill(); break;
            case 3: setTabClans(); break;
            case 4: setTabMod(); break;
            case 6: setTabTool(); break;
            case DiscipleTab: OpenDiscipleTab(); break;
        }
        HudLayout.Invalidate(); SyncInventoryFrame();
    }

    private bool UpdateInventoryFrameInput()
    {
         SyncInventoryFrame();EnsureInventoryDetailLayout();
        if (isClose || cmtoX > 0) return true;
        if (!isShow || (chatTField != null && chatTField.isShow) || InfoDlg.isShow
            || tabIcon != null && tabIcon.isShow || GameCanvas.currentDialog != null || GameCanvas.menu.showMenu) return false;
        if (currentTabIndex == 2) return UpdateSkillPanelInput();
        string hit = HudLayout.HitTest(new Vector2(GameCanvas.px, GameCanvas.py));
        if (GameCanvas.isPointerJustDown) {
            inventoryPressed = hit;
            if (hit == null || !hit.StartsWith("inventory.bag.cell.")) inventoryBagDoubleTap.Reset();
            if (hit != InventoryFrame.Slot(7)) inventoryEquipmentDoubleTap.Reset();
            inventoryDragY = GameCanvas.py; inventoryDragX = GameCanvas.px; inventoryDragScroll = cmy; inventoryDragging = false;
        }
        UpdateInventoryBagDetailDrag();
        bool frameGesture = currentTabIndex == 1 || DisciplePanelActive || inventoryPressed == InventoryFrame.Close
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
            if (click && HandleDiscipleClick(hit)) { GameCanvas.clearAllPointerEvent(); return true; }
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
            if (DisciplePanelActive && discipleSkill >= 0) CloseDiscipleDetail();
            else if (InventoryBagDetailOpen) CloseInventoryBagDetail(); else CloseInventoryFrame();
            return true;
        }
        if (DisciplePanelActive) return UpdateDiscipleKeyboard();
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

    // Header ownership follows the active tab; body rendering never paints over it.
    private void PaintInventoryHeader(mGraphics g)
    {
        Char character = DisciplePanelActive ? Char.myPetz() : Char.myCharz();
        if (currentTabIndex == 0) {
            mFont.tahoma_7b_white.drawString(g, character.cName, 46, 1, 0);
            mFont.tahoma_7_yellow.drawString(g, mResources.vitality + ": " + character.cStamina + "/" + character.cMaxStamina, 46, 12, 0);
            mFont.tahoma_7_yellow.drawString(g, character.me ? character.getStrLevel() : character.currStrLevel, 46, 23, 0);
            mFont.tahoma_7_yellow.drawString(g, mResources.power + ": " + NinjaUtil.getMoneys(character.cPower), 46, 34, 0);
        } else if (DisciplePanelActive) {
            PaintDiscipleHeader(g);
        } else if (currentTabIndex == 1 || currentTabIndex == 2) {
            mFont.tahoma_7_yellow.drawString(g, mResources.HP + ": " + character.cHP + "/" + character.cHPFull, 46, 1, 0);
            mFont.tahoma_7_yellow.drawString(g, mResources.KI + ": " + character.cMP + "/" + character.cMPFull, 46, 12, 0);
            mFont.tahoma_7_yellow.drawString(g, mResources.hit_point + ": " + character.cDamFull + "  " + mResources.critical + ": " + character.cCriticalFull + "%", 46, 23, 0);
            mFont.tahoma_7_yellow.drawString(g, mResources.giamsatthuong + ": " + character.cGiamST + "%  " + mResources.critdame + ": " + character.cCritDameFull + "%", 46, 34, 0);
        } else if (currentTabIndex == 3) {
            if (mainTabName.Length == 7) paintClanInfo(g);
            else paintToolInfo(g);
        } else if (currentTabIndex == 4 || currentTabIndex == 5) {
            paintToolInfoMod(g);
        } else {
            paintToolInfo(g);
        }
    }

    private void PaintInventoryFrame(mGraphics g)
    {
        SyncInventoryFrame();
        Rect root;

        GameScr.resetTranslate(g);
        g.setColor(0, .25f); g.fillRect(0, 0, GameCanvas.w, GameCanvas.h);
        root = HudLayout.GetRect(InventoryFrame.Root);
        GameCanvas.paintz.paintFrameSimple(0, 0, W, H, g);
        g.setColor(InventoryFrame.SlotBackground); g.fillRect(1, 1, W - 2, 46);
        InventoryFrame.Clip(g, new Rect(2, 1, W - 4, 46));

        SmallImage.drawSmallImage(g, (DisciplePanelActive ? Char.myPetz() : Char.myCharz()).avatarz(), 23, 44, 0, 33);
        InventoryFrame.Clip(g, new Rect(46, 1, W - 49, 46));
        PaintInventoryHeader(g);
        Rect close = HudLayout.GetRect(InventoryFrame.Close);
        InventoryFrame.Clip(g, root);
        InventoryFrame.Rounded(g, close, InventoryFrame.EquipmentBackground);
        g.drawImage(imgX, (int)close.center.x, (int)close.center.y, mGraphics.HCENTER | mGraphics.VCENTER);

        int[] ids = InventoryFrameTabIds;
        int activeTab = currentTabName.Length >= 7 && (currentTabIndex == 4 || currentTabIndex == 5) ? 6 : currentTabIndex;
        for (int i = 0; i < ids.Length; i++) {
            Rect tab = HudLayout.GetRect(InventoryFrame.Tab(i));
            InventoryFrame.Clip(g, tab);
            PopUp.paintPopUp(g, (int)tab.x, (int)tab.y, (int)tab.width, (int)tab.height,
                activeTab == ids[i] ? 1 : 0, true);
            inventoryTabTextBounds[i] = tab;
            inventoryTabTextLabels[i] = currentTabName.Length >= 7 ? inventoryMainTabLabels[Array.IndexOf(inventoryMainTabs, ids[i])] : string.Join("\n", currentTabName[ids[i]]);
        }
        inventoryTabText.Paint(mFont.tahoma_7_grey, inventoryTabTextLabels, inventoryTabTextBounds,
            ids.Length, HudLayout.GetRect(InventoryFrame.Tabs), Array.IndexOf(ids, activeTab), mFont.tahoma_7_grey.color1, mFont.tahoma_7_green2.color1);

        GameScr.resetTranslate(g); InventoryFrame.Clip(g, new Rect(1, 73, W - 2, H - 94));
        switch (currentTabIndex) {
            case 0: paintTask(g); break;
            case 1:

                InventoryFrame.PaintSection(g, new Rect(3, 73 - cmy, W - 6, InventoryFrame.EquipmentBottom + cmy - 73), InventoryFrame.EquipmentBackground);
                mFont.tahoma_7b_dark.drawString(g, "Trang bị", W / 2, 73 + InventoryFrame.Padding - cmy, mFont.CENTER);
                for (int slot = 0; slot < 9; slot++) PaintInventoryEquipment(g, slot);
                PaintInventoryBag(g);

                break;
            case 2: PaintSkillPanel(g); break;
            case 3: paintClans(g); break;
            case 4: paintModTabs(g); break;
            case 5: paintBox(g); break;
            case 6: paintTools(g); break;
            case DiscipleTab: PaintDisciplePanel(g); break;
        }
        GameScr.resetTranslate(g); PaintInventoryMoney(g);

        if (DisciplePanelActive && discipleSkill >= 0) PaintDiscipleSkillDetail(g);
        else if (currentTabIndex == 2) PaintSkillDetail(g);
        else {
                if (InventoryBagDetailOpen) PaintInventoryBagDetail(g); else paintDetail(g);
        }

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
            if (item != null) InventoryItemVisuals.PaintLevel(g, rect, viewport, level, false);
        }
        InventoryItemVisuals.PaintStars(rect, viewport, item);
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
