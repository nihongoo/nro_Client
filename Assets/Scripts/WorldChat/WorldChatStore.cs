using System;
using System.Collections.Generic;
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
        private static readonly List<WorldChatEntry> entries = new List<WorldChatEntry>();
        private static long sequence, clock, clockAnchor, nextRequest, pendingSince;
        public static IReadOnlyList<WorldChatEntry> Entries => entries;
        public static bool Ready { get; private set; }
        public static int GemFee { get; private set; }
        public static long GoldFee { get; private set; }
        public static int MaxCharacters { get; private set; }
        public static int CooldownMs { get; private set; }
        public static int HistoryLimit { get; private set; }
        public static long NextAllowedAt { get; private set; }
        public static long PendingId { get; private set; }
        public static int Revision { get; private set; }
        public static int ResultRevision { get; private set; }
        public static string ResultText { get; private set; }
        public static bool ResultAccepted { get; private set; }
        public static long Now => clock + (Stopwatch.GetTimestamp() - clockAnchor) * 1000 / Stopwatch.Frequency;
        public static string FeeLabel => Ready ? GemFee + " gem + " + GoldFee.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + " vàng" : "Đang nhận phí...";

        public static void BeginSession()
        {
            entries.Clear(); sequence = 0; Ready = false; PendingId = 0; NextAllowedAt = 0;
            GemFee = 0; GoldFee = 0; MaxCharacters = 0; CooldownMs = 0; HistoryLimit = 0;
            ResultText = null; ResultAccepted = false; SetClock(0); Revision++; ResultRevision++;
        }
        private static void SetClock(long time) { clock = time; clockAnchor = Stopwatch.GetTimestamp(); }

        public static void Snapshot(List<WorldChatEntry> snapshot, long value, long time,
            int gems, long gold, int maxCharacters, int cooldown, int retention, long nextAllowedAt)
        {
            if (Ready && value < sequence) return;
            entries.Clear(); entries.AddRange(snapshot); sequence = value;
            GemFee = gems; GoldFee = gold; MaxCharacters = maxCharacters; CooldownMs = cooldown;
            HistoryLimit = retention; NextAllowedAt = nextAllowedAt; Ready = true; SetClock(time); Revision++;
        }

        public static bool Append(WorldChatEntry entry, long value, long time)
        {
            if (value <= sequence) return true;
            if (!Ready || value != sequence + 1) return false;
            entries.Add(entry);
            while (entries.Count > HistoryLimit) entries.RemoveAt(0);
            sequence = value; SetClock(time); Revision++; return true;
        }

        public static long StartRequest()
        {
            if (!Ready || PendingId != 0 || Now < NextAllowedAt) return 0;
            PendingId = ++nextRequest; pendingSince = Stopwatch.GetTimestamp(); return PendingId;
        }
        public static void SendFailed(long id) { if (PendingId == id) PendingId = 0; }
        public static bool Result(long requestId, bool accepted, string text, long nextAllowedAt, long value, long time)
        {
            if (requestId != PendingId || PendingId == 0) return true;
            PendingId = 0; ResultText = text; ResultAccepted = accepted; ResultRevision++;
            NextAllowedAt = nextAllowedAt; SetClock(time);
            return value <= sequence;
        }
        public static bool CheckTimeout()
        {
            if (PendingId == 0 || Stopwatch.GetTimestamp() - pendingSince < Stopwatch.Frequency * 10) return false;
            PendingId = 0; ResultAccepted = false; ResultText = "Chưa nhận được kết quả. Đang đồng bộ lịch sử; kiểm tra tin trước khi gửi lại.";
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
