using Domain;
using UnityEngine;

namespace Application.Interfaces
{
    public interface IGameStateController
    {
        GameStatus Status { get; }
        int Score { get; }
        void SetStatus(GameStatus newStatus);
        void TriggerEventInstance(string triggerObjectName, PortalType portalType, Vector3 location);
        bool IsClearedDungeonEntrance(IntegerPoint entrancePoint);
    }

    public enum GameStatus
    {
        Menu,
        Initialise,
        InOverworld,
        InDungeon
    }

    public enum PortalType
    {
        Entrance,
        Exit,
    }
}