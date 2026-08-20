using MaterialAccumulation;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public sealed class AccumulationJobTests
{
    private const float Tolerance = 1e-4f;

    [Test]
    public void Execute_RaisesHeightsUnderSegment_LeavesFarCellsAtZero()
    {
        using var heights = new NativeArray<float>(25, Allocator.Temp);

        var job = new AccumulationJob
        {
            Origin = new float2(-2f, -2f),
            CellSize = new float2(1f, 1f),
            Width = 5,
            SegmentStart = new float2(0f, 0f),
            SegmentEnd = new float2(0f, 0f),
            Radius = 1.5f,
            Rate = 10f,
            DeltaTime = 1f,
            Heights = heights,
        };

        for (int i = 0; i < heights.Length; i++)
        {
            job.Execute(i);
        }

        int centerIndex = 2 + 2 * 5;
        Assert.That(heights[centerIndex], Is.GreaterThan(0f));

        int cornerIndex = 0 + 0 * 5;
        Assert.That(heights[cornerIndex], Is.EqualTo(0f).Within(Tolerance));
    }

    [Test]
    public void Execute_IsMonotonic_AcrossRepeatedCallsWithShrinkingRadius()
    {
        using var heights = new NativeArray<float>(1, Allocator.Temp);

        var bigRadiusJob = new AccumulationJob
        {
            Origin = float2.zero,
            CellSize = new float2(1f, 1f),
            Width = 1,
            SegmentStart = float2.zero,
            SegmentEnd = float2.zero,
            Radius = 5f,
            Rate = 100f,
            DeltaTime = 1f,
            Heights = heights,
        };
        bigRadiusJob.Execute(0);
        float afterBigRadius = heights[0];
        Assert.That(afterBigRadius, Is.GreaterThan(0f));

        var smallRadiusJob = bigRadiusJob;
        smallRadiusJob.Radius = 0.01f;
        smallRadiusJob.Execute(0);

        Assert.That(heights[0], Is.EqualTo(afterBigRadius).Within(Tolerance));
    }
}
