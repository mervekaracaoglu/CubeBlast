using System.Collections.Generic;

namespace CubeBlast.Core
{
    /// <summary>
    /// The board grid. Keeps track of which item occupies which cell and provides
    /// cell-level queries for the gameplay systems. Multi-cell items (Chalice Box)
    /// occupy several cells that all resolve to the same item instance.
    /// </summary>
    public sealed class Board
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        private readonly BoardItem[,] _cells;
        private readonly List<BoardItem> _items = new List<BoardItem>();

        public Board(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new BoardItem[width, height];
        }

        public bool InBounds(GridPosition position)
        {
            return position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;
        }

        public BoardItem ItemAt(GridPosition position)
        {
            return InBounds(position) ? _cells[position.X, position.Y] : null;
        }

        public bool IsEmpty(GridPosition position)
        {
            return InBounds(position) && _cells[position.X, position.Y] == null;
        }

        public void Place(BoardItem item, GridPosition origin)
        {
            item.Origin = origin;
            foreach (var cell in item.OccupiedCells)
            {
                _cells[cell.X, cell.Y] = item;
            }
            _items.Add(item);
        }

        public void Remove(BoardItem item)
        {
            foreach (var cell in item.OccupiedCells)
            {
                if (InBounds(cell) && _cells[cell.X, cell.Y] == item)
                {
                    _cells[cell.X, cell.Y] = null;
                }
            }
            _items.Remove(item);
        }

        public void Move(BoardItem item, GridPosition newOrigin)
        {
            foreach (var cell in item.OccupiedCells)
            {
                if (_cells[cell.X, cell.Y] == item)
                {
                    _cells[cell.X, cell.Y] = null;
                }
            }
            item.Origin = newOrigin;
            foreach (var cell in item.OccupiedCells)
            {
                _cells[cell.X, cell.Y] = item;
            }
        }

        /// <summary>Swaps the positions of two single-cell items (used by the shuffler).</summary>
        public void SwapPositions(BoardItem a, BoardItem b)
        {
            var positionA = a.Origin;
            var positionB = b.Origin;
            _cells[positionA.X, positionA.Y] = b;
            _cells[positionB.X, positionB.Y] = a;
            a.Origin = positionB;
            b.Origin = positionA;
        }

        /// <summary>All items currently on the board.</summary>
        public IEnumerable<BoardItem> Items
        {
            get { return _items; }
        }

        public IEnumerable<ObstacleItem> Obstacles
        {
            get
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    var obstacle = _items[i] as ObstacleItem;
                    if (obstacle != null)
                    {
                        yield return obstacle;
                    }
                }
            }
        }

        public bool HasObstacles
        {
            get
            {
                foreach (var obstacle in Obstacles)
                {
                    return true;
                }
                return false;
            }
        }
    }
}
