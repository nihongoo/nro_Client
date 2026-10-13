using System;

namespace WorldChat
{
    // One routing point for OK, Enter and native-keyboard callbacks. Normal chat keeps its listener.
    public sealed class WorldChatComposer
    {
        public bool WorldMode { get; private set; }
        public bool Confirming { get; private set; }
        public string Draft { get; private set; }
        private int confirmedGems;
        private long confirmedGold;
        public void Select(bool world) { WorldMode = world; Confirming = false; }
        public string Submit(string text, Action<string> normal, Action<string> confirm)
        {
            if (!WorldMode) { normal(text); return null; }
            if (Confirming) return null;
            string error = WorldChatStore.Validate(text);
            if (error != null) return error;
            Draft = text; confirmedGems = WorldChatStore.GemFee; confirmedGold = WorldChatStore.GoldFee;
            Confirming = true;
            confirm("Gửi tin thế giới với phí " + WorldChatStore.FeeLabel + "?");
            return null;
        }
        public void Decline() { Confirming = false; }
        public long Agree(out string error)
        {
            error = null;
            if (!Confirming) return 0;
            Confirming = false;
            error = WorldChatStore.Validate(Draft);
            if (error == null && (confirmedGems != WorldChatStore.GemFee || confirmedGold != WorldChatStore.GoldFee))
                error = "Phí đã thay đổi. Vui lòng bấm OK để xác nhận lại.";
            return error == null ? WorldChatStore.StartRequest() : 0;
        }
    }
}
