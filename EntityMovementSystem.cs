using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GridLocked
{
    /// <summary>
    /// Core navigation, territory wander selection, dynamic collision resolution,
    /// and turn/speed coordination for creatures and NPCs.
    /// </summary>
    public static class EntityMovementSystem
    {
        private static readonly Point[] CardinalOffsets =
        {
            new Point(0, -1), // North
            new Point(1, 0),  // East
            new Point(0, 1),  // South
            new Point(-1, 0)  // West
        };

        /// <summary>
        /// Validates whether a tile can be traversed or stood on by an entity.
        /// Checks bounds, walls, pits, stairs, closed doors, pillars/solid props, and blocking sub-items.
        /// </summary>
        public static bool IsValidStandableTile(DungeonLevel level, int x, int y, bool checkOccupant = false, MonsterInstance self = null)
        {
            if (level == null) return false;
            if (x < 0 || x >= level.Width || y < 0 || y >= level.Height) return false;

            var tile = level.Tiles[y, x];
            if (tile == null) return false;

            // 1. Basic tile types: walls, pits, stairs
            if (tile.Type == TileType.Wall) return false;
            if (tile.Type == TileType.VoidOrPit) return false;
            if (tile.Type == TileType.StairsUp || tile.Type == TileType.StairsDown) return false;

            // 2. Door transitions: closed doors block movement
            if (tile.Transition is DoorTransition door && !door.IsOpen && door.BlocksMovement)
                return false;

            // 3. Large objects: pillars, sarcophagi, altars, statues
            if (tile.CenterOccupant is LargeObjectInstance lo && lo.BlocksMovement)
                return false;

            // 4. Sub-square blocking clutter (boulders, iron portcullis)
            for (int s = 0; s < 4; s++)
            {
                var it = tile.Slots[s];
                if (it != null && it.Definition != null && !it.Definition.PassThru)
                    return false;
            }

            // 5. Dynamic entity occupancy
            if (checkOccupant && tile.CenterOccupant != null && tile.CenterOccupant != self)
            {
                if (tile.CenterOccupant.BlocksMovement)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Selects a wander target tile from the entity's painted territory.
        /// Re-rolls if the selected tile is an invalid obstacle (pit, stairs, pillar).
        /// If rolling the same tile the monster is currently on, enters a 3-movement-cycle idle period.
        /// </summary>
        public static Point? SelectWanderDestination(MonsterInstance monster, DungeonLevel level, Random rand = null)
        {
            if (monster == null || level == null) return null;
            if (rand == null) rand = new Random();

            // Build candidate territory pool
            List<Point> pool;
            if (monster.WanderAreaTiles != null && monster.WanderAreaTiles.Count > 0)
            {
                pool = monster.WanderAreaTiles;
            }
            else
            {
                // Fallback radius around spawn/current tile
                pool = new List<Point>();
                int r = Math.Max(1, monster.WanderRadius);
                int cx = monster.TileX;
                int cy = monster.TileY;
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int tx = cx + dx;
                        int ty = cy + dy;
                        if (IsValidStandableTile(level, tx, ty, checkOccupant: false))
                            pool.Add(new Point(tx, ty));
                    }
                }
            }

            if (pool.Count == 0) return null;

            // Re-roll loop: pick candidate from pool, verifying standability (not a pillar, pit, stairs)
            Point candidate = new Point(monster.TileX, monster.TileY);
            bool foundValid = false;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                var pt = pool[rand.Next(pool.Count)];
                if (IsValidStandableTile(level, pt.X, pt.Y, checkOccupant: false))
                {
                    candidate = pt;
                    foundValid = true;
                    break;
                }
            }

            if (!foundValid)
            {
                // If every painted tile was blocked, search orthogonal neighbors of current tile
                foreach (var offset in CardinalOffsets)
                {
                    int nx = monster.TileX + offset.X;
                    int ny = monster.TileY + offset.Y;
                    if (IsValidStandableTile(level, nx, ny, checkOccupant: false))
                    {
                        candidate = new Point(nx, ny);
                        foundValid = true;
                        break;
                    }
                }
            }

            if (!foundValid)
            {
                monster.LastNavStatus = "No valid standable tile in territory";
                return null;
            }

            // Same-tile roll: Idle for 3 movement cycles, then re-roll
            if (candidate.X == monster.TileX && candidate.Y == monster.TileY)
            {
                monster.IdleCyclesRemaining = 3;
                monster.CurrentPath.Clear();
                monster.TargetDestination = candidate;
                monster.LastNavStatus = "Rolled same tile -> Idling for 3 cycles";
                return candidate;
            }

            // Different tile: proceed with pathfinding
            monster.IdleCyclesRemaining = 0;
            monster.TargetDestination = candidate;
            monster.LastNavStatus = $"New target: ({candidate.X}, {candidate.Y})";
            return candidate;
        }

        /// <summary>
        /// Breadth-First Search (BFS) pathfinder on 4-way orthogonal grid.
        /// Respects static obstacles, temporary dynamic blockages, and territory constraints.
        /// </summary>
        public static List<Point> FindPath(
            DungeonLevel level,
            Point start,
            Point goal,
            List<Point> allowedTerritory = null,
            HashSet<Point> temporaryBlocked = null,
            bool preferTerritory = true)
        {
            var path = new List<Point>();
            if (level == null) return path;
            if (start.X == goal.X && start.Y == goal.Y) return path;
            if (!IsValidStandableTile(level, goal.X, goal.Y, checkOccupant: false)) return path;

            HashSet<Point> territorySet = null;
            if (allowedTerritory != null && allowedTerritory.Count > 0)
                territorySet = new HashSet<Point>(allowedTerritory);

            var queue = new Queue<Point>();
            var visited = new HashSet<Point>();
            var parent = new Dictionary<Point, Point>();

            queue.Enqueue(start);
            visited.Add(start);

            bool reached = false;

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur.X == goal.X && cur.Y == goal.Y)
                {
                    reached = true;
                    break;
                }

                foreach (var offset in CardinalOffsets)
                {
                    var next = new Point(cur.X + offset.X, cur.Y + offset.Y);
                    if (visited.Contains(next)) continue;

                    // Cannot walk through walls, pits, closed doors, pillars
                    if (!IsValidStandableTile(level, next.X, next.Y, checkOccupant: false))
                        continue;

                    // Cannot step onto temporary blocked tiles (another monster currently blocking)
                    if (temporaryBlocked != null && temporaryBlocked.Contains(next))
                        continue;

                    // Territory preference: prefer staying inside painted zone unless corridor required
                    if (preferTerritory && territorySet != null && !territorySet.Contains(next) && next != goal)
                    {
                        // Allow only if start was outside territory (e.g. returning from chase)
                        if (territorySet.Contains(start)) continue;
                    }

                    visited.Add(next);
                    parent[next] = cur;
                    queue.Enqueue(next);
                }
            }

            if (!reached) return path;

            // Reconstruct path backward from goal to start
            var trace = goal;
            while (trace != start)
            {
                path.Add(trace);
                if (!parent.TryGetValue(trace, out trace)) break;
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// Executes a single movement step for an entity.
        /// Handles idling, destination re-rolling, turn coordination, and dynamic obstacle resolution.
        /// </summary>
        public static void StepEntity(MonsterInstance monster, DungeonLevel level, Random rand = null)
        {
            if (monster == null || level == null) return;
            if (monster.MovementMode == EntityMovementMode.Static) return;
            if (rand == null) rand = new Random();

            // 1. Idle cycles countdown (e.g. rolled same tile or waiting for occupant to clear)
            if (monster.IdleCyclesRemaining > 0)
            {
                monster.IdleCyclesRemaining--;
                monster.LastNavStatus = $"Idling ({monster.IdleCyclesRemaining} cycle(s) remaining)";

                // When idle countdown finishes and path is empty, re-roll immediately
                if (monster.IdleCyclesRemaining == 0 && monster.CurrentPath.Count == 0)
                {
                    var newDest = SelectWanderDestination(monster, level, rand);
                    if (newDest.HasValue && (newDest.Value.X != monster.TileX || newDest.Value.Y != monster.TileY))
                    {
                        var route = FindPath(level, new Point(monster.TileX, monster.TileY), newDest.Value, monster.WanderAreaTiles);
                        monster.CurrentPath.Clear();
                        monster.CurrentPath.AddRange(route);
                    }
                }
                return;
            }

            // 2. Need a new route
            if (monster.CurrentPath.Count == 0)
            {
                if (monster.MovementMode == EntityMovementMode.Wander)
                {
                    var dest = SelectWanderDestination(monster, level, rand);
                    if (dest.HasValue && (dest.Value.X != monster.TileX || dest.Value.Y != monster.TileY))
                    {
                        var route = FindPath(level, new Point(monster.TileX, monster.TileY), dest.Value, monster.WanderAreaTiles);
                        if (route.Count > 0)
                        {
                            monster.CurrentPath.Clear();
                            monster.CurrentPath.AddRange(route);
                            monster.LastNavStatus = $"Path found ({route.Count} steps to ({dest.Value.X}, {dest.Value.Y}))";
                        }
                        else
                        {
                            monster.IdleCyclesRemaining = 1;
                            monster.LastNavStatus = "Target unreachable -> Idling 1 cycle";
                        }
                    }
                    return;
                }
                else if (monster.MovementMode == EntityMovementMode.PatrolRoute)
                {
                    if (monster.PatrolWaypoints.Count == 0) return;
                    AdvancePatrolWaypoint(monster, level);
                    return;
                }
            }

            // 3. Step along CurrentPath
            if (monster.CurrentPath.Count > 0)
            {
                Point nextPt = monster.CurrentPath[0];

                // Check static validity
                if (!IsValidStandableTile(level, nextPt.X, nextPt.Y, checkOccupant: false))
                {
                    // Next tile became blocked by a closed door or obstacle -> recompute
                    monster.CurrentPath.Clear();
                    monster.LastNavStatus = "Path blocked by terrain -> Rerolling route";
                    return;
                }

                // Check occupant on next tile
                var occupant = level.Tiles[nextPt.Y, nextPt.X].CenterOccupant;

                if (occupant is PlayerOccupant)
                {
                    // Player is in front of the creature
                    UpdateFacing(monster, nextPt);
                    monster.IsInCombat = true;
                    monster.LastNavStatus = $"Engaged player at ({nextPt.X}, {nextPt.Y})";
                    return;
                }
                else if (occupant is MonsterInstance otherMon && otherMon != monster)
                {
                    // Dynamic conflict: another creature occupies the tile
                    if (monster.BlockedCyclesCount == 0)
                    {
                        // Action 1: Idle 1 cycle and check if the occupant moves on the next turn
                        monster.BlockedCyclesCount = 1;
                        monster.IdleCyclesRemaining = 1;
                        monster.LastNavStatus = $"Tile ({nextPt.X}, {nextPt.Y}) occupied by {otherMon.Id} -> Idling 1 cycle";
                        return;
                    }
                    else
                    {
                        // Action 2: Still blocked after idling! Detour or re-roll
                        var tempBlocked = new HashSet<Point> { nextPt };
                        if (monster.TargetDestination.HasValue)
                        {
                            var altPath = FindPath(level, new Point(monster.TileX, monster.TileY), monster.TargetDestination.Value, monster.WanderAreaTiles, tempBlocked);
                            if (altPath.Count > 0)
                            {
                                monster.CurrentPath.Clear();
                                monster.CurrentPath.AddRange(altPath);
                                monster.BlockedCyclesCount = 0;
                                monster.LastNavStatus = $"Detouring around {otherMon.Id}";
                                return;
                            }
                        }

                        // No detour available: re-roll a new destination within territory
                        monster.CurrentPath.Clear();
                        monster.BlockedCyclesCount = 0;
                        monster.LastNavStatus = "Tile permanently blocked with no detour -> Rerolling destination";

                        var newDest = SelectWanderDestination(monster, level, rand);
                        if (newDest.HasValue && (newDest.Value.X != monster.TileX || newDest.Value.Y != monster.TileY))
                        {
                            var newRoute = FindPath(level, new Point(monster.TileX, monster.TileY), newDest.Value, monster.WanderAreaTiles, tempBlocked);
                            monster.CurrentPath.AddRange(newRoute);
                        }
                        return;
                    }
                }

                // Tile is clear! Step into it
                level.Tiles[monster.TileY, monster.TileX].CenterOccupant = null;
                UpdateFacing(monster, nextPt);

                monster.TileX = nextPt.X;
                monster.TileY = nextPt.Y;
                level.Tiles[nextPt.Y, nextPt.X].CenterOccupant = monster;

                monster.CurrentPath.RemoveAt(0);
                monster.BlockedCyclesCount = 0;
                monster.LastNavStatus = $"Moved to ({monster.TileX}, {monster.TileY})";

                if (monster.CurrentPath.Count == 0)
                {
                    monster.TargetDestination = null;
                    monster.LastNavStatus = $"Arrived at target ({monster.TileX}, {monster.TileY}) -> Ready to re-roll";
                }
            }
        }

        private static void UpdateFacing(MonsterInstance monster, Point targetPt)
        {
            int dx = targetPt.X - monster.TileX;
            int dy = targetPt.Y - monster.TileY;
            if (dx > 0) monster.FacingAngle = 0.0;             // East
            else if (dx < 0) monster.FacingAngle = Math.PI;    // West
            else if (dy > 0) monster.FacingAngle = 0.5 * Math.PI; // South
            else if (dy < 0) monster.FacingAngle = 1.5 * Math.PI; // North
        }

        private static void AdvancePatrolWaypoint(MonsterInstance monster, DungeonLevel level)
        {
            if (monster.PatrolWaypoints.Count == 0) return;

            int targetIdx = monster.CurrentPatrolIndex;
            if (targetIdx >= monster.PatrolWaypoints.Count) targetIdx = 0;

            var goal = monster.PatrolWaypoints[targetIdx];
            var route = FindPath(level, new Point(monster.TileX, monster.TileY), goal, preferTerritory: false);

            monster.CurrentPath.Clear();
            monster.CurrentPath.AddRange(route);

            // Circuit index stepping
            if (monster.CircuitStyle == PatrolCircuitStyle.Loop)
            {
                monster.CurrentPatrolIndex = (monster.CurrentPatrolIndex + 1) % monster.PatrolWaypoints.Count;
            }
            else if (monster.CircuitStyle == PatrolCircuitStyle.PingPong)
            {
                if (!monster.PatrolReverse)
                {
                    monster.CurrentPatrolIndex++;
                    if (monster.CurrentPatrolIndex >= monster.PatrolWaypoints.Count)
                    {
                        monster.CurrentPatrolIndex = monster.PatrolWaypoints.Count - 2;
                        monster.PatrolReverse = true;
                    }
                }
                else
                {
                    monster.CurrentPatrolIndex--;
                    if (monster.CurrentPatrolIndex < 0)
                    {
                        monster.CurrentPatrolIndex = 1;
                        monster.PatrolReverse = false;
                    }
                }
            }
            else // Once
            {
                if (monster.CurrentPatrolIndex < monster.PatrolWaypoints.Count - 1)
                    monster.CurrentPatrolIndex++;
            }
        }

        /// <summary>
        /// Updates all entities across the dungeon level in order of Speed (Initiative).
        /// Higher speed entities step first, clearing tiles before slower entities take their turn.
        /// </summary>
        public static void UpdateLevelEntities(DungeonLevel level, double deltaTime = 0, bool forceCycle = false, Random rand = null)
        {
            if (level == null || level.Monsters == null || level.Monsters.Count == 0) return;
            if (rand == null) rand = new Random();

            // Sort by Speed descending so fast entities move first
            var sortedMonsters = level.Monsters.OrderByDescending(m => m.Speed).ToList();

            foreach (var monster in sortedMonsters)
            {
                if (forceCycle)
                {
                    StepEntity(monster, level, rand);
                }
                else if (deltaTime > 0)
                {
                    monster.WanderTimerSeconds += deltaTime;
                    if (monster.WanderTimerSeconds >= monster.WanderIntervalSeconds)
                    {
                        monster.WanderTimerSeconds = 0;
                        StepEntity(monster, level, rand);
                    }
                }
            }
        }

        /// <summary>
        /// Computes minimum Chebyshev distance from the monster's current tile to its painted territory.
        /// </summary>
        public static int GetDistanceToTerritory(MonsterInstance monster)
        {
            if (monster == null) return 0;
            if (monster.WanderAreaTiles == null || monster.WanderAreaTiles.Count == 0) return 0;

            int minDist = int.MaxValue;
            foreach (var pt in monster.WanderAreaTiles)
            {
                int dist = Math.Max(Math.Abs(monster.TileX - pt.X), Math.Abs(monster.TileY - pt.Y));
                if (dist < minDist) minDist = dist;
            }
            return minDist;
        }

        /// <summary>
        /// Checks whether the monster was kited beyond MaxChaseDistance away from territory.
        /// If exceeded, disengages combat and pathfinds back to pre-combat square.
        /// </summary>
        public static bool CheckCombatLeash(MonsterInstance monster, DungeonLevel level)
        {
            if (monster == null || level == null || !monster.IsInCombat) return false;

            int dist = GetDistanceToTerritory(monster);
            if (dist > monster.MaxChaseDistance)
            {
                // Disengage combat!
                monster.IsInCombat = false;
                monster.IsReturningToLeash = true;
                monster.LastNavStatus = $"Kited beyond leash ({dist} > {monster.MaxChaseDistance}) -> Disengaging";

                Point returnTarget = monster.ReturnToPreCombatSquare ? monster.PreCombatTile : (monster.WanderAreaTiles.FirstOrDefault());
                if (returnTarget.IsEmpty && monster.SpawnTile != Point.Empty) returnTarget = monster.SpawnTile;

                var returnPath = FindPath(level, new Point(monster.TileX, monster.TileY), returnTarget, preferTerritory: false);
                monster.CurrentPath.Clear();
                monster.CurrentPath.AddRange(returnPath);
                return true;
            }

            return false;
        }
    }
}
