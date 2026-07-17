using System.Collections.Generic;

namespace CubeBlast.Core
{
    /// <summary>
    /// Makes items fall straight down into empty cells and spawns new random
    /// cubes from the top of the grid. Items never fall through other items,
    /// so static obstacles act as barriers and cells below them are only
    /// filled by items already trapped there.
    /// </summary>
    public static class GravitySystem
    {
        public static GravityStep Apply(Board board, IRandomProvider random)
        {
            var step = new GravityStep();
            ApplyFalls(board, step);
            SpawnNewCubes(board, random, step);
            return step;
        }

        private static void ApplyFalls(Board board, GravityStep step)
        {
            for (int x = 0; x < board.Width; x++)
            {
                int writeY = 0;
                for (int y = 0; y < board.Height; y++)
                {
                    var position = new GridPosition(x, y);
                    var item = board.ItemAt(position);
                    if (item == null)
                    {
                        continue;
                    }
                    if (!item.CanFall || item.CellWidth > 1)
                    {
                        // Static items (and every cell of the 2x2 box) are barriers.
                        writeY = y + 1;
                        continue;
                    }
                    if (writeY < y)
                    {
                        var target = new GridPosition(x, writeY);
                        board.Move(item, target);
                        step.Falls.Add(new FallMove { Item = item, From = position, To = target });
                    }
                    writeY++;
                }
            }
        }

        private static void SpawnNewCubes(Board board, IRandomProvider random, GravityStep step)
        {
            for (int x = 0; x < board.Width; x++)
            {
                // Only the empty cells reachable from the top of the grid are refilled.
                var targets = new List<GridPosition>();
                for (int y = board.Height - 1; y >= 0; y--)
                {
                    var position = new GridPosition(x, y);
                    if (board.ItemAt(position) != null)
                    {
                        break;
                    }
                    targets.Add(position);
                }
                // targets are ordered top to bottom; spawn the deepest one first so
                // the falling column keeps its order.
                for (int i = targets.Count - 1; i >= 0; i--)
                {
                    var cube = new CubeItem(BoardFactory.RandomColor(random));
                    board.Place(cube, targets[i]);
                    step.Spawns.Add(new SpawnedCube
                    {
                        Cube = cube,
                        Target = targets[i],
                        SpawnRow = board.Height + (targets.Count - 1 - i)
                    });
                }
            }
        }
    }
}
