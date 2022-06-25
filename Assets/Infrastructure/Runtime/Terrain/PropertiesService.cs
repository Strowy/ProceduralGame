using AIR.Flume;
using Application.Interfaces;
using UnityEngine;

namespace Infrastructure.Runtime.Terrain
{
    public class PropertiesService : DependentBehaviour, IPropertiesService
    {
        [SerializeField] private int uChunkSize = 8;
        [SerializeField] private int uBiomeSize = 32;
        [SerializeField] private int uMaxHeight = 100;
        [SerializeField] private float uCellSize = 2f;

        public TerrainProperties TerrainProperties => GetTerrainProperties();

        private TerrainProperties GetTerrainProperties()
        {
            return new TerrainProperties
            {
                BiomeSize = uBiomeSize,
                CellSize = uCellSize,
                ChunkSize = uChunkSize,
                MaxHeight = uMaxHeight,
            };
        }
    }
}