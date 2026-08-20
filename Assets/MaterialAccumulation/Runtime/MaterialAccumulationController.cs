using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace MaterialAccumulation
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class MaterialAccumulationController : MonoBehaviour
    {
        private const float ZoneMarkerHeight = 0.02f;

        [Min(1), SerializeField] private int _resolutionX = 128;
        [Min(1), SerializeField] private int _resolutionZ = 128;

        [Tooltip("X = размер поверхности по мировой оси X, Y = размер по мировой оси Z (плоскость в XZ, не world-Y).")]
        [SerializeField] private Vector2 _planeSize = new Vector2(20f, 20f);

        [SerializeField] private Material _material;

        [Header("Movement")]
        [Min(0f), SerializeField] private float _moveSpeed = 3f;

        [Header("Radius")]
        [Min(AccumulationZone.MinRadius), SerializeField] private float _radiusBase = 1.5f;
        [SerializeField] private float _radiusAmplitude = 0.75f;
        [Min(0f), SerializeField] private float _radiusFrequency = 0.25f;
        [SerializeField] private AnimationCurve _radiusCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

        [Header("Zone Marker (Game View)")]
        [SerializeField] private Color _zoneMarkerColor = new Color(1f, 0.6f, 0.1f, 0.9f);
        [Min(0.001f), SerializeField] private float _zoneMarkerWidth = 0.05f;
        [Min(8), SerializeField] private int _zoneMarkerSegments = 32;

        [Header("Accumulation")]
        [Min(0f), SerializeField] private float _accumulationRate = 0.5f;

        private MaterialHeightmap _heightmap;
        private SurfaceMeshSync _meshSync;
        private MeshFilter _meshFilter;
        private MaterialAccumulationActions _actions;
        private AccumulationZone _zone;
        private LineRenderer _zoneMarker;
        private Material _zoneMarkerMaterial;

        public void SyncMesh()
        {
            if (_meshSync == null || _heightmap == null)
            {
                return;
            }

            _meshSync.SyncFrom(_heightmap);
        }

        public void ResetSurface()
        {
            if (_heightmap == null)
            {
                return;
            }

            _heightmap.Reset();
            SyncMesh();
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

            var boundsMin = _heightmap.Origin;
            var boundsMax = _heightmap.Origin + new float2(
                (_heightmap.Width - 1) * _heightmap.CellSize.x,
                (_heightmap.Depth - 1) * _heightmap.CellSize.y);
            _zone = new AccumulationZone(float2.zero, boundsMin, boundsMax);
            _zone.Tick(float2.zero, 0f, 0f, _radiusBase, _radiusAmplitude, _radiusFrequency, _radiusCurve);

            _zoneMarker = CreateZoneMarker();
            UpdateZoneMarker();
        }

        private void OnEnable()
        {
            _actions = new MaterialAccumulationActions();
            _actions.Enable();
        }

        private void OnDisable()
        {
            _actions?.Disable();
            _actions?.Dispose();
            _actions = null;
        }

        private void Update()
        {
            if (_zone == null || _actions == null)
            {
                return;
            }

            Vector2 moveInput = _actions.Gameplay.Move.ReadValue<Vector2>();
            _zone.Tick(
                new float2(moveInput.x, moveInput.y),
                _moveSpeed,
                Time.deltaTime,
                _radiusBase,
                _radiusAmplitude,
                _radiusFrequency,
                _radiusCurve);

            UpdateZoneMarker();

            if (_actions.Gameplay.Reset.WasPressedThisFrame())
            {
                ResetSurface();
            }

            if (_heightmap != null && _actions.Gameplay.Accumulate.IsPressed())
            {
                var job = new AccumulationJob
                {
                    Origin = _heightmap.Origin,
                    CellSize = _heightmap.CellSize,
                    Width = _heightmap.Width,
                    SegmentStart = _zone.PrevLocalPosition,
                    SegmentEnd = _zone.LocalPosition,
                    Radius = _zone.Radius,
                    Rate = _accumulationRate,
                    DeltaTime = Time.deltaTime,
                    Heights = _heightmap.Heights,
                };
                job.Schedule(_heightmap.Heights.Length, 64).Complete();

                SyncMesh();
            }
        }

        private void OnDestroy()
        {
            DestroyZoneMarker();

            if (_meshFilter != null)
            {
                _meshFilter.sharedMesh = null;
            }

            _meshSync?.Dispose();
            _meshSync = null;
            _heightmap?.Dispose();
            _heightmap = null;
        }

        private void OnDrawGizmos()
        {
            if (_zone == null)
            {
                return;
            }

            Gizmos.color = _zoneMarkerColor;
            Vector3 worldCenter = transform.TransformPoint(
                new Vector3(_zone.LocalPosition.x, ZoneMarkerHeight, _zone.LocalPosition.y));
            DrawWireCircleXZ(worldCenter, _zone.Radius, segments: 32);
        }

        private LineRenderer CreateZoneMarker()
        {
            var markerObject = new GameObject("ZoneMarker") { hideFlags = HideFlags.DontSave };
            markerObject.transform.SetParent(transform, worldPositionStays: false);

            var line = markerObject.AddComponent<LineRenderer>();
            line.loop = true;
            line.useWorldSpace = true;
            line.positionCount = math.max(8, _zoneMarkerSegments);
            line.widthMultiplier = _zoneMarkerWidth;
            _zoneMarkerMaterial = new Material(ResolveZoneMarkerShader()) { hideFlags = HideFlags.DontSave };
            line.sharedMaterial = _zoneMarkerMaterial;
            line.startColor = _zoneMarkerColor;
            line.endColor = _zoneMarkerColor;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private void UpdateZoneMarker()
        {
            if (_zoneMarker == null || _zone == null)
            {
                return;
            }

            Vector3 worldCenter = transform.TransformPoint(
                new Vector3(_zone.LocalPosition.x, ZoneMarkerHeight, _zone.LocalPosition.y));
            float radius = _zone.Radius;
            int segments = _zoneMarker.positionCount;
            float step = Mathf.PI * 2f / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = step * i;
                Vector3 point = worldCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                _zoneMarker.SetPosition(i, point);
            }
        }

        private void DestroyZoneMarker()
        {
            if (_zoneMarker == null && _zoneMarkerMaterial == null)
            {
                return;
            }

            GameObject markerObject = _zoneMarker != null ? _zoneMarker.gameObject : null;
            _zoneMarker = null;

            if (Application.isPlaying)
            {
                if (_zoneMarkerMaterial != null)
                {
                    Destroy(_zoneMarkerMaterial);
                }

                if (markerObject != null)
                {
                    Destroy(markerObject);
                }
            }
            else
            {
                if (_zoneMarkerMaterial != null)
                {
                    DestroyImmediate(_zoneMarkerMaterial);
                }

                if (markerObject != null)
                {
                    DestroyImmediate(markerObject);
                }
            }

            _zoneMarkerMaterial = null;
        }

        private static Shader ResolveZoneMarkerShader()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                return shader;
            }

            return Shader.Find("Hidden/Internal-Colored");
        }

        private static void DrawWireCircleXZ(Vector3 center, float radius, int segments)
        {
            float step = Mathf.PI * 2f / segments;
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = step * i;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
