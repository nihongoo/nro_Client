using UnityEngine;

namespace Hud
{
    // Immediate vertical layout: each block follows the complete measured predecessor.
    // Optional blocks contribute neither height nor a second gap when they are empty.
    internal static class TopLeftHudStack
    {
        internal static float After(Rect stats, Rect buffs, float gap)
            => (buffs.height > 0 ? Mathf.Max(stats.yMax, buffs.yMax) : stats.yMax) + gap;

        internal static float ItemsTop(float gap)
            => After(HudLayout.GetRect("top-info.bounds"), BuffIconsHud.StackBounds, gap);
    }
}
