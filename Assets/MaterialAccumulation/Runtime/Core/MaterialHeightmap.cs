using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MaterialAccumulation
{
    /// <summary>
    /// Independent heightmap state in surface-local coordinates. Does not know about Mesh or Transform.
    /// </summary>
    public sealed class MaterialHeightmap : IDisposable
    {
        private NativeArray<float> _heights;

        public int Width { get; }
        public int Depth { get; }
        public float2 Origin { get; }
        public float2 CellSize { get; }

        public NativeArray<float> Heights => _heights;

        public MaterialHeightmap(int width, int depth, float2 origin, float2 cellSize)
        {
            if (width < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be at least 2.");
            }

            if (depth < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(depth), depth, "Depth must be at least 2.");
            }

            if (cellSize.x <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize.x, "CellSize.x must be greater than 0.");
            }

            if (cellSize.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize.y, "CellSize.y must be greater than 0.");
            }

            Width = width;
            Depth = depth;
            Origin = origin;
            CellSize = cellSize;
            _heights = new NativeArray<float>(width * depth, Allocator.Persistent);
        }

        public int Index(int ix, int iz)
        {
            Debug.Assert((uint)ix < (uint)Width && (uint)iz < (uint)Depth, "Grid index is out of range.");
            return ix + iz * Width;
        }

        public float GetHeight(int ix, int iz)
        {
            return _heights[Index(ix, iz)];
        }

        public void SetHeight(int ix, int iz, float value)
        {
            _heights[Index(ix, iz)] = value;
        }

        public float3 GridToLocal(int ix, int iz)
        {
            return SurfaceGridMath.GridToLocalPosition(ix, iz, Origin, CellSize);
        }

        public float2 LocalToGrid(float2 localXZ)
        {
            return (localXZ - Origin) / CellSize;
        }

        public void LocalToGridClamped(float2 localXZ, out int ix, out int iz)
        {
            float2 grid = LocalToGrid(localXZ);
            ix = math.clamp((int)math.round(grid.x), 0, Width - 1);
            iz = math.clamp((int)math.round(grid.y), 0, Depth - 1);
        }

        public bool IsInside(float2 localXZ)
        {
            float2 max = Origin + new float2((Width - 1) * CellSize.x, (Depth - 1) * CellSize.y);
            return localXZ.x >= Origin.x && localXZ.x <= max.x
                && localXZ.y >= Origin.y && localXZ.y <= max.y;
        }

        public void Reset()
        {
            if (!_heights.IsCreated)
            {
                return;
            }

            for (int i = 0; i < _heights.Length; i++)
            {
                _heights[i] = 0f;
            }
        }

        public void Dispose()
        {
            if (!_heights.IsCreated)
            {
                return;
            }

            _heights.Dispose();
            _heights = default;
        }
    }
}
