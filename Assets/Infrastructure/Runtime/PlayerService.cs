using Application.Interfaces;
using UnityEngine;

namespace Infrastructure.Runtime
{
    public class PlayerService : IPlayerService
    {
        private Transform _playerCharacter;

        private Vector3 _markedLocation = Vector3.zero;

        public void SetPlayerTransform(Transform playerTransform)
        {
            _playerCharacter = playerTransform;
        }

        public void MarkLocation(Vector3 location)
        {
            _markedLocation = location;
        }

        public void SetAtMarkedLocation()
        {
            _playerCharacter.SetPositionAndRotation(_markedLocation, _playerCharacter.rotation);
        }

        public void SetActive(bool isActive)
        {
            _playerCharacter.gameObject.SetActive(isActive);
        }
    }
}