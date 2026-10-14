using System;
using System.Diagnostics;

namespace WorldChat
{
    public sealed class WorldChatEntry
    {
        public long Id, SentAt;
        public int SenderId;
        public string Sender, Text;
        public string Display => Sender + ": " + Text;
    }

    // Main-thread store. Policy values arrive from the server, never from translated fee labels.
    public static class WorldChatStore
    {
        public static readonly WorldChatTicker Ticker = new WorldChatTicker();
        private static long sequence, clock, clockAnchor, nextRequest, pendingSince;
        public static bool Ready { get; private set; }
        public static int GemFee { get; private set; }
        public static long GoldFee { get; private set; }
        public static int MaxCharacters { get; private set; }
        public static int CooldownMs { get; private set; }
        public static long NextAllowedAt { get; private set; }
        public static long PendingId { get; private set; }
        public static int ResultRevision { get; private set; }
        public static string ResultText { get; private set; }
        public static bool ResultAccepted { get; private set; }
        public static long Now => clock + (Stopwatch.GetTimestamp() - clockAnchor) * 1000 / Stopwatch.Frequency;
        public static string FeeLabel => Ready ? GemFee + " gem + " + GoldFee.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + " vàng" : "Đang nhận phí...";

        public static void BeginSession()
        {
            Ticker.Reset(); sequence = 0; Ready = false; PendingId = 0; NextAllowedAt = 0;
            GemFee = 0; GoldFee = 0; MaxCharacters = 0; CooldownMs = 0;
            ResultText = null; ResultAccepted = false; SetClock(0); ResultRevision++;
        }
        private static void SetClock(long time) { clock = time; clockAnchor = Stopwatch.GetTimestamp(); }

        public static void Snapshot(long value, long time, int gems, long gold,
            int maxCharacters, int cooldown, long nextAllowedAt)
        {
            if (Ready && value < sequence) return;
            sequence = value; GemFee = gems; GoldFee = gold; MaxCharacters = maxCharacters;
            CooldownMs = cooldown; NextAllowedAt = nextAllowedAt; Ready = true; SetClock(time);
        }
        public static bool Receive(WorldChatEntry entry, long value, long time)
        {
            if (value <= sequence) return false;
            // No replay/history: accept live gaps after a disconnect, never requeue old messages.
            sequence = value; SetClock(time); Ticker.Enqueue(entry.Display); return true;
        }

        public static long StartRequest()
        {
            if (!Ready || PendingId != 0 || Now < NextAllowedAt) return 0;
            PendingId = ++nextRequest; pendingSince = Stopwatch.GetTimestamp(); return PendingId;
        }
        public static void SendFailed(long id) { if (PendingId == id) PendingId = 0; }
        public static void Result(long requestId, bool accepted, string text, long nextAllowedAt, long time)
        {
            if (requestId != PendingId || PendingId == 0) return;
            PendingId = 0; ResultText = text; ResultAccepted = accepted; ResultRevision++;
            NextAllowedAt = nextAllowedAt; SetClock(time);
        }
        public static bool CheckTimeout()
        {
            if (PendingId == 0 || Stopwatch.GetTimestamp() - pendingSince < Stopwatch.Frequency * 10) return false;
            PendingId = 0; ResultAccepted = false; ResultText = "Chưa nhận được kết quả. Tin có thể đã phát; không tự gửi lại. Đang đồng bộ phí và cooldown.";
            ResultRevision++; return true;
        }
        public static int CharacterCount(string text)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++, count++)
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) i++;
            return count;
        }
        public static string Validate(string text)
        {
            if (!Ready) return "Đang nhận cấu hình chat từ máy chủ.";
            if (string.IsNullOrWhiteSpace(text)) return "Không thể gửi tin trống.";
            if (CharacterCount(text.Trim()) > MaxCharacters) return "Tin nhắn tối đa " + MaxCharacters + " ký tự.";
            foreach (char c in text.Trim()) if (char.IsControl(c)) return "Tin nhắn không được chứa xuống dòng hoặc ký tự điều khiển.";
            if (PendingId != 0) return "Đang gửi tin trước...";
            if (Now < NextAllowedAt) return "Vui lòng đợi " + ((NextAllowedAt - Now + 999) / 1000) + " giây giữa hai tin.";
            return null;
        }
    }
}
