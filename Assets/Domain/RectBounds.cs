using System;

namespace Domain
{
    public readonly struct RectBounds
    {
        public int MaxX { get; }
        public int MaxY { get; }
        public int MinX { get; }
        public int MinY { get; }
        public int Height => (MaxY - MinY) + 1;
        public int Width => (MaxX - MinX) + 1;

        public RectBounds(int minX, int minY, int maxX, int maxY)
        {
            MaxX = Math.Max(minX, maxX);
            MaxY = Math.Max(minY, maxY);
            MinX = Math.Min(minX, maxX);
            MinY = Math.Min(minY, maxY);
        }

        public bool WithinBounds(IntegerPoint point, int buffer = 0)
            => WithinBounds(point.X, point.Y, buffer);

        private bool WithinBounds(int x, int y, int buffer = 0)
        {
            if (x < MinX + buffer) return false;
            if (y < MinY + buffer) return false;
            if (x > MaxX - buffer) return false;
            if (y > MaxY - buffer) return false;

            return true;
        }

        public bool Overlaps(RectBounds rhs, int buffer = 0)
        {
            if (rhs.WithinBounds(MinX, MinY, -buffer)) return true;
            if (rhs.WithinBounds(MinX, MaxY, -buffer)) return true;
            if (rhs.WithinBounds(MaxX, MinY, -buffer)) return true;
            if (rhs.WithinBounds(MaxX, MaxY, -buffer)) return true;

            if (WithinBounds(rhs.MinX, rhs.MinY, -buffer)) return true;
            if (WithinBounds(rhs.MinX, rhs.MaxY, -buffer)) return true;
            if (WithinBounds(rhs.MaxX, rhs.MinY, -buffer)) return true;
            if (WithinBounds(rhs.MaxX, rhs.MaxY, -buffer)) return true;

            return false;
        }

        public RectBounds GetBufferlessOverlap(RectBounds rhs)
        {
            if (!Overlaps(rhs))
                throw new ArgumentException("RectBounds do not overlap.");

            var minX = Math.Max(MinX, rhs.MinX);
            var maxX = Math.Min(MaxX, rhs.MaxX);
            var minY = Math.Max(MinY, rhs.MinY);
            var maxY = Math.Min(MaxY, rhs.MaxY);
            return new RectBounds(minX, minY, maxX, maxY);
        }

        public override string ToString()
        {
            return $"Bounds: minX: {MinX}, maxX: {MaxX}, minY: {MinY}, maxY: {MaxY}";
        }
    }
}