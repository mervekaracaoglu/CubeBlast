using System.Collections.Generic;

namespace CubeBlast.Core
{
    /// <summary>
    /// Resolves special item explosions, combo explosions and their chain
    /// reactions into an ordered list of <see cref="ExplosionStep"/>s.
    /// </summary>
    public sealed class ExplosionResolver
    {
        private struct PendingExplosion
        {
            public ExplosionKind Kind;
            public GridPosition Center;
            public RocketOrientation Orientation;
            public int Wave;
            public SpecialItem Source;
        }

        private readonly Board _board;
        private readonly Queue<PendingExplosion> _queue = new Queue<PendingExplosion>();

        public ExplosionResolver(Board board)
        {
            _board = board;
        }

        /// <summary>Queues the explosion of a single special item at its own position.</summary>
        public void EnqueueSpecial(SpecialItem item, int wave)
        {
            var rocket = item as RocketItem;
            if (rocket != null)
            {
                Enqueue(ExplosionKind.Rocket, rocket.Origin, rocket.Orientation, wave, rocket);
            }
            else
            {
                Enqueue(ExplosionKind.Tnt, item.Origin, RocketOrientation.Horizontal, wave, item);
            }
        }

        /// <summary>Queues the primitive explosions that make up a combo, centered at the tap cell.</summary>
        public void EnqueueCombo(ComboKind kind, GridPosition center)
        {
            switch (kind)
            {
                case ComboKind.RocketRocket:
                    // 2 perpendicular rockets in a + shape.
                    Enqueue(ExplosionKind.Rocket, center, RocketOrientation.Horizontal, 0);
                    Enqueue(ExplosionKind.Rocket, center, RocketOrientation.Vertical, 0);
                    break;
                case ComboKind.TntTnt:
                    // 7x7 area.
                    Enqueue(ExplosionKind.TntTnt, center, RocketOrientation.Horizontal, 0);
                    break;
                case ComboKind.TntRocket:
                    // A 3x3 array of rockets exploding in a + shape:
                    // three horizontal rows and three vertical columns centered on the tap.
                    // Rows/columns outside the board are skipped.
                    for (int offset = -1; offset <= 1; offset++)
                    {
                        int row = center.Y + offset;
                        if (row >= 0 && row < _board.Height)
                        {
                            Enqueue(ExplosionKind.Rocket, new GridPosition(center.X, row), RocketOrientation.Horizontal, 0);
                        }
                        int column = center.X + offset;
                        if (column >= 0 && column < _board.Width)
                        {
                            Enqueue(ExplosionKind.Rocket, new GridPosition(column, center.Y), RocketOrientation.Vertical, 0);
                        }
                    }
                    break;
            }
        }

        private void Enqueue(ExplosionKind kind, GridPosition center, RocketOrientation orientation, int wave,
            SpecialItem source = null)
        {
            _queue.Enqueue(new PendingExplosion
            {
                Kind = kind,
                Center = center,
                Orientation = orientation,
                Wave = wave,
                Source = source
            });
        }

        /// <summary>Processes the queue, including chained special items, and returns the ordered steps.</summary>
        public List<ExplosionStep> Resolve()
        {
            var steps = new List<ExplosionStep>();
            while (_queue.Count > 0)
            {
                var pending = _queue.Dequeue();
                var step = Explode(pending);
                if (step.AffectedCells.Count > 0)
                {
                    steps.Add(step);
                }
            }
            return steps;
        }

        private ExplosionStep Explode(PendingExplosion explosion)
        {
            var step = new ExplosionStep
            {
                Kind = explosion.Kind,
                Center = explosion.Center,
                Orientation = explosion.Orientation,
                Wave = explosion.Wave,
                Source = explosion.Source
            };
            CollectAffectedCells(explosion, step.AffectedCells);

            // One explosion is one damage source: obstacles take a single damage
            // event based on how many of their cells were covered.
            var obstacleCells = new Dictionary<ObstacleItem, int>();

            foreach (var cell in step.AffectedCells)
            {
                var item = _board.ItemAt(cell);
                if (item == null)
                {
                    continue;
                }

                var cube = item as CubeItem;
                if (cube != null)
                {
                    _board.Remove(cube);
                    cube.IsDestroyed = true;
                    step.DestroyedCubes.Add(cube);
                    continue;
                }

                var special = item as SpecialItem;
                if (special != null)
                {
                    // Special items chain-explode when damaged by another special item.
                    _board.Remove(special);
                    special.IsDestroyed = true;
                    step.TriggeredSpecials.Add(special);
                    EnqueueSpecial(special, explosion.Wave + 1);
                    continue;
                }

                var obstacle = item as ObstacleItem;
                if (obstacle != null)
                {
                    int count;
                    obstacleCells.TryGetValue(obstacle, out count);
                    obstacleCells[obstacle] = count + 1;
                }
            }

            foreach (var pair in obstacleCells)
            {
                var result = pair.Key.ApplyExplosionDamage(pair.Value);
                if (result.TookDamage)
                {
                    if (result.Destroyed)
                    {
                        _board.Remove(pair.Key);
                    }
                    step.Damages.Add(new ObstacleDamage
                    {
                        Obstacle = pair.Key,
                        Damage = result.Damage,
                        ChalicesCollected = result.ChalicesCollected,
                        Destroyed = result.Destroyed,
                        DoorBroken = result.DoorBroken
                    });
                }
            }
            return step;
        }

        private void CollectAffectedCells(PendingExplosion explosion, List<GridPosition> cells)
        {
            if (explosion.Kind == ExplosionKind.Rocket)
            {
                CollectRocketCells(explosion, cells);
            }
            else
            {
                int radius = explosion.Kind == ExplosionKind.TntTnt ? GameRules.TntTntRadius : GameRules.TntRadius;
                CollectAreaCells(explosion.Center, radius, cells);
            }
        }

        /// <summary>Rocket parts damage cells one by one while moving away from the center.</summary>
        private void CollectRocketCells(PendingExplosion explosion, List<GridPosition> cells)
        {
            var center = explosion.Center;
            if (_board.InBounds(center))
            {
                cells.Add(center);
            }
            int max = explosion.Orientation == RocketOrientation.Horizontal ? _board.Width : _board.Height;
            for (int distance = 1; distance < max; distance++)
            {
                GridPosition a, b;
                if (explosion.Orientation == RocketOrientation.Horizontal)
                {
                    a = center.Offset(-distance, 0);
                    b = center.Offset(distance, 0);
                }
                else
                {
                    a = center.Offset(0, -distance);
                    b = center.Offset(0, distance);
                }
                if (_board.InBounds(a)) { cells.Add(a); }
                if (_board.InBounds(b)) { cells.Add(b); }
            }
        }

        /// <summary>Square area cells ordered by distance from the center (for staggered animations).</summary>
        private void CollectAreaCells(GridPosition center, int radius, List<GridPosition> cells)
        {
            for (int ring = 0; ring <= radius; ring++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        int chebyshev = System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy));
                        if (chebyshev != ring)
                        {
                            continue;
                        }
                        var cell = center.Offset(dx, dy);
                        if (_board.InBounds(cell))
                        {
                            cells.Add(cell);
                        }
                    }
                }
            }
        }
    }
}
