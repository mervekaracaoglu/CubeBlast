using System.Collections.Generic;

namespace CubeBlast.Core
{
    /// <summary>Flood-fill helpers for finding connected groups of items.</summary>
    public static class GroupFinder
    {
        /// <summary>All same-coloured cubes orthogonally connected to the cube at the given position.</summary>
        public static List<CubeItem> FindCubeGroup(Board board, GridPosition start)
        {
            var result = new List<CubeItem>();
            var startCube = board.ItemAt(start) as CubeItem;
            if (startCube == null)
            {
                return result;
            }

            var visited = new HashSet<int>();
            var queue = new Queue<CubeItem>();
            visited.Add(startCube.Id);
            queue.Enqueue(startCube);

            while (queue.Count > 0)
            {
                var cube = queue.Dequeue();
                result.Add(cube);
                foreach (var neighbourPos in cube.Origin.Neighbours())
                {
                    var neighbour = board.ItemAt(neighbourPos) as CubeItem;
                    if (neighbour != null && neighbour.Color == startCube.Color && visited.Add(neighbour.Id))
                    {
                        queue.Enqueue(neighbour);
                    }
                }
            }
            return result;
        }

        /// <summary>All special items orthogonally connected to the given special item (combo group).</summary>
        public static List<SpecialItem> FindSpecialGroup(Board board, SpecialItem start)
        {
            var result = new List<SpecialItem>();
            var visited = new HashSet<int>();
            var queue = new Queue<SpecialItem>();
            visited.Add(start.Id);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                result.Add(item);
                foreach (var neighbourPos in item.Origin.Neighbours())
                {
                    var neighbour = board.ItemAt(neighbourPos) as SpecialItem;
                    if (neighbour != null && visited.Add(neighbour.Id))
                    {
                        queue.Enqueue(neighbour);
                    }
                }
            }
            return result;
        }

        /// <summary>What special item, if any, blasting this group would create.</summary>
        public static SpecialHint HintForGroupSize(int groupSize)
        {
            if (groupSize >= GameRules.TntGroupSize)
            {
                return SpecialHint.Tnt;
            }
            if (groupSize >= GameRules.RocketGroupSize)
            {
                return SpecialHint.Rocket;
            }
            return SpecialHint.None;
        }
    }
}
