using System;
using System.Collections.Generic;

namespace CubeBlast.Core
{
    /// <summary>Immutable 2D grid coordinate. (0,0) is the bottom-left cell of the board.</summary>
    public struct GridPosition : IEquatable<GridPosition>
    {
        public readonly int X;
        public readonly int Y;

        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public GridPosition Offset(int dx, int dy)
        {
            return new GridPosition(X + dx, Y + dy);
        }

        /// <summary>The four orthogonal neighbours (may be out of board bounds).</summary>
        public IEnumerable<GridPosition> Neighbours()
        {
            yield return new GridPosition(X + 1, Y);
            yield return new GridPosition(X - 1, Y);
            yield return new GridPosition(X, Y + 1);
            yield return new GridPosition(X, Y - 1);
        }

        public bool Equals(GridPosition other) { return X == other.X && Y == other.Y; }
        public override bool Equals(object obj) { return obj is GridPosition && Equals((GridPosition)obj); }
        public override int GetHashCode() { return (X * 397) ^ Y; }
        public static bool operator ==(GridPosition a, GridPosition b) { return a.Equals(b); }
        public static bool operator !=(GridPosition a, GridPosition b) { return !a.Equals(b); }
        public override string ToString() { return "(" + X + "," + Y + ")"; }
    }
}
