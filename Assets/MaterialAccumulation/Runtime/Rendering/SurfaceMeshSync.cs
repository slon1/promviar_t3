using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MaterialAccumulation
{
    /// <summary>
    /// Owns a procedural grid Mesh and a reusable vertex buffer; copies heightmap Y into vertices without per-frame allocations.
    /// </summary>
    public sealed class SurfaceMeshSync : IDisposable
    {
        private readonly int _width;
        private readonly int _depth;
        private readonly float2 _origin;
        private readonly float2 _cellSize;
        private NativeArray<float3> _vertexBuffer;
        private Mesh _mesh;

        public Mesh Mesh => _mesh;

        public SurfaceMeshSync(int width, int depth, float2 origin, float2 cellSize)
        {
            _width = width;
            _depth = depth;
            _origin = origin;
            _cellSize = cellSize;
            _mesh = SurfaceMeshGenerator.Generate(width, depth, origin, cellSize);
            _vertexBuffer = new NativeArray<float3>(width * depth, Allocator.Persistent);
            SurfaceGridMath.WriteGridPositions(_vertexBuffer, width, depth, origin, cellSize);
        }

        public void SyncFrom(MaterialHeightmap heightmap)
        {
            Debug.Assert(heightmap != null, "Heightmap is null.");
            Debug.Assert(
                heightmap.Width == _width
                && heightmap.Depth == _depth
                && heightmap.Origin.Equals(_origin)
                && heightmap.CellSize.Equals(_cellSize),
                "MaterialHeightmap layout does not match SurfaceMeshSync layout");

            NativeArray<float> heights = heightmap.Heights;
            for (int i = 0; i < _vertexBuffer.Length; i++)
            {
                float3 vertex = _vertexBuffer[i];
                vertex.y = heights[i];
                _vertexBuffer[i] = vertex;
            }

            _mesh.SetVertices(_vertexBuffer);
        }

        public void Dispose()
        {
            if (_mesh != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(_mesh);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(_mesh);
                }

                _mesh = null;
            }

            if (_vertexBuffer.IsCreated)
            {
                _vertexBuffer.Dispose();
                _vertexBuffer = default;
            }
        }
    }
}
