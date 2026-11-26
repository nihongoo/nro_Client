using System;
using System.Collections.Generic;
using Hud;
using UnityEngine;

public partial class Panel
{
    private readonly List<int> inventoryBagMap = new List<int>();
    private readonly InventoryBagDoubleTap inventoryBagDoubleTap = new InventoryBagDoubleTap();
    private readonly InventoryBagDoubleTap inventoryEquipmentDoubleTap = new InventoryBagDoubleTap();
    private readonly List<int> inventoryBagKeyboardSlots = new List<int>();
    private InventoryBagFilter inventoryBagFilter;
    private int inventoryDragX, inventoryDetailDragScroll;
    private bool inventoryBagDetailOpen;
    private Item inventoryBagDetailItem;
    private int inventoryBagDetailQuantity;
    private static readonly string[] inventoryBagFilters = { "Tất cả", "Trang bị", "Tiêu hao", "Nguyên liệu" };
    internal int InventoryBagCellCount => inventoryBagMap.Count;
    internal bool InventoryBagDetailOpen => inventoryBagDetailOpen && (currentTabIndex == 1 || DisciplePanelActive) && inventoryBagDetailItem?.template != null;
    private bool TradeBagActive => type == TYPE_GIAODICH && currentTabIndex == 0 && Equals(GameCanvas.panel);
    private Rect TradeBagViewport => new Rect(xScroll + 3, yScroll, Mathf.Max(0, wScroll - 6), hScroll);

    private void SetTradeBag()
    {
        RefreshTradeBagBounds();
        cmy = cmtoY = Mathf.Clamp(cmyLast[currentTabIndex], 0, cmyLim);
        selected = !GameCanvas.isTouch && currentListLength > 0 ? 0 : -1;
    }

    private void RefreshTradeBagBounds()
    {
        currentListLength = Char.myCharz().arrItemBag?.Length ?? 0;
        int last = InventoryBag.DisplaySlots(currentListLength, InventoryBagFilter.All) - 1;
        Rect viewport = TradeBagViewport;
        cmyLim = Mathf.Max(0, Mathf.CeilToInt(InventoryFrame.BagCellBounds(last, viewport, 0).yMax + 3 - viewport.yMax));
        if (selected >= currentListLength) selected = -1;
        cmtoY = Mathf.Clamp(cmtoY, 0, cmyLim);
    }

    private int TradeBagSlotAt(int x, int y)
    {
        Rect viewport = TradeBagViewport;
        var point = new Vector2(x, y);
        if (!viewport.Contains(point)) return -1;
        for (int slot = 0; slot < currentListLength; slot++) {
            if (InventoryFrame.BagCellBounds(slot, viewport, cmy).Contains(point)) return slot;
        }
        return -1;
    }

    private void MoveTradeBagSelection()
    {
        int offset;
        if (GameCanvas.keyPressed[Main.isPC ? 21 : 2]) offset = -InventoryBag.Columns;
        else if (GameCanvas.keyPressed[Main.isPC ? 22 : 8]) offset = InventoryBag.Columns;
        else if (GameCanvas.keyPressed[Main.isPC ? 23 : 4]) offset = -1;
        else if (GameCanvas.keyPressed[Main.isPC ? 24 : 6]) offset = 1;
        else return;
        selected = selected < 0 ? 0 : Mathf.Clamp(selected + offset, 0, currentListLength - 1);
        lastSelect[currentTabIndex] = selected;
        Rect viewport = TradeBagViewport;
        Rect cell = InventoryFrame.BagCellBounds(selected, viewport, cmy);
        if (cell.yMin < viewport.yMin) cmy -= Mathf.CeilToInt(viewport.yMin - cell.yMin);
        else if (cell.yMax > viewport.yMax) cmy += Mathf.CeilToInt(cell.yMax - viewport.yMax);
        cmy = cmtoY = Mathf.Clamp(cmy, 0, cmyLim);
    }

    private void PaintTradeBag(mGraphics g)
    {
        Item[] bag = Char.myCharz().arrItemBag;
        int length = bag?.Length ?? 0;
        Rect viewport = TradeBagViewport;
        int cells = InventoryBag.DisplaySlots(length, InventoryBagFilter.All);
        for (int slot = 0; slot < cells; slot++) {
            Rect cell = InventoryFrame.BagCellBounds(slot, viewport, cmy);
            bool allocated = slot < length;
            PaintBagCell(g, cell, viewport, allocated ? bag[slot] : null, allocated, slot == selected, bag != null);
        }
        InventoryFrame.Clip(g, viewport);
        paintScrollArrow(g);
    }

    private Item GetTradeBagItem(int slot)
    {
        Item[] bag = Char.myCharz().arrItemBag;
        return bag != null && slot >= 0 && slot < bag.Length ? bag[slot] : null;
    }

    private bool CanOfferTradeBagItem(Item item)
        => TradeBagActive && !isLock && item?.template != null && !item.IsPetItem
            && ReferenceEquals(item, GetTradeBagItem(item.indexUI));

    private void AddTradeBagOffer(Item source, int quantity)
    {
        var offer = new Item {
            template = source.template, itemOption = source.itemOption,
            indexUI = source.indexUI, quantity = quantity
        };
        vMyGD.addElement(offer);
        Service.gI().giaodich(2, -1, (sbyte)offer.indexUI, quantity);
    }

    internal void RejectTradeBagOffer(int slot)
    {
        if (type != TYPE_GIAODICH) return;
        for (int i = 0; i < vMyGD.size(); i++) {
            var offer = (Item)vMyGD.elementAt(i);
            if (offer.indexUI != slot) continue;
            vMyGD.removeElement(offer);
            if (currentTabIndex == 1) setTabGiaoDich(isMe: true);
            return;
        }
    }

    private void OpenTradeBagItem()
    {
        currItem = GetTradeBagItem(selected);
        if (currItem?.template == null) { cp = null; return; }
        if (Char.myCharz().statusMe == 14) {
            GameCanvas.startOKDlg(mResources.can_not_do_when_die);
            return;
        }
        var commands = new MyVector();
        if (!currItem.IsPetItem)
            commands.addElement(new Command(mResources.use_for_trade, this, 7000, currItem));
        if (commands.size() == 0)
            commands.addElement(new Command(mResources.CLOSE, this, 8000, currItem));
        Rect cell = InventoryFrame.BagCellBounds(selected, TradeBagViewport, cmy);
        GameCanvas.menu.startAt(commands, X, (int)cell.yMax);
        addItemDetail(currItem);
    }

    private void SyncInventoryBag()
    {
        if (mSystem.currentTimeMillis() >= discipleNextRequest) RequestDiscipleInfo();
        Item[] bag = Char.myCharz().arrItemBag;
        InventoryBag.Map(bag, inventoryBagFilter, inventoryBagMap);
        if (inventoryBagDetailOpen) {
            Item[] body = Char.myCharz().arrItemBody;
            int bodyLength = body?.Length ?? 0;
            int index = selected - bodyLength;
            Item current = selected >= 0 && selected < bodyLength ? body[selected]
                : bag != null && index >= 0 && index < bag.Length ? bag[index] : null;
            if (!ReferenceEquals(current, inventoryBagDetailItem)) {
                if (!InventoryBag.SameDetailItem(inventoryBagDetailItem, current)) { CloseInventoryBagDetail(); return; }
                inventoryBagDetailItem = currItem = current;
                inventoryBagDoubleTap.Reset(); inventoryEquipmentDoubleTap.Reset();
                int top = inventoryDetailTopScroll, bottom = inventoryDetailBottomScroll;
                BuildInventoryDetail(); // Rebind commands to the refreshed server item.
                inventoryDetailTopScroll = top; inventoryDetailBottomScroll = bottom;
            } else if (current != null && inventoryBagDetailQuantity != current.quantity) PrepareInventoryBagDetail();
            inventoryBagDetailQuantity = current?.quantity ?? 0;
        }
    }
    private void CloseInventoryBagDetail()
    {
        inventoryBagDetailOpen = false; inventoryBagDetailItem = null; cp = null;
        ClearInventoryDetail();
        inventoryBagDoubleTap.Reset(); HudLayout.Invalidate();
        inventoryEquipmentDoubleTap.Reset();
    }
    private bool IsInventoryBagDragTarget(string hit) => currentTabIndex == 1 && hit != null
        && (hit == InventoryFrame.Bag || hit.StartsWith("inventory.bag.cell.")
            || hit.StartsWith("inventory.equipment.") || hit.StartsWith("inventory.filter."));

    private bool HandleInventoryBagClick(string hit)
    {
        if (currentTabIndex != 1 || hit == null) return false;
        if (hit == InventoryFrame.DetailClose) { CloseInventoryBagDetail(); return true; }
        if (HandleInventoryDetailButton(hit)) return true;
        if (hit.StartsWith("inventory.filter.")) {
            inventoryBagFilter = (InventoryBagFilter)int.Parse(hit.Substring("inventory.filter.".Length));
            CloseInventoryBagDetail(); selected = -1; waitToPerform = -1;
            SyncInventoryBag(); HudLayout.Invalidate(); return true;
        }
        if (!hit.StartsWith("inventory.bag.cell.")) return false;
        int visual = int.Parse(hit.Substring("inventory.bag.cell.".Length));
        if (visual < 0 || visual >= inventoryBagMap.Count) return true;
        int index = inventoryBagMap[visual];
        Item[] bag = Char.myCharz().arrItemBag;
        if (bag == null || index >= bag.Length) return true;
        Item item = bag[index];
        selected = (Char.myCharz().arrItemBody?.Length ?? 0) + index; waitToPerform = -1;
        if (item?.template == null) { CloseInventoryBagDetail(); return true; }
        bool equip = inventoryBagDoubleTap.Tap(index, item, mSystem.currentTimeMillis());
        if (equip) {
            if (Char.myCharz().statusMe == 14) GameCanvas.startOKDlg(mResources.can_not_do_when_die);
            else { currItem = item; perform(2000, item); }
            return true;
        }
        OpenInventoryItemDetail(item); return true;
    }

    private void HandleInventoryEquipmentClick(int slot)
    {
        Item[] body = Char.myCharz().arrItemBody;
        selected = inventoryBodySlots[slot];
        if (selected < 0 || selected >= (body?.Length ?? 0)) { selected = -1; return; }
        Item item = body[selected];
        if (selected == InventoryBag.EquippedPorataIndex(body)) {
            if (inventoryEquipmentDoubleTap.Tap(selected, item, mSystem.currentTimeMillis())) {
                if (Char.myCharz().statusMe == 14) GameCanvas.startOKDlg(mResources.can_not_do_when_die);
                else { currItem = item; perform(2001, item); }
            } else OpenInventoryItemDetail(item);
            return;
        }
        if (item != null) OpenInventoryItemDetail(item); else CloseInventoryBagDetail();
    }

    private void OpenInventoryItemDetail(Item item)
    {
        currItem = item; inventoryBagDetailItem = item; inventoryBagDetailOpen = true;
        inventoryBagDetailQuantity = item.quantity;
        cp = null; // Detail C replaces the legacy popup, including its mutating getCompare().
        BuildInventoryDetail(); HudLayout.Invalidate();
    }

    private void ScrollInventoryBagIntoView(Rect cell)
    {
        Rect viewport = HudLayout.GetRect(InventoryFrame.Bag);
        if (cell.y < viewport.y) cmy -= (int)(viewport.y - cell.y);
        else if (cell.yMax + 4 > viewport.yMax) cmy += (int)(cell.yMax + 4 - viewport.yMax);
        cmy = cmtoY = Mathf.Clamp(cmy, 0, cmyLim); HudLayout.Invalidate();
    }
    private void MoveInventoryBagSelection(int direction)
    {
        inventoryBagDoubleTap.Reset();
        inventoryEquipmentDoubleTap.Reset();
        inventoryBagKeyboardSlots.Clear();
        int bodyLength = Char.myCharz().arrItemBody?.Length ?? 0;
        foreach (int index in inventoryBodySlots)
            if (index >= 0 && index < bodyLength) inventoryBagKeyboardSlots.Add(index);
        foreach (int index in inventoryBagMap) inventoryBagKeyboardSlots.Add(bodyLength + index);
        if (inventoryBagKeyboardSlots.Count == 0) return;
        int old = inventoryBagKeyboardSlots.IndexOf(selected);
        int next = old < 0 ? (direction > 0 ? 0 : inventoryBagKeyboardSlots.Count - 1)
            : (old + direction + inventoryBagKeyboardSlots.Count) % inventoryBagKeyboardSlots.Count;
        selected = inventoryBagKeyboardSlots[next];
        if (inventoryBagDetailOpen) CloseInventoryBagDetail();
    }
    private bool ScrollInventoryBagMouse(int amount)
    {
        if (!InventoryFrameActive || (currentTabIndex != 1 && !DisciplePanelActive)) return false;
        if (amount == 0 || GameCanvas.menu.showMenu || GameCanvas.currentDialog != null) return true;
        Vector2 point = new Vector2(GameCanvas.pxMouse, GameCanvas.pyMouse);
        if (InventoryBagDetailOpen && HudLayout.GetRect(InventoryFrame.Detail).Contains(point))
            ScrollInventoryDetail(point, -amount * 24);
        else if (DiscipleSkillOpen && HudLayout.GetRect(InventoryFrame.Detail).Contains(point)) {
            discipleSkillScroll = System.Math.Max(0, discipleSkillScroll - amount * 24);
        }
        else if (currentTabIndex == 1 && HudLayout.GetRect(InventoryFrame.Bag).Contains(point)) {
            inventoryBagDoubleTap.Reset();
            inventoryEquipmentDoubleTap.Reset();
            cmy = cmtoY = Mathf.Clamp(cmy - amount * 24, 0, cmyLim); HudLayout.Invalidate();
        }
        return true;
    }

    private void PaintInventoryBag(mGraphics g)
    {
        Rect viewport = HudLayout.GetRect(InventoryFrame.Bag), filter = InventoryFrame.FilterBounds(0);
        Rect filterViewport = new Rect(3, 73, W - 6, Mathf.Max(0, HudLayout.GetRect(InventoryFrame.Money).y - 75));
        InventoryFrame.Clip(g, filterViewport);
        mFont.tahoma_7b_dark.drawString(g, "Túi đồ (" + (Char.myCharz().arrItemBag?.Length ?? 0) + " ô)",
            W / 2, (int)filter.y - 13, mFont.CENTER);
        for (int i = 0; i < inventoryBagFilters.Length; i++) {
            Rect chip = InventoryFrame.FilterBounds(i);
            if (chip.yMax < filterViewport.y || chip.y > filterViewport.yMax) continue;
            InventoryItemVisuals.PaintNativeTab(g, chip, filterViewport, i == (int)inventoryBagFilter);
            mFont font = i == (int)inventoryBagFilter ? mFont.tahoma_7_green2 : mFont.tahoma_7_grey;
            string label = inventoryBagFilters[i];
            if (font.getWidth(label) + 4 <= chip.width)
                font.drawString(g, label, (int)chip.center.x, (int)chip.y + 5, mFont.CENTER);
            else {
                string[] words = label.Split(' ');
                font.drawString(g, words[0], (int)chip.center.x, (int)chip.y + 1, mFont.CENTER);
                font.drawString(g, string.Join(" ", words, 1, words.Length - 1), (int)chip.center.x, (int)chip.y + 11, mFont.CENTER);
            }
        }
        if (viewport.height <= 0) return;
        int cells = InventoryBag.DisplaySlots(inventoryBagMap.Count, inventoryBagFilter);
        Item[] bag = Char.myCharz().arrItemBag;
        for (int visual = 0; visual < cells; visual++) {
            Rect cell = InventoryFrame.BagCellBounds(visual);
            bool allocated = visual < inventoryBagMap.Count;
            int index = allocated ? inventoryBagMap[visual] : -1;
            Item item = index >= 0 && bag != null && index < bag.Length ? bag[index] : null;
            bool chosen = allocated && selected == (Char.myCharz().arrItemBody?.Length ?? 0) + index;
            PaintBagCell(g, cell, viewport, item, allocated, chosen, inventoryBagFilter == InventoryBagFilter.All && bag != null);
        }
        InventoryFrame.Clip(g, viewport);
    }

    private static void PaintBagCell(mGraphics g, Rect cell, Rect viewport, Item item, bool allocated, bool chosen, bool showLock)
    {
        if (cell.yMax + 4 < viewport.y || cell.y > viewport.yMax) return;
        InventoryFrame.Clip(g, viewport);
        InventoryFrame.Rounded(g, cell, allocated ? (chosen ? InventoryFrame.Accent : InventoryFrame.SlotBackground) : InventoryFrame.MoneyBackground,
            allocated ? (byte)255 : (byte)100);
        if (!allocated) {
            if (showLock) {
                InventoryFrame.Clip(g, InventoryFrame.Intersection(cell, viewport));
                PaintClosedBagSlot(g, cell);
            }
            return;
        }
        if (item?.template == null) return;
        if (item.template.type >= 0 && item.template.type <= 4) {
            int setColor = InventoryFrame.SetColor(item);
            if (setColor != InventoryFrame.Border) InventoryItemVisuals.PaintAura(cell, viewport, setColor);
        }
        InventoryFrame.Clip(g, InventoryFrame.Intersection(cell, viewport));
        SmallImage.drawSmallImage(g, item.template.iconID, (int)cell.center.x, (int)cell.center.y, 0,
            mGraphics.HCENTER | mGraphics.VCENTER);
        InventoryFrame.Clip(g, viewport);
        if (InventoryBag.HasLevel(item))
            InventoryItemVisuals.PaintLevel(g, cell, viewport, InventoryFrame.Option(item, Item.OPT_LVITEM), true);
        if (item.quantity > 1) InventoryBagBadge.Paint(g, cell, viewport, item.quantity);
    }

    private static void PaintClosedBagSlot(mGraphics g, Rect cell)
    {
        mFont font = mFont.tahoma_7_grey;
        int line = font.getHeight();
        if (font.getWidth("Chưa") + 2 <= cell.width && 2 * line <= cell.height) {
            font.drawString(g, "Chưa", (int)cell.center.x, (int)cell.center.y - line, mFont.CENTER);
            font.drawString(g, "mở", (int)cell.center.x, (int)cell.center.y, mFont.CENTER);
        } else {
            // Keep the locked state recognizable on small logical cells.
            int size = (int)Mathf.Max(4, Mathf.Min(10, cell.width - 4));
            int x = (int)cell.center.x - size / 2, y = (int)cell.center.y - size / 2;
            g.setColor(InventoryFrame.Border);
            g.drawRect(x + size / 4, y - size / 4, size / 2, size / 2);
            g.fillRect(x, y, size, size * 3 / 4);
        }
    }
}
