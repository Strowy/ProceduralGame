using UnityEngine;

namespace Application.Interfaces
{
    public interface IDungeonController
    {
        void SetGoalFlag(bool goalReached);
        void UpdateSeedLocation(Vector3 location);
    }
}