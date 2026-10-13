using System.Collections.Generic;
using UnityEngine;

namespace WorldChat
{
    public static class WorldChatLayout
    {
        private static readonly List<Rect> controls = new List<Rect>();
        private static readonly List<float> edges = new List<float>();
        private static Rect cachedWindow;
        private static int cachedKey;
        private static bool hasWindow;

        public static Rect BarBounds => new Rect(0, GameCanvas.h - 13, GameCanvas.w, 13);

        public static bool CanTapBar(Vector2 point)
        {
            if (!BarBounds.Contains(point)) return false;
            GameScr.CollectChatObstacles(controls);
            foreach (Rect control in controls) if (control.Contains(point)) return false;
            return true;
        }

        public static Rect HistoryBounds()
        {
            GameScr.CollectChatObstacles(controls);
            // Reserve the character/status toolbar too. Do not move any gameplay control.
            controls.Add(new Rect(0, 0, GameScr.imgPanel.getWidth()
                + Mod.CuongLe.DoHoa.imgSetting.getWidth() + Mod.CuongLe.DoHoa.imgBoxOpenItem.getWidth() + 20,
                Mathf.Max(45, GameScr.imgPanel.getHeight() + 4)));
            Rect viewport = new Rect(6, 6, GameCanvas.w - 12, GameCanvas.h - 25);
            int key = viewport.GetHashCode();
            foreach (Rect control in controls) key = unchecked(key * 397 ^ control.GetHashCode());
            if (hasWindow && key == cachedKey) return cachedWindow;
            cachedKey = key; hasWindow = true;
            cachedWindow = FindFreeWindow(viewport, controls);
            return cachedWindow;
        }

        // Search horizontal obstacle edges, then free vertical intervals. Cached until controls change.
        public static Rect FindFreeWindow(Rect viewport, IList<Rect> obstacles)
        {
            edges.Clear(); edges.Add(viewport.xMin); edges.Add(viewport.xMax);
            foreach (Rect box in obstacles)
                if (box.width > 0 && box.height > 0 && box.Overlaps(viewport))
                {
                    edges.Add(Mathf.Clamp(box.xMin - 4, viewport.xMin, viewport.xMax));
                    edges.Add(Mathf.Clamp(box.xMax + 4, viewport.xMin, viewport.xMax));
                }
            edges.Sort();
            Rect best = new Rect(); float bestScore = -1;
            var blocked = new List<Rect>();
            for (int left = 0; left < edges.Count; left++)
                for (int right = left + 1; right < edges.Count; right++)
                {
                    float width = Mathf.Min(380, edges[right] - edges[left]);
                    if (width < 128) continue;
                    float x = (edges[left] + edges[right] - width) / 2;
                    blocked.Clear();
                    foreach (Rect box in obstacles)
                        if (box.width > 0 && box.height > 0 && box.xMin - 4 < x + width && box.xMax + 4 > x)
                            blocked.Add(Rect.MinMaxRect(x, box.yMin - 2, x + width, box.yMax + 2));
                    blocked.Sort((a, b) => a.yMin.CompareTo(b.yMin));
                    float y = viewport.yMin;
                    for (int i = 0; i <= blocked.Count; i++)
                    {
                        float end = i == blocked.Count ? viewport.yMax : Mathf.Min(viewport.yMax, blocked[i].yMin);
                        float height = Mathf.Min(320, end - y);
                        if (height >= 86)
                        {
                            Rect candidate = new Rect(x, (y + end - height) / 2, width, height);
                            float score = width * height - Vector2.Distance(candidate.center, viewport.center);
                            if (score > bestScore) { bestScore = score; best = candidate; }
                        }
                        if (i < blocked.Count) y = Mathf.Max(y, blocked[i].yMax);
                        if (y >= viewport.yMax) break;
                    }
                }
            return best; // On an unsupported tiny viewport, refuse to cover controls.
        }
    }
}
