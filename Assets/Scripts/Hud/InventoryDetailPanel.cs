using System;
using System.Collections.Generic;
using Hud;
using Mod.CuongLe;
using UnityEngine;

public partial class Panel
{
    private sealed class InventoryDetailLine
    {
        internal string Text;
        internal mFont Font;
        internal int Meter, Value;
        internal Item StarItem;
    }
    private sealed class InventoryDetailButton
    {
        internal string Text;
        internal Command Action;
        internal int Group, Position;
        internal string[] Lines;
        internal string HitTarget;
    }
    private MyVector inventoryDetailCommands;
    private readonly List<InventoryDetailLine> inventoryDetailInfo = new List<InventoryDetailLine>();
    private readonly List<InventoryDetailLine> inventoryDetailDescription = new List<InventoryDetailLine>();
    private readonly List<InventoryDetailButton> inventoryDetailButtons = new List<InventoryDetailButton>();
    private int inventoryDetailTopScroll, inventoryDetailBottomScroll;
    private float inventoryDetailWidth, inventoryDetailHeight;
    private Item[] inventoryDetailBody;
    private bool inventoryDetailQuick, inventoryDetailDiscard;
    internal int InventoryDetailButtonCount => inventoryDetailButtons.Count;
    private int InventoryDetailBottomCount
    {
        get { int count = 0; foreach (var button in inventoryDetailButtons) if (button.Group == 0) count++; return count; }
    }
    private float InventoryDetailControlsHeight => (inventoryDetailQuick ? 26 : 0) + (inventoryDetailDiscard ? 26 : 0);
    private float InventoryDetailActionTop => HudLayout.GetRect(InventoryFrame.Detail).height - 5
        - ((InventoryDetailBottomCount + 1) / 2) * 26 - InventoryDetailControlsHeight;
    private float InventoryDetailDividerY => Mathf.Min(HudLayout.GetRect(InventoryFrame.Detail).height / 2, InventoryDetailActionTop - 20);
    internal Rect InventoryDetailUpperBounds
    {
        get { Rect r = HudLayout.GetRect(InventoryFrame.Detail); return new Rect(r.x + 6, 27, Mathf.Max(0, r.width - 12), Mathf.Max(0, InventoryDetailDividerY - 30)); }
    }
    internal Rect InventoryDetailLowerBounds
    {
        get { Rect r = HudLayout.GetRect(InventoryFrame.Detail); return new Rect(r.x + 6, InventoryDetailDividerY + 5, Mathf.Max(0, r.width - 12), Mathf.Max(0, InventoryDetailActionTop - InventoryDetailDividerY - 8)); }
    }
    internal Rect InventoryDetailButtonBounds(int index)
    {
        if (index < 0 || index >= inventoryDetailButtons.Count) return new Rect();
        Rect r = HudLayout.GetRect(InventoryFrame.Detail);
        var button = inventoryDetailButtons[index];
        if (button.Group == 1) {
            float w = Mathf.Max(0, (r.width - 16) / 3);
            return InventoryDetailNativeButtonRect(r.x + 6 + button.Position * (w + 2), InventoryDetailActionTop, w);
        }
        if (button.Group == 2) return InventoryDetailNativeButtonRect(r.x + 6, r.height - 5 - ((InventoryDetailBottomCount + 1) / 2) * 26 - 26, r.width - 12);
        bool fullRow = InventoryDetailBottomCount % 2 == 1 && button.Position == InventoryDetailBottomCount - 1;
        float width = Mathf.Max(0, fullRow ? r.width - 12 : (r.width - 16) / 2);
        int rows = (InventoryDetailBottomCount + 1) / 2;
        return InventoryDetailNativeButtonRect(r.x + 6 + button.Position % 2 * (width + 4), r.height - 5 - rows * 26 + button.Position / 2 * 26, width);
    }
    private static Rect InventoryDetailNativeButtonRect(float x, float y, float width)
        => new Rect(Mathf.Round(x), Mathf.Round(y), Mathf.Floor(Mathf.Max(0, width)), 24);
    private void ClearInventoryDetail()
    {
        inventoryDetailCommands = null; inventoryDetailButtons.Clear(); inventoryDetailInfo.Clear(); inventoryDetailDescription.Clear();
        inventoryDetailWidth = inventoryDetailHeight = 0; inventoryDetailBody = null;
        inventoryDetailTopScroll = inventoryDetailBottomScroll = 0;
        inventoryDetailQuick = inventoryDetailDiscard = false;
    }
    private void AddInventoryDetailButton(string text, Command command, int group, int position)
    {
        inventoryDetailButtons.Add(new InventoryDetailButton { Text = text, Action = command, Group = group, Position = position,
            HitTarget = "inventory.detail.button." + inventoryDetailButtons.Count });
    }
    private void BuildInventoryDetail()
    {
        inventoryDetailButtons.Clear(); inventoryDetailQuick = inventoryDetailDiscard = false;
        inventoryDetailCommands = null;
        if (DisciplePanelActive) {
            if (!DiscipleFused) AddInventoryDetailButton("Tháo", new Command("Tháo", this, 2106, inventoryBagDetailItem), 0, 0);
            inventoryDetailTopScroll = inventoryDetailBottomScroll = 0;
            PrepareInventoryBagDetail(); HudLayout.Invalidate(); return;
        }
        doFireInventory(true); // Build exactly the old commands, without opening its menu.
        Item item = inventoryBagDetailItem;
        if (item?.template == null) return;
        bool worn = selected >= 0 && selected < (Char.myCharz().arrItemBody?.Length ?? 0);
        bool equipment = InventoryBag.CanEquip(item);
        bool consumable = !worn && !equipment && InventoryBag.Category(item) == InventoryBagFilter.Consumable;
        int bottom = 0;
        if (!worn && equipment) AddInventoryDetailButton("Mặc", new Command("Mặc", this, 2000, item), 0, bottom++);
        if (inventoryDetailCommands != null) for (int i = 0; i < inventoryDetailCommands.size(); i++) {
            Command command = inventoryDetailCommands.elementAt(i) as Command;
            if (command == null) continue;
            if (command.actionListener == this) {
                if (command.idAction == 2002 && worn) AddInventoryDetailButton("Tháo", command, 0, bottom++);
                else if (command.idAction == 2001 && (!equipment || worn)) AddInventoryDetailButton("Sử dụng", command, 0, bottom++);
                else if (command.idAction == 2003 && !worn) AddInventoryDetailButton("Bỏ ra", command, 0, bottom++);
            } else if (consumable && command.actionListener is AutoItem && (command.idAction == 1 || command.idAction == 2)) {
                AddInventoryDetailButton(command.idAction == 2 ? "Ngưng Auto" : "Auto Sử Dụng", command, 0, bottom++);
            } else if (consumable && command.actionListener is AutoVutDo && (command.idAction == 7 || command.idAction == 8)) {
                inventoryDetailDiscard = true;
                AddInventoryDetailButton(command.idAction == 8 ? "[x] Danh sách vứt" : "[ ] Thêm vào danh sách vứt", command, 2, 0);
            }
        }
        // Keep Auto in place; exchange Use and Discard so Use owns the wide last row.
        InventoryDetailButton useButton = null, discardButton = null;
        foreach (var button in inventoryDetailButtons) {
            if (button.Group != 0 || button.Action?.actionListener != this) continue;
            if (button.Action.idAction == 2001) useButton = button;
            else if (button.Action.idAction == 2003) discardButton = button;
        }
        if (useButton != null && discardButton != null) {
            int usePosition = useButton.Position;
            useButton.Position = discardButton.Position;
            discardButton.Position = usePosition;
        }
        if (consumable) {
            inventoryDetailQuick = true;
            for (int slot = 0; slot < 3; slot++) {
                Command assignment = null;
                bool assigned = MobileQuickItems.Template(slot) == item.template.id;
                if (inventoryDetailCommands != null) for (int i = 0; i < inventoryDetailCommands.size(); i++) {
                    Command command = inventoryDetailCommands.elementAt(i) as Command;
                    if (command?.actionListener is MobileQuickItems && command.idAction == (assigned ? 4 : slot + 1)) assignment = command;
                }
                AddInventoryDetailButton((assigned ? "[x] " : "") + (slot + 1) + (slot == 0 ? " trái" : slot == 1 ? " giữa" : " phải"), assignment, 1, slot);
            }
        }
        // Upgrading/socketing requires the existing NPC session, never an inventory packet.
        inventoryDetailTopScroll = inventoryDetailBottomScroll = 0;
        PrepareInventoryBagDetail(); HudLayout.Invalidate();
    }
    private void EnsureInventoryDetailLayout()
    {
        if (!InventoryBagDetailOpen) return;
        Rect r = HudLayout.GetRect(InventoryFrame.Detail);
        if (inventoryDetailWidth != r.width || inventoryDetailHeight != r.height || !ReferenceEquals(inventoryDetailBody, DisciplePanelActive ? Char.myPetz().arrItemBody : Char.myCharz().arrItemBody)) PrepareInventoryBagDetail();
    }
    private void AddInventoryDetailText(List<InventoryDetailLine> lines, string text, mFont font)
    {
        if (string.IsNullOrEmpty(text)) return;
        int width = Mathf.Max(10, (int)HudLayout.GetRect(InventoryFrame.Detail).width - 20);
        foreach (string line in font.splitFontArray(text.Replace("$", ""), width)) lines.Add(new InventoryDetailLine { Text = line, Font = font });
    }
    private static bool IsInventorySetOption(int id)
    {
        return id >= 127 && id <= 135 || id == 233 || id == 237 || id == 241 || id == 245;
    }
    private void PrepareInventoryBagDetail()
    {
        Item item = inventoryBagDetailItem;
        if (item?.template == null) return;
        Rect r = HudLayout.GetRect(InventoryFrame.Detail);
        inventoryDetailWidth = r.width; inventoryDetailHeight = r.height; inventoryDetailBody = DisciplePanelActive ? Char.myPetz().arrItemBody : Char.myCharz().arrItemBody;
        inventoryDetailInfo.Clear(); inventoryDetailDescription.Clear();
        int level = Mathf.Clamp(InventoryFrame.Option(item, Item.OPT_LVITEM), 0, 7);
        int slots = InventoryFrame.StarSlots(item), stars = InventoryFrame.FilledStars(item);
        bool worn = DisciplePanelActive || selected >= 0 && selected < (inventoryDetailBody?.Length ?? 0);
        AddInventoryDetailText(inventoryDetailInfo, item.template.name + (InventoryBag.HasLevel(item) ? " [+" + level + "]" : ""), mFont.tahoma_7b_dark);
        AddInventoryDetailText(inventoryDetailInfo, worn ? "Đang mặc" : "Chưa mặc", mFont.tahoma_7_grey);
        if (InventoryBag.HasLevel(item)) {
            inventoryDetailInfo.Add(new InventoryDetailLine { Text = "Cấp " + level + "/7", Font = mFont.tahoma_7b_dark, Meter = 7, Value = level });
        }
        if (slots > 0) inventoryDetailInfo.Add(new InventoryDetailLine { Text = "Sao " + stars + "/" + slots, Font = mFont.tahoma_7b_dark, StarItem = item });
        if (item.itemOption != null) foreach (ItemOption option in item.itemOption) {
            if (option?.optionTemplate == null) continue;
            int id = option.optionTemplate.id;
            if (id == Item.OPT_LVITEM || id == Item.OPT_STARSLOT || id == Item.OPT_MAXSTARSLOT) continue;
            if (IsInventorySetOption(id) && (item.template.type < 0 || item.template.type > 4)) continue;
            AddInventoryDetailText(inventoryDetailInfo, option.getOptionString(), IsInventorySetOption(id) ? mFont.tahoma_7_green2 : mFont.tahoma_7_blue);
            if (item.template.type >= 0 && item.template.type <= 4 && IsInventorySetOption(id)) {
                int count = 0;
                if (inventoryDetailBody != null) foreach (Item gear in inventoryDetailBody)
                    if (gear?.template != null && gear.template.type >= 0 && gear.template.type <= 4 && gear.itemOption != null)
                        foreach (ItemOption attached in gear.itemOption) if (attached?.optionTemplate != null && attached.optionTemplate.id == id) { count++; break; }
                AddInventoryDetailText(inventoryDetailInfo, "Đang mặc set: " + count + "/5", mFont.tahoma_7_green2);
            }
        }
        AddInventoryDetailComparison(item, worn);
        AddInventoryDetailText(inventoryDetailInfo, "Số lượng: " + item.quantity, mFont.tahoma_7b_dark);
        if (item.template.strRequire > 1) AddInventoryDetailText(inventoryDetailInfo, mResources.pow_request + ": " + item.template.strRequire,
            Char.myCharz().cPower < item.template.strRequire ? mFont.tahoma_7_red : mFont.tahoma_7_grey);
        if (!DisciplePanelActive && worn && InventoryBag.HasLevel(item)) AddInventoryDetailText(inventoryDetailInfo, "Nâng cấp / Gắn sao: đến NPC như trước", mFont.tahoma_7_grey);
        AddInventoryDetailText(inventoryDetailDescription, item.template.description, mFont.tahoma_7b_dark);
        if (!string.IsNullOrEmpty(item.content) && item.content != item.template.description) AddInventoryDetailText(inventoryDetailDescription, item.content, mFont.tahoma_7_grey);
        if (inventoryDetailDescription.Count == 0) AddInventoryDetailText(inventoryDetailDescription, "Không có mô tả", mFont.tahoma_7_grey);
        for (int i = 0; i < inventoryDetailButtons.Count; i++) {
            var button = inventoryDetailButtons[i];
            mFont font = button.Action == null ? mFont.tahoma_7_grey : mFont.tahoma_7b_dark;
            button.Lines = font.splitFontArray(button.Text, Mathf.Max(10, (int)InventoryDetailButtonBounds(i).width - 4));
        }
    }
    private void AddInventoryDetailComparison(Item item, bool worn)
    {
        int type = item.template.type;
        if (worn || !InventoryBag.HasLevel(item)) return;
        int index = type == 32 ? 6 : type;
        string slot = type == 32 ? "GLT" : inventorySlotLabels[type];
        Item other = inventoryDetailBody != null && index < inventoryDetailBody.Length ? inventoryDetailBody[index] : null;
        if (other?.template == null) { AddInventoryDetailText(inventoryDetailInfo, "So với ô " + slot + ": đang trống", mFont.tahoma_7_grey); return; }
        if (item.itemOption != null && other.itemOption != null) foreach (ItemOption option in item.itemOption) {
            if (option?.optionTemplate == null) continue;
            int id = option.optionTemplate.id;
            // Compare a shared primary numeric stat, never unlike units or set/level metadata.
            if (id != 0 && id != 6 && id != 7 && id != 14 && id != 47 && id != 48 && id != 49 && id != 50 && id != 77 && id != 103) continue;
            foreach (ItemOption baseline in other.itemOption) if (baseline?.optionTemplate != null && baseline.optionTemplate.id == id) {
                long delta = (long)option.param - baseline.param;
                string text = NinjaUtil.replace(option.optionTemplate.name.Replace("+#", "#"), "#", (delta > 0 ? "+" : "") + delta);
                AddInventoryDetailText(inventoryDetailInfo, "So với ô " + slot + ": " + text, delta > 0 ? mFont.tahoma_7_green2 : delta < 0 ? mFont.tahoma_7_red : mFont.tahoma_7_grey);
                return;
            }
        }
        AddInventoryDetailText(inventoryDetailInfo, "So với ô " + slot + ": không có chỉ số chung để so", mFont.tahoma_7_grey);
    }
    private bool HandleInventoryDetailButton(string hit)
    {
        const string prefix = "inventory.detail.button.";
        if (hit == null || !hit.StartsWith(prefix)) return false;
        if (!DisciplePanelActive) SyncInventoryBag();
        if (!InventoryBagDetailOpen || !int.TryParse(hit.Substring(prefix.Length), out int index) || index < 0 || index >= inventoryDetailButtons.Count) return true;
        if (Char.myCharz().statusMe == 14) { GameCanvas.startOKDlg(mResources.can_not_do_when_die); return true; }
        Command action = inventoryDetailButtons[index].Action;
        if (action?.idAction == 2106) {
            if (!DiscipleFused && Char.myCharz().havePet) Service.gI().getItem(PET_BAG, (sbyte)selected);
            CloseInventoryBagDetail(); return true;
        }
        if (action != null) { action.performAction(); if (InventoryBagDetailOpen) BuildInventoryDetail(); }
        return true;
    }
    private void ScrollInventoryDetail(Vector2 point, int delta)
    {
        if (HudLayout.GetRect(InventoryFrame.DetailUpper).Contains(point)) inventoryDetailTopScroll = Mathf.Clamp(inventoryDetailTopScroll + delta, 0, Mathf.Max(0, inventoryDetailInfo.Count * 12 + 8 - (int)InventoryDetailUpperBounds.height));
        else if (HudLayout.GetRect(InventoryFrame.DetailLower).Contains(point)) inventoryDetailBottomScroll = Mathf.Clamp(inventoryDetailBottomScroll + delta, 0, Mathf.Max(0, inventoryDetailDescription.Count * 12 + 8 - (int)InventoryDetailLowerBounds.height));
    }
    private void UpdateInventoryBagDetailDrag()
    {
        bool upper = inventoryPressed == InventoryFrame.DetailUpper, lower = inventoryPressed == InventoryFrame.DetailLower;
        if (!upper && !lower) return;
        if (GameCanvas.isPointerJustDown) inventoryDetailDragScroll = upper ? inventoryDetailTopScroll : inventoryDetailBottomScroll;
        if (!(GameCanvas.isPointerDown || GameCanvas.isPointerJustRelease)) return;
        int delta = GameCanvas.py - inventoryDragY;
        if (System.Math.Abs(delta) > 4) { inventoryDragging = true; inventoryBagDoubleTap.Reset(); inventoryEquipmentDoubleTap.Reset(); }
        if (!inventoryDragging) return;
        int limit = Mathf.Max(0, (upper ? inventoryDetailInfo.Count : inventoryDetailDescription.Count) * 12 + 8 - (int)(upper ? InventoryDetailUpperBounds.height : InventoryDetailLowerBounds.height));
        int position = Mathf.Clamp(inventoryDetailDragScroll - delta, 0, limit);
        if (upper) inventoryDetailTopScroll = position; else inventoryDetailBottomScroll = position;
    }
    private void PaintInventoryDetailLines(mGraphics g, Rect bounds, List<InventoryDetailLine> lines, int scroll)
    {
        InventoryFrame.Clip(g, bounds);
        for (int i = 0; i < lines.Count; i++) {
            int y = (int)bounds.y + 6 + i * 12 - scroll;
            if (y + 12 < bounds.y || y > bounds.yMax) continue;
            // Native setTypePaint shifts text left; keep glyphs inside the clip on both sides.
            var line = lines[i]; line.Font.drawString(g, line.Text, (int)bounds.x + 4, y, mFont.LEFT);
            if (line.StarItem != null) InventoryItemVisuals.PaintStars(new Rect(bounds.xMax - 44, y - 3.5f, 44, 0), bounds, line.StarItem);
            else if (line.Meter == 7) for (int n = 0; n < 7; n++) {
                int x = (int)(bounds.xMax - 45 + n * 6);
                g.setColor(n < line.Value ? InventoryFrame.Accent : InventoryFrame.SlotBackground); g.fillRect(x, y + 4, 5, 4);
            }
        }
    }
    private void PaintInventoryBagDetail(mGraphics g)
    {
        EnsureInventoryDetailLayout();
        Rect r = HudLayout.GetRect(InventoryFrame.Detail);
        InventoryFrame.Clip(g, r); InventoryFrame.Rounded(g, r, InventoryFrame.EquipmentBackground);
        SmallImage.drawSmallImage(g, inventoryBagDetailItem.template.iconID, (int)r.x + 17, 14, 0, mGraphics.HCENTER | mGraphics.VCENTER);
        g.setColor(InventoryFrame.Border); g.fillRect((int)r.x + 4, (int)InventoryDetailDividerY, (int)r.width - 8, 1);
        PaintInventoryDetailLines(g, InventoryDetailUpperBounds, inventoryDetailInfo, inventoryDetailTopScroll);
        PaintInventoryDetailLines(g, InventoryDetailLowerBounds, inventoryDetailDescription, inventoryDetailBottomScroll);
        for (int i = 0; i < inventoryDetailButtons.Count; i++) {
            Rect button = InventoryDetailButtonBounds(i); var entry = inventoryDetailButtons[i];
            InventoryFrame.Clip(g, button);
            bool focused = entry.Action != null && inventoryPressed == entry.HitTarget && GameCanvas.isPointerDown && !inventoryDragging;
            Command.paintOngMau(focused ? Command.btn1left : Command.btn0left,
                focused ? Command.btn1mid : Command.btn0mid, focused ? Command.btn1right : Command.btn0right,
                (int)button.x, (int)button.y, (int)button.width, g);
            mFont font = entry.Action == null ? mFont.tahoma_7_grey : focused ? mFont.tahoma_7b_green2 : mFont.tahoma_7b_dark;
            string[] text = entry.Lines ?? Array.Empty<string>();
            for (int row = 0; row < System.Math.Min(2, text.Length); row++) font.drawString(g, text[row], (int)button.center.x, (int)button.center.y - System.Math.Min(2, text.Length) * 5 + row * 10, mFont.CENTER);
        }
        Rect close = HudLayout.GetRect(InventoryFrame.DetailClose);
        InventoryFrame.Clip(g, close); InventoryFrame.Rounded(g, close, InventoryFrame.EquipmentBackground);
        g.drawImage(imgX, (int)close.center.x, (int)close.center.y, mGraphics.HCENTER | mGraphics.VCENTER);
        InventoryFrame.Clip(g, HudLayout.ScreenBounds);
    }
}
