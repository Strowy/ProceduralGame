using System;
using System.Collections.Generic;

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

        public IEnumerable<IntegerPoint> MapCells()
        {
            for (var x = Bounds.MinX; x < Bounds.MaxX + 1; x++)
            {
                for (var y = Bounds.MinY; y < Bounds.MaxY + 1; y++)
                {
                    yield return new IntegerPoint(x, y);
                }
            }
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

        public bool WithinMap(IntegerPoint point)
        {
            return Bounds.WithinBounds(point);
        }

        public IntegerPoint DirectionToClosestEdge(IntegerPoint position)
        {
            var currDistance = Math.Max(Bounds.Width, Bounds.Height);
            var (x, y) = (0, 0);
            if (Bounds.MaxX - position.X < currDistance)
            {
                currDistance = Bounds.MaxX - position.X;
                x = 1;
            }

            if (position.X - Bounds.MinX < currDistance)
            {
                currDistance = position.X - Bounds.MinX;
                x = -1;
            }

            if (Bounds.MaxY - position.Y < currDistance)
            {
                currDistance = Bounds.MaxY - position.Y;
                x = 0;
                y = 1;
            }

            if (position.Y - Bounds.MinY < currDistance)
            {
                y = -1;
            }

            return new IntegerPoint(x, y);
        }
    }
}