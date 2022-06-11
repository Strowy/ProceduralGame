using UnityEngine;

namespace Application.Interfaces
{
    public interface IPlayerService
    {
        void SetPlayerTransform(Transform playerTransform);
        void MarkLocation(Vector3 location);
        void SetAtMarkedLocation();
        void SetActive(bool isActive);
    }
}