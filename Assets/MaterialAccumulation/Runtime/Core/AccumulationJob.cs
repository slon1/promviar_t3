using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MaterialAccumulation
{
    [BurstCompile]
    public struct AccumulationJob : IJobParallelFor
    {
        [ReadOnly] public float2 Origin;
        [ReadOnly] public float2 CellSize;
        [ReadOnly] public int Width;

        [ReadOnly] public float2 SegmentStart;
        [ReadOnly] public float2 SegmentEnd;
        [ReadOnly] public float Radius;
        [ReadOnly] public float Rate;
        [ReadOnly] public float DeltaTime;

        // Not [ReadOnly]: each Execute(index) reads/writes exactly Heights[index] —
        // the standard, safe IJobParallelFor pattern (disjoint indices, no aliasing).
        public NativeArray<float> Heights;

        public void Execute(int index)
        {
            int ix = index % Width;
            int iz = index / Width;
            float3 cellLocal3 = SurfaceGridMath.GridToLocalPosition(ix, iz, Origin, CellSize);
            var cellLocal = new float2(cellLocal3.x, cellLocal3.z);

            float distSq = AccumulationMath.PointToSegmentDistanceSquared(cellLocal, SegmentStart, SegmentEnd);
            float capHeight = AccumulationMath.EvaluateCapHeight(Radius, distSq);
            Heights[index] = AccumulationMath.AccumulateHeight(Heights[index], capHeight, Rate, DeltaTime);
        }
    }
}
