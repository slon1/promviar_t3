using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MaterialAccumulation
{
    [BurstCompile]
    public struct SurfaceNormalJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> Heights;
        [ReadOnly] public float2 CellSize;
        [ReadOnly] public int Width;
        [ReadOnly] public int Depth;

        [WriteOnly] public NativeArray<float3> Normals;

        public void Execute(int index)
        {
            int ix = index % Width;
            int iz = index / Width;

            // Clamp neighbor indices at grid edges instead of branching on boundary
            // cases: at an edge, left/rightIx (or down/upIz) collapse to the same value
            // as the center cell, which naturally turns the central difference into a
            // correct single-sided (forward/backward) difference once divided by the
            // resulting spacing below.
            int leftIx = math.max(ix - 1, 0);
            int rightIx = math.min(ix + 1, Width - 1);
            int downIz = math.max(iz - 1, 0);
            int upIz = math.min(iz + 1, Depth - 1);

            float heightLeft = Heights[leftIx + iz * Width];
            float heightRight = Heights[rightIx + iz * Width];
            float heightDown = Heights[ix + downIz * Width];
            float heightUp = Heights[ix + upIz * Width];

            // 2 cells in the interior, 1 cell at an edge (rightIx==leftIx never happens
            // because MaterialHeightmap guarantees Width/Depth >= 2).
            float spacingX = (rightIx - leftIx) * CellSize.x;
            float spacingZ = (upIz - downIz) * CellSize.y;

            Normals[index] = SurfaceNormalMath.ComputeNormal(
                heightLeft, heightRight, heightDown, heightUp, spacingX, spacingZ);
        }
    }
}
