using Domain;

namespace Application.Interfaces
{
    public interface ITerrainService
    {
        TerrainInfo GetTerrainData(int x, int y);
        IntegerPoint GetBiome(int x, int y);
    }

    public struct TerrainInfo
    {
        public float Height;
        public int Zone;
        public Prop Prop;
    }

    public enum Prop
    {
        None,
        Water,
        Portal,
        Floor,
        Wall,
    }
}