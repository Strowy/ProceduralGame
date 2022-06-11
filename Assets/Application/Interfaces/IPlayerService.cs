using UnityEngine;

namespace Application.Interfaces
{
    public interface IPlayerService
    {
        Vector3 Position { get; }
        void SetPlayerTransform(Transform playerTransform);
        void MarkLocation(Vector3 location);
        void SetAtMarkedLocation();
        void SetActive(bool isActive);
        void WarpToLocation(Vector3 location);
    }
}