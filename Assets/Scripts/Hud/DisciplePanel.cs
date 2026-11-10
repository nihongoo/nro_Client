using System;
using Hud;
using UnityEngine;

namespace Hud
{
    internal static class DiscipleLayout
    {
        internal const string Close = "disciple.detail.close", ScrollArea = "disciple.content";
        private const float CellPixels = 72, GapPixels = 12;
        internal static bool Visible => InventoryFrame.Visible && GameCanvas.panel.DisciplePanelActive;
        private struct Geometry
        {
            internal Rect Viewport;
            internal float GridLeft, Size, Gap, Overhang, TitleHeight, ChipHeight;
            internal float EquipmentY, StatusTitleY, ChipY, SkillTitleY, SkillY;
        }
        private static Geometry geometry;
        private static long frameVersion = -1;
        private static Geometry Layout {
            get {
                long version = InventoryFrame.MetricsVersion;
                if (frameVersion == version) return geometry;

                Rect tabs = HudLayout.GetRect(InventoryFrame.Tabs);
                float blockGap = HudLayout.PixelsToLogical(6), labelGap = HudLayout.PixelsToLogical(3);
                float top = tabs.yMax + blockGap;
                var next = new Geometry {
                    Viewport = new Rect(tabs.x, top, Mathf.Max(0, tabs.width - 1),
                        Mathf.Max(0, HudLayout.GetRect(InventoryFrame.Money).y - blockGap - top)),
                    Size = HudLayout.PixelsToLogical(CellPixels), Gap = HudLayout.PixelsToLogical(GapPixels),
                    Overhang = HudLayout.PixelsToLogical(2), TitleHeight = HudLayout.PixelsToLogical(20),
                    ChipHeight = HudLayout.PixelsToLogical(32)
                };
                float gridWidth = 4 * next.Size + 3 * next.Gap;
                // Keep the reference sizes unless the viewport is too narrow for four cells.
                float fit = Mathf.Clamp01((next.Viewport.width - next.Overhang) / gridWidth);
                next.Size *= fit; next.Gap *= fit;
                next.GridLeft = next.Viewport.center.x - (gridWidth * fit + next.Overhang) / 2;
                next.EquipmentY = top + next.TitleHeight + labelGap;
                next.StatusTitleY = next.EquipmentY + 2 * next.Size + next.Gap + next.Overhang + blockGap;
                next.ChipY = next.StatusTitleY + next.TitleHeight + labelGap;
                next.SkillTitleY = next.ChipY + next.ChipHeight + blockGap;
                next.SkillY = next.SkillTitleY + next.TitleHeight + labelGap;
                geometry = next; frameVersion = version;
                return geometry;
            }
        }
        private static int Scroll => GameCanvas.panel?.DiscipleScroll ?? 0;
        internal static Rect Content => Layout.Viewport;
        internal static int MaxScroll {
            get {
                Geometry layout = Layout;
                return Mathf.Max(0, Mathf.CeilToInt(layout.SkillY + layout.Size + layout.Overhang - layout.Viewport.yMax));
            }
        }
        internal static Rect Chip(int i)
        {
            Geometry layout = Layout;
            float gap = layout.Overhang, width = Mathf.Max(0, (layout.Viewport.width - 4 * gap) / 5);
            return new Rect(layout.Viewport.x + i * (width + gap), layout.ChipY - Scroll, width, layout.ChipHeight);
        }
        internal static Rect EquipmentTitle => Title(Layout.Viewport.y);
        internal static Rect StatusTitle => Title(Layout.StatusTitleY);
        internal static Rect SkillTitle => Title(Layout.SkillTitleY);
        private static Rect Title(float y) => new Rect(Layout.Viewport.x, y - Scroll, Layout.Viewport.width, Layout.TitleHeight);
        internal static Rect Cell(int i)
        {
            Geometry layout = Layout;
            float stride = layout.Size + layout.Gap;
            float top = i < 8 ? layout.EquipmentY + i / 4 * stride : layout.SkillY;
            return new Rect(layout.GridLeft + i % 4 * stride, top - Scroll, layout.Size, layout.Size);
        }
        internal static Rect Badge(Rect cell)
        {
            float width = .30f * cell.width, height = Mathf.Min(HudLayout.PixelsToLogical(14), .24f * cell.height);
            float overhang = Layout.Overhang;
            return new Rect(cell.xMax - width + overhang, cell.yMax - height + overhang, width, height);
        }
        internal static Rect StarStrip(Rect cell, Rect badge)
        {
            float inset = Layout.Overhang, height = Mathf.Min(HudLayout.PixelsToLogical(7), cell.height / 5);
            return new Rect(cell.x + inset, cell.yMax - inset - height,
                Mathf.Max(0, badge.x - cell.x - 2 * inset), height);
        }
        internal static void Register()
        {
            HudLayout.Register(ScrollArea, () => Content, HudLayout.OverlayOrder + 11, () => Visible);
            for (int i = 0; i < 5; i++) {
                int slot = i;
                HudLayout.Register("disciple.status." + i, () => InventoryFrame.Intersection(Chip(slot), Content), HudLayout.OverlayOrder + 12,
                    () => Visible);
            }
            for (int i = 0; i < 12; i++) {
                int slot = i;
                HudLayout.Register("disciple.cell." + i, () => InventoryFrame.Intersection(Cell(slot), Content), HudLayout.OverlayOrder + 12,
                    () => Visible);
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
    private readonly InventoryItemVisuals.FittedTextRow discipleStatusText = new InventoryItemVisuals.FittedTextRow(clipToViewport: true);
    private readonly InventoryItemVisuals.FittedTextRow discipleRuneText = new InventoryItemVisuals.FittedTextRow(clipToViewport: true);
    private readonly InventoryItemVisuals.FittedTextRow discipleLevelText = new InventoryItemVisuals.FittedTextRow(12, 1, 1, true);
    private readonly InventoryItemVisuals.FittedTextRow discipleTitleText = new InventoryItemVisuals.FittedTextRow(3, 0, 0, true, TextAnchor.MiddleLeft);
    private readonly string[] discipleTitles = { "Trang bị", "Trạng thái", "Kỹ năng" };
    private readonly Rect[] discipleTitleBounds = new Rect[3];
    private static readonly string[] discipleLevels = { "Lv0", "Lv1", "Lv2", "Lv3", "Lv4", "Lv5", "Lv6", "Lv7" };
    private readonly string[] discipleLevelLabels = new string[12];
    private readonly Rect[] discipleLevelBounds = new Rect[12];
    private readonly Rect[] discipleRuneBounds = new Rect[1];
    private static readonly string[] discipleRuneLabel = { "NGỌC" };
    private readonly Rect[] discipleStatusBounds = new Rect[5];
    private int discipleSkill = -1, discipleFocus, discipleSkillScroll, discipleSkillLines;
    internal int DiscipleScroll { get; private set; }
    private bool discipleLoaded;
    private int discipleKind = -1;
    private Char discipleOwner;
    private long discipleNextRequest, discipleFusionUntil;
    internal bool DisciplePanelActive => InventoryFrameActive && Char.myCharz().havePet && currentTabIndex == DiscipleTab;
    internal bool DiscipleSkillOpen => DisciplePanelActive && discipleSkill >= 0;
    internal bool DiscipleFused => Char.myPetz().petStatus >= 4;
    private int InventoryToolIndex(int visual) => InventoryFrameActive && Char.myCharz().havePet && visual >= 3 ? visual + 1 : visual;

    internal void ClearDiscipleInfo()
    {
        discipleLoaded = false; discipleKind = -1; discipleFusionUntil = 0;
        CloseDiscipleDetail();
        if (InventoryBagDetailOpen) CloseInventoryBagDetail();
    }
    internal void SetDiscipleAvailable(bool available)
    {
        bool changed = Char.myCharz().havePet != available;
        Char.myCharz().havePet = available;
        if (!available) ClearDiscipleInfo();
        if (InventoryFrameActive && changed) {
            if (!available && currentTabIndex == DiscipleTab) SelectInventoryFrameTab(1);
            else if (currentTabIndex == 6) setTabTool();
            if (InventoryBagDetailOpen) BuildInventoryDetail();
        }
        HudLayout.Invalidate();
    }
    private void CloseDiscipleDetail() { discipleSkill = -1; discipleSkillScroll = 0; HudLayout.Invalidate(); }
    private void OpenDiscipleTab()
    {
        currentListLength = 0; cmy = cmtoY = cmyLim = 0; discipleFocus = 0;
        DiscipleScroll = 0;
        RequestDiscipleInfo();
    }
    private void RequestDiscipleInfo()
    {
        if (!ReferenceEquals(discipleOwner, Char.myCharz())) { ClearDiscipleInfo(); discipleOwner = Char.myCharz(); }
        discipleNextRequest = mSystem.currentTimeMillis() + 3000;
        if (!Char.myCharz().havePet) return;
        Service.gI().petInfo();
    }
    internal void ReceiveDiscipleInfo(Message message)
    {
        discipleOwner = Char.myCharz(); discipleLoaded = true;
        if (message.reader().available() >= 7 && message.reader().readUnsignedByte() == 0xD1) {
            Char.myPetz().cgender = message.reader().readByte();
            discipleKind = message.reader().readByte();
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
        if (InventoryBagDetailOpen && currentTabIndex == 1) {
            int top = inventoryDetailTopScroll, bottom = inventoryDetailBottomScroll;
            BuildInventoryDetail(); inventoryDetailTopScroll = top; inventoryDetailBottomScroll = bottom;
        }
        HudLayout.Invalidate();
    }
    private void SyncDisciplePanel()
    {
        cmy = cmtoY = cmyLim = 0;
        SetDiscipleScroll(DiscipleScroll);
        if (mSystem.currentTimeMillis() >= discipleNextRequest) RequestDiscipleInfo();
    }
    private void SetDiscipleScroll(int value)
    {
        int next = Mathf.Clamp(value, 0, DiscipleLayout.MaxScroll);
        if (DiscipleScroll == next) return;
        DiscipleScroll = next;
        HudLayout.Invalidate();
    }
    private static bool IsDiscipleContentHit(string hit) => hit != null && (hit == DiscipleLayout.ScrollArea
        || hit.StartsWith("disciple.cell.", StringComparison.Ordinal) || hit.StartsWith("disciple.status.", StringComparison.Ordinal));
    private void UpdateDiscipleContentDrag()
    {
        if (!DisciplePanelActive || !discipleLoaded) return;
        if (IsDiscipleContentHit(inventoryPressed) && (GameCanvas.isPointerDown || GameCanvas.isPointerJustRelease)) {
            int delta = GameCanvas.py - inventoryDragY;
            if (System.Math.Abs(delta) > 4) inventoryDragging = true;
            if (inventoryDragging) SetDiscipleScroll(inventoryDragScroll - delta);
        }
        int wheel = GameCanvas.pXYScrollMouse;
        if (wheel != 0 && IsDiscipleContentHit(HudLayout.HitTest(new Vector2(GameCanvas.pxMouse, GameCanvas.pyMouse)))) {
            SetDiscipleScroll(DiscipleScroll - wheel * Mathf.Max(1, Mathf.RoundToInt(InventoryFrame.Unit(24))));
            GameCanvas.pXYScrollMouse = 0;
        }
    }
    private void RevealDiscipleCell(int index)
    {
        Rect cell = DiscipleLayout.Cell(index), viewport = DiscipleLayout.Content;
        if (cell.yMin < viewport.yMin || cell.height >= viewport.height)
            SetDiscipleScroll(DiscipleScroll + Mathf.FloorToInt(cell.yMin - viewport.yMin));
        else if (cell.yMax > viewport.yMax)
            SetDiscipleScroll(DiscipleScroll + Mathf.CeilToInt(cell.yMax - viewport.yMax));
    }
    private static int DiscipleSlot(Item item)
    {
        if (item?.template == null) return -1;
        int type = item.template.type;
        if (type >= 0 && type <= 5) return type;
        return type == 32 ? 6 : type == DiscipleRuneType ? 7 : -1;
    }
    private bool CanEquipForDisciple(Item item)
    {
        return Char.myCharz().havePet && ReferenceEquals(discipleOwner, Char.myCharz()) && discipleLoaded
            && discipleKind >= 0 && !DiscipleFused && DiscipleSlot(item) >= 0
            && (discipleKind > 0 || item.template.gender >= 3 || item.template.gender == Char.myPetz().cgender);
    }
    private void EquipForDisciple(Item item)
    {
        int bagIndex = selected - (Char.myCharz().arrItemBody?.Length ?? 0);
        Item[] bag = Char.myCharz().arrItemBag;
        if (!CanEquipForDisciple(item) || bagIndex < 0 || bagIndex >= (bag?.Length ?? 0)
            || !ReferenceEquals(item, bag[bagIndex])) return;
        Service.gI().getItem(BAG_PET, (sbyte)bagIndex);
        CloseInventoryBagDetail();
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
            RevealDiscipleCell(discipleFocus);
            OpenDiscipleCell(discipleFocus);
        }
        if (GameCanvas.keyPressed[12] || GameCanvas.keyPressed[fire]) {
            GameCanvas.keyPressed[12] = GameCanvas.keyPressed[fire] = false; OpenDiscipleCell(discipleFocus);
        }
        return true;
    }
    private void PaintDiscipleHeader(mGraphics g)
    {
        Char pet = Char.myPetz();
        mFont.tahoma_7_yellow.drawString(g, "HP: " + pet.cHP + "/" + pet.cHPFull, 46, 1, 0);
        mFont.tahoma_7_yellow.drawString(g, "KI: " + pet.cMP + "/" + pet.cMPFull, 46, 12, 0);
        mFont.tahoma_7_yellow.drawString(g, "Sức đánh: " + pet.cDamFull + " · Chí mạng: " + pet.cCriticalFull, 46, 23, 0);
        mFont.tahoma_7_yellow.drawString(g, "Giáp: " + pet.cDefull + " · SM: " + NinjaUtil.getMoneys(pet.cPower), 46, 34, 0);
    }
    private void PaintDisciplePanel(mGraphics g)
    {
        Rect content = DiscipleLayout.Content;
        if (content.width <= 0 || content.height <= 0) return;
        InventoryFrame.Clip(g, content);
        if (!discipleLoaded) {
            mFont.tahoma_7_grey.drawString(g, "Đang tải đệ tử...", (int)content.center.x, (int)content.y, mFont.CENTER); return;
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
        discipleTitles[1] = statusTitle;
        discipleTitleBounds[0] = DiscipleLayout.EquipmentTitle;
        discipleTitleBounds[1] = DiscipleLayout.StatusTitle;
        discipleTitleBounds[2] = DiscipleLayout.SkillTitle;
        discipleTitleText.Paint(mFont.tahoma_7_grey, discipleTitles, discipleTitleBounds,
            3, content, -1, mFont.tahoma_7_grey.color1, mFont.tahoma_7_grey.color1);
        int badgeCount = 0;
        float iconInset = HudLayout.PixelsToLogical(3);
        for (int i = 0; i < 12; i++) {
            Rect cell = DiscipleLayout.Cell(i);
            Rect badge = DiscipleLayout.Badge(cell);
            if (cell.yMin >= content.yMax || badge.yMax <= content.yMin) continue;
            bool chosen = i < 8 ? InventoryBagDetailOpen && selected == discipleBodySlots[i] : discipleSkill == i - 8;
            int level = -1;
            if (i < 8) {
                InventoryFrame.Rounded(g, cell, InventoryFrame.SlotBackground, 255, chosen ? InventoryFrame.Accent : 0);
                Item item = DiscipleItem(i);
                if (item?.template != null) {
                    InventoryItemVisuals.PaintCompactIcon(g, item.template.iconID, InventoryFrame.Inset(cell, iconInset), content);
                    if (i < 6) level = InventoryFrame.Option(item, Item.OPT_LVITEM);
                    InventoryItemVisuals.PaintStars(cell, content, item, DiscipleLayout.StarStrip(cell, badge));
                } else if (i == 7) {
                    discipleRuneBounds[0] = cell;
                    discipleRuneText.Paint(mFont.tahoma_7_grey, discipleRuneLabel, discipleRuneBounds,
                        1, content, -1, mFont.tahoma_7_grey.color1, mFont.tahoma_7_grey.color1);
                }
            } else {
                Skill skill = DiscipleSkill(i - 8);
                InventoryItemVisuals.PaintSkillIcon(g, skill?.template?.iconId ?? -1, cell, content, chosen, frameOnTop: true);
                if (skill?.template != null) level = skill.point;
            }
            InventoryFrame.Clip(g, content);
            if (level >= 0) {
                PaintDiscipleBadge(badge, content);
                discipleLevelLabels[badgeCount] = discipleLevels[Mathf.Clamp(level, 0, 7)];
                discipleLevelBounds[badgeCount++] = badge;
            }
        }
        discipleLevelText.Paint(mFont.tahoma_7, discipleLevelLabels, discipleLevelBounds,
            badgeCount, content, -1, Color.black, Color.black);
    }
    private static void PaintDiscipleBadge(Rect badge, Rect viewport)
    {
        Rect clip = InventoryFrame.Intersection(badge, viewport);
        if (clip.width <= 0 || clip.height <= 0) return;
        const int fill = InventoryFrame.Accent;
        int zoom = Mathf.Max(1, mGraphics.zoomLevel);
        Color previous = GUI.color;
        try {
            GUI.color = new Color32((byte)(fill >> 16), (byte)((fill >> 8) & 255), (byte)(fill & 255), 255);
            GUI.DrawTexture(new Rect(clip.x * zoom, clip.y * zoom, clip.width * zoom, clip.height * zoom), Texture2D.whiteTexture);
        } finally { GUI.color = previous; }
    }
    private void PaintDiscipleSkillDetail(mGraphics g)
    {
        Rect r = HudLayout.GetRect(InventoryFrame.Detail);
        InventoryFrame.Clip(g, r); InventoryFrame.Rounded(g, r, InventoryFrame.EquipmentBackground);
        Skill skill = DiscipleSkill(discipleSkill);
        int maxScroll = Mathf.Max(0, discipleSkillLines * 12 + 36 - (int)r.height);
        discipleSkillScroll = Mathf.Clamp(discipleSkillScroll, 0, maxScroll);
        InventoryFrame.Clip(g, new Rect(r.x + 3, 27, Mathf.Max(0, r.width - 6), Mathf.Max(0, r.height - 30)));
        int y = 30 - discipleSkillScroll;
        if (skill?.template != null) {
            DrawDiscipleText(g, r, skill.template.name + " · Cấp " + skill.point, ref y);
            DrawDiscipleText(g, r, "KI: " + skill.manaUse + " · Hồi: " + skill.strTimeReplay() + "s", ref y);
            if (skill.template.description != null) foreach (string text in skill.template.description) DrawDiscipleText(g, r, text, ref y);
            DrawDiscipleText(g, r, skill.moreInfo, ref y);
        } else DrawDiscipleText(g, r, skill?.moreInfo ?? "Chưa học kỹ năng", ref y);
        discipleSkillLines = (y - 30 + discipleSkillScroll) / 12;
        InventoryFrame.Clip(g, new Rect(r.x, 0, r.width, 26));
        if (skill?.template != null) SmallImage.drawSmallImage(g, skill.template.iconId, (int)r.x + 17, 14, 0, 3);
        Rect close = HudLayout.GetRect(DiscipleLayout.Close);
        InventoryFrame.Clip(g, close); InventoryFrame.Rounded(g, close, InventoryFrame.EquipmentBackground);
        g.drawImage(imgX, (int)close.center.x, (int)close.center.y, 3);
        InventoryFrame.Clip(g, HudLayout.ScreenBounds);
    }
    private void UpdateDiscipleSkillDrag()
    {
        if (!DiscipleSkillOpen || inventoryPressed != "disciple.detail") return;
        if (GameCanvas.isPointerJustDown) inventoryDetailDragScroll = discipleSkillScroll;
        if (!(GameCanvas.isPointerDown || GameCanvas.isPointerJustRelease)) return;
        int delta = GameCanvas.py - inventoryDragY;
        if (System.Math.Abs(delta) > 4) inventoryDragging = true;
        if (inventoryDragging) discipleSkillScroll = Mathf.Clamp(inventoryDetailDragScroll - delta, 0,
            Mathf.Max(0, discipleSkillLines * 12 + 36 - GameCanvas.h));
    }
    private void PaintDiscipleOverview(mGraphics g)
    {
        if (!discipleLoaded || !Char.myCharz().havePet) return;
        Rect r = HudLayout.GetRect(InventoryFrame.Detail);
        InventoryFrame.Clip(g, r); InventoryFrame.Rounded(g, r, InventoryFrame.EquipmentBackground);
        int y = 8; Char pet = Char.myPetz();
        DrawDiscipleText(g, r, pet.cName, ref y);
        DrawDiscipleText(g, r, "Tiềm năng: " + NinjaUtil.getMoneys(pet.cTiemNang), ref y);
        DrawDiscipleText(g, r, "Thể lực: " + pet.cStamina + "/" + pet.cMaxStamina, ref y);
        DrawDiscipleText(g, r, "Chọn trang bị hoặc kỹ năng để xem chi tiết", ref y);
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
