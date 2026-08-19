using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MaterialAccumulation
{
    /// <summary>
    /// Single source of truth for regular-grid cell positions in surface-local space.
    /// </summary>
    public static class SurfaceGridMath
    {
        public static float3 GridToLocalPosition(int ix, int iz, float2 origin, float2 cellSize, float height = 0f)
        {
            return new float3(origin.x + ix * cellSize.x, height, origin.y + iz * cellSize.y);
        }

        public static void WriteGridPositions(
            NativeArray<float3> buffer,
            int width,
            int depth,
            float2 origin,
            float2 cellSize)
        {
            Debug.Assert(buffer.Length == width * depth, "Grid position buffer length does not match width*depth.");

            for (int iz = 0; iz < depth; iz++)
            {
                for (int ix = 0; ix < width; ix++)
                {
                    buffer[ix + iz * width] = GridToLocalPosition(ix, iz, origin, cellSize);
                }
            }
        }
    }
}
