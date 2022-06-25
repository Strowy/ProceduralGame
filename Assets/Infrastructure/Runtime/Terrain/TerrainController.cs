using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AIR.Flume;
using Application.Interfaces;
using Domain;
using UnityEngine;

namespace Infrastructure.Runtime.Terrain
{
    public class TerrainController : DependentBehaviour
    {
        [SerializeField] private TerrainChunkView uTerrainChunkViewPrefab;
        [SerializeField] private int uRenderRange = 4;

        private readonly Queue<TerrainChunkView> _pooledChunks = new Queue<TerrainChunkView>();
        private readonly List<TerrainChunkView> _activeChunks = new List<TerrainChunkView>();

        private IntegerPoint _lastChunk;

        private IPlayerService _playerService;
        private IGameStateController _gameStateController;
        private IPropertiesService _propertiesService;
        private ITerrainService _terrainService;

        public bool IsInitialised { get; private set; } = false;
        public bool IsSpawned { get; private set; } = false;
        public IntegerPoint CurrentChunk => GetPlayerChunk();

        public void Inject(
            IPlayerService playerService,
            IGameStateController gameStateController,
            IPropertiesService propertiesService,
            ITerrainService terrainService)
        {
            _playerService = playerService;
            _gameStateController = gameStateController;
            _propertiesService = propertiesService;
            _terrainService = terrainService;
        }

        public void Initialise()
        {
            StartCoroutine(InitialisationCoroutine());
        }

        public bool TrySpawnPlayer()
        {
            if (!IsInitialised)
                return false;

            DisplayStarterChunks();
            SetPlayerAtZero();
            return true;
        }

        public void Update()
        {
            if (!IsInitialised || !IsSpawned)
                return;

            var currentChunk = GetPlayerChunk();
            if (currentChunk == _lastChunk)
                return;

            _lastChunk = currentChunk;
            UpdateSurrounding(_lastChunk);
        }

        public void ForceUpdate(IntegerPoint chunk)
        {
            ForceUpdateChunk(chunk.X, chunk.Y);
        }

        public void OnDrawGizmos()
        {
            if (!UnityEngine.Application.isPlaying) return;
            if (!IsSpawned) return;
            var tp = _propertiesService.TerrainProperties;
            var realSize = tp.CellSize * tp.ChunkSize;
            var chunk = GetPlayerChunk();
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(
                new Vector3(
                    chunk.X * realSize + realSize / 2,
                    10,
                    chunk.Y * realSize + realSize / 2),
                Vector3.one * realSize);
        }

        private IntegerPoint GetPlayerChunk()
        {
            var tp = _propertiesService.TerrainProperties;
            var realSize = tp.CellSize * tp.ChunkSize;
            var position = _playerService.Position;
            var dx = Mathf.FloorToInt(position.x / realSize);
            var dz = Mathf.FloorToInt(position.z / realSize);
            return new IntegerPoint(dx, dz);
        }

        private void SetPlayerAtZero()
        {
            var posX = 0.5f * _propertiesService.TerrainProperties.CellSize;
            var posZ = posX;
            var posY = _terrainService.GetTerrainData(0, 0).Height + 4;
            _playerService.WarpToLocation(new Vector3(posX, posY, posZ));
            _playerService.SetActive(true);
            _lastChunk = GetPlayerChunk();
            IsSpawned = true;
        }

        private void UpdateSurrounding(IntegerPoint chunkIndex)
        {
            for (var x = -uRenderRange; x < uRenderRange; x++)
            {
                for (var y = -uRenderRange; y < uRenderRange; y++)
                {
                    TryUpdateChunk(chunkIndex.X + x, chunkIndex.Y + y);
                }
            }

            var distantChunks = _activeChunks.Where(c => DistantFromPlayerChunk(c.ChunkPosition));
            foreach (var chunk in distantChunks)
            {
                chunk.gameObject.SetActive(false);
                _pooledChunks.Enqueue(chunk);
            }

            _activeChunks.RemoveAll(c => DistantFromPlayerChunk(c.ChunkPosition));
        }

        private void TryUpdateChunk(int x, int y)
        {
            var chunkSize = _propertiesService.TerrainProperties.ChunkSize;
            var testPosition = new IntegerPoint(x * chunkSize, y * chunkSize);
            if (_activeChunks.Any(c => c.ChunkPosition == testPosition))
                return;

            var chunk = _pooledChunks.Dequeue();
            chunk.gameObject.SetActive(true);
            chunk.UpdateAtPosition(testPosition.X, testPosition.Y);
            _activeChunks.Add(chunk);
        }

        private void ForceUpdateChunk(int x, int y)
        {
            var chunkSize = _propertiesService.TerrainProperties.ChunkSize;
            var testPosition = new IntegerPoint(x * chunkSize, y * chunkSize);
            var chunk = _activeChunks.FirstOrDefault(c => c.ChunkPosition == testPosition);
            if (chunk == default)
            {
                chunk = _pooledChunks.Dequeue();
                chunk.gameObject.SetActive(true);
                _activeChunks.Add(chunk);
            }

            chunk.UpdateAtPosition(testPosition.X, testPosition.Y);
        }

        private bool DistantFromPlayerChunk(IntegerPoint position)
        {
            var chunkSize = _propertiesService.TerrainProperties.ChunkSize;
            var deltaX = Math.Abs(_lastChunk.X * chunkSize - position.X);
            var deltaY = Math.Abs(_lastChunk.Y * chunkSize - position.Y);
            return (deltaX + deltaY) > (uRenderRange * 2 + 1) * chunkSize;
        }

        private void DisplayStarterChunks()
        {
            var chunkSize = _propertiesService.TerrainProperties.ChunkSize;

            for (var x = -uRenderRange; x < uRenderRange; x++)
            {
                for (var y = -uRenderRange; y < uRenderRange; y++)
                {
                    var chunk = _pooledChunks.Dequeue();
                    chunk.gameObject.SetActive(true);
                    chunk.UpdateAtPosition(x * chunkSize, y * chunkSize);
                    _activeChunks.Add(chunk);
                }
            }
        }

        private IEnumerator InitialisationCoroutine()
        {
            var sideLength = 2 * uRenderRange + 1;
            for (var i = 0; i < sideLength * sideLength * 2; i++)
            {
                var terrainChunk = Instantiate(uTerrainChunkViewPrefab, transform);
                terrainChunk.Initialise();
                terrainChunk.gameObject.SetActive(false);
                _pooledChunks.Enqueue(terrainChunk);
                yield return null;
            }

            IsInitialised = true;
        }
    }
}