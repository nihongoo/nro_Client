using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hud
{
    internal sealed class BuffIconEntry
    {
        internal int Id, Icon;
        internal long ExpiresAt;
        internal string Name, Effect;
        private int countdownValue = int.MinValue;
        private string countdown, tooltipCountdown;

        internal string Countdown(long now)
        {
            int seconds = BuffIconStore.Remaining(this, now);
            int value = seconds >= 60 ? (int)((seconds + 59L) / 60) : -seconds;
            if (value != countdownValue) {
                countdownValue = value; countdown = BuffIconStore.FormatTime(seconds); tooltipCountdown = null;
            }
            return countdown;
        }
        internal string TooltipCountdown(long now)
        {
            Countdown(now);
            return tooltipCountdown ?? (tooltipCountdown = "Còn " + countdown);
        }
        internal void ReuseCountdown(BuffIconEntry previous)
        {
            countdownValue = previous.countdownValue;
            countdown = previous.countdown;
            tooltipCountdown = previous.tooltipCountdown;
        }
    }

    // Server wall-time is anchored to a monotonic clock at each complete snapshot.
    // Frame rate, device clock/time-zone changes and application suspension do not extend a timer.
    internal static class BuffIconStore
    {
        internal const int MaxEntries = 64, MaxVisible = 7;
        private static List<BuffIconEntry> entries = new List<BuffIconEntry>();
        private static long sequence = -1, serverTime, receivedAt;
        internal static long Now => (long)(Stopwatch.GetTimestamp() * (1000d / Stopwatch.Frequency));
        internal static bool Ready { get; private set; }
        internal static long Revision { get; private set; }

        internal static void Reset()
        {
            entries.Clear(); sequence = -1; serverTime = receivedAt = 0; Ready = false; Revision++;
        }
        internal static bool Apply(List<BuffIconEntry> values, long value, long time, long now)
        {
            if (value <= sequence) return false;
            if (value < 0 || time <= 0 || values.Count > MaxEntries) throw new ArgumentException("Buff snapshot");
            var ids = new HashSet<int>();
            foreach (BuffIconEntry entry in values) {
                if (entry.Id <= 0 || entry.Icon < 0 || !ids.Add(entry.Id) || string.IsNullOrEmpty(entry.Name)
                    || entry.Name.Length > 100 || entry.Effect == null || entry.Effect.Length > 1000)
                    throw new ArgumentException("Buff entry");
            }
            values.Sort((a, b) => a.Id.CompareTo(b.Id));
            int previous = 0;
            foreach (BuffIconEntry entry in values) {
                while (previous < entries.Count && entries[previous].Id < entry.Id) previous++;
                if (previous < entries.Count && entries[previous].Id == entry.Id) entry.ReuseCountdown(entries[previous]);
            }
            entries = values; sequence = value; serverTime = time; receivedAt = now; Ready = true; Revision++;
            return true;
        }
        internal static int Remaining(BuffIconEntry entry, long now)
        {
            if (entry.ExpiresAt <= serverTime) return 0;
            long duration = entry.ExpiresAt - serverTime, elapsed = System.Math.Max(0, now - receivedAt);
            return elapsed >= duration ? 0 : (int)System.Math.Min(int.MaxValue, 1 + (duration - elapsed - 1) / 1000);
        }
        internal static long CopyVisible(List<BuffIconEntry> output, long now)
        {
            output.Clear();
            long nextExpiry = long.MaxValue;
            foreach (BuffIconEntry entry in entries) {
                // IDs 30-46 are the NPC charm family, including snapshots from older servers.
                if (entry.Id >= 30 && entry.Id <= 46 || Remaining(entry, now) <= 0) continue;
                output.Add(entry);
                nextExpiry = Math.Min(nextExpiry, receivedAt + entry.ExpiresAt - serverTime);
            }
            return nextExpiry;
        }
        internal static string FormatTime(int seconds) => seconds >= 60
            ? ((seconds + 59L) / 60) + "p" : System.Math.Max(0, seconds) + "s";
    }
}
