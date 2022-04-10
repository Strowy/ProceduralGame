using System;

namespace Domain
{
    public struct Cell : IEquatable<Cell>
    {
        public int x;
        public int y;

        public Cell(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(Cell other)
            => x == other.x && y == other.y;

        public override bool Equals(object obj)
            => obj is Cell other && Equals(other);

        public override int GetHashCode()
            => (x, y).GetHashCode();

        public static bool operator ==(Cell lhs, Cell rhs) => lhs.Equals(rhs);
        
        public static bool operator !=(Cell lhs, Cell rhs) => !lhs.Equals(rhs);

        public override string ToString() => $"(x: {x}, y: {y})";
    }
}