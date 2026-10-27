using System;
using System.Collections.Generic;

namespace Hud
{
    internal static class BuffIconProtocol
    {
        internal const byte Snapshot = 8;
        private static long nextRequest;
        private static int map = -1, zone = -1;
        internal static void BeginSession()
        {
            BuffIconStore.Reset(); BuffIconsHud.Reset(); nextRequest = 0; map = zone = -1;
        }
        internal static void RequestResync()
        {
            nextRequest = 0; BuffIconsHud.Reset();
        }
        internal static void Update()
        {
            if (GameCanvas.currentScreen != GameScr.instance || Char.ExistingLocalPlayer == null
                || GameCanvas.isLoading || Char.ischangingMap) return;
            long now = BuffIconStore.Now;
            if (map != TileMap.mapID || zone != TileMap.zoneID) {
                map = TileMap.mapID; zone = TileMap.zoneID; nextRequest = 0;
                BuffIconsHud.Reset();
            }
            if (now < nextRequest) return;
            nextRequest = now + (BuffIconStore.Ready ? 10000 : 3000);
            Service.gI().requestBuffIconSnapshot();
        }
        internal static bool TryRead(Message message)
        {
            myReader reader = message.reader(); reader.mark(0);
            if (reader.available() >= 2 && reader.readByte() == Notifications.NotificationProtocol.Version
                && reader.readByte() == Snapshot) { Read(reader); return true; }
            reader.reset(); return false;
        }
        internal static void Read(myReader reader)
        {
            try {
                long sequence = reader.readLong(), time = reader.readLong();
                int count = reader.readByte() & 255;
                if (count > BuffIconStore.MaxEntries) throw new InvalidOperationException("Buff count");
                var entries = new List<BuffIconEntry>(count);
                for (int i = 0; i < count; i++) entries.Add(new BuffIconEntry {
                    Id = reader.readUnsignedShort(), Icon = reader.readUnsignedShort(), ExpiresAt = reader.readLong(),
                    Name = reader.readUTF(), Effect = reader.readUTF()
                });
                if (BuffIconStore.Apply(entries, sequence, time, BuffIconStore.Now)) {
                    nextRequest = BuffIconStore.Now + 15000; HudLayout.Invalidate();
                }
            } catch (Exception) {
                // A truncated/invalid packet never replaces the last complete snapshot.
                nextRequest = System.Math.Min(nextRequest, BuffIconStore.Now + 1000);
            }
        }
    }
}
