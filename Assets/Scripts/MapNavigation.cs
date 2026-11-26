using System.Collections.Generic;
using Stopwatch = System.Diagnostics.Stopwatch;

internal static class MapNavigation
{
    private const int StalledMs = 650, TargetCheckMs = 250, ReplanIntervalMs = 600, TargetDrift = 12;
    private static Char owner;
    private static IMapObject target;
    private static int map, zone, goalX, goalY, next;
    private static long lastProgress, nextTargetCheck, nextReplan;
    private static Grid grid;
    private static List<Leg> route;
    private static RouteSearch search;
    private static Motion motion;
    private static bool attackOnArrival, attackByShortcut;

    internal static bool ChoosingPoint { get; private set; }
    internal static bool FollowingRoute => route != null || search != null;
    internal static bool Active => ChoosingPoint || FollowingRoute;

    internal static void ChoosePoint()
    {
        Cancel();
        Char player = Char.ExistingLocalPlayer;
        if (!CanMove(player)) return;
        RememberMap(player);
        GameScr.instance.ClearPendingMapClick();
        ChoosingPoint = true;
        GameScr.info1?.addInfo("Chọn một điểm trên bản đồ.", 0);
    }

    internal static void GoTo(int x, int y)
    {
        if (!Begin()) return;
        goalX = x;
        goalY = y;
        PlanRoute(pointClick: true);
    }

    internal static void GoToTarget(IMapObject entry, bool attack = false)
    {
        if (!Begin()) return;
        target = entry;
        attackOnArrival = attack;
        if (!TargetAvailable()) { StopMovement(); return; }
        UpdateGoal();
        PlanRoute();
    }

    internal static bool TryQueueAttack(bool shortcut = false)
    {
        if (!FollowingRoute || !IsCombatTarget || !TargetSelected()
            || !GameScr.IsRoutedAttackSkill(owner.myskill)) return false;
        attackOnArrival = true;
        attackByShortcut = shortcut;
        return true;
    }

    private static bool Begin()
    {
        Cancel();
        Char player = Char.ExistingLocalPlayer;
        if (!CanMove(player)) return false;
        RememberMap(player);
        GameScr.instance.ClearPendingMapClick();
        player.cancelAttack();
        player.cvx = player.cvy = 0;
        player.currentMovePoint = null;
        player.endMovePointCommand = null;
        player.delayFall = 0;
        GameScr.instance.auto = 0;
        GameScr.instance.clickMoving = false;
        GameScr.isAutoPlay = false;
        return true;
    }

    private static bool TargetAvailable()
    {
        if (target is ItemMap item) return GameScr.instance.CanPickHudItem(item);
        return GameScr.CanSelectTarget(target) && (target is Mob || target is Char person && GameScr.IsLivePlayerTarget(person));
    }

    private static bool TargetSelected()
    {
        return target == null || ReferenceEquals(target, owner.itemFocus)
            || ReferenceEquals(target, owner.mobFocus) || ReferenceEquals(target, owner.charFocus);
    }

    internal static bool CanAttackFromHere(Char player, IMapObject entry)
    {
        ReadTargetPosition(entry, out int x, out int y);
        Skill skill = player.myskill;
        if (System.Math.Abs(player.cx - x) > skill.dx || System.Math.Abs(player.cy - y) > skill.dy) return false;
        return new Grid(player).HasInteractionLine(new Point(player.cx, player.cy), new Point(x, y));
    }

    private static void ReadTargetPosition(IMapObject entry, out int x, out int y)
    {
        // Combat range uses the Mob coordinates, including for bosses with a separate HUD hitbox.
        if (entry is Mob mob) {
            x = mob.x;
            y = mob.y;
            return;
        }
        x = entry.getX();
        y = entry.getY();
    }

    private static void UpdateGoal()
    {
        ReadTargetPosition(target, out goalX, out goalY);
    }

    private static void TargetRange(out int x, out int y)
    {
        if (target is ItemMap) {
            x = GameScr.ItemPickupRange;
            y = GameScr.ItemPickupRange - 1;
            return;
        }
        Skill skill = owner.myskill;
        x = skill != null && skill.dx > 0 ? System.Math.Min(48, skill.dx) : 40;
        y = skill != null && skill.dy > 0 ? System.Math.Min(32, skill.dy) : 24;
    }

    private static bool CanMove(Char player) => player != null && player.cHP > 0
        && !player.meDead && player.statusMe != 5 && player.statusMe != 14
        && !Char.ischangingMap && !Char.isLoadingMap && !player.isTeleport && !Char.isLockKey && !player.isLockMove
        && !player.stone && !player.holder && !player.isFlyAndCharge && !Controller.isStopReadMessage
        && GameScr.instance != null && GameCanvas.currentScreen == GameScr.instance && !GameScr.instance.isLockKey;

    private static bool CanFly(Char player) => player.canFly && player.cMP > 0;
    private static bool IsCombatTarget => target is Mob || target is Char;

    private static void RememberMap(Char player)
    {
        owner = player;
        map = TileMap.mapID;
        zone = TileMap.zoneID;
    }

    private static void PlanRoute(bool pointClick = false)
    {
        grid = new Grid(owner);
        var start = new Point(owner.cx, owner.cy);
        var destination = new Point(goalX, goalY);
        if (pointClick) {
            if (grid.IsWallClick(destination)) {
                StopMovement();
                return;
            }
            GameScr.instance.ShowClickPositionEffect(goalX, goalY);
        }
        if (owner.statusMe == 1 || owner.statusMe == 2 || owner.statusMe == 4) {
            Point aligned = grid.AlignToGround(start);
            if (!aligned.Equals(start)) {
                owner.ApplyNavigationMotion(aligned.X, aligned.Y, 1);
                Service.gI().charMove();
                start = aligned;
            }
        }
        int rangeX = 0, rangeY = 0;
        if (target != null) {
            TargetRange(out rangeX, out rangeY);
            if (grid.CanApproach(start, destination, rangeX, rangeY, IsCombatTarget)) {
                Arrive();
                return;
            }
        }
        search?.Dispose();
        search = new RouteSearch(grid.Find(start, destination, rangeX, rangeY, IsCombatTarget));
        long now = mSystem.currentTimeMillis();
        nextTargetCheck = now + TargetCheckMs;
        nextReplan = now + ReplanIntervalMs;
        route = null;
        next = 0;
        owner.cvx = owner.cvy = 0;
        BeginLeg(start);
    }

    // Work can run once per rendered frame. Gameplay consumes the result on its usual tick.
    internal static void AdvancePlanning()
    {
        if (search != null && GameCanvas.currentScreen == GameScr.instance) search.Advance();
    }

    internal static void Update()
    {
        if (!Active) return;
        if (owner != Char.ExistingLocalPlayer || !CanMove(owner) || map != TileMap.mapID || zone != TileMap.zoneID) {
            Cancel();
            return;
        }
        if (ManualDirection()) {
            Cancel();
            return;
        }
        if (!FollowingRoute) return;
        if (!TargetSelected() || owner.skillPaint != null || GameScr.instance.auto != 0
            || owner.currentMovePoint != null) {
            StopMovement();
            return;
        }
        if (owner.cx != motion.Position.X || owner.cy != motion.Position.Y) {
            Blocked();
            return;
        }

        if (!grid.Matches(owner)) {
            if (target != null) {
                if (!TargetAvailable()) { StopMovement(); return; }
                UpdateGoal();
            }
            PlanRoute();
            return;
        }
        if (search != null) {
            if (!search.Complete) return;
            route = search.Result;
            search.Dispose();
            search = null;
            if (route == null) {
                StopMovement();
                GameScr.info1?.addInfo("Không tìm được đường tới điểm đã chọn.", 0);
                return;
            }
            BeginLeg(motion.Start);
            if (route.Count == 0) nextTargetCheck = 0;
        }
        long now = mSystem.currentTimeMillis();
        if (next < route.Count && now - lastProgress >= StalledMs) {
            Blocked();
            return;
        }
        if (target == null) {
            if (next == route.Count) StopMovement();
            return;
        }
        if (next < route.Count && motion.Ticks != 0
            && (route[next].Kind == MotionKind.Jump || route[next].Kind == MotionKind.Fall)) return;
        if (now < nextTargetCheck) return;
        nextTargetCheck = now + TargetCheckMs;

        if (!TargetAvailable()) { StopMovement(); return; }
        TargetRange(out int rangeX, out int rangeY);
        ReadTargetPosition(target, out int targetX, out int targetY);
        var destination = new Point(targetX, targetY);
        if (grid.CanApproach(motion.Position, destination, rangeX, rangeY, IsCombatTarget)) {
            Arrive();
            return;
        }
        if (now < nextReplan) return;
        if (next < route.Count) {
            if (grid.CanApproach(route[route.Count - 1].End, destination, rangeX, rangeY, IsCombatTarget)) return;
            if (IsCombatTarget && System.Math.Abs(targetX - goalX) < System.Math.Min(TargetDrift, System.Math.Max(1, rangeX / 2))
                && System.Math.Abs(targetY - goalY) < System.Math.Min(TargetDrift, System.Math.Max(1, rangeY / 2))) return;
        }
        UpdateGoal();
        PlanRoute();
    }

    private static void BeginLeg(Point start)
    {
        motion = new Motion(start);
        lastProgress = mSystem.currentTimeMillis();
    }

    // Called at the local character's movement dispatch point, in place of legacy MovePoint physics.
    internal static bool ApplyMovement(Char player)
    {
        if (!FollowingRoute || player != owner) return false;
        if (player.cx != motion.Position.X || player.cy != motion.Position.Y || player.currentMovePoint != null) {
            Blocked();
            return false;
        }
        if (search != null || next == route.Count) return true;
        Leg leg = route[next];
        Point previous = motion.Position;
        bool tickOk = grid.Tick(ref motion, leg.Kind, leg.Input, out int pose, out bool done);
        if (!tickOk || done != (motion.Ticks == leg.Ticks) || (done && !motion.Position.Equals(leg.End))) {
            Blocked();
            return false;
        }

        Point position = motion.Position;
        player.ApplyNavigationMotion(position.X, position.Y, pose);
        if (!position.Equals(previous)) lastProgress = mSystem.currentTimeMillis();
        if (System.Math.Abs(player.cx - player.cxSend) >= 70 || System.Math.Abs(player.cy - player.cySend) >= 24
            || (done && ShouldSendAtWaypoint(leg)))
            Service.gI().charMove();

        if (done) {
            next++;
            if (next == route.Count) nextTargetCheck = 0;
            BeginLeg(position);
        }
        return true;
    }

    private static bool ShouldSendAtWaypoint(Leg leg)
    {
        if (next + 1 == route.Count) return true;
        Leg following = route[next + 1];
        return leg.Kind != following.Kind || leg.Kind == MotionKind.Jump || leg.Kind == MotionKind.Fall
            || System.Math.Sign(leg.End.X - motion.Start.X) != System.Math.Sign(following.End.X - leg.End.X)
            || System.Math.Sign(leg.End.Y - motion.Start.Y) != System.Math.Sign(following.End.Y - leg.End.Y);
    }

    private static bool ManualDirection()
    {
        bool[] keys = GameCanvas.keyHold;
        return keys[Main.isPC ? 21 : 2] || keys[Main.isPC ? 22 : 8]
            || keys[Main.isPC ? 23 : 4] || keys[Main.isPC ? 24 : 6]
            || (!Main.isPC && (keys[1] || keys[3]));
    }

    private static void Arrive()
    {
        IMapObject arrivedTarget = target;
        ItemMap item = target as ItemMap;
        bool fire = attackOnArrival && IsCombatTarget;
        bool shortcut = attackByShortcut;
        StopMovement();
        if (item != null) {
            Service.gI().charMove();
            GameScr.instance.PickHudItemOnArrival(item);
        }
        else if (fire) {
            Service.gI().charMove();
            GameScr.instance.FireOnNavigationArrival(arrivedTarget, shortcut);
        }
    }

    private static void Blocked()
    {
        StopMovement();
        GameScr.info1?.addInfo("Đã dừng vì đường đi bị chặn.", 0);
    }

    internal static void Cancel()
    {
        ChoosingPoint = false;
        StopMovement();
    }

    internal static void StopMovement()
    {
        if (grid != null && owner.currentMovePoint == null)
            owner.EndNavigationMotion(grid.IsGrounded(new Point(owner.cx, owner.cy)));
        search?.Dispose();
        search = null;
        route = null;
        grid = null;
        target = null;
        attackOnArrival = attackByShortcut = false;
        if (!ChoosingPoint) owner = null;
    }

    private sealed class RouteSearch
    {
        private const int BudgetMs = 2;
        private readonly IEnumerator<List<Leg>> work;
        internal bool Complete { get; private set; }
        internal List<Leg> Result { get; private set; }

        internal RouteSearch(IEnumerator<List<Leg>> work)
        {
            this.work = work;
        }

        internal void Advance()
        {
            if (Complete) return;
            long deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * BudgetMs / 1000;
            do {
                if (!work.MoveNext()) { Complete = true; return; }
                // A null yield leaves the search pending after one A* expansion.
                if (work.Current != null) {
                    Result = work.Current;
                    Complete = true;
                    return;
                }
            } while (Stopwatch.GetTimestamp() < deadline);
        }

        internal void Dispose() => work.Dispose();
    }

    private readonly struct Point
    {
        internal readonly int X, Y;
        internal Point(int x, int y) { X = x; Y = y; }
        internal bool Equals(Point other) => X == other.X && Y == other.Y;
    }

    private enum MotionKind { Walk, Fly, Jump, Fall }

    private struct Motion
    {
        internal readonly Point Start;
        internal Point Position;
        internal int VerticalSpeed, Ticks;
        internal bool Airborne;

        internal Motion(Point start)
        {
            Start = Position = start;
            VerticalSpeed = Ticks = 0;
            Airborne = false;
        }
    }

    private readonly struct Leg
    {
        internal readonly MotionKind Kind;
        internal readonly Point Input, End;
        internal readonly int Length, Ticks;

        internal Leg(MotionKind kind, Point input, Point end, int length, int ticks)
        {
            Kind = kind;
            Input = input;
            End = end;
            Length = length;
            Ticks = ticks;
        }
    }

    private sealed class Grid
    {
        private const int GoalRadius = 48, ArcTickLimit = 128;
        private const int JumpSpeed = 10, JumpHeight = JumpSpeed * (JumpSpeed + 1) / 2;
        private const int Wall = TileMap.T_LEFT | TileMap.T_RIGHT;
        private const int Solid = Wall | TileMap.T_SOLIDGROUND | TileMap.T_OUTSIDE | TileMap.T_DIE;
        private static readonly int[] Directions = { -1, 1 };
        // Only one RouteSearch runs at a time. Preserve capacity across clicks and replans.
        private static readonly Dictionary<int, int> searchGoals = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> searchLengths = new Dictionary<int, int>();
        private static readonly Dictionary<int, (int previous, Leg leg)> searchParents = new Dictionary<int, (int, Leg)>();
        private static readonly List<(int priority, int estimate, int key)> frontier = new List<(int, int, int)>();
        private static readonly List<Leg> searchNeighbors = new List<Leg>(14);
        private static readonly List<Leg> rawPath = new List<Leg>();
        private static readonly List<Leg> plannedPath = new List<Leg>();
        private readonly int columns, width, height, size, stride, halfBody, bodyHeight, runSpeed, flightSpeed, exitY;
        private readonly List<Waypoint> exits = new List<Waypoint>();
        private readonly int[] tiles;
        private readonly bool canFly;

        internal Grid(Char player)
        {
            size = TileMap.size;
            columns = TileMap.tmw;
            width = columns * size;
            height = TileMap.tmh * size;
            exitY = TileMap.isInAirMap() ? height - 48 : height;
            stride = System.Math.Max(1, size / 2);
            tiles = TileMap.types;
            halfBody = System.Math.Max(1, player.chw);
            bodyHeight = System.Math.Max(1, player.chh);
            runSpeed = System.Math.Max(1, player.cspeed);
            flightSpeed = System.Math.Max(1, player.FlightHorizontalSpeed);
            canFly = MapNavigation.CanFly(player);
            for (int i = 0; TileMap.vGo != null && i < TileMap.vGo.size(); i++)
                if (TileMap.vGo.elementAt(i) is Waypoint exit) exits.Add(exit);
        }

        private bool Valid => size > 0 && width > 0 && height > 0 && tiles != null
            && tiles.Length >= columns * (height / size);

        internal bool Matches(Char player) => tiles == TileMap.types && size == TileMap.size
            && width == TileMap.tmw * size && height == TileMap.tmh * size && canFly == MapNavigation.CanFly(player)
            && halfBody == System.Math.Max(1, player.chw) && bodyHeight == System.Math.Max(1, player.chh)
            && runSpeed == System.Math.Max(1, player.cspeed)
            && flightSpeed == System.Math.Max(1, player.FlightHorizontalSpeed);

        internal IEnumerator<List<Leg>> Find(Point start, Point target, int rangeX, int rangeY, bool combatTarget)
        {
            if (!Valid || !InBounds(target) || !CanSweep(start, start)) yield break;
            bool interaction = rangeX > 0;
            bool airApproach = CanApproachInAir(target, combatTarget);
            bool airGoal = !interaction && canFly && !IsExit(target)
                && (Flags(target.X / size, target.Y / size) & TileMap.T_TOP) == 0
                && CanSweep(target, target);
            int bestScore = 0;
            var goals = searchGoals;
            goals.Clear();
            plannedPath.Clear();
            if (airGoal) {
                goals.Add(Key(target), 0);
            }
            else {
                if (!interaction) rangeX = rangeY = GoalRadius;
                bestScore = FindGoals(start, target, rangeX, rangeY, interaction, combatTarget, goals);
            }
            if (goals.Count == 0) yield break;
            if (!interaction) {
                if (FindDirectRoute(start, goals, bestScore, plannedPath)) {
                    yield return plannedPath;
                    yield break;
                }
                yield return null;
            }

            int left = width, right = 0, top = height, bottom = 0;
            foreach (var goal in goals) {
                if (goal.Value != bestScore) continue;
                int x = goal.Key % width, y = goal.Key / width;
                left = System.Math.Min(left, x);
                right = System.Math.Max(right, x);
                top = System.Math.Min(top, y);
                bottom = System.Math.Max(bottom, y);
            }
            int startKey = Key(start);
            int startEstimate = Estimate(start);
            frontier.Clear();
            PushFrontier((startEstimate, startEstimate, startKey));
            var lengths = searchLengths;
            lengths.Clear();
            lengths.Add(startKey, 0);
            var parents = searchParents;
            parents.Clear();
            var neighbors = searchNeighbors;
            int reached = -1;
            while (frontier.Count > 0) {
                var current = PopFrontier();
                int key = current.key;
                int previousLength = lengths[key];
                if (current.priority != previousLength + current.estimate) {
                    yield return null;
                    continue;
                }
                Point from = new Point(key % width, key / width);
                if (goals.TryGetValue(key, out int score) && score == bestScore) {
                    reached = key;
                    break;
                }

                neighbors.Clear();
                FindNeighbors(from, neighbors, followEdges: !interaction || airApproach);
                if (airGoal || airApproach) {
                    // The target can lie between two lanes. Join its axes with the same swept flight steps.
                    if (from.X != target.X && System.Math.Abs(from.X - target.X) < stride)
                        AddLeg(neighbors, from, MotionKind.Fly, new Point(target.X, from.Y));
                    if (from.Y != target.Y && System.Math.Abs(from.Y - target.Y) < stride)
                        AddLeg(neighbors, from, MotionKind.Fly, new Point(from.X, target.Y));
                }
                else if (!interaction && from.X != target.X && System.Math.Abs(from.X - target.X) < stride) {
                    var exactX = new Point(target.X, from.Y);
                    if (goals.TryGetValue(Key(exactX), out int exactScore) && exactScore == bestScore) {
                        if (IsGrounded(from)) AddLeg(neighbors, from, MotionKind.Walk, exactX);
                        else if (canFly) AddLeg(neighbors, from, MotionKind.Fly, exactX);
                    }
                }
                foreach (Leg leg in neighbors) {
                    int neighbor = Key(leg.End);
                    int length = previousLength + leg.Length;
                    if (lengths.TryGetValue(neighbor, out int oldLength) && oldLength <= length) continue;

                    int estimate = Estimate(leg.End);
                    lengths[neighbor] = length;
                    parents[neighbor] = (key, leg);
                    PushFrontier((length + estimate, estimate, neighbor));
                }
                yield return null;
            }

            if (reached < 0) yield break;
            var result = rawPath;
            result.Clear();
            while (reached != startKey) {
                var parent = parents[reached];
                result.Add(parent.leg);
                reached = parent.previous;
            }
            result.Reverse();
            var merged = plannedPath;
            Point groupStart = start;
            for (int first = 0; first < result.Count;) {
                Leg leg = result[first];
                Point end = leg.End;
                int after = first + 1;
                if (leg.Kind == MotionKind.Walk || leg.Kind == MotionKind.Fly) {
                    int dx = System.Math.Sign(end.X - groupStart.X);
                    int dy = System.Math.Sign(end.Y - groupStart.Y);
                    if (dx == 0 || dy == 0) {
                        while (after < result.Count && result[after].Kind == leg.Kind
                            && System.Math.Sign(result[after].End.X - end.X) == dx
                            && System.Math.Sign(result[after].End.Y - end.Y) == dy) {
                            end = result[after++].End;
                        }
                    }
                }
                if (after - first > 1 && TryBuildLeg(groupStart, leg.Kind, end, out Leg combined))
                    merged.Add(combined);
                else
                    for (int i = first; i < after; i++) merged.Add(result[i]);
                groupStart = end;
                first = after;
                yield return null;
            }
            yield return merged;

            // Distance to the bounding box of valid ends is a lower bound on the remaining work.
            int Estimate(Point from) => Distance(System.Math.Max(0, System.Math.Max(left - from.X, from.X - right)),
                System.Math.Max(0, System.Math.Max(top - from.Y, from.Y - bottom)));
        }

        private static void PushFrontier((int priority, int estimate, int key) entry)
        {
            int index = frontier.Count;
            frontier.Add(entry);
            while (index > 0) {
                int parent = (index - 1) / 2;
                if (frontier[parent].CompareTo(entry) <= 0) break;
                frontier[index] = frontier[parent];
                index = parent;
            }
            frontier[index] = entry;
        }

        private static (int priority, int estimate, int key) PopFrontier()
        {
            var entry = frontier[0];
            int lastIndex = frontier.Count - 1;
            var last = frontier[lastIndex];
            frontier.RemoveAt(lastIndex);
            if (lastIndex == 0) return entry;

            int index = 0;
            while (index * 2 + 1 < lastIndex) {
                int child = index * 2 + 1;
                int right = child + 1;
                if (right < lastIndex && frontier[right].CompareTo(frontier[child]) < 0) child = right;
                if (last.CompareTo(frontier[child]) <= 0) break;
                frontier[index] = frontier[child];
                index = child;
            }
            frontier[index] = last;
            return entry;
        }

        private int Key(Point point) => point.Y * width + point.X;
        private static int Distance(int dx, int dy) => dx + dy;
        private static int Distance(Point a, Point b) => Distance(System.Math.Abs(a.X - b.X), System.Math.Abs(a.Y - b.Y));

        private bool FindDirectRoute(Point start, Dictionary<int, int> goals, int bestScore, List<Leg> result)
        {
            Leg best = default;
            foreach (var goal in goals) {
                if (goal.Value != bestScore) continue;
                var end = new Point(goal.Key % width, goal.Key / width);
                if (start.Equals(end)) return true;
                bool walking = start.Y == end.Y && IsGrounded(start);
                if ((walking && TryBuildLeg(start, MotionKind.Walk, end, out Leg leg))
                    || (canFly && TryBuildLeg(start, MotionKind.Fly, end, out leg))) {
                    if (best.Length == 0 || leg.Length < best.Length) best = leg;
                }
            }
            if (best.Length == 0) return false;
            result.Add(best);
            return true;
        }

        private int FindGoals(Point start, Point target, int rangeX, int rangeY, bool interaction,
            bool combatTarget, Dictionary<int, int> goals)
        {
            int best = int.MaxValue;
            int firstX = System.Math.Max(0, target.X - rangeX);
            int firstY = System.Math.Max(0, target.Y - rangeY);
            bool airApproach = CanApproachInAir(target, combatTarget);
            int rowStep = airApproach ? stride : size;
            for (int y = (firstY + rowStep - 1) / rowStep * rowStep; y <= target.Y + rangeY && y < height; y += rowStep)
                AddRow(y);
            if (airApproach && target.Y % rowStep != 0) AddRow(target.Y);
            return best;

            void AddRow(int y)
            {
                int x = interaction ? NextLane(firstX - 1, 1, stride) : NextSideLane(firstX - 1, 1);
                for (; x <= target.X + rangeX && x < width;
                    x = interaction ? NextLane(x, 1, stride) : NextSideLane(x, 1))
                    AddGoal(new Point(x, y));
                if (start.X >= firstX && start.X <= target.X + rangeX)
                    AddGoal(new Point(start.X, y));
                if (!interaction || airApproach) AddGoal(new Point(target.X, y));
            }

            void AddGoal(Point position)
            {
                if (IsExit(position)) return;
                if (interaction) {
                    if (position.Equals(target) || !CanApproach(position, target, rangeX, rangeY, combatTarget)) return;
                }
                else if (!IsGrounded(position) || !CanSweep(position, position)) return;
                int score = interaction ? 0 : Distance(position, target);
                goals[Key(position)] = score;
                best = System.Math.Min(best, score);
            }
        }

        private static int NextLane(int coordinate, int direction, int interval, int offset = 0)
        {
            int remainder = (coordinate - offset) % interval;
            if (remainder < 0) remainder += interval;
            return direction > 0 ? coordinate + interval - remainder
                : coordinate - (remainder == 0 ? interval : remainder);
        }

        private static int NearestLane(int a, int b, int direction) => direction > 0 ? System.Math.Min(a, b) : System.Math.Max(a, b);

        private int NextSideLane(int x, int direction)
        {
            // Align either side of the body immediately outside a tile boundary.
            int left = NextLane(x, direction, size, halfBody);
            int right = NextLane(x, direction, size, -halfBody);
            return NearestLane(NextLane(x, direction, stride), NearestLane(left, right, direction), direction);
        }

        private void FindNeighbors(Point from, List<Leg> neighbors, bool followEdges)
        {
            bool grounded = IsGrounded(from);
            foreach (int direction in Directions) {
                int nextX = followEdges && canFly ? NextSideLane(from.X, direction) : NextLane(from.X, direction, stride);
                var horizontal = new Point(nextX, from.Y);
                bool walk = grounded && AddLeg(neighbors, from, MotionKind.Walk, horizontal);
                if (canFly) {
                    if (!walk) AddLeg(neighbors, from, MotionKind.Fly, horizontal);
                    int nextY = NextLane(from.Y, direction, stride);
                    if (followEdges) nextY = NearestLane(nextY, NextLane(from.Y, direction, size, bodyHeight), direction);
                    AddLeg(neighbors, from, MotionKind.Fly, new Point(from.X, nextY));
                }
                else {
                    if (followEdges && grounded) {
                        int edgeX = NextSideLane(from.X, direction);
                        if (edgeX != nextX) AddLeg(neighbors, from, MotionKind.Walk, new Point(edgeX, from.Y));
                    }
                    if (!walk) {
                        AddLeg(neighbors, from, MotionKind.Fall, horizontal);
                        AddLeg(neighbors, from, MotionKind.Fall, new Point(nextX + direction * stride, from.Y));
                    }
                    if (!grounded) continue;
                    for (int lanes = 0; lanes < 6; lanes++) {
                        var input = new Point(nextX + direction * stride * lanes, from.Y);
                        if (!walk || HasHigherLanding(input)) AddLeg(neighbors, from, MotionKind.Jump, input);
                    }
                }
            }
            if (!canFly) {
                if (!grounded) AddLeg(neighbors, from, MotionKind.Fall, from);
                else if (HasHigherLanding(from)) AddLeg(neighbors, from, MotionKind.Jump, from);
            }
        }

        private bool HasHigherLanding(Point point)
        {
            for (int rise = size; rise <= JumpHeight; rise += size)
                if (IsGrounded(new Point(point.X, point.Y - rise))) return true;
            return false;
        }

        // Simulate each candidate with exactly the same input, gravity and swept body used at runtime.
        private bool AddLeg(List<Leg> neighbors, Point from, MotionKind kind, Point input)
        {
            if (!TryBuildLeg(from, kind, input, out Leg leg)) return false;
            neighbors.Add(leg);
            return true;
        }

        private bool TryBuildLeg(Point from, MotionKind kind, Point input, out Leg leg)
        {
            leg = default;
            if (!InBounds(input)) return false;
            if (kind == MotionKind.Walk) {
                if (from.X == input.X || from.Y != input.Y || !CanSweep(from, input, walking: true)) return false;
                int ticks = 0;
                Point position = from;
                while (position.X != input.X) {
                    position = WalkStep(position, input);
                    ticks++;
                }
                leg = new Leg(kind, input, input, Distance(from, input), ticks);
                return true;
            }
            if (kind == MotionKind.Fly && (from.X == input.X || from.Y == input.Y)) {
                // Axis flight visits the same pixels whether swept once or split into animation ticks.
                int ticks = FlightTicks(from, input);
                if (ticks == 0 || !CanSweep(from, input)) return false;
                leg = new Leg(kind, input, input, Distance(from, input), ticks);
                return true;
            }
            var candidate = new Motion(from);
            int length = 0;
            int tickLimit = kind == MotionKind.Fly ? FlightTicks(from, input) : ArcTickLimit;
            for (int tick = 0; tick < tickLimit; tick++) {
                Point previous = candidate.Position;
                if (!Tick(ref candidate, kind, input, out _, out bool done)) return false;
                length += Distance(previous, candidate.Position);
                if (!done) continue;
                if (length == 0 || candidate.Position.Equals(from)) return false;
                leg = new Leg(kind, input, candidate.Position, length, candidate.Ticks);
                return true;
            }
            return false;
        }

        internal bool Tick(ref Motion motion, MotionKind kind, Point input, out int pose, out bool done)
        {
            pose = 1;
            done = false;
            Point from = motion.Position;
            motion.Ticks++;
            if (kind == MotionKind.Walk || kind == MotionKind.Fly) {
                Point to;
                if (kind == MotionKind.Walk) {
                    to = WalkStep(from, input);
                }
                else {
                    int ticks = FlightTicks(motion.Start, input);
                    if (ticks == 0) return false;
                    to = new Point(motion.Start.X + (input.X - motion.Start.X) * motion.Ticks / ticks,
                        motion.Start.Y + (input.Y - motion.Start.Y) * motion.Ticks / ticks);
                }
                if (!CanSweep(from, to, kind == MotionKind.Walk)) return false;
                motion.Position = to;
                done = to.Equals(input);
                pose = kind == MotionKind.Walk ? 2 : IsGrounded(to) && done ? 1 : 10;
                return true;
            }

            if (motion.Ticks == 1) {
                motion.Airborne = kind == MotionKind.Jump || !IsGrounded(from);
                motion.VerticalSpeed = kind == MotionKind.Jump ? -JumpSpeed : 0;
            }
            Point position = from;
            int direction = System.Math.Sign(input.X - from.X);
            int horizontal = System.Math.Min(System.Math.Abs(input.X - from.X), kind == MotionKind.Jump ? 4 : runSpeed);
            for (int i = 0; i < horizontal; i++) {
                var to = new Point(position.X + direction, position.Y);
                if (!CanSweep(position, to)) break;
                position = to;
                if (!motion.Airborne && !IsGrounded(position)) {
                    motion.Airborne = true;
                    break;
                }
            }
            if (!motion.Airborne) {
                motion.Position = position;
                pose = 2;
                return !position.Equals(from) && position.X != input.X;
            }

            int vertical = motion.VerticalSpeed < 0 ? motion.VerticalSpeed : System.Math.Min(Char.TerminalFallSpeed, motion.VerticalSpeed + 1);
            motion.VerticalSpeed = vertical < 0 ? vertical + 1 : vertical;
            int verticalDirection = System.Math.Sign(vertical);
            for (int i = 0; i < System.Math.Abs(vertical); i++) {
                if (verticalDirection > 0 && IsGrounded(position)) {
                    done = true;
                    break;
                }
                var to = new Point(position.X, position.Y + verticalDirection);
                if (!CanSweep(position, to)) {
                    if (verticalDirection > 0) return false;
                    motion.VerticalSpeed = 0;
                    break;
                }
                position = to;
            }
            done |= vertical > 0 && IsGrounded(position);
            motion.Position = position;
            pose = done ? 1 : vertical < 0 ? 3 : 4;
            return !done || position.X == input.X;
        }

        private static int Advance(int value, int target, int speed) => value
            + System.Math.Sign(target - value) * System.Math.Min(speed, System.Math.Abs(target - value));

        private Point WalkStep(Point from, Point input)
        {
            int speed = (Flags(from.X / size, (from.Y - 1) / size) & TileMap.T_WATERFLOW) == 0
                ? runSpeed : System.Math.Max(1, runSpeed / 2);
            return new Point(Advance(from.X, input.X, speed), from.Y);
        }

        private int FlightTicks(Point from, Point to)
        {
            int verticalSpeed = to.Y < from.Y ? Char.HeldJumpSpeed : Char.TerminalFallSpeed;
            return System.Math.Max(
                (System.Math.Abs(to.X - from.X) + flightSpeed - 1) / flightSpeed,
                (System.Math.Abs(to.Y - from.Y) + verticalSpeed - 1) / verticalSpeed);
        }

        private bool CanApproachInAir(Point target, bool combatTarget) => combatTarget && canFly && !IsGrounded(target);

        internal bool CanApproach(Point from, Point target, int rangeX, int rangeY, bool combatTarget)
        {
            return (IsGrounded(from) || CanApproachInAir(target, combatTarget))
                && System.Math.Abs(target.X - from.X) <= rangeX && System.Math.Abs(target.Y - from.Y) <= rangeY
                && HasInteractionLine(from, target);
        }

        internal bool HasInteractionLine(Point from, Point target)
        {
            if (!Valid || !InBounds(target) || IsExit(from) || !CanSweep(from, from)) return false;
            // An interaction uses the same solid tiles with a narrow ray above the feet.
            var rayStart = new Point(from.X, System.Math.Max(1, from.Y - 3));
            var rayEnd = new Point(target.X, System.Math.Max(1, target.Y - 3));
            return CanSweep(rayStart, rayEnd, ray: true);
        }

        private bool InBounds(Point point) => point.X >= 0 && point.X < width && point.Y >= 0 && point.Y < height;

        private int Flags(int column, int row) => tiles[row * columns + column];

        internal Point AlignToGround(Point point)
        {
            if (!Valid || !InBounds(point) || point.Y % size == 0) return point;

            // Map spawn coordinates can put the feet inside the floor tile.
            var floor = new Point(point.X, point.Y / size * size);
            return IsGrounded(floor) && CanSweep(floor, floor) ? floor : point;
        }

        private static bool InExit(Point point, Waypoint exit) => point.X >= exit.minX && point.X <= exit.maxX
            && point.Y >= exit.minY && point.Y <= exit.maxY;

        private bool IsExit(Point point)
        {
            foreach (Waypoint exit in exits)
                if (InExit(point, exit)) return true;
            return false;
        }

        internal bool IsGrounded(Point point) => Valid && InBounds(point) && point.Y % size == 0
            && (Flags(point.X / size, point.Y / size) & TileMap.T_TOP) != 0;

        internal bool IsWallClick(Point point)
        {
            if (!Valid || !InBounds(point) || IsGrounded(point)) return false;
            return (Flags(point.X / size, point.Y / size) & (Wall | TileMap.T_SOLIDGROUND | TileMap.T_OUTSIDE)) != 0;
        }

        private bool CanOccupy(int minX, int maxX, int y, int halfWidth, int body)
        {
            int left = minX - halfWidth;
            int right = RightmostBodyPixel(maxX, halfWidth);
            if (left < 0 || right >= width || y < body || y >= height)
                return false;
            for (int row = (y - body) / size; row <= (y - 1) / size; row++)
                for (int column = left / size; column <= right / size; column++)
                    if ((Flags(column, row) & Solid) != 0) return false;
            return true;
        }

        // Char can stop exactly at tileLeft - chw. The boundary belongs to the wall, not the body.
        private static int RightmostBodyPixel(int x, int halfWidth) => x + System.Math.Max(0, halfWidth - 1);

        private bool CanSweep(Point from, Point to, bool walking = false, bool ray = false)
        {
            int halfWidth = ray ? 0 : halfBody;
            int body = ray ? 1 : bodyHeight;
            if (from.Y == to.Y && from.X != to.X)
                return CanSweepHorizontal(from, to, halfWidth, body, walking, ray);
            if (!ClearPixel(from, from, halfWidth, body, ray)) return false;

            int dx = to.X - from.X, dy = to.Y - from.Y;
            int pixels = System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy));
            Point previous = from;
            for (int i = 1; i <= pixels; i++) {
                var next = new Point(from.X + dx * i / pixels, from.Y + dy * i / pixels);
                if (next.X != previous.X && next.Y != previous.Y) {
                    // Both orthogonal sides of a diagonal must clear the body.
                    if (!ClearPixel(previous, new Point(next.X, previous.Y), halfWidth, body, ray)
                        || !ClearPixel(previous, new Point(previous.X, next.Y), halfWidth, body, ray)) return false;
                }
                if (!ClearPixel(previous, next, halfWidth, body, ray)) return false;
                if (walking && !IsGrounded(next)) return false;
                previous = next;
            }
            return true;
        }

        private bool CanSweepHorizontal(Point from, Point to, int halfWidth, int body, bool walking, bool ray)
        {
            // The union of the body across a horizontal step is one rectangle.
            int left = System.Math.Min(from.X, to.X), right = System.Math.Max(from.X, to.X);
            int y = from.Y;
            if (!CanOccupy(left, right, y, halfWidth, body)) return false;
            if (ray) return true;
            if (y >= exitY) return false;
            // A character who spawned inside a waypoint may leave it, but cannot enter one.
            foreach (Waypoint exit in exits)
                if (y >= exit.minY && y <= exit.maxY && left <= exit.maxX && right >= exit.minX
                    && !InExit(from, exit)) return false;

            int row = y / size;
            for (int column = left / size; column <= right / size; column++)
                if ((Flags(column, row) & TileMap.T_DIE) != 0) return false;

            if (!walking) return true;
            if (y % size != 0) return false;
            // The original walking rule checks floor support after leaving the starting pixel.
            int firstStep = from.X + System.Math.Sign(to.X - from.X);
            int firstColumn = System.Math.Min(firstStep, to.X) / size;
            int lastColumn = System.Math.Max(firstStep, to.X) / size;
            for (int column = firstColumn; column <= lastColumn; column++)
                if ((Flags(column, row) & TileMap.T_TOP) == 0) return false;
            return true;
        }

        private bool ClearPixel(Point from, Point to, int halfWidth, int body, bool ray)
        {
            if (!CanOccupy(to.X, to.X, to.Y, halfWidth, body)) return false;
            if (!ray) {
                if (to.Y >= exitY) return false;
                if ((Flags(to.X / size, to.Y / size) & TileMap.T_DIE) != 0)
                    return false;
                foreach (Waypoint exit in exits)
                    if (InExit(to, exit) && !InExit(from, exit)) return false;
            }
            if (to.Y == from.Y) return true;

            if (to.Y > from.Y) {
                if (!ray) {
                    if (IsGrounded(new Point(to.X, from.Y)))
                        return false;
                    return true;
                }
                if ((to.Y - 1) % size != 0) return true;
                int lowerRow = (to.Y - 1) / size;
                return (Flags(to.X / size, lowerRow) & TileMap.T_TOP) == 0
                    && (Flags(from.X / size, lowerRow - 1) & TileMap.T_BOTTOM) == 0;
            }

            int fromHead = from.Y - body;
            if (fromHead % size != 0) return true;
            int ceilingRow = (to.Y - body) / size;
            for (int column = (to.X - halfWidth) / size; column <= RightmostBodyPixel(to.X, halfWidth) / size; column++)
                if ((Flags(column, ceilingRow) & TileMap.T_BOTTOM) != 0)
                    return false;
            return !ray || (Flags(from.X / size, fromHead / size) & TileMap.T_TOP) == 0;
        }

    }
}
