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
    }

    // Server wall-time is anchored to a monotonic clock at each complete snapshot.
    // Frame rate, device clock/time-zone changes and application suspension do not extend a timer.
    internal static class BuffIconStore
    {
        internal const int MaxEntries = 64, MaxVisible = 16;
        private static List<BuffIconEntry> entries = new List<BuffIconEntry>();
        private static long sequence = -1, serverTime, receivedAt;
        internal static long Now => (long)(Stopwatch.GetTimestamp() * (1000d / Stopwatch.Frequency));
        internal static bool Ready { get; private set; }

        internal static void Reset()
        {
            entries.Clear(); sequence = -1; serverTime = receivedAt = 0; Ready = false;
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
            entries = values; sequence = value; serverTime = time; receivedAt = now; Ready = true;
            return true;
        }
        internal static int Remaining(BuffIconEntry entry, long now)
        {
            if (entry.ExpiresAt <= serverTime) return 0;
            long duration = entry.ExpiresAt - serverTime, elapsed = System.Math.Max(0, now - receivedAt);
            return elapsed >= duration ? 0 : (int)System.Math.Min(int.MaxValue, 1 + (duration - elapsed - 1) / 1000);
        }
        internal static void CopyVisible(List<BuffIconEntry> output, long now)
        {
            output.Clear();
            foreach (BuffIconEntry entry in entries) {
                if (Remaining(entry, now) <= 0) continue;
                output.Add(entry);
                if (output.Count == MaxVisible) break;
            }
        }
        internal static string FormatTime(int seconds) => seconds >= 60
            ? ((seconds + 59L) / 60) + "p" : System.Math.Max(0, seconds) + "s";
    }
}
