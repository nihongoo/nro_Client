using System;
using System.Collections.Generic;

namespace Hud
{
    // Local shortcut assignments only. Inventory and item-use protocol remain the source of truth.
    internal sealed class MobileQuickItems : IActionListener
    {
        private static readonly MobileQuickItems listener = new MobileQuickItems();
        private static readonly int[] templates = { -1, -1, -1 };
        internal const int CooldownMillis = 10000; // Match the existing client potion button.
        private static readonly Dictionary<int,long> readyAt = new Dictionary<int,long>();
        private static readonly List<int> expired = new List<int>();
        private static string owner;
        private static void Load()
        {
            string host = BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(GameMidlet.IP)).Replace("-","");
            string key = "mobile-quick-" + host + "-" + GameMidlet.PORT + "-" + Char.myCharz().charID;
            if (owner == key) return;
            owner = key;
            readyAt.Clear();
            string saved = Rms.loadRMSString(key);
            string[] values = saved == null ? new string[0] : saved.Split(',');
            for (int i=0;i<3;i++) templates[i] = i < values.Length && int.TryParse(values[i],out int id) ? id : -1;
        }
        internal static int Template(int slot) { Load(); return templates[slot]; }
        private static int BagIndex(int template)
        {
            Item[] bag = Char.myCharz().arrItemBag;
            if (template < 0 || bag == null) return -1;
            for (int i=0;i<bag.Length;i++)
                if (bag[i] != null && bag[i].template.id == template && bag[i].quantity > 0) return i;
            return -1;
        }
        internal static Item Get(int slot)
        {
            int index = BagIndex(Template(slot));
            return index < 0 ? null : Char.myCharz().arrItemBag[index];
        }
        private static int CooldownGroup(Item item) => item.template.type == 6 ? -1 : item.template.id;
        internal static long Remaining(Item item)
        {
            Load();
            return RemainingAt(item,mSystem.currentTimeMillis());
        }
        private static long RemainingAt(Item item, long now)
        {
            if (item == null) return 0;
            long remaining = readyAt.TryGetValue(CooldownGroup(item),out long deadline) ? System.Math.Max(0,deadline-now) : 0;
            // All pea levels share the legacy HP timer, including uses by AutoPean/key.
            if (item.template.type == 6 && GameScr.instance != null)
                remaining = System.Math.Max(remaining,GameScr.instance.MobilePotionRemaining(now));
            return remaining;
        }
        internal static void Use(int slot, int pressedTemplate)
        {
            // Resolve again on release: a moved/depleted/reassigned slot cannot use another item.
            if (pressedTemplate < 0 || Template(slot) != pressedTemplate) return;
            int index = BagIndex(pressedTemplate);
            if (index < 0) return;
            Item item = Char.myCharz().arrItemBag[index];
            long now = mSystem.currentTimeMillis();
            if (RemainingAt(item,now) > 0) return;
            Service.gI().useItem(0,1,(sbyte)index,-1); // Same bag-use call as Panel action 2001.
            expired.Clear();
            foreach (KeyValuePair<int,long> entry in readyAt) if (entry.Value <= now) expired.Add(entry.Key);
            foreach (int key in expired) readyAt.Remove(key);
            readyAt[CooldownGroup(item)] = now+CooldownMillis;
            if (item.template.type == 6 && GameScr.instance != null) GameScr.instance.MarkMobilePotionUse(now);
            if (pressedTemplate == 193 || pressedTemplate == 194) GameCanvas.panel.hide();
        }
        internal static void AddAssignmentCommands(MyVector menu, Item item)
        {
            if (!HudLayout.IsMobileLayout || item == null || item.isTypeBody()) return;
            for (int i=0;i<3;i++) menu.addElement(new Command("Gắn ô nhanh " + (i+1),listener,i+1,item));
            menu.addElement(new Command("Bỏ gán ô nhanh",listener,4,item));
        }
        public void perform(int action, object parameter)
        {
            if (!HudLayout.IsMobileLayout || !(parameter is Item item)) return;
            Load();
            if (action >= 1 && action <= 3) templates[action-1] = item.template.id;
            else if (action == 4) for(int i=0;i<3;i++) if(templates[i] == item.template.id) templates[i] = -1;
            HudLayout.Invalidate();
            Rms.saveRMSString(owner,string.Join(",",Array.ConvertAll(templates,id => id.ToString())));
        }
    }
}
