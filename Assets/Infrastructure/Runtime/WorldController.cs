using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AIR.Flume;
using Application.Interfaces;
using Domain;
using UnityEngine;

namespace Infrastructure.Runtime
{
    public class WorldController : DependentBehaviour, IGameStateController
    {
        private Terrain.TerrainController _terrainController;
        private IntegerPoint _cachedChunk = IntegerPoint.Zero;

        public GameStatus Status { get; private set; }

        public int Score { get; private set; }

        // Player
        public Transform player;
        // Dungeon Controller
        public Transform dungeonController;

        private IntegerPoint _lastEnteredDungeonEntrance = IntegerPoint.Zero;

        private List<IntegerPoint> _clearedDungeonEntrances;

        private IPlayerService _playerService;
        private IPropertiesService _propertiesService;

        public void Inject(
            IPlayerService playerService,
            IPropertiesService propertiesService)
        {
            _playerService = playerService;
            _playerService.SetPlayerTransform(player);
            _propertiesService = propertiesService;
        }


        public void SetStatus(GameStatus newStatus)
        {
            Status = newStatus;
            if (newStatus != GameStatus.Initialise)
                return;

            Initialise();
        }

        public void TriggerEventInstance(string triggerObjectName, PortalType portalType, Vector3 location)
        {
            switch (portalType)
            {
                case PortalType.Entrance:
                    // Dungeon entrance triggered in overworld
                    Status = GameStatus.InDungeon;
                    _lastEnteredDungeonEntrance = ConvertToPoint(location);

                    // Hide terrain (so it doesn't have to re-generate later as there's no change)
                    _cachedChunk = _terrainController.CurrentChunk;
                    _terrainController.gameObject.SetActive(false);

                    // Trigger dungeon generation
                    dungeonController.GetComponent<DungeonController>().SetSeedLocation(location);
                    dungeonController.GetComponent<DungeonController>().SetGoalFlag(true);
                    break;
                case PortalType.Exit:
                    // Dungeon exit portal on deepest floor triggered in dungeon
                    Status = GameStatus.InOverworld;
                    ReturnToOverworld();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(portalType), portalType, null);
            }
        }

        private IntegerPoint ConvertToPoint(Vector3 location)
        {
            var cellSize = _propertiesService.TerrainProperties.CellSize;
            return new IntegerPoint(Mathf.FloorToInt(location.x / cellSize), Mathf.FloorToInt(location.z / cellSize));
        }

        private void ReturnToOverworld()
        {
            _playerService.SetActive(false);
            _clearedDungeonEntrances.Add(_lastEnteredDungeonEntrance);
            Score += 1;
            _terrainController.gameObject.SetActive(true);
            _terrainController.ForceUpdate(_cachedChunk);
            _playerService.SetAtMarkedLocation();
            _playerService.SetActive(true);
        }

        public bool IsClearedDungeonEntrance(IntegerPoint entrancePoint)
        {
            return _clearedDungeonEntrances.Any(location => entrancePoint == location);
        }

        // Start is called before the first frame update
        void Start()
        {
            _clearedDungeonEntrances = new List<IntegerPoint>();
            Status = GameStatus.Menu;
        }

        private void Initialise()
        {
            dungeonController.gameObject.SetActive(true);
            _terrainController = FindObjectOfType<Terrain.TerrainController>();
            _terrainController.Initialise();
            StartCoroutine(StartUpCycle());
        }

        private IEnumerator StartUpCycle()
        {
            yield return new WaitUntil(() => _terrainController.IsInitialised);
            _terrainController.TrySpawnPlayer();
            Status = GameStatus.InOverworld;
        }
    }
}
