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
    private static readonly string[] inventoryBagFilters = { "Tất cả", "Trang bị", "Tiêu hao", "Nguyên liệu" };
    internal int InventoryBagCellCount => inventoryBagMap.Count;
    internal bool InventoryBagDetailOpen => inventoryBagDetailOpen && currentTabIndex == 1 && inventoryBagDetailItem?.template != null;

    private void SyncInventoryBag()
    {
        Item[] bag = Char.myCharz().arrItemBag;
        InventoryBag.Map(bag, inventoryBagFilter, inventoryBagMap);
        if (inventoryBagDetailOpen) {
            Item[] body = Char.myCharz().arrItemBody;
            int bodyLength = body?.Length ?? 0;
            int index = selected - bodyLength;
            Item current = selected >= 0 && selected < bodyLength ? body[selected]
                : bag != null && index >= 0 && index < bag.Length ? bag[index] : null;
            if (!ReferenceEquals(current, inventoryBagDetailItem))
                CloseInventoryBagDetail();
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
        if (!InventoryFrameActive || currentTabIndex != 1) return false;
        if (amount == 0 || GameCanvas.menu.showMenu || GameCanvas.currentDialog != null) return true;
        Vector2 point = new Vector2(GameCanvas.pxMouse, GameCanvas.pyMouse);
        if (InventoryBagDetailOpen && HudLayout.GetRect(InventoryFrame.Detail).Contains(point))
            ScrollInventoryDetail(point, -amount * 24);
        else if (HudLayout.GetRect(InventoryFrame.Bag).Contains(point)) {
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
            InventoryFrame.Clip(g, InventoryFrame.Intersection(chip, filterViewport));
            PopUp.paintPopUp(g, (int)chip.x, (int)chip.y, (int)chip.width, (int)chip.height,
                i == (int)inventoryBagFilter ? 1 : 0, true);
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
        int cells = InventoryBag.Rows(inventoryBagMap.Count) * InventoryBag.Columns;
        Item[] bag = Char.myCharz().arrItemBag;
        for (int visual = 0; visual < cells; visual++) {
            Rect cell = InventoryFrame.BagCellBounds(visual);
            if (cell.yMax + 4 < viewport.y || cell.y > viewport.yMax) continue;
            bool allocated = visual < inventoryBagMap.Count;
            int index = allocated ? inventoryBagMap[visual] : -1;
            Item item = index >= 0 && bag != null && index < bag.Length ? bag[index] : null;
            bool chosen = allocated && selected == (Char.myCharz().arrItemBody?.Length ?? 0) + index;
            InventoryFrame.Clip(g, viewport);
            InventoryFrame.Rounded(g, cell, chosen ? InventoryFrame.Accent : allocated ? InventoryFrame.SlotBackground : InventoryFrame.MoneyBackground,
                allocated ? (byte)255 : (byte)100);
            if (!allocated) continue;
            if (item?.template == null) continue;
            if (item.template.type >= 0 && item.template.type <= 4) {
                int setColor = InventoryFrame.SetColor(item);
                if (setColor != InventoryFrame.Border) InventoryItemVisuals.PaintAura(cell, viewport, setColor);
            }
            // Icon position never depends on quantity, badge width or level.
            InventoryFrame.Clip(g, InventoryFrame.Intersection(cell, viewport));
            SmallImage.drawSmallImage(g, item.template.iconID, (int)cell.center.x, (int)cell.center.y, 0,
                mGraphics.HCENTER | mGraphics.VCENTER);
            InventoryFrame.Clip(g, viewport);
            if (InventoryBag.HasLevel(item))
                InventoryItemVisuals.PaintLevel(g, cell, viewport, InventoryFrame.Option(item, Item.OPT_LVITEM), true);
            if (item.quantity > 1) InventoryBagBadge.Paint(g, cell, viewport, item.quantity);
        }
        InventoryFrame.Clip(g, viewport);
    }
}
