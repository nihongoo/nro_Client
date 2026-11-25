using System.Collections.Generic;

namespace Mod.CuongLe
{
    public class PointTrain
    {
        public int x;

        public int y;

        public int gCost;

        public int hCost;

        public PointTrain parent;

        private static readonly int[][] directions4 = {
            new[] { 0, -1 }, new[] { 0, 1 }, new[] { -1, 0 }, new[] { 1, 0 }
        };

        public int fCost => gCost + hCost;

        public PointTrain()
        {
        }

        public PointTrain(int x, int y, int g = 0, int h = 0, PointTrain parent = null)
        {
            this.x = x;
            this.y = y;
            gCost = g;
            hCost = h;
            this.parent = parent;
        }

        public override bool Equals(object obj)
        {
            if (obj is PointTrain point && x == point.x)
            {
                return y == point.y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (x << 16) | y;
        }

        public static List<PointTrain> FindPath_AStar(int sx, int sy, int ex, int ey)
        {
            List<PointTrain> openList = new List<PointTrain>();
            Dictionary<string, PointTrain> openDict = new Dictionary<string, PointTrain>();
            HashSet<string> closedSet = new HashSet<string>();

            PointTrain start = new PointTrain(sx, sy, 0, Heuristic(sx, sy, ex, ey));
            openList.Add(start);
            openDict.Add(Key(sx, sy), start);

            while (openList.Count > 0)
            {
                // 🔍 Tìm node có fCost thấp nhất (không sort toàn bộ list)
                int lowestIndex = 0;
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].fCost < openList[lowestIndex].fCost)
                        lowestIndex = i;
                }

                PointTrain current = openList[lowestIndex];
                openList.RemoveAt(lowestIndex);
                openDict.Remove(Key(current.x, current.y));
                closedSet.Add(Key(current.x, current.y));

                // ✅ Đã đến đích
                if (current.x == ex && current.y == ey)
                {
                    List<PointTrain> path = new List<PointTrain>();
                    while (current != null)
                    {
                        path.Insert(0, current);
                        current = current.parent;
                    }
                    return path;
                }

                int[][] dirs = directions4;
                int tileTypeCur = TileMap.tileTypeAt(current.x, current.y);

                foreach (int[] dir in dirs)
                {
                    int nx = current.x + dir[0];
                    int ny = current.y + dir[1];
                    string key = Key(nx, ny);

                    if (closedSet.Contains(key) || isBlock(nx, ny, ex, ey))
                        continue;

                    int tileTypeNext = TileMap.tileTypeAt(nx, ny);
                    int dy = ny - current.y;

                    // Chặn nhảy không hợp lệ
                    if ((tileTypeCur == 2 && tileTypeNext == 0 && dy > 0) ||
                        (tileTypeCur == 0 && tileTypeNext == 2 && dy < 0))
                        continue;

                    int moveCost = current.gCost + 50;

                    if (!openDict.TryGetValue(key, out PointTrain neighbor))
                    {
                        neighbor = new PointTrain(nx, ny, moveCost, Heuristic(nx, ny, ex, ey), current);
                        openList.Add(neighbor);
                        openDict.Add(key, neighbor);
                    }
                    else if (moveCost < neighbor.gCost)
                    {
                        neighbor.gCost = moveCost;
                        neighbor.parent = current;
                    }
                }
            }

            return null; // ❌ Không tìm thấy đường đi
        }

        private static string Key(int x, int y)
        {
            return x + "_" + y;
        }

        private static int Heuristic(int x, int y, int ex, int ey)
        {
            return (Math.abs(x - ex) + Math.abs(y - ey)) * 10;
        }

        public static bool isBlock(int tileX, int tileY, int ex, int ey)
        {
            if (tileX == ex && tileY == ey)
            {
                return false;
            }
            if (tileX < 0 || tileY < 0 || tileX >= TileMap.tmw || tileY >= TileMap.tmh)
            {
                return true;
            }
            HashSet<int> hashSet = new HashSet<int> { 4, 8, 64, 8192, 8200, 8196, 8204 };
            try
            {
                int[,] array = new int[4, 2]
                {
                    { -1, 0 },
                    { 1, 0 },
                    { 0, -1 },
                    { 0, 1 }
                };
                for (int i = 0; i < 4; i++)
                {
                    int num = tileX + array[i, 0];
                    int num2 = tileY + array[i, 1];
                    if (num >= 0 && num2 >= 0 && num < TileMap.tmw && num2 < TileMap.tmh)
                    {
                        int item = TileMap.tileTypeAt(num, num2);
                        if (hashSet.Contains(item))
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
                return true;
            }
            return false;
        }
    }
}
