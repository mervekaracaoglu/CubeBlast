using System.Collections.Generic;

namespace CubeBlast.Core
{
    /// <summary>
    /// Base class for everything that can occupy board cells:
    /// cubes, special items and obstacles.
    /// </summary>
    public abstract class BoardItem
    {
        private static int _nextId;

        /// <summary>Stable unique id, used by the view layer to track items across steps.</summary>
        public int Id { get; private set; }

        /// <summary>Bottom-left cell occupied by this item.</summary>
        public GridPosition Origin { get; internal set; }

        /// <summary>Footprint in cells. Everything is 1x1 except the 2x2 Chalice Box.</summary>
        public virtual int CellWidth { get { return 1; } }
        public virtual int CellHeight { get { return 1; } }

        /// <summary>Whether the item falls into empty cells below it.</summary>
        public abstract bool CanFall { get; }

        public bool IsDestroyed { get; internal set; }

        protected BoardItem()
        {
            Id = ++_nextId;
        }

        public IEnumerable<GridPosition> OccupiedCells
        {
            get
            {
                for (int dx = 0; dx < CellWidth; dx++)
                {
                    for (int dy = 0; dy < CellHeight; dy++)
                    {
                        yield return Origin.Offset(dx, dy);
                    }
                }
            }
        }

        public bool Occupies(GridPosition position)
        {
            return position.X >= Origin.X && position.X < Origin.X + CellWidth
                && position.Y >= Origin.Y && position.Y < Origin.Y + CellHeight;
        }
    }
}
