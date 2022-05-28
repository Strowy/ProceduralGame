using System;

namespace Domain
{
    public readonly struct IntegerPoint : IEquatable<IntegerPoint>
    {
        public int X { get; }
        public int Y { get; }

        public IntegerPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static IntegerPoint Zero => new IntegerPoint(0, 0);

        public IntegerPoint Invert() => new IntegerPoint(Y, X);

        public IntegerPoint Abs() => new IntegerPoint(Math.Abs(X), Math.Abs(Y));

        public bool Equals(IntegerPoint other)
            => X == other.X && Y == other.Y;

        public override bool Equals(object obj)
            => obj is IntegerPoint other && Equals(other);

        public override int GetHashCode()
            => (x: X, y: Y).GetHashCode();

        public static bool operator ==(IntegerPoint lhs, IntegerPoint rhs)
            => lhs.Equals(rhs);

        public static bool operator !=(IntegerPoint lhs, IntegerPoint rhs)
            => !lhs.Equals(rhs);

        public static IntegerPoint operator +(IntegerPoint lhs, IntegerPoint rhs)
            => new IntegerPoint(lhs.X + rhs.X, lhs.Y + rhs.Y);

        public static IntegerPoint operator +(IntegerPoint lhs, int rhs)
            => new IntegerPoint(lhs.X + rhs, lhs.Y + rhs);

        public static IntegerPoint operator -(IntegerPoint lhs, IntegerPoint rhs)
            => new IntegerPoint(lhs.X - rhs.X, lhs.Y - rhs.Y);

        public static IntegerPoint operator -(IntegerPoint lhs, int rhs)
            => new IntegerPoint(lhs.X - rhs, lhs.Y - rhs);

        public static IntegerPoint operator *(IntegerPoint lhs, IntegerPoint rhs)
            => new IntegerPoint(lhs.X * rhs.X, lhs.Y * rhs.Y);

        public static IntegerPoint operator *(IntegerPoint lhs, int rhs)
            => new IntegerPoint(lhs.X * rhs, lhs.Y * rhs);

        public static IntegerPoint operator /(IntegerPoint lhs, int rhs)
            => new IntegerPoint(lhs.X / rhs, lhs.Y / rhs);

        public override string ToString()
            => $"(X: {X}, Y: {Y})";
    }
}