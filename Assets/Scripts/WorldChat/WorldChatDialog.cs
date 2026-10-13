using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldChat
{
    public sealed class WorldChatDialog : Dialog, IActionListener
    {
        private readonly TField input = new TField();
        private readonly List<string> lines = new List<string>();
        private Rect window, history, send, close;
        private int revision = -1, cachedWidth, cachedHeight, resultRevision = WorldChatStore.ResultRevision;
        private int scroll, contentHeight, dragY, dragScroll;
        private bool dragging;
        private string confirmedText, submittedText, status;

        public WorldChatDialog()
        {
            input.name = "Tin thế giới"; input.loginInput = true;
            input.setFocus(true);
            input.height = System.Math.Min(22, mScreen.ITEM_HEIGHT + 2);
            input.setMaxTextLenght(WorldChatStore.Ready ? WorldChatStore.MaxCharacters * 2 : 200);
            input.cmdDoneAction = new Command("Gửi", this, 3, null);
            status = WorldChatStore.ResultText; Layout();
        }
        private void Layout()
        {
            window = WorldChatLayout.HistoryBounds();
            int width = (int)window.width;
            close = new Rect(window.xMax - 29, window.y + 3, 24, 20);
            int sendWidth = width < 160 ? 44 : 68;
            send = new Rect(window.xMax - sendWidth - 8, window.yMax - 26, sendWidth, 22);
            input.x = (int)window.x + 8; input.width = width - sendWidth - 24;
            input.y = (int)send.y;
            history = new Rect(window.x + 8, window.y + 32, width - 16, System.Math.Max(0, input.y - window.y - 48));
            if (WorldChatStore.Ready) input.setMaxTextLenght(WorldChatStore.MaxCharacters * 2);
            if (revision == WorldChatStore.Revision && cachedWidth == (int)history.width && cachedHeight == (int)history.height) return;
            bool atBottom = revision < 0 || scroll >= System.Math.Max(0, contentHeight - cachedHeight) - 4;
            revision = WorldChatStore.Revision; cachedWidth = (int)history.width; cachedHeight = (int)history.height; lines.Clear();
            foreach (WorldChatEntry entry in WorldChatStore.Entries)
            {
                lines.AddRange(mFont.tahoma_7b_dark.splitFontArray(entry.Display, System.Math.Max(20, (int)history.width - 8)));
            }
            contentHeight = lines.Count * 12;
            int maxScroll = System.Math.Max(0, contentHeight - (int)history.height);
            scroll = atBottom ? maxScroll : System.Math.Min(scroll, maxScroll);
        }
        public override void show() { GameCanvas.currentDialog = this; if (Main.isPC) input.setFocus(true); }
        private void HideKeyboard()
        {
            if (TField.kb != null) TField.kb.active = false;
            TField.kb = null; input.setFocus(false); GameCanvas.closeKeyBoard();
        }
        public void Close()
        {
            HideKeyboard(); GameCanvas.endDlg(); Consume();
        }
        private static void Consume()
        {
            GameCanvas.clearAllPointerEvent(); GameCanvas.clearKeyHold(); GameCanvas.clearKeyPressed(); GameCanvas.keyAsciiPress = 0;
        }
        private string Check(string text)
        {
            string error = WorldChatStore.Validate(text);
            if (error != null) return error;
            bool gems = Char.myCharz().luong < WorldChatStore.GemFee, gold = Char.myCharz().xu < WorldChatStore.GoldFee;
            return gems || gold ? "Không đủ " + (gems && gold ? "gem và vàng" : gems ? "gem" : "vàng") + "." : null;
        }
        private void Confirm()
        {
            confirmedText = input.getText().Trim(); status = Check(confirmedText);
            if (status != null) return;
            HideKeyboard(); Consume();
            GameCanvas.startYesNoDlg("Gửi tin thế giới với phí " + WorldChatStore.FeeLabel + "?",
                new Command("Gửi", this, 1, null), new Command("Hủy", this, 2, null));
        }
        public void perform(int action, object unused)
        {
            if (action == 3) { Confirm(); return; }
            show(); Consume();
            if (action != 1) return;
            status = Check(confirmedText);
            if (status != null) return;
            long requestId = WorldChatStore.StartRequest();
            if (requestId == 0) return;
            if (!Service.gI().submitWorldChat(requestId, confirmedText))
            {
                WorldChatStore.SendFailed(requestId); status = "Chưa gửi được tin. Kiểm tra kết nối."; return;
            }
            submittedText = confirmedText; status = "Đang gửi...";
        }
        public override void keyPress(int key)
        {
            if (key == 27 || key == -7 || key == -22) { Close(); return; }
            if (key == 10 || key == 13 || key == -5) { Confirm(); return; }
            input.keyPressed(key);
        }
        public override void update()
        {
            if (GameCanvas.currentScreen != GameScr.instance || WorldChatLayout.HistoryBounds().width == 0) { Close(); return; }
            Layout();
            if (resultRevision != WorldChatStore.ResultRevision)
            {
                resultRevision = WorldChatStore.ResultRevision; status = WorldChatStore.ResultText;
                if (WorldChatStore.ResultAccepted && input.getText().Trim() == submittedText) input.setText("");
            }
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            Vector2 pointer = new Vector2(GameCanvas.px, GameCanvas.py);
            if (GameCanvas.isPointerJustRelease)
            {
                if (!window.Contains(pointer)) { Close(); return; }
                if (close.Contains(pointer)) { Close(); return; }
                if (send.Contains(pointer)) { Consume(); Confirm(); return; }
                dragging = false;
            }
            else if (GameCanvas.isPointerDown && history.Contains(pointer))
            {
                if (!dragging) { dragging = true; dragY = GameCanvas.py; dragScroll = scroll; }
                scroll = System.Math.Max(0, System.Math.Min(System.Math.Max(0, contentHeight - (int)history.height), dragScroll + dragY - GameCanvas.py));
            }
            input.update(); Consume();
        }
        public override void paint(mGraphics g)
        {
            Layout(); g.translate(-g.getTranslateX(), -g.getTranslateY()); g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
            Notifications.NotificationSkin.PaintFrame(g, (int)window.x, (int)window.y, (int)window.width, (int)window.height, false);
            mFont.tahoma_7b_dark.drawString(g, "Chat thế giới", (int)window.x + 10, (int)window.y + 4, 0);
            g.setClip((int)window.x + 6, (int)window.y + 19, (int)window.width - 12, 13);
            mFont.tahoma_7_grey.drawString(g, WorldChatStore.FeeLabel, (int)window.x + 6, (int)window.y + 19, 0);
            g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
            PaintButton(g, close, "X");
            g.setClip((int)history.x, (int)history.y, (int)history.width, (int)history.height);
            if (lines.Count == 0) mFont.tahoma_7_grey.drawString(g, WorldChatStore.Ready ? "Chưa có tin thế giới." : "Đang nhận lịch sử...", (int)history.x + 4, (int)history.y + 4, 0);
            for (int i = 0; i < lines.Count; i++) mFont.tahoma_7b_dark.drawString(g, lines[i], (int)history.x + 4, (int)history.y + i * 12 - scroll, 0);
            g.setClip(0, 0, GameCanvas.w, GameCanvas.h); input.paint(g);
            PaintButton(g, send, WorldChatStore.PendingId != 0 ? "Đang gửi" : "Gửi");
            string label = status ?? (WorldChatStore.Ready ? WorldChatStore.CharacterCount(input.getText()) + "/" + WorldChatStore.MaxCharacters + " ký tự" : "Đang đồng bộ...");
            g.setClip((int)window.x + 8, (int)send.y - 15, (int)window.width - 16, 13);
            mFont.tahoma_7_grey.drawString(g, label, (int)window.x + 8, (int)send.y - 15, 0);
            g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
        }
        private static void PaintButton(mGraphics g, Rect bounds, string text)
        {
            Notifications.NotificationSkin.PaintFrame(g, (int)bounds.x, (int)bounds.y, (int)bounds.width, (int)bounds.height, false);
            mFont.tahoma_7b_dark.drawString(g, text, (int)bounds.center.x, (int)bounds.center.y - 6, 2);
        }
    }
}
