using Domain;

namespace Application.Interfaces
{
    public interface IDungeonGenerator
    {
        DungeonMap Map { get; }

        void Generate(IntegerPoint entrancePosition);
    }
}