using UnityEngine;

namespace Hud
{
    internal struct BuffIconGrid
    {
        internal float X, Y, Size, Gap, RowHeight;
        internal int Columns, Count;
        internal Rect Bounds => Columns == 0 || Count == 0 ? new Rect()
            : new Rect(X, Y, Columns * (Size + Gap) - Gap, ((Count + Columns - 1) / Columns) * RowHeight - Gap);
        internal Rect Cell(int index) => index < 0 || index >= Count || Columns == 0 ? new Rect()
            : new Rect(X + index % Columns * (Size + Gap), Y + index / Columns * RowHeight, Size, Size);
        internal static BuffIconGrid Resolve(Rect info, Rect bar, Rect target, Rect actions,
            float screenWidth, float screenHeight, int zoom, int fontHeight, int count)
        {
            float unit = 1f / Mathf.Max(1, zoom), gap = Mathf.Max(1, 4 * unit), size = Mathf.Max(8, 30 * unit);
            var grid = new BuffIconGrid { X = Mathf.Ceil(bar.x), Y = Mathf.Ceil(info.yMax + gap),
                Size = Mathf.Round(size), Gap = gap };
            grid.RowHeight = grid.Size + 1 + fontHeight + gap;
            float right = Mathf.Min(screenWidth - gap, actions.xMin - gap);
            // The central target/map information can be taller than the player's three bars.
            if (target.width > 0 && grid.Y < target.yMax && target.xMax > grid.X)
                right = Mathf.Min(right, target.xMin - gap);
            if (bar.width <= 0 || grid.Y + grid.RowHeight > screenHeight) return grid;
            grid.Columns = Mathf.Clamp(Mathf.FloorToInt((right - grid.X + gap) / (grid.Size + gap)), 0, 8);
            int rows = Mathf.Min(2, Mathf.FloorToInt((screenHeight - grid.Y + gap) / grid.RowHeight));
            grid.Count = Mathf.Min(count, grid.Columns * rows);
            return grid;
        }
    }
}
