using Unity.Mathematics;
using UnityEngine;

namespace MaterialAccumulation
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class MaterialAccumulationController : MonoBehaviour
    {
        [Min(1), SerializeField] private int _resolutionX = 128;
        [Min(1), SerializeField] private int _resolutionZ = 128;

        [Tooltip("X = размер поверхности по мировой оси X, Y = размер по мировой оси Z (плоскость в XZ, не world-Y).")]
        [SerializeField] private Vector2 _planeSize = new Vector2(20f, 20f);

        [SerializeField] private Material _material;

        private MaterialHeightmap _heightmap;
        private SurfaceMeshSync _meshSync;
        private MeshFilter _meshFilter;

        public void SyncMesh()
        {
            if (_meshSync == null || _heightmap == null)
            {
                return;
            }

            _meshSync.SyncFrom(_heightmap);
        }

        private void Awake()
        {
            _resolutionX = math.max(1, _resolutionX);
            _resolutionZ = math.max(1, _resolutionZ);
            _planeSize.x = math.max(0.01f, _planeSize.x);
            _planeSize.y = math.max(0.01f, _planeSize.y);

            int width = _resolutionX + 1;
            int depth = _resolutionZ + 1;
            var origin = new float2(-_planeSize.x * 0.5f, -_planeSize.y * 0.5f);
            var cellSize = new float2(_planeSize.x / _resolutionX, _planeSize.y / _resolutionZ);

            _meshFilter = GetComponent<MeshFilter>();
            _heightmap = new MaterialHeightmap(width, depth, origin, cellSize);
            _meshSync = new SurfaceMeshSync(width, depth, origin, cellSize);
            _meshFilter.sharedMesh = _meshSync.Mesh;
            _meshSync.SyncFrom(_heightmap);

            if (_material != null)
            {
                GetComponent<MeshRenderer>().sharedMaterial = _material;
            }
        }

        private void OnDestroy()
        {
            if (_meshFilter != null)
            {
                _meshFilter.sharedMesh = null;
            }

            _meshSync?.Dispose();
            _meshSync = null;
            _heightmap?.Dispose();
            _heightmap = null;
        }
    }
}
