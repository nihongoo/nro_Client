using UnityEngine;

namespace Hud
{
    internal struct BuffIconGrid
    {
        internal float X, Y, Size, Gap, RowHeight, CaptionHeight, CaptionGap;
        internal int CaptionFontPixels;
        internal int Columns, Count;
        internal Rect Bounds => Columns == 0 || Count == 0 ? new Rect()
            : new Rect(X, Y, Columns * (Size + Gap) - Gap, RowHeight - Gap);
        internal Rect Cell(int index) => index < 0 || index >= Count || Columns == 0 ? new Rect()
            : new Rect(X + index * (Size + Gap), Y, Size, Size);
        internal static BuffIconGrid Resolve(Rect info, Rect bar, Rect target, Rect actions,
            float screenWidth, float screenHeight, int zoom, int fontHeight, int count, bool mobile = false)
        {
            float unit = 1f / Mathf.Max(1, zoom), gap = Mathf.Max(1, 4 * unit), size = Mathf.Max(8, (mobile ? 72 : 30) * unit);
            var grid = new BuffIconGrid { X = Mathf.Ceil(bar.x), Y = Mathf.Ceil(TopLeftHudStack.After(info, new Rect(), gap)),
                Size = Mathf.Round(size), Gap = gap };
            float right = Mathf.Min(screenWidth - gap, actions.xMin - gap);
            // The central target/map information can be taller than the player's three bars.
            if (target.width > 0 && grid.Y < target.yMax && target.xMax > grid.X)
                right = Mathf.Min(right, target.xMin - gap);
            if (mobile) {
                // Grow inside the same anchor/available lane, retaining seven cells where they fit.
                float sevenCellSize = Mathf.FloorToInt((right - grid.X - gap * (BuffIconStore.MaxVisible - 1)) / BuffIconStore.MaxVisible);
                if (sevenCellSize >= 8) grid.Size = Mathf.Min(grid.Size, sevenCellSize);
            }
            grid.CaptionGap = mobile ? unit : 1;
            grid.CaptionFontPixels = mobile ? Mathf.RoundToInt(Mathf.Clamp(grid.Size / unit * .42f, 20, 32)) : 0;
            grid.CaptionHeight = mobile ? Mathf.Ceil(grid.CaptionFontPixels * 1.25f) * unit : fontHeight;
            grid.RowHeight = grid.Size + grid.CaptionGap + grid.CaptionHeight + unit + gap;
            if (bar.width <= 0 || grid.Y + grid.RowHeight > screenHeight) return grid;
            grid.Columns = Mathf.Clamp(Mathf.FloorToInt((right - grid.X + gap) / (grid.Size + gap)), 0, BuffIconStore.MaxVisible);
            grid.Count = Mathf.Min(count, grid.Columns);
            return grid;
        }
    }
}
