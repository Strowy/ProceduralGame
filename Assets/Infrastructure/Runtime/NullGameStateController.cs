using Application.Interfaces;
using Domain;
using UnityEngine;

namespace Infrastructure.Runtime
{
    public class NullGameStateController : IGameStateController
    {
        public GameStatus Status { get; private set; } = GameStatus.Menu;

        public int Score { get; private set; } = 0;

        public void SetStatus(GameStatus newStatus)
        {
            Status = newStatus;
        }

        public void TriggerEventInstance(string triggerObjectName, PortalType portalType, Vector3 location)
        {
        }

        public bool IsClearedDungeonEntrance(IntegerPoint entrancePoint)
        {
            return false;
        }
    }
}