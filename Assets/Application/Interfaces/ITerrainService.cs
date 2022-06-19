namespace Application.Interfaces
{
    public interface ITerrainService
    {
        TerrainData GetTerrainData(int x, int y);
    }

    public struct TerrainData
    {
        public float Height;
        public int Biome;
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