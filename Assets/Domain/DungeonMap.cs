using System;

namespace Domain
{
    public class DungeonMap
    {
        public int[] Cells { get; }
        public RectBounds Bounds { get; }

        public DungeonMap(int minX, int minY, int maxX, int maxY)
        {
            Bounds = new RectBounds(minX, minY, maxX, maxY);
            Cells = new int[Bounds.Width * Bounds.Height];
        }

        public void SetValue(IntegerPoint point, int value)
        {
            if (!Bounds.WithinBounds(point))
                throw new IndexOutOfRangeException($"Point {point.ToString()} is outside bounds of dungeon map.");

            Cells[point.Y * Bounds.Width + point.X] = value;
        }

        public int GetValue(IntegerPoint point)
        {
            return Cells[point.Y * Bounds.Width + point.X];
        }

        public RectBounds GetBufferedBounds(int buffer)
        {
            return new RectBounds(
                Bounds.MinX + buffer,
                Bounds.MinY + buffer,
                Bounds.MaxX - buffer,
                Bounds.MaxY - buffer);
        }
    }
}