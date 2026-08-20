using MaterialAccumulation;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public sealed class SurfaceNormalJobTests
{
    private const float Tolerance = 1e-4f;

    [Test]
    public void Execute_FlatHeightmap_AllNormalsPointUp()
    {
        using var heights = new NativeArray<float>(9, Allocator.Temp); // 3x3, all zero
        using var normals = new NativeArray<float3>(9, Allocator.Temp);

        var job = new SurfaceNormalJob
        {
            Heights = heights, CellSize = new float2(1f, 1f), Width = 3, Depth = 3, Normals = normals,
        };

        for (int i = 0; i < 9; i++)
        {
            job.Execute(i);
        }

        for (int i = 0; i < 9; i++)
        {
            Assert.That(normals[i].y, Is.EqualTo(1f).Within(Tolerance));
        }
    }

    [Test]
    public void Execute_EdgeCell_UsesSingleSidedSpacing_MatchesManualComputation()
    {
        // 3x3 grid, heights increase by 1 per +X step: h(ix,iz) = ix.
        // Fill via a helper: `using var` makes NativeArray readonly (CS1654).
        using var heights = CreateXRampHeights();
        using var normals = new NativeArray<float3>(9, Allocator.Temp);

        var job = new SurfaceNormalJob
        {
            Heights = heights, CellSize = new float2(1f, 1f), Width = 3, Depth = 3, Normals = normals,
        };
        for (int i = 0; i < 9; i++)
        {
            job.Execute(i);
        }

        // ix=0 (left edge), iz=1 (interior row): leftIx/rightIx clamp -> spacingX=1,
        // heightLeft=h(0)=0, heightRight=h(1)=1. Compare against SurfaceNormalMath
        // with those exact clamped inputs, not an independent re-derivation.
        float3 expected = SurfaceNormalMath.ComputeNormal(
            heightLeft: 0f, heightRight: 1f, heightDown: 0f, heightUp: 0f,
            spacingX: 1f, spacingZ: 1f);
        float3 actual = normals[0 + 1 * 3];

        Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance));
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance));
        Assert.That(actual.z, Is.EqualTo(expected.z).Within(Tolerance));
    }

    private static NativeArray<float> CreateXRampHeights()
    {
        var heights = new NativeArray<float>(9, Allocator.Temp);
        for (int iz = 0; iz < 3; iz++)
        {
            for (int ix = 0; ix < 3; ix++)
            {
                heights[ix + iz * 3] = ix;
            }
        }

        return heights;
    }
}
