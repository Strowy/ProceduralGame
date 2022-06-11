using System;
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
        public GameStatus Status { get; private set; }

        public int Score { get; private set; }

        // Player
        public Transform player;
        // Terrain Controller
        public Transform terrainController;
        // Dungeon Controller
        public Transform dungeonController;
        // Replacement object once portal is used
        public Transform deadPortal;

        private Vector3 _lastEnteredDungeonEntrance = Vector3.zero;

        private List<IntegerPoint> _clearedDungeonEntrances;

        private IPlayerService _playerService;

        public void Inject(IPlayerService playerService)
        {
            _playerService = playerService;
            _playerService.SetPlayerTransform(player);
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
                    _lastEnteredDungeonEntrance = location;

                    // Portal breaks once used
                    Transform portal = GameObject.Find(triggerObjectName).transform;
                    Instantiate(deadPortal, location, Quaternion.identity, portal.parent);
                    Destroy(portal.gameObject);


                    // Hide terrain (so it doesn't have to re-generate later as there's no change)
                    terrainController.gameObject.SetActive(false);

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

        private void ReturnToOverworld()
        {
            _playerService.SetActive(false);
            var entranceLocation = new IntegerPoint(
                (int) _lastEnteredDungeonEntrance.x,
                (int) _lastEnteredDungeonEntrance.z);
            _clearedDungeonEntrances.Add(entranceLocation);
            Score += 1;
            terrainController.gameObject.SetActive(true);
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
            // Activate player
            _playerService.SetActive(true);
            // Ensure activation of the environment controllers
            terrainController.gameObject.SetActive(true);
            dungeonController.gameObject.SetActive(true);
            Status = GameStatus.InOverworld;
        }
    }
}
