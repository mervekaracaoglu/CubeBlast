using System.Collections.Generic;

namespace CubeBlast.Core
{
    /// <summary>Remaining amount for one goal kind shown in the level UI.</summary>
    public struct GoalCount
    {
        public string Key;
        public int Remaining;
    }

    /// <summary>
    /// Engine-independent game simulation for one level. The Unity layer feeds it
    /// taps and animates the returned <see cref="TurnResult"/> steps. The engine
    /// never touches UnityEngine, which keeps the rules fully unit-testable.
    /// </summary>
    public sealed class BoardEngine
    {
        public Board Board { get; private set; }
        public int MovesLeft { get; private set; }
        public LevelState State { get; private set; }

        private readonly IRandomProvider _random;

        public BoardEngine(LevelDefinition definition, IRandomProvider random)
        {
            _random = random;
            Board = BoardFactory.Create(definition, random);
            MovesLeft = definition.MoveCount;
            State = Board.HasObstacles ? LevelState.Playing : LevelState.Won;

            // Levels are authored to be playable, but 'rand' cells could in theory
            // start without any valid move; fix it silently before the first render.
            if (State == LevelState.Playing && !HasAnyValidMove())
            {
                TryShuffle(new ShuffleStep());
            }
        }

        /// <summary>Processes a tap on a board cell and returns everything that happened.</summary>
        public TurnResult Tap(GridPosition position)
        {
            var result = new TurnResult { State = State, MovesLeft = MovesLeft };
            if (State != LevelState.Playing || !Board.InBounds(position))
            {
                return result;
            }

            var item = Board.ItemAt(position);
            if (item == null)
            {
                return result;
            }

            var cube = item as CubeItem;
            if (cube != null)
            {
                var group = GroupFinder.FindCubeGroup(Board, position);
                if (group.Count < GameRules.MinBlastGroupSize)
                {
                    result.RejectedItem = cube;
                    return result;
                }
                PlayBlast(result, position, group);
                return result;
            }

            var special = item as SpecialItem;
            if (special != null)
            {
                PlaySpecial(result, position, special);
                return result;
            }

            // Obstacles are not tappable.
            result.RejectedItem = item;
            return result;
        }

        // ---------------------------------------------------------------- blasts

        private void PlayBlast(TurnResult result, GridPosition tap, List<CubeItem> group)
        {
            SpendMove(result);

            var step = new BlastStep { TapPosition = tap };
            step.Cubes.AddRange(group);

            // Obstacles react to the blast based on how many group cubes touch them.
            var adjacentCubes = new Dictionary<ObstacleItem, HashSet<int>>();
            foreach (var cube in group)
            {
                foreach (var neighbourPos in cube.Origin.Neighbours())
                {
                    var obstacle = Board.ItemAt(neighbourPos) as ObstacleItem;
                    if (obstacle == null)
                    {
                        continue;
                    }
                    HashSet<int> cubes;
                    if (!adjacentCubes.TryGetValue(obstacle, out cubes))
                    {
                        cubes = new HashSet<int>();
                        adjacentCubes[obstacle] = cubes;
                    }
                    cubes.Add(cube.Id);
                }
            }

            foreach (var cube in group)
            {
                Board.Remove(cube);
                cube.IsDestroyed = true;
            }

            foreach (var pair in adjacentCubes)
            {
                var damage = pair.Key.ApplyBlastDamage(pair.Value.Count);
                if (damage.TookDamage)
                {
                    if (damage.Destroyed)
                    {
                        Board.Remove(pair.Key);
                    }
                    step.Damages.Add(new ObstacleDamage
                    {
                        Obstacle = pair.Key,
                        Damage = damage.Damage,
                        ChalicesCollected = damage.ChalicesCollected,
                        Destroyed = damage.Destroyed,
                        DoorBroken = damage.DoorBroken
                    });
                }
            }

            // Groups of 4-5 create a Rocket, 6+ create a TNT, at the tapped cell.
            var hint = GroupFinder.HintForGroupSize(group.Count);
            if (hint == SpecialHint.Tnt)
            {
                step.CreatedSpecial = new TntItem();
            }
            else if (hint == SpecialHint.Rocket)
            {
                var orientation = _random.Next(2) == 0 ? RocketOrientation.Horizontal : RocketOrientation.Vertical;
                step.CreatedSpecial = new RocketItem(orientation);
            }
            if (step.CreatedSpecial != null)
            {
                Board.Place(step.CreatedSpecial, tap);
            }

            result.Steps.Add(step);
            FinishTurn(result);
        }

        // ---------------------------------------------------------- special items

        private void PlaySpecial(TurnResult result, GridPosition tap, SpecialItem tapped)
        {
            SpendMove(result);

            var resolver = new ExplosionResolver(Board);
            var comboGroup = GroupFinder.FindSpecialGroup(Board, tapped);
            if (comboGroup.Count == 1)
            {
                Board.Remove(tapped);
                tapped.IsDestroyed = true;
                resolver.EnqueueSpecial(tapped, 0);
            }
            else
            {
                // Adjacent special items merge into a single combo at the tapped cell.
                var kind = DetermineCombo(comboGroup);
                var gather = new ComboGatherStep { Center = tap, Kind = kind };
                foreach (var item in comboGroup)
                {
                    Board.Remove(item);
                    item.IsDestroyed = true;
                    gather.Items.Add(item);
                }
                result.Steps.Add(gather);
                resolver.EnqueueCombo(kind, tap);
            }

            foreach (var step in resolver.Resolve())
            {
                result.Steps.Add(step);
            }
            FinishTurn(result);
        }

        private static ComboKind DetermineCombo(List<SpecialItem> group)
        {
            int tntCount = 0;
            for (int i = 0; i < group.Count; i++)
            {
                if (group[i] is TntItem)
                {
                    tntCount++;
                }
            }
            if (tntCount == 0)
            {
                return ComboKind.RocketRocket;
            }
            if (tntCount >= 2)
            {
                return ComboKind.TntTnt;
            }
            return ComboKind.TntRocket;
        }

        // ------------------------------------------------------------- turn flow

        private void SpendMove(TurnResult result)
        {
            MovesLeft--;
            result.IsValidMove = true;
        }

        private void FinishTurn(TurnResult result)
        {
            result.Steps.Add(GravitySystem.Apply(Board, _random));

            if (!Board.HasObstacles)
            {
                State = LevelState.Won;
            }
            else if (MovesLeft <= 0)
            {
                State = LevelState.Failed;
            }
            else if (!HasAnyValidMove())
            {
                var shuffle = new ShuffleStep();
                if (TryShuffle(shuffle) && shuffle.Moves.Count > 0)
                {
                    result.Steps.Add(shuffle);
                }
            }

            result.State = State;
            result.MovesLeft = MovesLeft;
        }

        // ------------------------------------------------------------ deadlocks

        public bool HasAnyValidMove()
        {
            var visited = new HashSet<int>();
            foreach (var item in Board.Items)
            {
                if (item is SpecialItem)
                {
                    return true;
                }
                var cube = item as CubeItem;
                if (cube == null || !visited.Add(cube.Id))
                {
                    continue;
                }
                var group = GroupFinder.FindCubeGroup(Board, cube.Origin);
                if (group.Count >= GameRules.MinBlastGroupSize)
                {
                    return true;
                }
                foreach (var member in group)
                {
                    visited.Add(member.Id);
                }
            }
            return false;
        }

        /// <summary>Re-arranges cubes randomly until a valid move exists.</summary>
        private bool TryShuffle(ShuffleStep step)
        {
            var cubes = new List<CubeItem>();
            foreach (var item in Board.Items)
            {
                var cube = item as CubeItem;
                if (cube != null)
                {
                    cubes.Add(cube);
                }
            }
            if (cubes.Count < 2)
            {
                return false;
            }

            var positions = new List<GridPosition>();
            var origins = new Dictionary<int, GridPosition>();
            for (int i = 0; i < cubes.Count; i++)
            {
                positions.Add(cubes[i].Origin);
                origins[cubes[i].Id] = cubes[i].Origin;
            }

            for (int attempt = 0; attempt < 32; attempt++)
            {
                // Fisher-Yates over the cube positions.
                for (int i = positions.Count - 1; i > 0; i--)
                {
                    int j = _random.Next(i + 1);
                    var tmp = positions[i];
                    positions[i] = positions[j];
                    positions[j] = tmp;
                }
                for (int i = 0; i < cubes.Count; i++)
                {
                    Board.Move(cubes[i], positions[i]);
                }
                if (HasAnyValidMove())
                {
                    for (int i = 0; i < cubes.Count; i++)
                    {
                        var from = origins[cubes[i].Id];
                        if (from != cubes[i].Origin)
                        {
                            step.Moves.Add(new FallMove { Item = cubes[i], From = from, To = cubes[i].Origin });
                        }
                    }
                    return true;
                }
            }

            // Random attempts failed (only possible with very few cubes); place two
            // same-coloured cubes next to each other deterministically if we can.
            for (int i = 0; i < cubes.Count; i++)
            {
                for (int j = i + 1; j < cubes.Count; j++)
                {
                    if (cubes[i].Color != cubes[j].Color)
                    {
                        continue;
                    }
                    foreach (var cell in positions)
                    {
                        foreach (var neighbour in cell.Neighbours())
                        {
                            if (!positions.Contains(neighbour) || neighbour == cell)
                            {
                                continue;
                            }
                            // Swap so the matching pair sits on cell + neighbour.
                            SwapInto(cubes, cubes[i], cell);
                            SwapInto(cubes, cubes[j], neighbour);
                            for (int k = 0; k < cubes.Count; k++)
                            {
                                var from = origins[cubes[k].Id];
                                if (from != cubes[k].Origin)
                                {
                                    step.Moves.Add(new FallMove { Item = cubes[k], From = from, To = cubes[k].Origin });
                                }
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void SwapInto(List<CubeItem> cubes, CubeItem cube, GridPosition target)
        {
            if (cube.Origin == target)
            {
                return;
            }
            var occupant = Board.ItemAt(target) as CubeItem;
            if (occupant != null)
            {
                Board.SwapPositions(cube, occupant);
            }
            else
            {
                Board.Move(cube, target);
            }
        }

        // ----------------------------------------------------------------- info

        /// <summary>Special item hints per cube (rocket/TNT logos shown on eligible groups).</summary>
        public Dictionary<int, SpecialHint> GetSpecialHints()
        {
            var hints = new Dictionary<int, SpecialHint>();
            var visited = new HashSet<int>();
            foreach (var item in Board.Items)
            {
                var cube = item as CubeItem;
                if (cube == null || !visited.Add(cube.Id))
                {
                    continue;
                }
                var group = GroupFinder.FindCubeGroup(Board, cube.Origin);
                var hint = GroupFinder.HintForGroupSize(group.Count);
                foreach (var member in group)
                {
                    visited.Add(member.Id);
                    hints[member.Id] = hint;
                }
            }
            return hints;
        }

        /// <summary>Remaining goal amounts grouped by obstacle kind, in a stable order.</summary>
        public List<GoalCount> GetGoals()
        {
            var order = new List<string>();
            var totals = new Dictionary<string, int>();
            foreach (var obstacle in Board.Obstacles)
            {
                if (!totals.ContainsKey(obstacle.GoalKey))
                {
                    totals[obstacle.GoalKey] = 0;
                    order.Add(obstacle.GoalKey);
                }
                totals[obstacle.GoalKey] += obstacle.RemainingGoalAmount;
            }
            var result = new List<GoalCount>();
            for (int i = 0; i < order.Count; i++)
            {
                result.Add(new GoalCount { Key = order[i], Remaining = totals[order[i]] });
            }
            return result;
        }
    }
}
