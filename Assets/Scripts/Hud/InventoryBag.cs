using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Hud
{
    internal enum InventoryBagFilter { All, Equipment, Consumable, Material }

    // Indices always refer to the server's original arrItemBag, including when filtered.
    internal static class InventoryBag
    {
        internal const int Columns = 5, MinimumRows = 7, DoubleTapMillis = 350;
        // The inventory packet has no instance ID. Only accept a refresh in the same slot
        // with the same template/metadata/options; quantity may change independently.
        internal static bool SameDetailItem(Item before, Item after)
        {
            if (before?.template == null || after?.template == null
                || before.template.id != after.template.id || before.info != after.info || before.content != after.content) return false;
            int count = before.itemOption?.Length ?? 0;
            if (count != (after.itemOption?.Length ?? 0)) return false;
            for (int i = 0; i < count; i++) {
                ItemOption a = before.itemOption[i], b = after.itemOption[i];
                if (a == null || b == null) { if (!ReferenceEquals(a, b)) return false; continue; }
                if (a.optionTemplate?.id != b.optionTemplate?.id || a.param != b.param) return false;
            }
            return true;
        }
        internal static int EquippedPorataIndex(Item[] body) => body != null && body.Length > 7
            && body[7]?.template != null && body[7].template.id == 454 ? 7 : -1;
        internal static bool CanEquip(Item item) => item?.template != null && (item.isTypeBody()
            || item.template.type == 24 || item.template.type == 27
                && (item.template.id == 454 || item.template.id == 921 || item.template.id == 1819));
        internal static bool HasLevel(Item item) => item?.template != null
            && (item.template.type >= 0 && item.template.type <= 4 || item.template.type == 32);
        internal static InventoryBagFilter Category(Item item)
        {
            if (item?.template != null && (item.template.id == 933 || item.template.id == 934 || item.template.id == 1820))
                return InventoryBagFilter.Material;
            if (CanEquip(item)) return InventoryBagFilter.Equipment;
            if (item?.template == null) return InventoryBagFilter.Material;
            switch (item.template.type) {
                case 6: case 7: case 13: case 16: case 22: case 25:
                case 27: case 28: case 29: case 31: case 37: case 75:
                    return InventoryBagFilter.Consumable;
                default: return InventoryBagFilter.Material;
            }
        }
        internal static void Map(Item[] bag, InventoryBagFilter filter, List<int> result)
        {
            result.Clear();
            if (bag == null) return;
            for (int i = 0; i < bag.Length; i++)
                if (filter == InventoryBagFilter.All || bag[i] != null && Category(bag[i]) == filter) result.Add(i);
        }
        internal static int Rows(int count) => System.Math.Max(MinimumRows, (count + Columns - 1) / Columns);
        internal static Rect BadgeBounds(Rect cell, float width, float height)
            => new Rect(cell.xMax + 2 - width, cell.yMax + 2 - height, width, height);
        internal static string Quantity(int quantity)
        {
            return quantity > 999 ? "999+" : quantity.ToString(CultureInfo.InvariantCulture);
        }
    }

    internal sealed class InventoryBagDoubleTap
    {
        private int index = -1;
        private Item item;
        private long at;
        internal void Reset() { index = -1; item = null; }
        internal bool Tap(int bagIndex, Item current, long now)
        {
            bool equip = InventoryBag.CanEquip(current) && index == bagIndex
                && ReferenceEquals(item, current) && now >= at && now - at <= InventoryBag.DoubleTapMillis;
            if (equip) { Reset(); return true; }
            index = bagIndex; item = current; at = now;
            return false;
        }
    }
}
