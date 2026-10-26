using System;
using Hud;
using UnityEngine;

namespace Hud
{
    internal static class DiscipleLayout
    {
        internal const string Close = "disciple.detail.close";
        internal static bool Visible => InventoryFrame.Visible && GameCanvas.panel.DisciplePanelActive;
        internal const float GroupGap = InventoryFrame.Gap, TitleHeight = 12, BadgeOverflow = 3;
        internal static float ChipHeight => Mathf.Min(24, Mathf.Max(16,
            Content.height - 3 * TitleHeight - 6 * GroupGap - 3 * BadgeOverflow - 3));
        internal static Rect Content {
            get {
                Rect tabs = HudLayout.GetRect(InventoryFrame.Tabs);
                float top = tabs.yMax + GroupGap;
                return new Rect(tabs.x, top, Mathf.Max(0, tabs.width - 1),
                    Mathf.Max(0, HudLayout.GetRect(InventoryFrame.Money).y - GroupGap - top));
            }
        }
        internal static Rect Chip(int i) {
            float width = Mathf.Floor((Content.width - 8f / Mathf.Max(1, mGraphics.zoomLevel)) / 5);
            float gap = (Content.width - 5 * width) / 4;
            return new Rect(Content.x + Mathf.Floor(i * (width + gap)),
                StatusTitle.yMax + GroupGap, width, ChipHeight);
        }
        internal static Rect StatusTitle => new Rect(Content.x, Cell(4).yMax + BadgeOverflow + GroupGap, Content.width, TitleHeight);
        internal static Rect EquipmentTitle => new Rect(Content.x, Content.y, Content.width, TitleHeight);
        internal static float Size => Mathf.Max(1, Mathf.Floor(Mathf.Min(InventoryFrame.Unit(HudLayout.IsMobileLayout ? 44 : 64),
            Mathf.Min((Content.width - 3 * GroupGap - BadgeOverflow) / 4,
                (Content.height - ChipHeight - 3 * TitleHeight - 6 * GroupGap - 3 * BadgeOverflow) / 3))));
        internal static Rect SkillTitle => new Rect(Content.x, Chip(0).yMax + GroupGap, Content.width, TitleHeight);
        internal static Rect Cell(int i)
        {
            float gap = (Content.width - BadgeOverflow - 4 * Size) / 3;
            float equipmentTop = EquipmentTitle.yMax + GroupGap;
            float top = i < 8 ? equipmentTop + i / 4 * (Size + BadgeOverflow + GroupGap)
                : SkillTitle.yMax + GroupGap;
            return new Rect(Content.x + Mathf.Floor(i % 4 * (Size + gap)), top, Size, Size);
        }
        internal static void Register()
        {
            for (int i = 0; i < 5; i++) {
                int slot = i;
                HudLayout.Register("disciple.status." + i, () => Chip(slot), HudLayout.OverlayOrder + 12,
                    () => Visible && Char.myCharz().havePet);
            }
            for (int i = 0; i < 12; i++) {
                int slot = i;
                HudLayout.Register("disciple.cell." + i, () => Cell(slot), HudLayout.OverlayOrder + 12,
                    () => Visible && Char.myCharz().havePet);
            }
            HudLayout.Register("disciple.detail", () => HudLayout.GetRect(InventoryFrame.Detail), HudLayout.OverlayOrder + 14,
                () => Visible && GameCanvas.panel.DiscipleSkillOpen);
            HudLayout.Register(Close, () => new Rect(HudLayout.GetRect(InventoryFrame.Detail).xMax - 21, 3, 18, 18),
                HudLayout.OverlayOrder + 16, () => Visible && GameCanvas.panel.DiscipleSkillOpen);
        }
    }
}

public partial class Panel
{
    internal const int DiscipleTab = 7;
    // New rune item contract. Slot 7 is separate from legacy pet accessories at slot 8.
    internal const int DiscipleRuneType = 80;
    private static readonly int[] discipleBodySlots = { 0, 1, 2, 3, 4, 6, 5, 7 };
    private static readonly string[] discipleStatuses = { "Đi theo", "Bảo vệ", "Tấn công", "Về nhà", "Hợp thể" };
    private readonly InventoryItemVisuals.FittedTextRow discipleStatusText = new InventoryItemVisuals.FittedTextRow();
    private readonly InventoryItemVisuals.FittedTextRow discipleRuneText = new InventoryItemVisuals.FittedTextRow();
    private readonly Rect[] discipleRuneBounds = new Rect[1];
    private static readonly string[] discipleRuneLabel = { "NGỌC" };
    private readonly Rect[] discipleStatusBounds = new Rect[5];
    private int discipleSkill = -1, discipleFocus;
    private bool discipleLoaded;
    private long discipleNextRequest, discipleFusionUntil;
    internal bool DisciplePanelActive => InventoryFrameActive && currentTabIndex == DiscipleTab;
    internal bool DiscipleSkillOpen => DisciplePanelActive && discipleSkill >= 0;
    internal bool DiscipleFused => Char.myPetz().petStatus >= 4;
    private int InventoryToolIndex(int visual) => InventoryFrameActive && Char.myCharz().havePet && visual >= 3 ? visual + 1 : visual;

    internal void ClearDiscipleInfo()
    {
        discipleLoaded = false; discipleFusionUntil = 0;
        CloseDiscipleDetail();
        if (DisciplePanelActive) CloseInventoryBagDetail();
    }
    private void CloseDiscipleDetail() { discipleSkill = -1; HudLayout.Invalidate(); }
    private void OpenDiscipleTab()
    {
        currentListLength = 0; cmy = cmtoY = cmyLim = 0; discipleFocus = 0;
        RequestDiscipleInfo();
    }
    private void RequestDiscipleInfo()
    {
        if (!Char.myCharz().havePet) return;
        Service.gI().petInfo(); discipleNextRequest = mSystem.currentTimeMillis() + 3000;
    }
    internal void ReceiveDiscipleInfo(Message message)
    {
        discipleLoaded = true;
        if (message.reader().available() >= 7 && message.reader().readUnsignedByte() == 0xD1) {
            Char.myPetz().cgender = message.reader().readByte();
            message.reader().readByte(); // Pet kind is reserved for equipment eligibility.
            int remaining = message.reader().readInt();
            discipleFusionUntil = remaining > 0 ? mSystem.currentTimeMillis() + remaining * 1000L : remaining;
        }
        if (DisciplePanelActive) {
            if (InventoryBagDetailOpen) {
                int slot = selected;
                Item[] body = Char.myPetz().arrItemBody;
                if (slot < 0 || slot >= (body?.Length ?? 0) || body[slot]?.template == null) CloseInventoryBagDetail();
                else {
                    int top = inventoryDetailTopScroll, bottom = inventoryDetailBottomScroll;
                    inventoryBagDetailItem = currItem = body[slot]; BuildInventoryDetail();
                    inventoryDetailTopScroll = top; inventoryDetailBottomScroll = bottom;
                }
            }
        }
        HudLayout.Invalidate();
    }
    private void SyncDisciplePanel()
    {
        cmy = cmtoY = cmyLim = 0;
        if (mSystem.currentTimeMillis() >= discipleNextRequest) RequestDiscipleInfo();
    }
    private Item DiscipleItem(int visual)
    {
        Item[] body = Char.myPetz().arrItemBody;
        int slot = discipleBodySlots[visual];
        return body != null && slot < body.Length ? body[slot] : null;
    }
    private Skill DiscipleSkill(int visual)
    {
        Skill[] skills = Char.myPetz().arrPetSkill;
        return skills != null && visual < skills.Length ? skills[visual] : null;
    }
    private bool HandleDiscipleClick(string hit)
    {
        if (!DisciplePanelActive || hit == null) return false;
        if (hit == DiscipleLayout.Close || hit == InventoryFrame.DetailClose) {
            CloseDiscipleDetail(); CloseInventoryBagDetail(); return true;
        }
        if (hit.StartsWith("inventory.detail.button.")) return HandleInventoryDetailButton(hit);
        if (hit.StartsWith("disciple.status.")) {
            int status = int.Parse(hit.Substring("disciple.status.".Length));
            if (Char.myCharz().statusMe == 14) GameCanvas.startOKDlg(mResources.can_not_do_when_die);
            else { Service.gI().petStatus((sbyte)status); RequestDiscipleInfo(); }
            return true;
        }
        if (!hit.StartsWith("disciple.cell.")) return false;
        discipleFocus = int.Parse(hit.Substring("disciple.cell.".Length));
        OpenDiscipleCell(discipleFocus); return true;
    }
    private void OpenDiscipleCell(int visual)
    {
        CloseInventoryBagDetail(); CloseDiscipleDetail();
        if (!discipleLoaded || !Char.myCharz().havePet) return;
        if (visual < 8) {
            selected = discipleBodySlots[visual];
            Item item = DiscipleItem(visual);
            if (item?.template != null) OpenInventoryItemDetail(item);
        } else { discipleSkill = visual - 8; selected = -1; }
        HudLayout.Invalidate();
    }
    private bool UpdateDiscipleKeyboard()
    {
        int up = Main.isPC ? 21 : 2, down = Main.isPC ? 22 : 8, fire = Main.isPC ? 25 : 5;
        if (GameCanvas.keyPressed[up] || GameCanvas.keyPressed[down]) {
            discipleFocus = (discipleFocus + (GameCanvas.keyPressed[down] ? 1 : 11)) % 12;
            GameCanvas.keyPressed[up] = GameCanvas.keyPressed[down] = false;
            OpenDiscipleCell(discipleFocus);
        }
        if (GameCanvas.keyPressed[12] || GameCanvas.keyPressed[fire]) {
            GameCanvas.keyPressed[12] = GameCanvas.keyPressed[fire] = false; OpenDiscipleCell(discipleFocus);
        }
        return true;
    }
    private void PaintDiscipleHeader(mGraphics g)
    {
        if (!Char.myCharz().havePet) { mFont.tahoma_7_yellow.drawString(g, "Chưa có đệ tử", 46, 12, 0); return; }
        Char pet = Char.myPetz();
        mFont.tahoma_7_yellow.drawString(g, "HP: " + pet.cHP + "/" + pet.cHPFull, 46, 1, 0);
        mFont.tahoma_7_yellow.drawString(g, "KI: " + pet.cMP + "/" + pet.cMPFull, 46, 12, 0);
        mFont.tahoma_7_yellow.drawString(g, "Sức đánh: " + pet.cDamFull + " · Chí mạng: " + pet.cCriticalFull, 46, 23, 0);
        mFont.tahoma_7_yellow.drawString(g, "Giáp: " + pet.cDefull + " · SM: " + NinjaUtil.getMoneys(pet.cPower), 46, 34, 0);
    }
    private void PaintDisciplePanel(mGraphics g)
    {
        Rect content = DiscipleLayout.Content;
        InventoryFrame.Clip(g, content);
        if (!Char.myCharz().havePet || !discipleLoaded) {
            string text = Char.myCharz().havePet ? "Đang tải đệ tử..." : "Chưa có đệ tử";
            mFont.tahoma_7_grey.drawString(g, text, W / 2, 90, mFont.CENTER); return;
        }
        int activeStatus = DiscipleFused ? 4 : Char.myPetz().petStatus;
        for (int i = 0; i < 5; i++) {
            Rect chip = DiscipleLayout.Chip(i);
            discipleStatusBounds[i] = chip;
            InventoryItemVisuals.PaintNativeTab(g, chip, content, i == activeStatus);
        }
        discipleStatusText.Paint(mFont.tahoma_7_grey, discipleStatuses, discipleStatusBounds,
            5, content, activeStatus, mFont.tahoma_7_grey.color1, mFont.tahoma_7_green2.color1);
        string statusTitle = "Trạng thái";
        if (DiscipleFused && discipleFusionUntil > 0) {
            int seconds = (int)System.Math.Max(0, (discipleFusionUntil - mSystem.currentTimeMillis() + 999) / 1000);
            statusTitle += " · Hợp thể " + seconds / 60 + ":" + (seconds % 60).ToString("00");
        }
        InventoryFrame.Clip(g, content);
        mFont.tahoma_7_grey.drawString(g, statusTitle, (int)DiscipleLayout.StatusTitle.x, (int)DiscipleLayout.StatusTitle.y, mFont.LEFT);
        mFont.tahoma_7_grey.drawString(g, "Trang bị", (int)DiscipleLayout.EquipmentTitle.x, (int)DiscipleLayout.EquipmentTitle.y, mFont.LEFT);
        mFont.tahoma_7_grey.drawString(g, "Kỹ năng", (int)DiscipleLayout.SkillTitle.x, (int)DiscipleLayout.SkillTitle.y, mFont.LEFT);
        for (int i = 0; i < 12; i++) {
            Rect cell = DiscipleLayout.Cell(i);
            bool chosen = i < 8 ? InventoryBagDetailOpen && selected == discipleBodySlots[i] : discipleSkill == i - 8;
            if (i < 8) {
                InventoryFrame.Rounded(g, cell, InventoryFrame.SlotBackground, 255, chosen ? InventoryFrame.Accent : 0);
                InventoryFrame.Clip(g, InventoryFrame.Intersection(cell, content));
                Item item = DiscipleItem(i);
                if (item?.template != null) {
                    SmallImage.drawSmallImage(g, item.template.iconID, (int)cell.center.x, (int)cell.center.y, 0, 3);
                    if (i < 6) InventoryItemVisuals.PaintLevel(g, cell, content, Mathf.Clamp(InventoryFrame.Option(item, Item.OPT_LVITEM), 0, 7), false);
                } else if (i == 7) {
                    discipleRuneBounds[0] = cell;
                    discipleRuneText.Paint(mFont.tahoma_7_grey, discipleRuneLabel, discipleRuneBounds,
                        1, content, -1, mFont.tahoma_7_grey.color1, mFont.tahoma_7_grey.color1);
                }
            } else {
                Skill skill = DiscipleSkill(i - 8);
                InventoryItemVisuals.PaintSkillIcon(g, skill?.template?.iconId ?? -1, cell, content, chosen);
                if (skill?.template != null) {
                    InventoryItemVisuals.PaintLevel(g, cell, content, skill.point, false);
                }
            }
            InventoryFrame.Clip(g, content);
        }
    }
    private void PaintDiscipleSkillDetail(mGraphics g)
    {
        Rect r = HudLayout.GetRect(InventoryFrame.Detail);
        InventoryFrame.Clip(g, r); InventoryFrame.Rounded(g, r, InventoryFrame.EquipmentBackground);
        Skill skill = DiscipleSkill(discipleSkill);
        int y = 30;
        if (skill?.template != null) {
            SmallImage.drawSmallImage(g, skill.template.iconId, (int)r.x + 17, 14, 0, 3);
            DrawDiscipleText(g, r, skill.template.name + " · Cấp " + skill.point, ref y);
            DrawDiscipleText(g, r, "KI: " + skill.manaUse + " · Hồi: " + skill.strTimeReplay() + "s", ref y);
            if (skill.template.description != null) foreach (string text in skill.template.description) DrawDiscipleText(g, r, text, ref y);
            DrawDiscipleText(g, r, skill.moreInfo, ref y);
        } else DrawDiscipleText(g, r, skill?.moreInfo ?? "Chưa học kỹ năng", ref y);
        Rect close = HudLayout.GetRect(DiscipleLayout.Close);
        InventoryFrame.Clip(g, close); InventoryFrame.Rounded(g, close, InventoryFrame.EquipmentBackground);
        g.drawImage(imgX, (int)close.center.x, (int)close.center.y, 3);
        InventoryFrame.Clip(g, HudLayout.ScreenBounds);
    }
    private static void DrawDiscipleText(mGraphics g, Rect r, string text, ref int y)
    {
        if (string.IsNullOrEmpty(text)) return;
        foreach (string line in mFont.tahoma_7_grey.splitFontArray(text, Mathf.Max(10, (int)r.width - 12))) {
            mFont.tahoma_7_grey.drawString(g, line, (int)r.x + 6, y, mFont.LEFT); y += 12;
        }
    }
}
