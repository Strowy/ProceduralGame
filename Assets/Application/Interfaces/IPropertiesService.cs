namespace Application.Interfaces
{
    public interface IPropertiesService
    {
        TerrainProperties TerrainProperties { get; }
    }

    public struct TerrainProperties
    {
        public int ChunkSize;
        public int BiomeSize;
        public int MaxHeight;
        public float CellSize;
    }
}