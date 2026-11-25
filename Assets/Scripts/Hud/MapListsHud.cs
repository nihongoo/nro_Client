using System.Collections.Generic;
using Mod.CuongLe;
using UnityEngine;

namespace Hud
{
    internal static class MapListsHud
    {
        private const int Order = HudLayout.MenuOrder + 10, MaxRows = 5, Slots = MaxRows + 1;
        private const float RefreshInterval = 0.1f;
        private const string ModePreference = "map-lists-combat";
        // World-logic distances, never converted through screen pixels or zoom.
        private const int HalfWidthLeft = 120, HalfWidthRight = 120, Up = 90, Down = 60;
        private enum ListKind { Items, People, Bosses, Mobs }
        private sealed class RowSlot
        {
            internal string Full, Hit, Name, Dot, Icon;
            internal string SourceName, OwnerName, Label;
            internal int LabelWidth, LabelZoom;
            internal mFont LabelFont;
        }
        private sealed class ListBox
        {
            internal string Header, Body, Rail;
            internal bool LeftSide, Open = true;
            internal ListKind Kind;
            internal ListKind CaptionKind;
            internal string Caption;
            internal int CaptionCount, CaptionWidth, CaptionZoom;
            internal mFont CaptionFont;
            internal bool Items => Kind == ListKind.Items;
            internal bool Combat => Kind == ListKind.Bosses || Kind == ListKind.Mobs;
            internal int Scroll;
            internal readonly RowSlot[] Rows = new RowSlot[Slots];
            internal List<IMapObject> Entries => entries[(int)Kind];
            internal int Count => Entries.Count;
            internal int RowHeight => Mathf.Max(
                Items && HudLayout.IsMobileLayout ? Mathf.CeilToInt(HudLayout.GetRect(TopActionsHud.Hub).height) : Unit(Items ? 32 : 28),
                mFont.tahoma_7_white.getHeight() + Unit(Combat ? 10 : 4));
            internal int First => Scroll / RowHeight;
        }
        private static readonly string[] captions = { "Vật phẩm", "Người", "Boss", "Quái" };
        private static readonly List<IMapObject>[] entries = {
            new List<IMapObject>(), new List<IMapObject>(), new List<IMapObject>(), new List<IMapObject>()
        };
        private static readonly ListBox left = new ListBox { Header = "map-lists.items.header",
            Body = "map-lists.items.body", Rail = "map-lists.items.rail", LeftSide = true, Kind = ListKind.Items };
        private static readonly ListBox right = new ListBox { Header = "map-lists.people.header",
            Body = "map-lists.people.body", Rail = "map-lists.people.rail", Kind = ListKind.People };
        private static readonly ListBox[] boxes = { left, right };
        internal static bool ShowsCombatLists => left.Kind == ListKind.Bosses;
        private static readonly List<Rect> obstacles = new List<Rect>();
        private static int consumedTick = -1, mapId = -1, zoneId = -1, startScroll;
        private static bool hasSnapshot, modePreferenceDirty;
        private static float nextRefresh;
        private static bool captured, moved;
        private static Vector2 pressPoint;
        private static string pressed;
        private static IMapObject pressedEntry;
        private static ListBox active;
        internal static bool ConsumesInput => consumedTick == GameCanvas.gameTick;
        private static int Unit(float pixels) => Mathf.Max(1, Mathf.RoundToInt(HudLayout.PixelsToLogical(pixels)));
        private static int Gap => Unit(4);
        private static int Padding => Unit(8);
        private const int Cream = 0xfff3da, PanelColor = 0x28180c, Accent = 0xf28c1b, Divider = 0xf2b45a;
        private static readonly Dictionary<Char, Char> owners = new Dictionary<Char, Char>();
        private static readonly Dictionary<int, Char> charsById = new Dictionary<int, Char>();
        private static readonly HashSet<int> bossCharIds = new HashSet<int>();
        private static readonly HashSet<int> mobIds = new HashSet<int>();
        private static Rect paintClipPixels;
        private static float Pixel => HudLayout.PixelsToLogical(1);
        // The zoom-1 bitmap font has no middle-dot/ellipsis glyphs; keep that font and use
        // its existing period glyphs as a fallback instead of displaying missing characters.
        private static string Separator => mGraphics.zoomLevel == 1 ? " . " : " · ";
        private static int HeaderHeight => Mathf.Max(Unit(24), mFont.tahoma_7b_white.getHeight() + Unit(8));
        private static bool Visible => GameScr.instance != null && GameCanvas.currentScreen == GameScr.instance
            && GameScr.isPaint && !GameScr.isPaintOther && !GameCanvas.isLoading && !Char.ischangingMap
            && Char.ExistingLocalPlayer != null;
        private static bool CanInput => Visible && GameCanvas.currentDialog == null && !GameCanvas.panel.isShow
            && !GameCanvas.menu.showMenu && !ChatTextField.gI().isShow && ChatPopup.currChatPopup == null
            && ChatPopup.serverChatPopUp == null && !DoHoa.isShowMenuVIP && !InfoDlg.isLock
            && !GameScr.instance.isPaintPopup() && !Notifications.NotificationCenter.ConsumesInput
            && !TopActionsHud.ConsumesInput;
        internal static bool CanCaptureInput => CanInput;
        internal static bool Owns(string name) => name != null
            && (name.StartsWith("map-lists.items.") || name.StartsWith("map-lists.people."));
        internal static void CancelPointer()
        {
            captured = false; pressed = null; pressedEntry = null; active = null;
        }

        internal static void ToggleMode()
        {
            bool showCombat = !ShowsCombatLists;
            SetMode(showCombat);
            PlayerPrefs.SetInt(ModePreference, showCombat ? 1 : 0);
            modePreferenceDirty = true;
        }

        internal static void FlushModePreference()
        {
            if (!modePreferenceDirty) return;
            PlayerPrefs.Save();
            modePreferenceDirty = false;
        }

        private static void SetMode(bool showCombat)
        {
            left.Kind = showCombat ? ListKind.Bosses : ListKind.Items;
            right.Kind = showCombat ? ListKind.Mobs : ListKind.People;
            foreach (ListBox box in boxes) { box.Open = true; box.Scroll = 0; }
            hasSnapshot = false;
            CancelPointer();
            HudLayout.Invalidate();
        }

        internal static void Register()
        {
            int savedMode = PlayerPrefs.HasKey(ModePreference) ? PlayerPrefs.GetInt(ModePreference) : Rms.loadRMSInt(ModePreference);
            SetMode(savedMode == 1);
            foreach (ListBox box in boxes) RegisterBox(box);
        }
        private static void RegisterBox(ListBox box)
        {
            HudLayout.Register(box.Header, () => HeaderBounds(box), Order, () => Visible && box.Count > 0);
            HudLayout.Register(box.Body, () => BodyBounds(box), Order, () => Visible && box.Count > 0 && box.Open);
            HudLayout.Register(box.Rail, () => {
                Rect body = HudLayout.GetRect(box.Body);
                return box.Count * box.RowHeight <= body.height ? new Rect()
                    : new Rect(body.xMax - Unit(8), body.y, Unit(8), body.height);
            }, Order, () => Visible && box.Open);
            for (int i = 0; i < Slots; i++) {
                int slot = i;
                string prefix = box.Body + "." + i;
                var row = new RowSlot { Full = prefix + ".full", Hit = prefix + ".hit", Name = prefix + ".name", Dot = prefix + ".dot", Icon = prefix + ".icon" };
                box.Rows[i] = row;
                System.Func<bool> show = () => Visible && box.Open && Index(box, slot) < box.Count;
                HudLayout.Register(row.Full, () => {
                    if (Index(box, slot) >= box.Count) return new Rect();
                    Rect body = HudLayout.GetRect(box.Body);
                    return new Rect(body.x, body.y + Index(box, slot) * box.RowHeight - box.Scroll,
                        Mathf.Max(0, body.width - Unit(8)), box.RowHeight);
                }, Order, show, false);
                // Gray items still capture a click for focus only (user's follow-up choice).
                HudLayout.Register(row.Hit, () => Intersection(HudLayout.GetRect(row.Full), HudLayout.GetRect(box.Body)), Order, show);
                HudLayout.Register(row.Icon, () => {
                    Rect full = HudLayout.GetRect(row.Full); int size = box.Items && HudLayout.IsMobileLayout
                        ? Mathf.Max(Unit(20), box.RowHeight - Gap * 2) : Unit(20);
                    return new Rect(full.x + Padding, full.center.y - size / 2f, size, size);
                }, Order, show, false);
                HudLayout.Register(row.Dot, () => {
                    Rect full = HudLayout.GetRect(row.Full); int size = Unit(8);
                    return new Rect(box.Items ? full.xMax - Padding - size : full.x + Padding, full.center.y - size / 2f, size, size);
                }, Order, show, false);
                HudLayout.Register(row.Name, () => {
                    Rect full = HudLayout.GetRect(row.Full), dot = HudLayout.GetRect(row.Dot);
                    float left = box.Items ? HudLayout.GetRect(row.Icon).xMax + Gap : dot.xMax + Gap;
                    float right = box.Items ? dot.x - Gap : full.xMax - Padding;
                    return new Rect(left, full.y, Mathf.Max(0, right - left), full.height);
                }, Order, show, false);
            }
        }
        private static int Index(ListBox box, int slot) => box.First + slot;

        private static float BottomLimit(float x, float width, float minTop)
        {
            float bottom = HudLayout.GetRect(HudLayout.WorldBar).yMin - Gap;
            HudLayout.CopyVisualControls(obstacles);
            foreach (Rect obstacle in obstacles)
                if (obstacle.xMax > x && obstacle.xMin < x + width && obstacle.yMax > minTop)
                    bottom = Mathf.Min(bottom, Mathf.Max(minTop, obstacle.yMin - Gap));
            return bottom;
        }
        private static Rect HeaderBounds(ListBox box)
        {
            if (box.Count == 0) return new Rect();
            Rect top = HudLayout.GetRect("top-info.bounds"), chat = HudLayout.GetRect(HudLayout.ChatButton);
            // Both platforms match the visible HUD spans, excluding FPS from the power bar.
            Rect start = HudLayout.GetRect(box.LeftSide ? "top-info.avatar" : TopActionsHud.Hub);
            Rect end = box.LeftSide ? HudLayout.GetRect("top-info.exp") : chat;
            float width = Mathf.Max(0,end.xMax-start.xMin);
            float margin = box.LeftSide ? top.xMin : GameCanvas.w - chat.xMax;
            float minTop = box.LeftSide ? TopLeftHudStack.ItemsTop(Gap)
                : Mathf.Max(chat.yMax, HudLayout.GetRect(TopActionsHud.MapListSwitch).yMax) + Gap;
            Rect anchored = HudLayout.Anchor(box.LeftSide ? HorizontalAnchor.Left : HorizontalAnchor.Right,
                VerticalAnchor.Top, new Vector2(width, HeaderHeight), new Vector2(margin, minTop));
            float bottom = BottomLimit(anchored.x, width, minTop);
            return bottom < anchored.yMax || width <= 0 ? new Rect() : anchored;
        }
        private static Rect BodyBounds(ListBox box)
        {
            Rect header = HudLayout.GetRect(box.Header);
            if (!box.Open || header.width <= 0) return new Rect();
            float top = header.yMax + Gap;
            float available = BottomLimit(header.x, header.width, top) - top;
            int rows = Mathf.Min(MaxRows,Mathf.Min(box.Count,Mathf.FloorToInt(Mathf.Max(0,available)/box.RowHeight)));
            int height = rows*box.RowHeight;
            return new Rect(header.x, top, header.width, height);
        }
        internal static void BeginSession()
        {
            SetMode(ShowsCombatLists);
            MapNavigation.Cancel(); mapId = zoneId = -1;
            ClearEntries(); consumedTick = -1; HudLayout.Invalidate();
        }
        private static void ClearEntries()
        {
            foreach (List<IMapObject> list in entries) list.Clear();
            owners.Clear(); charsById.Clear();
            bossCharIds.Clear(); mobIds.Clear();
        }

        private static bool SnapshotCurrent => hasSnapshot && mapId == TileMap.mapID && zoneId == TileMap.zoneID;

        internal static void RefreshAfterWorldUpdate()
        {
            if (Visible && (!SnapshotCurrent || Time.unscaledTime >= nextRefresh)) Refresh();
        }

        private static void Refresh()
        {
            bool changed = !SnapshotCurrent;
            int oldLeftCount = left.Count, oldRightCount = right.Count;
            ClearEntries();
            if (mapId != TileMap.mapID || zoneId != TileMap.zoneID) {
                mapId = TileMap.mapID; zoneId = TileMap.zoneID;
                foreach (ListBox box in boxes) box.Scroll = 0;
                CancelPointer();
            }
            if (ShowsCombatLists) RefreshCombat();
            else RefreshNearby();
            hasSnapshot = true;
            nextRefresh = Time.unscaledTime + RefreshInterval;
            if (changed || left.Count != oldLeftCount || right.Count != oldRightCount)
                HudLayout.Invalidate();
            foreach (ListBox box in boxes)
                if (box.Scroll > 0) ClampScroll(box);
        }

        private static void RefreshCombat()
        {
            for (int i = 0; i < GameScr.vCharInMap.size(); i++) {
                if (GameScr.vCharInMap.elementAt(i) is Char person && person.charID < 0
                    && !string.IsNullOrEmpty(person.cName) && person.cName != "Trọng tài" && !person.cName.StartsWith("#")
                    && GameScr.TargetPriority(person) == 0 && IsAlive(person) && bossCharIds.Add(person.charID))
                    entries[(int)ListKind.Bosses].Add(person);
            }
            for (int i = 0; i < GameScr.vMob.size(); i++) {
                if (!(GameScr.vMob.elementAt(i) is Mob mob) || mob.isMobMe || mob.hp <= 0
                    || mob.status == 0 || mob.status == 1) continue;
                bool boss = mob.checkIsBoss() || mob.isBigBoss();
                if ((boss || InRange(mob)) && mobIds.Add(mob.mobId))
                    entries[(int)(boss ? ListKind.Bosses : ListKind.Mobs)].Add(mob);
            }
        }

        private static void RefreshNearby()
        {
            bool indexedOwners = false;
            for (int i = 0; i < GameScr.vCharInMap.size(); i++) {
                if (!(GameScr.vCharInMap.elementAt(i) is Char person) || !InRange(person)
                    || person.charID == Char.myCharz().charID || ReferenceEquals(person, Char.myCharz())) continue;
                bool disciple = person.isPet || (person.cName != null && person.cName.StartsWith("$"));
                if (disciple) {
                    // PetService.createNewPet assigns -master.id for a player disciple.
                    // Require the positive-ID owner to be loaded; never guess from names.
                    long ownerId = -(long)person.charID;
                    if (ownerId <= 0 || ownerId > int.MaxValue || ownerId == Char.myCharz().charID) continue;
                    if (!indexedOwners) {
                        IndexOwners();
                        indexedOwners = true;
                    }
                    if (!charsById.TryGetValue((int)ownerId, out Char owner)) continue;
                    owners[person] = owner;
                }
                else {
                    if (person.charID < 0 || person.isMiniPet || string.IsNullOrEmpty(person.cName) || person.cName == "Trọng tài"
                        || person.cName.StartsWith("#")) continue;
                }
                entries[(int)ListKind.People].Add(person);
            }
            for (int i = 0; i < GameScr.vItemMap.size(); i++)
                if (GameScr.vItemMap.elementAt(i) is ItemMap item && item.template != null && item.status != 2
                    && (!HudLayout.IsMobileLayout || item.template.type != 22) && InRange(item)) entries[(int)ListKind.Items].Add(item);
        }
        private static bool IsAlive(Char person) => person.cHP > 0 && person.statusMe != 5 && person.statusMe != 14;

        private static void IndexOwners()
        {
            for (int i = 0; i < GameScr.vCharInMap.size(); i++)
                if (GameScr.vCharInMap.elementAt(i) is Char owner && owner.charID > 0
                    && !owner.isPet && !owner.isMiniPet && !charsById.ContainsKey(owner.charID))
                    charsById.Add(owner.charID, owner);
        }
        // One fixed world rectangle for both lists, centered horizontally at the player's feet.
        // Characters use their feet; items use the center of their existing IMapObject bounds.
        internal static bool InRange(IMapObject value)
        {
            Char localPlayer = Char.ExistingLocalPlayer;
            if (localPlayer == null || Char.ischangingMap || value == null || value.isInvisible()) return false;
            if (value is Char person && (person.isInvisiblez || person.IsHiddenFromHudList
                    && person.cHP > 0 && person.statusMe != 5 && person.statusMe != 14)) return false;
            long dx = (long)value.getX() - localPlayer.cx;
            long itemY = value is Char ? value.getY() : (long)value.getY() - value.getH() / 2;
            long dy = itemY - localPlayer.cy;
            return dx >= -HalfWidthLeft && dx <= HalfWidthRight && dy >= -Up && dy <= Down;
        }
        private static void ClampScroll(ListBox box) => SetScroll(box, box.Scroll);
        private static void SetScroll(ListBox box, int value)
        {
            int next = Mathf.Clamp(value, 0, Mathf.Max(0, box.Count * box.RowHeight - (int)HudLayout.GetRect(box.Body).height));
            if (next == box.Scroll) return;
            box.Scroll = next; HudLayout.Invalidate();
        }
        private static ListBox Owner(string name)
        {
            if (name == null) return null;
            foreach (ListBox box in boxes) {
                if (name == box.Header || name == box.Rail || name == box.Body) return box;
                foreach (RowSlot row in box.Rows) if (name == row.Hit) return box;
            }
            return null;
        }
        private static IMapObject Entry(ListBox box, string name)
        {
            for (int i = 0; i < Slots; i++)
                if (name == box.Rows[i].Hit && Index(box, i) < box.Count)
                    return box.Entries[Index(box, i)];
            return null;
        }
        internal static void UpdateInput()
        {
            consumedTick = -1;
            if (!Visible) { CancelPointer(); MapNavigation.Cancel(); return; }
            if (!CanInput) { CancelPointer(); return; }
            int wheel = GameCanvas.pXYScrollMouse;
            if (!captured && !GameCanvas.isPointerJustDown && wheel == 0) return;
            if (!SnapshotCurrent) Refresh();
            Vector2 pointer = new Vector2(GameCanvas.px, GameCanvas.py);
            string hit = captured || GameCanvas.isPointerJustDown ? HudLayout.HitTest(pointer) : null;
            if (GameCanvas.isPointerJustDown) {
                active = Owner(hit); captured = active != null; pressed = hit; moved = false;
                pressPoint = pointer;
                if (captured) {
                    startScroll = active.Scroll; pressedEntry = Entry(active, hit);
                    MapNavigation.StopMovement();
                    Char player = Char.myCharz();
                    if (player.currentMovePoint != null) {
                        player.currentMovePoint = null;
                        player.endMovePointCommand = null;
                        player.cvx = player.cvy = 0;
                    }
                }
            }
            if (captured) {
                if (Mathf.Abs(pointer.y - pressPoint.y) >= 6 || Mathf.Abs(pointer.x - pressPoint.x) >= 6) moved = true;
                if (moved && pressed != active.Header) SetScroll(active, startScroll + Mathf.RoundToInt(pressPoint.y - pointer.y));
                if (GameCanvas.isPointerJustRelease) {
                    if (!moved && hit == pressed) Activate(active, hit);
                    captured = false; pressed = null;
                }
                Consume();
            }
            if (wheel != 0) {
                Vector2 mouse = new Vector2(GameCanvas.pxMouse, GameCanvas.pyMouse);
                string mouseHit = HudLayout.HitTest(mouse);
                foreach (ListBox box in boxes)
                    if (box.Open && Owner(mouseHit) == box && HudLayout.GetRect(box.Body).Contains(mouse)) {
                        SetScroll(box, box.Scroll - wheel * box.RowHeight); moved = true; Consume(); break;
                    }
            }
        }
        private static void Activate(ListBox box, string name)
        {
            if (name == box.Header) {
                box.Open = !box.Open; HudLayout.Invalidate(); ClampScroll(box); return;
            }
            IMapObject entry = Entry(box, name);
            if (entry == null || !ReferenceEquals(entry, pressedEntry)) return;
            if (box.Kind != ListKind.Bosses && !InRange(entry)) return;
            if (box.Combat && GameScr.CanSelectTarget(entry)) {
                Char.myCharz().cancelAttack();
                Char.myCharz().focusManualTo(entry);
                MapNavigation.GoToTarget(entry);
            }
            else if (box.Kind == ListKind.People && GameScr.CanSelectTarget(entry)) {
                MapNavigation.Cancel();
                Char.myCharz().cancelAttack();
                Char.myCharz().focusManualTo(entry);
            }
            else if (entry is ItemMap item && GameScr.vItemMap.contains(item)) {
                Char.myCharz().cancelAttack();
                Char.myCharz().focusManualTo(item);
                if (GameScr.instance.CanPickHudItem(item)) MapNavigation.GoToTarget(item);
                else MapNavigation.Cancel();
            }
        }
        private static void Consume()
        {
            consumedTick = GameCanvas.gameTick;
            GameCanvas.clearAllPointerEvent(); GameCanvas.clearKeyHold(); GameCanvas.clearKeyPressed();
            GameCanvas.keyAsciiPress = 0; GameCanvas.pXYScrollMouse = 0;
        }

        private static Rect Intersection(Rect a, Rect b)
        {
            float x = Mathf.Max(a.xMin, b.xMin), y = Mathf.Max(a.yMin, b.yMin);
            return Rect.MinMaxRect(x, y, Mathf.Max(x, Mathf.Min(a.xMax, b.xMax)), Mathf.Max(y, Mathf.Min(a.yMax, b.yMax)));
        }
        private static void Clip(mGraphics g, Rect rect)
        {
            paintClipPixels = HudButtonSkin.ScreenPixels(rect);
            g.setClip((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        }
        private static string ShortName(string value, int width, mFont font = null)
        {
            if (string.IsNullOrEmpty(value) || width <= 0) return "";
            if (font == null) font = mFont.tahoma_7_white;
            if (font.getWidth(value) <= width) return value;
            string suffix = mGraphics.zoomLevel == 1 ? "..." : "…";
            if (font.getWidth(suffix) > width) return "";
            int length = value.Length;
            while (length > 0 && font.getWidth(value.Substring(0, length) + suffix) > width) length--;
            return value.Substring(0, length) + suffix;
        }
        private static void Fill(Rect rect, int rgb, float alpha)
        {
            SolidPixels(HudButtonSkin.ScreenPixels(rect), rgb, alpha);
        }
        private static void SolidPixels(Rect pixels, int rgb, float alpha)
        {
            pixels = Intersection(pixels, paintClipPixels);
            if (pixels.width <= 0 || pixels.height <= 0) return;
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            try {
                GUI.matrix = Matrix4x4.identity;
                GUI.color = new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alpha);
                GUI.DrawTexture(pixels, Texture2D.whiteTexture);
            }
            finally { GUI.matrix = matrix; GUI.color = color; }
        }
        private static void Dot(Rect rect, Color color, float alpha)
        {
            int rgb = ((int)(color.r * 255) << 16) | ((int)(color.g * 255) << 8) | (int)(color.b * 255);
            Rect pixels = HudButtonSkin.ScreenPixels(rect);
            float radius = pixels.height / 2f;
            int height = Mathf.CeilToInt(pixels.height);
            int first = 0, previousInset = -1;
            for (int y = 0; y < height; y++) {
                float dy = y + 0.5f - radius;
                int inset = Mathf.FloorToInt(radius - Mathf.Sqrt(Mathf.Max(0, radius * radius - dy * dy)));
                if (inset == previousInset) continue;
                if (previousInset >= 0) DrawStrip(previousInset, first, y);
                previousInset = inset;
                first = y;
            }
            if (previousInset >= 0) DrawStrip(previousInset, first, height);

            void DrawStrip(int inset, int start, int end) =>
                SolidPixels(new Rect(pixels.x + inset, pixels.y + start, pixels.width - 2 * inset, end - start), rgb, alpha);
        }
        // Solid geometry in actual screen pixels, including the one-pixel corner cuts.
        private static void Rounded(Rect rect, int color, float alpha)
        {
            if (rect.width <= 0 || rect.height <= 0) return;
            Rect pixels = HudButtonSkin.ScreenPixels(rect);
            SolidPixels(new Rect(pixels.x + 1, pixels.y, Mathf.Max(0, pixels.width - 2), 1), color, alpha);
            SolidPixels(new Rect(pixels.x, pixels.y + 1, pixels.width, Mathf.Max(0, pixels.height - 2)), color, alpha);
            SolidPixels(new Rect(pixels.x + 1, pixels.yMax - 1, Mathf.Max(0, pixels.width - 2), 1), color, alpha);
        }
        private static void Text(mGraphics g, mFont font, string value, int x, int y, float alpha = 1)
            => font.drawTintedString(g, value, x, y, new Color(1, 243f / 255, 218f / 255, alpha));
        private static string RowLabel(RowSlot row, IMapObject entry, int width)
        {
            string source, ownerName = null;
            if (entry is Char person) {
                source = person.cName;
                if (owners.TryGetValue(person, out Char owner)) ownerName = owner.cName ?? "";
            }
            else if (entry is ItemMap item) source = item.template?.name;
            else source = MobName((Mob)entry);

            mFont font = mFont.tahoma_7_white;
            int zoom = mGraphics.zoomLevel;
            if (row.Label != null && row.SourceName == source && row.OwnerName == ownerName
                && row.LabelWidth == width && row.LabelZoom == zoom && row.LabelFont == font) return row.Label;

            row.SourceName = source;
            row.OwnerName = ownerName;
            row.LabelWidth = width;
            row.LabelZoom = zoom;
            row.LabelFont = font;
            row.Label = ownerName == null ? ShortName(source, width, font) : DiscipleLabel(source, ownerName, width, font);
            return row.Label;
        }

        private static string DiscipleLabel(string source, string ownerName, int width, mFont font)
        {
            string pet = (source ?? "Đệ tử").TrimStart('$');
            string separator = Separator;
            string label = pet + separator + ownerName;
            if (font.getWidth(label) <= width) return label;
            // Keep both names represented even when a narrow row needs ellipses.
            int remaining = Mathf.Max(0, width - font.getWidth(separator));
            string owner = ShortName(ownerName, remaining / 2, font);
            return ShortName(pet, remaining - font.getWidth(owner), font) + separator + owner;
        }

        private static string HeaderCaption(ListBox box, int width)
        {
            mFont font = mFont.tahoma_7b_white;
            int zoom = mGraphics.zoomLevel;
            int count = box.Count;
            if (box.Caption != null && box.CaptionKind == box.Kind && box.CaptionWidth == width
                && box.CaptionCount == count && box.CaptionZoom == zoom && box.CaptionFont == font) return box.Caption;

            box.CaptionKind = box.Kind;
            box.CaptionWidth = width;
            box.CaptionCount = count;
            box.CaptionZoom = zoom;
            box.CaptionFont = font;
            string suffix = Separator + count;
            box.Caption = ShortName(captions[(int)box.Kind], width - font.getWidth(suffix) - 2, font) + suffix;
            return box.Caption;
        }
        private static Color StatusColor(mFont font)
        {
            if (mGraphics.zoomLevel != 1) return font.color1;
            // Bitmap fonts leave color1 white. Read the same named font's existing colorJava
            // palette entry (IDs from mFont.init), not a new HUD RGB mapping.
            sbyte id = font == mFont.nameFontRed || font == mFont.tahoma_7_red ? (sbyte)15
                : font == mFont.nameFontYellow ? (sbyte)13
                : font == mFont.nameFontGreen ? (sbyte)17
                : font == mFont.tahoma_7_blue ? (sbyte)16
                : font == mFont.tahoma_7_blue1Small ? (sbyte)11 : (sbyte)3;
            return font.setColorFont(id);
        }
        internal static void Paint(mGraphics g)
        {
            if (!Visible) return;
            if (!SnapshotCurrent) Refresh();
            if (left.Count == 0 && right.Count == 0) return;
            string hover = CanInput && !HudLayout.IsMobileLayout
                ? HudLayout.HitTest(new Vector2(GameCanvas.pxMouse, GameCanvas.pyMouse)) : null;
            int tx = g.getTranslateX(), ty = g.getTranslateY() - mGraphics.addYWhenOpenKeyBoard;
            Color oldColor = GUI.color;
            g.translate(-tx, -ty); Clip(g, HudLayout.ScreenBounds);
            try {
                GUI.color = Color.white;
                foreach (ListBox box in boxes) PaintBox(g, box, hover);
            }
            finally { GUI.color = oldColor; Clip(g, HudLayout.ScreenBounds); g.translate(tx, ty); }
        }
        private static void PaintBox(mGraphics g, ListBox box, string hover)
        {
            Rect header = HudLayout.GetRect(box.Header), body = HudLayout.GetRect(box.Body);
            if (header.width <= 0) return;
            Clip(g, HudLayout.ScreenBounds);
            Rounded(header, PanelColor, 0.55f);
            Rounded(header, 0, 0.25f);
            if (pressed == box.Header || hover == box.Header) Rounded(header, Accent, 0.35f);
            int chevronSize = Unit(8);
            Rect label = new Rect(header.x + Padding - 1, header.y, Mathf.Max(0, header.width - Padding * 2 - chevronSize - Gap), header.height);
            Clip(g, label);
            string caption = HeaderCaption(box, (int)label.width);
            Text(g, mFont.tahoma_7b_white, caption, (int)header.x + Padding,
                (int)header.center.y - mFont.tahoma_7b_white.getHeight() / 2);
            // Chevron uses the same font-independent pixel strokes as the existing HUD.
            Clip(g, header);
            int cx = (int)header.xMax - Padding - chevronSize / 2, cy = (int)header.center.y;
            for (int i = -chevronSize / 2; i <= chevronSize / 2; i++)
                Fill(new Rect(cx + i, cy + (box.Open ? -Mathf.Abs(i) : Mathf.Abs(i)), 1, 1), Cream, 1);
            if (!box.Open || body.height <= 0) return;
            Clip(g, body);
            Rounded(body, PanelColor, 0.55f);
            for (int slot = 0; slot < Slots && Index(box, slot) < box.Count; slot++) {
                int index = Index(box, slot);
                RowSlot row = box.Rows[slot];
                Rect full = HudLayout.GetRect(row.Full), dot = HudLayout.GetRect(row.Dot), name = HudLayout.GetRect(row.Name);
                Clip(g, Intersection(full, body));
                IMapObject entry = box.Entries[index];
                ItemMap item = entry as ItemMap;
                Char person = entry as Char;
                Mob mob = entry as Mob;
                Char player = Char.myCharz();
                bool dead = person != null && !IsAlive(person);
                bool enabled = item != null ? GameScr.instance.CanPickHudItem(item)
                    : box.Combat ? GameScr.CanSelectTarget(entry) : !dead;
                float alpha = enabled ? 1 : 0.55f;
                bool focused = ReferenceEquals(entry, player.itemFocus) || ReferenceEquals(entry, player.charFocus)
                    || ReferenceEquals(entry, player.mobFocus);
                if (focused || hover == row.Hit || pressed == row.Hit) Fill(full, Accent, 0.35f * alpha);
                Fill(new Rect(full.x + Padding, full.yMax - Pixel, Mathf.Max(0, full.width - Padding * 2), Pixel), Divider, 0.2f);
                mFont statusFont = person != null ? person.GetNameStatusFont()
                    : item != null ? (enabled ? mFont.nameFontGreen : mFont.nameFontRed)
                    : box.Kind == ListKind.Bosses ? mFont.nameFontRed : mFont.nameFontYellow;
                if (person != null && owners.ContainsKey(person)
                    && (TileMap.mapID != 170 || (person.flagImage != 2323 && person.flagImage != 2325)))
                    statusFont = mFont.tahoma_7_blue1Small;
                Color status = dead ? Color.gray : StatusColor(statusFont);
                Dot(dot, status, alpha);
                if (item != null) {
                    Rect icon = HudLayout.GetRect(row.Icon);
                    Rounded(icon, Cream, alpha);
                    Rect inner = new Rect(icon.x + Pixel, icon.y + Pixel, Mathf.Max(0, icon.width - Pixel * 2), Mathf.Max(0, icon.height - Pixel * 2));
                    Rounded(inner, PanelColor, alpha);
                    PaintItemIcon(g, inner, item.template.iconID, Intersection(full, body), alpha);
                }
                Clip(g, Intersection(name, body));
                int nameWidth = (int)name.width - 2;
                string rowName = RowLabel(row, entry, nameWidth);
                int textHeight = mFont.tahoma_7_white.getHeight();
                int barHeight = Unit(3);
                int barGap = Unit(2);
                int textY = (int)name.center.y - (textHeight + (box.Combat ? barGap + barHeight : 0)) / 2;
                Text(g, dead ? mFont.tahoma_7_grey : mFont.tahoma_7_white, rowName, (int)name.x + 1, textY, alpha);
                if (box.Combat) {
                    long hp = mob != null ? mob.hp : person.cHP;
                    long maxHp = mob != null ? mob.maxHp : person.cHPFull;
                    var bar = new Rect(name.x + 1, textY + textHeight + barGap, Mathf.Max(0, name.width - 2), barHeight);
                    Fill(bar, Divider, 0.3f * alpha);
                    bar.width *= maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0;
                    Fill(bar, box.Kind == ListKind.Bosses ? 0xf06557 : 0x84c861, alpha);
                }
                GUI.color = Color.white;
            }
            Rect rail = HudLayout.GetRect(box.Rail);
            if (rail.height > 0) {
                Clip(g, body);
                float height = Mathf.Max(Unit(12), rail.height * rail.height / Mathf.Max(1, box.Count * box.RowHeight));
                float y = rail.y + (rail.height - height) * box.Scroll / Mathf.Max(1, box.Count * box.RowHeight - body.height);
                Rounded(new Rect(rail.x + 1, y, Mathf.Max(1, rail.width - 2), height), Cream, 0.4f);
            }
        }
        private static string MobName(Mob mob)
        {
            if (Mob.arrMobTemplate != null && mob.templateId >= 0 && mob.templateId < Mob.arrMobTemplate.Length)
                return mob.getTemplate()?.name ?? "Quái";
            return "Quái";
        }

        private static void PaintItemIcon(mGraphics g, Rect icon, int id, Rect clip, float alpha)
        {
            Texture2D texture = null; Rect source = new Rect();
            if (SmallImage.smallImg != null && SmallImage.imgbig != null && id >= 0 && id < SmallImage.smallImg.Length) {
                int[] region = SmallImage.smallImg[id];
                if (region != null && region.Length >= 5 && region[0] >= 0 && region[0] < SmallImage.imgbig.Length && SmallImage.imgbig[region[0]] != null
                    && region[1] < 256 && region[2] < 256 && region[3] < 256 && region[4] < 256) {
                    texture = SmallImage.imgbig[region[0]].texture;
                    source = new Rect(region[1] * mGraphics.zoomLevel, region[2] * mGraphics.zoomLevel, region[3] * mGraphics.zoomLevel, region[4] * mGraphics.zoomLevel);
                }
            }
            if (texture == null && SmallImage.imgNew != null && id >= 0 && id < SmallImage.imgNew.Length) {
                var small = SmallImage.imgNew[id];
                if (small == null) SmallImage.createImage(id);
                else if (small.img?.texture != null && small.img.getRealImageWidth() > 1) {
                    texture = small.img.texture; source = new Rect(0, 0, small.img.getRealImageWidth(), small.img.getRealImageHeight());
                }
            }
            if (texture == null || source.width <= 0 || source.height <= 0) { Rounded(icon, Divider, 0.55f * alpha); return; }
            Rect destination = HudButtonSkin.ScreenPixels(icon), limit = HudButtonSkin.ScreenPixels(Intersection(icon, clip));
            if (limit.width <= 0 || limit.height <= 0) return;
            float fit = Mathf.Min(1, Mathf.Min(destination.width / source.width, destination.height / source.height));
            destination = new Rect(destination.center.x - source.width * fit / 2, destination.center.y - source.height * fit / 2, source.width * fit, source.height * fit);
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            try {
                GUI.matrix = Matrix4x4.identity; GUI.color = new Color(1, 1, 1, alpha);
                GUI.BeginGroup(limit);
                try {
                    GUI.DrawTextureWithTexCoords(new Rect(destination.x - limit.x, destination.y - limit.y, destination.width, destination.height), texture,
                        new Rect(source.x / texture.width, 1 - source.yMax / texture.height, source.width / texture.width, source.height / texture.height));
                }
                finally { GUI.EndGroup(); }
            }
            finally { GUI.matrix = matrix; GUI.color = color; }
        }
    }
}
