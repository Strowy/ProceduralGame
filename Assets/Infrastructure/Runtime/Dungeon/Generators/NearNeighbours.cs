using Domain;

namespace Infrastructure.Runtime.Dungeon.Generators
{
    public static class NearNeighbours
    {
        public static IntegerPoint[] FullEight = {
            new IntegerPoint(-1, 0),
            new IntegerPoint(-1, -1),
            new IntegerPoint(0, -1),
            new IntegerPoint(1, -1),
            new IntegerPoint(1, 0),
            new IntegerPoint(1, 1),
            new IntegerPoint(0, 1),
            new IntegerPoint(-1, 1)
        };
    }
}