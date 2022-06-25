using Application.Interfaces;
using UnityEngine;

namespace Infrastructure.Runtime
{
    public class PlayerService : IPlayerService
    {
        private Transform _playerCharacter;

        private Vector3 _markedLocation = Vector3.zero;

        public Vector3 Position => _playerCharacter.position;

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
            Physics.SyncTransforms();
        }

        public void SetActive(bool isActive)
        {
            _playerCharacter.gameObject.SetActive(isActive);
        }

        public void WarpToLocation(Vector3 location)
        {
            _playerCharacter.SetPositionAndRotation(location, _playerCharacter.rotation);
            Physics.SyncTransforms();
        }
    }
}