using System;

namespace BubbleShooter.Grid
{
    [Serializable]
    public struct GridCoordinate : IEquatable<GridCoordinate>
    {
        public int Row;
        public int Column;

        public GridCoordinate(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public bool Equals(GridCoordinate other)
        {
            return Row == other.Row && Column == other.Column;
        }

        public override bool Equals(object obj)
        {
            return obj is GridCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Row * 397) ^ Column;
            }
        }

        public static bool operator ==(GridCoordinate left, GridCoordinate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridCoordinate left, GridCoordinate right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"({Row}, {Column})";
        }
    }
}
