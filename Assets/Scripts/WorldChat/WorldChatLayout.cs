using System.Collections.Generic;
using UnityEngine;

namespace WorldChat
{
    public static class WorldChatLayout
    {
        private static readonly List<Rect> controls = new List<Rect>();
        public static Rect BarBounds => new Rect(0, GameCanvas.h - 13, GameCanvas.w, 13);

        public static bool CanTapBar(Vector2 point)
        {
            if (!BarBounds.Contains(point)) return false;
            GameScr.CollectChatObstacles(controls);
            foreach (Rect control in controls) if (control.Contains(point)) return false;
            return true;
        }

    }
}
