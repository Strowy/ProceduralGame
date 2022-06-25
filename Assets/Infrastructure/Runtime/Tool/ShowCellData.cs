using System;
using AIR.Flume;
using Application.Interfaces;
using UnityEngine;

namespace Infrastructure.Runtime.Tool
{
    public class ShowCellData : DependentBehaviour
    {
        [SerializeField] private Transform uLocationIndicator;

        private int _lastX = int.MaxValue;
        private int _lastZ = int.MaxValue;
        private Vector3 _position = Vector3.zero;
        private Vector3 _scale = Vector3.one;

        private IPropertiesService _propertiesService;
        private ITerrainService _terrainService;

        public void Inject(
            IPropertiesService propertiesService,
            ITerrainService terrainService)
        {
            _propertiesService = propertiesService;
            _terrainService = terrainService;
        }

        public void Update()
        {
            var position = uLocationIndicator.position;
            var cellSize = _propertiesService.TerrainProperties.CellSize;
            var currentX = Mathf.FloorToInt(position.x / cellSize);
            var currentZ = Mathf.FloorToInt(position.z / cellSize);

            if (currentX == _lastX && currentZ == _lastZ)
                return;

            _lastX = currentX;
            _lastZ = currentZ;
            var terrainData = _terrainService.GetTerrainData(currentX, currentZ);
            _scale = cellSize * Vector3.one;
            var xPos = currentX * cellSize + 0.5f * cellSize;
            var zPos = currentZ * cellSize + 0.5f * cellSize;
            var yPos = terrainData.Height - 0.5f * cellSize;
            _position = new Vector3(xPos, yPos, zPos);
        }

        public void OnDrawGizmos()
        {
            if (!UnityEngine.Application.isPlaying)
                return;

            Gizmos.DrawCube(_position, _scale);
        }
    }
}