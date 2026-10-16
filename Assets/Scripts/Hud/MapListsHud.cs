using System.Collections.Generic;
using Mod.CuongLe;
using UnityEngine;

namespace Hud
{
    internal static class MapListsHud
    {
        private const int Order = HudLayout.MenuOrder + 10, Slots = 6;
        // World-logic distances, never converted through screen pixels or zoom.
        private const int HalfWidthLeft = 120, HalfWidthRight = 120, Up = 90, Down = 60;
        private const string MapTitle = "map-lists.title";
        private sealed class RowSlot
        {
            internal string Full, Hit, Name, Dot, Icon;
        }
        private sealed class ListBox
        {
            internal string Header, Body, Rail;
            internal bool Items, Open = true;
            internal int Scroll;
            internal readonly RowSlot[] Rows = new RowSlot[Slots];
            internal int Count => Items ? drops.Count : people.Count;
            internal int RowHeight => Mathf.Max(Unit(Items ? 32 : 28), mFont.tahoma_7_white.getHeight() + Unit(4));
            internal int First => Scroll / RowHeight;
        }
        private static readonly ListBox persons = new ListBox { Header = "map-lists.people.header", Body = "map-lists.people.body", Rail = "map-lists.people.rail" };
        private static readonly ListBox items = new ListBox { Header = "map-lists.items.header", Body = "map-lists.items.body", Rail = "map-lists.items.rail", Items = true };
        private static readonly List<Char> people = new List<Char>();
        private static readonly List<ItemMap> drops = new List<ItemMap>();
        private static readonly List<Rect> obstacles = new List<Rect>();
        private static int consumedTick = -1, mapId = -1, zoneId = -1, startScroll;
        private static bool captured, moved;
        private static Vector2 pressPoint;
        private static string pressed;
        private static object pressedEntry;
        private static ListBox active;
        internal static bool ConsumesInput => consumedTick == GameCanvas.gameTick;
        private static int Unit(float pixels) => Mathf.Max(1, Mathf.RoundToInt(HudLayout.PixelsToLogical(pixels)));
        private static int Gap => Unit(4);
        private static int Padding => Unit(8);
        private const int Cream = 0xfff3da, PanelColor = 0x28180c, Accent = 0xf28c1b, Divider = 0xf2b45a;
        private static readonly Dictionary<Char, string> names = new Dictionary<Char, string>();
        private static readonly Dictionary<Char, Char> owners = new Dictionary<Char, Char>();
        private static Rect paintClip;
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

        internal static void Register()
        {
            HudLayout.Register(MapTitle, TitleBounds, Order, () => Visible && !TargetMobileHud.Active, false);
            RegisterBox(persons); RegisterBox(items);
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
                    Rect full = HudLayout.GetRect(row.Full); int size = Unit(20);
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
            HudLayout.CopyObstaclesAtOrder(HudLayout.ControlsOrder, obstacles);
            foreach (Rect obstacle in obstacles)
                if (obstacle.xMax > x && obstacle.xMin < x + width && obstacle.yMax > minTop)
                    bottom = Mathf.Min(bottom, Mathf.Max(minTop, obstacle.yMin - Gap));
            return bottom;
        }
        private static Rect HeaderBounds(ListBox box)
        {
            if (box.Count == 0) return new Rect();
            Rect top = HudLayout.GetRect("top-info.bounds"), chat = HudLayout.GetRect(HudLayout.ChatButton);
            float width = Mathf.Max(Unit(box.Items ? 190 : 130),
                mFont.tahoma_7b_white.getWidth((box.Items ? "Vật phẩm" : "Người") + Separator + "999") + Gap * 2 + 7);
            if (HudLayout.IsMobileLayout) {
                // Match the visible HUD spans, excluding FPS from the left power bar.
                Rect start = HudLayout.GetRect(box.Items ? "top-info.avatar" : TopActionsHud.Hub);
                Rect end = box.Items ? HudLayout.GetRect("top-info.exp") : chat;
                width = Mathf.Max(0,end.xMax-start.xMin);
            }
            else width = Mathf.Min(width, Mathf.Max(0, GameCanvas.w / 2 - Gap * 2));
            float margin = box.Items ? top.xMin : GameCanvas.w - chat.xMax;
            float minTop = (box.Items ? top.yMax : chat.yMax) + Gap;
            Rect anchored = HudLayout.Anchor(box.Items ? HorizontalAnchor.Left : HorizontalAnchor.Right,
                VerticalAnchor.Top, new Vector2(width, HeaderHeight), new Vector2(margin, minTop));
            float bottom = BottomLimit(anchored.x, width, minTop);
            float desired = box.Items ? Mathf.Max(minTop, Unit(74)) : minTop;
            anchored.y = Mathf.Min(desired, bottom - HeaderHeight);
            return anchored.y < minTop || width <= 0 ? new Rect() : anchored;
        }
        private static Rect BodyBounds(ListBox box)
        {
            Rect header = HudLayout.GetRect(box.Header);
            if (!box.Open || header.width <= 0) return new Rect();
            float top = header.yMax + Gap;
            float available = BottomLimit(header.x, header.width, top) - top;
            int height = Mathf.FloorToInt(Mathf.Max(0, Mathf.Min(Unit(box.Items ? 160 : 140), available)) / box.RowHeight) * box.RowHeight;
            height = Mathf.Min(height, box.RowHeight * Mathf.Min(5, box.Count));
            return new Rect(header.x, top, header.width, height);
        }
        private static Rect TitleBounds()
        {
            Rect info = HudLayout.GetRect("top-info.bounds"), hub = HudLayout.GetRect(TopActionsHud.Hub);
            float left = info.xMax + Gap, right = hub.xMin - Gap;
            float center = GameCanvas.w / 2f;
            float width = Mathf.Max(0, Mathf.Min(mFont.tahoma_7_white.getWidth(Title) + 4, 2 * Mathf.Min(center - left, right - center)));
            return new Rect(center - width / 2,
                Unit(6), width, mFont.tahoma_7_white.getHeight() + 2);
        }
        private static string Title => TileMap.mapName + Separator + "Khu " + TileMap.zoneID;

        internal static void BeginSession()
        {
            persons.Open = items.Open = true; persons.Scroll = items.Scroll = 0;
            captured = false; pressed = null; active = null; mapId = zoneId = -1;
            people.Clear(); drops.Clear(); names.Clear(); owners.Clear(); consumedTick = -1; HudLayout.Invalidate();
        }
        private static void Refresh()
        {
            people.Clear(); drops.Clear(); names.Clear(); owners.Clear();
            if (mapId != TileMap.mapID || zoneId != TileMap.zoneID) {
                mapId = TileMap.mapID; zoneId = TileMap.zoneID; persons.Scroll = items.Scroll = 0;
                captured = false; pressed = null;
            }
            // Read live objects, not last update's cached projection: removals/movement must
            // be reflected before input and paint. Keep MainMod's list for its other callers.
            for (int i = 0; i < GameScr.vCharInMap.size(); i++) {
                if (!(GameScr.vCharInMap.elementAt(i) is Char person) || !InRange(person)
                    || person.charID == Char.myCharz().charID || ReferenceEquals(person, Char.myCharz())) continue;
                bool disciple = person.isPet || (person.cName != null && person.cName.StartsWith("$"));
                if (disciple) {
                    // PetService.createNewPet assigns -master.id for a player disciple.
                    // Require the positive-ID owner to be loaded; never guess from names.
                    Char owner = DiscipleOwner(person);
                    if (owner == null || owner.charID == Char.myCharz().charID) continue;
                    owners[person] = owner;
                    names[person] = (person.cName ?? "Đệ tử").TrimStart('$') + Separator + owner.cName;
                }
                else {
                    if (person.charID < 0 || person.isMiniPet || string.IsNullOrEmpty(person.cName) || person.cName == "Trọng tài"
                        || person.cName.StartsWith("#")) continue;
                    names[person] = person.cName;
                }
                people.Add(person);
            }
            for (int i = 0; i < GameScr.vItemMap.size(); i++)
                if (GameScr.vItemMap.elementAt(i) is ItemMap item && item.template != null && item.status != 2
                    && (!HudLayout.IsMobileLayout || item.template.type != 22) && InRange(item)) drops.Add(item);
            HudLayout.Invalidate();
            ClampScroll(persons); ClampScroll(items);
        }
        private static Char DiscipleOwner(Char disciple)
        {
            if (disciple.charID >= 0 || disciple.charID == -Char.myCharz().charID) return null;
            for (int i = 0; i < GameScr.vCharInMap.size(); i++)
                if (GameScr.vCharInMap.elementAt(i) is Char owner && owner.charID > 0
                    && (long)owner.charID == -(long)disciple.charID && !owner.isPet && !owner.isMiniPet)
                    return owner;
            return null;
        }
        // One fixed world rectangle for both lists, centered horizontally at the player's feet.
        // Characters use their feet; items use the center of their existing IMapObject bounds.
        internal static bool InRange(IMapObject value)
        {
            Char localPlayer = Char.ExistingLocalPlayer;
            if (localPlayer == null || Char.ischangingMap || value == null || value.isInvisible()) return false;
            if (value is Char person && person.IsHiddenFromHudList) return false;
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
            foreach (ListBox box in new[] { persons, items }) {
                if (name == box.Header || name == box.Rail || name == box.Body) return box;
                foreach (RowSlot row in box.Rows) if (name == row.Hit) return box;
            }
            return null;
        }
        private static object Entry(ListBox box, string name)
        {
            for (int i = 0; i < Slots; i++)
                if (name == box.Rows[i].Hit && Index(box, i) < box.Count)
                    return box.Items ? (object)drops[Index(box, i)] : people[Index(box, i)];
            return null;
        }
        internal static void UpdateInput()
        {
            consumedTick = -1;
            if (!Visible) { captured = false; return; }
            Refresh();
            if (!CanInput) { captured = false; pressed = null; return; }
            Vector2 pointer = new Vector2(GameCanvas.px, GameCanvas.py);
            string hit = HudLayout.HitTest(pointer);
            if (GameCanvas.isPointerJustDown) {
                active = Owner(hit); captured = active != null; pressed = hit; moved = false;
                pressPoint = pointer;
                if (captured) {
                    startScroll = active.Scroll; pressedEntry = Entry(active, hit);
                    Char.myCharz().currentMovePoint = null;
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
            int wheel = GameCanvas.pXYScrollMouse;
            if (wheel != 0) {
                Vector2 mouse = new Vector2(GameCanvas.pxMouse, GameCanvas.pyMouse);
                foreach (ListBox box in new[] { persons, items })
                    if (box.Open && HudLayout.GetRect(box.Body).Contains(mouse)) {
                        SetScroll(box, box.Scroll - wheel * box.RowHeight); moved = true; Consume(); break;
                    }
            }
        }
        private static void Activate(ListBox box, string name)
        {
            if (name == box.Header) {
                box.Open = !box.Open; HudLayout.Invalidate(); ClampScroll(box); return;
            }
            object entry = Entry(box, name);
            if (entry == null || !ReferenceEquals(entry, pressedEntry)) return;
            if (entry is Char person && GameScr.vCharInMap.contains(person)) {
                Char.myCharz().cancelAttack(); Char.myCharz().focusManualTo(person); person.stopMoving();
            }
            else if (entry is ItemMap item && GameScr.vItemMap.contains(item)) {
                if (GameScr.instance.CanPickHudItem(item)) GameScr.instance.PickHudItem(item);
                else {
                    Char.myCharz().cancelAttack();
                    Char.myCharz().focusManualTo(item); // Focus only: no MovePoint or pickup command.
                }
            }
        }
        private static void Consume()
        {
            consumedTick = GameCanvas.gameTick;
            GameCanvas.clearAllPointerEvent(); GameCanvas.clearKeyHold(); GameCanvas.clearKeyPressed();
            GameCanvas.keyAsciiPress = 0; GameCanvas.pXYScrollMouse = 0;
            // Preserve a MovePoint just scheduled by PickHudItem on release.
        }

        private static Rect Intersection(Rect a, Rect b)
        {
            float x = Mathf.Max(a.xMin, b.xMin), y = Mathf.Max(a.yMin, b.yMin);
            return Rect.MinMaxRect(x, y, Mathf.Max(x, Mathf.Min(a.xMax, b.xMax)), Mathf.Max(y, Mathf.Min(a.yMax, b.yMax)));
        }
        private static void Clip(mGraphics g, Rect rect)
        {
            paintClip = rect;
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
        private static void Fill(mGraphics g, Rect rect, int rgb, float alpha)
        {
            SolidPixels(HudButtonSkin.ScreenPixels(rect), rgb, alpha);
        }
        private static void SolidPixels(Rect pixels, int rgb, float alpha)
        {
            pixels = Intersection(pixels, HudButtonSkin.ScreenPixels(paintClip));
            if (pixels.width <= 0 || pixels.height <= 0) return;
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            try {
                GUI.matrix = Matrix4x4.identity;
                GUI.color = new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alpha);
                GUI.DrawTexture(pixels, Texture2D.whiteTexture);
            }
            finally { GUI.matrix = matrix; GUI.color = color; }
        }
        private static void Dot(mGraphics g, Rect rect, Color color, float alpha)
        {
            int rgb = ((int)(color.r * 255) << 16) | ((int)(color.g * 255) << 8) | (int)(color.b * 255);
            Rect pixels = HudButtonSkin.ScreenPixels(rect);
            float radius = pixels.height / 2f;
            for (int y = 0; y < pixels.height; y++) {
                float dy = y + 0.5f - radius;
                int inset = Mathf.FloorToInt(radius - Mathf.Sqrt(Mathf.Max(0, radius * radius - dy * dy)));
                SolidPixels(new Rect(pixels.x + inset, pixels.y + y, pixels.width - 2 * inset, 1), rgb, alpha);
            }
        }
        // Solid geometry in actual screen pixels, including the one-pixel corner cuts.
        private static void Rounded(mGraphics g, Rect rect, int color, float alpha)
        {
            if (rect.width <= 0 || rect.height <= 0) return;
            Rect pixels = HudButtonSkin.ScreenPixels(rect);
            SolidPixels(new Rect(pixels.x + 1, pixels.y, Mathf.Max(0, pixels.width - 2), 1), color, alpha);
            SolidPixels(new Rect(pixels.x, pixels.y + 1, pixels.width, Mathf.Max(0, pixels.height - 2)), color, alpha);
            SolidPixels(new Rect(pixels.x + 1, pixels.yMax - 1, Mathf.Max(0, pixels.width - 2), 1), color, alpha);
        }
        private static void Text(mGraphics g, mFont font, string value, int x, int y, float alpha = 1)
            => font.drawTintedString(g, value, x, y, new Color(1, 243f / 255, 218f / 255, alpha));
        private static string PersonName(Char person, int width)
        {
            if (!owners.TryGetValue(person, out Char owner) || mFont.tahoma_7_white.getWidth(names[person]) <= width)
                return ShortName(names[person], width);
            // Keep both names represented even when a narrow row needs ellipses.
            int remaining = Mathf.Max(0, width - mFont.tahoma_7_white.getWidth(Separator));
            string ownerName = ShortName(owner.cName, remaining / 2);
            string petName = ShortName((person.cName ?? "Đệ tử").TrimStart('$'), remaining - mFont.tahoma_7_white.getWidth(ownerName));
            return petName + Separator + ownerName;
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
            Refresh(); // Camera/game update occurs after UpdateInput; do not paint stale rows.
            int tx = g.getTranslateX(), ty = g.getTranslateY() - mGraphics.addYWhenOpenKeyBoard;
            Color oldColor = GUI.color;
            g.translate(-tx, -ty); Clip(g, HudLayout.ScreenBounds);
            try {
                Rect title = HudLayout.GetRect(MapTitle);
                if (!TargetMobileHud.Active) {
                    Clip(g, title); GUI.color = new Color(1, 1, 1, 0.65f);
                    string zone = Separator + "Khu " + TileMap.zoneID;
                    string titleText = ShortName(TileMap.mapName, (int)title.width - mFont.tahoma_7_white.getWidth(zone) - 4) + zone;
                    if (mFont.tahoma_7_white.getWidth(titleText) > title.width) titleText = ShortName(Title, (int)title.width - 2);
                    if (HudLayout.IsMobileLayout) {
                        GUI.color = Color.white;
                        int x = Mathf.RoundToInt(title.center.x-mFont.tahoma_7_white.getWidth(titleText)/2f);
                        int y = (int)title.y+1;
                        mFont.tahoma_7_white.drawTintedString(g,titleText,x+1,y+1,new Color32(43,26,10,255));
                        mFont.tahoma_7_white.drawTintedString(g,titleText,x,y,new Color32(255,243,218,185));
                    }
                    else (mGraphics.zoomLevel == 1 ? mFont.tahoma_7_grey : mFont.tahoma_7_white).drawString(g,
                        titleText, (int)title.center.x, (int)title.y + 1, mFont.CENTER);
                }
                GUI.color = Color.white;
                PaintBox(g, persons); PaintBox(g, items);
            }
            finally { GUI.color = oldColor; Clip(g, HudLayout.ScreenBounds); g.translate(tx, ty); }
        }
        private static void PaintBox(mGraphics g, ListBox box)
        {
            Rect header = HudLayout.GetRect(box.Header), body = HudLayout.GetRect(box.Body);
            if (header.width <= 0) return;
            Clip(g, HudLayout.ScreenBounds);
            string hover = CanInput && !HudLayout.IsMobileLayout ? HudLayout.HitTest(new Vector2(GameCanvas.pxMouse, GameCanvas.pyMouse)) : null;
            Rounded(g, header, PanelColor, 0.55f);
            Rounded(g, header, 0, 0.25f);
            if (pressed == box.Header || hover == box.Header) Rounded(g, header, Accent, 0.35f);
            int chevronSize = Unit(8);
            Rect label = new Rect(header.x + Padding - 1, header.y, Mathf.Max(0, header.width - Padding * 2 - chevronSize - Gap), header.height);
            Clip(g, label);
            string count = Separator + box.Count;
            string caption = ShortName(box.Items ? "Vật phẩm" : "Người",
                (int)label.width - mFont.tahoma_7b_white.getWidth(count) - 2, mFont.tahoma_7b_white) + count;
            Text(g, mFont.tahoma_7b_white, caption, (int)header.x + Padding,
                (int)header.center.y - mFont.tahoma_7b_white.getHeight() / 2);
            // Chevron uses the same font-independent pixel strokes as the existing HUD.
            Clip(g, header);
            int cx = (int)header.xMax - Padding - chevronSize / 2, cy = (int)header.center.y;
            for (int i = -chevronSize / 2; i <= chevronSize / 2; i++)
                Fill(g, new Rect(cx + i, cy + (box.Open ? -Mathf.Abs(i) : Mathf.Abs(i)), 1, 1), Cream, 1);
            if (!box.Open || body.height <= 0) return;
            Clip(g, body);
            Rounded(g, body, PanelColor, 0.55f);
            for (int slot = 0; slot < Slots && Index(box, slot) < box.Count; slot++) {
                int index = Index(box, slot);
                RowSlot row = box.Rows[slot];
                Rect full = HudLayout.GetRect(row.Full), dot = HudLayout.GetRect(row.Dot), name = HudLayout.GetRect(row.Name);
                Clip(g, Intersection(full, body));
                bool enabled = !box.Items || GameScr.instance.CanPickHudItem(drops[index]);
                float alpha = enabled ? 1 : 0.55f;
                bool focused = !box.Items && ReferenceEquals(people[index], Char.myCharz().charFocus);
                if (focused || hover == row.Hit || pressed == row.Hit) Fill(g, full, Accent, 0.35f * alpha);
                Fill(g, new Rect(full.x + Padding, full.yMax - Pixel, Mathf.Max(0, full.width - Padding * 2), Pixel), Divider, 0.2f);
                mFont statusFont = box.Items ? (enabled ? mFont.nameFontGreen : mFont.nameFontRed) : people[index].GetNameStatusFont();
                if (!box.Items && owners.ContainsKey(people[index])
                    && (TileMap.mapID != 170 || (people[index].flagImage != 2323 && people[index].flagImage != 2325)))
                    statusFont = mFont.tahoma_7_blue1Small;
                Color status = StatusColor(statusFont);
                Dot(g, dot, status, alpha);
                if (box.Items) {
                    Rect icon = HudLayout.GetRect(row.Icon);
                    Rounded(g, icon, Cream, alpha);
                    Rect inner = new Rect(icon.x + Pixel, icon.y + Pixel, Mathf.Max(0, icon.width - Pixel * 2), Mathf.Max(0, icon.height - Pixel * 2));
                    Rounded(g, inner, PanelColor, alpha);
                    PaintItemIcon(g, inner, drops[index].template.iconID, Intersection(full, body), alpha);
                }
                Clip(g, Intersection(name, body));
                Text(g, mFont.tahoma_7_white, box.Items ? ShortName(drops[index].template.name, (int)name.width - 2) : PersonName(people[index], (int)name.width - 2),
                    (int)name.x + 1, (int)name.center.y - mFont.tahoma_7_white.getHeight() / 2, alpha);
                GUI.color = Color.white;
            }
            Rect rail = HudLayout.GetRect(box.Rail);
            if (rail.height > 0) {
                Clip(g, body);
                float height = Mathf.Max(Unit(12), rail.height * rail.height / Mathf.Max(1, box.Count * box.RowHeight));
                float y = rail.y + (rail.height - height) * box.Scroll / Mathf.Max(1, box.Count * box.RowHeight - body.height);
                Rounded(g, new Rect(rail.x + 1, y, Mathf.Max(1, rail.width - 2), height), Cream, 0.4f);
            }
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
            if (texture == null || source.width <= 0 || source.height <= 0) { Rounded(g, icon, Divider, 0.55f * alpha); return; }
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
