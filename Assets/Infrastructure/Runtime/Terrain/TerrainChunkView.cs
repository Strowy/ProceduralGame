using System;
using System.Collections.Generic;
using AIR.Flume;
using Application.Interfaces;
using Domain;
using UnityEngine;

namespace Infrastructure.Runtime.Terrain
{
    public class TerrainChunkView : DependentBehaviour
    {
        [SerializeField] private Transform uTerrainCell;
        [SerializeField] private Transform uPortalActive;
        [SerializeField] private Transform uPortalClosed;
        [SerializeField] private Material[] uTerrainMaterials;

        private float _yOffset = 2;

        private IGameStateController _gameStateController;
        private IPropertiesService _propertiesService;
        private ITerrainService _terrainService;

        private TerrainCellView[] _terrainCells = null;
        private readonly List<Transform> _chunkProps = new List<Transform>();

        public IntegerPoint ChunkPosition { get; private set; } = IntegerPoint.Zero;

        public void Inject(
            IGameStateController gameStateController,
            IPropertiesService propertiesService,
            ITerrainService terrainService)
        {
            _gameStateController = gameStateController;
            _propertiesService = propertiesService;
            _terrainService = terrainService;
        }

        public void Initialise()
        {
            var terrainProperties = _propertiesService.TerrainProperties;
            uTerrainCell.localScale = new Vector3(1, 4, 1) * terrainProperties.CellSize;
            _yOffset = 2 * terrainProperties.CellSize;
            InstantiateCells(terrainProperties);
        }

        public void UpdateAtPosition(int x, int y)
        {
            TrashChunkProps();
            ChunkPosition = new IntegerPoint(x, y);
            var chunkSize = _propertiesService.TerrainProperties.ChunkSize;
            var cellSize = _propertiesService.TerrainProperties.CellSize;
            var xPos = x * cellSize + cellSize * 0.5f * chunkSize;
            var zPos = y * cellSize + cellSize * 0.5f * chunkSize;
            transform.position = new Vector3(xPos, 0, zPos);
            foreach (var terrainCellView in _terrainCells)
            {
                var pos = ChunkPosition + terrainCellView.LocalPosition;
                var info = _terrainService.GetTerrainData(pos.X, pos.Y);
                terrainCellView.SetHeight(info.Height - _yOffset);
                terrainCellView.SetMaterial(uTerrainMaterials[info.Zone]);
                UpdateProps(terrainCellView, pos, info);
            }
        }

        private void UpdateProps(TerrainCellView terrainCellView, IntegerPoint position, TerrainInfo info)
        {
            switch (info.Prop)
            {
                case Prop.None:
                    break;
                case Prop.Water:
                    break;
                case Prop.Portal:
                    _chunkProps.Add(InstantiatePortal(terrainCellView, position));
                    break;
                case Prop.Floor:
                    terrainCellView.SetMaterial(uTerrainMaterials[8]);
                    break;
                case Prop.Wall:
                    terrainCellView.SetMaterial(uTerrainMaterials[9]);
                    terrainCellView.SetHeight(info.Height);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void InstantiateCells(TerrainProperties terrainProperties)
        {
            TrashOldObjects();
            var chunkSize = terrainProperties.ChunkSize;
            var cellSize = terrainProperties.CellSize;
            var cellCount = chunkSize * chunkSize;
            _terrainCells = new TerrainCellView[cellCount];
            for (var x = 0; x < chunkSize; x++)
            {
                for (var z = 0; z < chunkSize; z++)
                {
                    var xPos = cellSize * (x - 0.5f * chunkSize) + cellSize * 0.5f;
                    var zPos = cellSize * (z - 0.5f * chunkSize) + cellSize * 0.5f;
                    var mCell = Instantiate(uTerrainCell, new Vector3(xPos, 0, zPos), Quaternion.identity, transform);
                    var view = mCell.gameObject.AddComponent<TerrainCellView>();
                    view.LocalPosition = new IntegerPoint(x, z);
                    _terrainCells[x + z * chunkSize] = view;
                }
            }
        }

        private Transform InstantiatePortal(TerrainCellView terrainCellView, IntegerPoint position)
        {
            terrainCellView.SetMaterial(uTerrainMaterials[8]);
            var portalType = _gameStateController.IsClearedDungeonEntrance(position)
                ? uPortalClosed
                : uPortalActive;
            return Instantiate(portalType, terrainCellView.transform.position, Quaternion.identity, transform);
        }

        private void TrashOldObjects()
        {
            TrashChunkProps();
            if (_terrainCells == null) return;
            foreach (var terrainCell in _terrainCells)
                Destroy(terrainCell.gameObject);

            _terrainCells = null;
        }

        private void TrashChunkProps()
        {
            if (_chunkProps.Count == 0) return;
            foreach (var prop in _chunkProps)
                Destroy(prop.gameObject);

            _chunkProps.Clear();
        }
    }
}