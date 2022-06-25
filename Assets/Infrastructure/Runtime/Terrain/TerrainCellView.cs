using Domain;
using UnityEngine;

namespace Infrastructure.Runtime.Terrain
{
    public class TerrainCellView : MonoBehaviour
    {
        private MeshRenderer _renderer;
        private Transform _transform;

        public IntegerPoint LocalPosition { get; set; }

        public void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _transform = GetComponent<Transform>();
        }

        public void SetPosition(Vector3 position)
            => _transform.localPosition = position;

        public void SetMaterial(Material material)
            => _renderer.material = material;

        public void SetHeight(float height)
        {
            var currentPos = _transform.localPosition;
            _transform.localPosition = new Vector3(currentPos.x, height, currentPos.z);
        }
    }
}