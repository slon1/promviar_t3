using MaterialAccumulation;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public sealed class AccumulationScenarioTests
{
    private const float Tolerance = 1e-4f;
    private const float FrameDt = 1f / 60f;

    [Test]
    public void RepeatedTicks_StationaryZone_HeightsNeverDecrease()
    {
        using var heightmap = CreateGrid();
        var segment = float2.zero;
        const float radius = 2f;
        const float rate = 1f;

        for (int frame = 0; frame < 8; frame++)
        {
            using var previous = CopyHeights(heightmap);
            RunJob(heightmap, segment, segment, radius, rate, FrameDt);

            NativeArray<float> heights = heightmap.Heights;
            for (int i = 0; i < heights.Length; i++)
            {
                Assert.That(heights[i], Is.GreaterThanOrEqualTo(previous[i]));
            }

            if (frame == 0)
            {
                heightmap.LocalToGridClamped(float2.zero, out int cx, out int cz);
                float center = heightmap.GetHeight(cx, cz);
                float3 cell = heightmap.GridToLocal(cx, cz);
                float cap = AccumulationMath.EvaluateCapHeight(
                    radius,
                    AccumulationMath.PointToSegmentDistanceSquared(
                        new float2(cell.x, cell.z), segment, segment));
                Assert.That(center, Is.GreaterThan(0f));
                Assert.That(center, Is.LessThan(cap));
            }
        }

        heightmap.LocalToGridClamped(float2.zero, out int ix, out int iz);
        Assert.That(heightmap.GetHeight(ix, iz), Is.GreaterThan(0f));
    }

    [Test]
    public void HeightsStayWhenZoneLeavesCell()
    {
        using var heightmap = CreateGrid();
        var deposit = float2.zero;
        var farAway = new float2(4f, 4f);

        for (int frame = 0; frame < 6; frame++)
        {
            RunJob(heightmap, deposit, deposit, radius: 2f, rate: 1f, FrameDt);
        }

        heightmap.LocalToGridClamped(deposit, out int cx, out int cz);
        float deposited = heightmap.GetHeight(cx, cz);
        Assert.That(deposited, Is.GreaterThan(0f));

        for (int frame = 0; frame < 6; frame++)
        {
            RunJob(heightmap, farAway, farAway, radius: 1.5f, rate: 1f, FrameDt);
        }

        Assert.That(heightmap.GetHeight(cx, cz), Is.EqualTo(deposited).Within(Tolerance));
    }

    [Test]
    public void IntersectingPasses_SumInSharedHeightmap()
    {
        using var heightmap = CreateGrid();
        var passAStart = new float2(-3f, -1f);
        var passAEnd = new float2(3f, -1f);
        var passBStart = new float2(-3f, 1f);
        var passBEnd = new float2(3f, 1f);
        const float radius = 1.5f;

        for (int frame = 0; frame < 8; frame++)
        {
            RunJob(heightmap, passAStart, passAEnd, radius, rate: 1f, FrameDt);
        }

        heightmap.LocalToGridClamped(new float2(0f, -1f), out int uniqueAIx, out int uniqueAIz);
        heightmap.LocalToGridClamped(new float2(0f, 1f), out int uniqueBIx, out int uniqueBIz);
        heightmap.LocalToGridClamped(float2.zero, out int overlapIx, out int overlapIz);

        float uniqueAAfterFirst = heightmap.GetHeight(uniqueAIx, uniqueAIz);
        float uniqueBAfterFirst = heightmap.GetHeight(uniqueBIx, uniqueBIz);
        float overlapAfterFirst = heightmap.GetHeight(overlapIx, overlapIz);

        Assert.That(uniqueAAfterFirst, Is.GreaterThan(0f));
        Assert.That(uniqueBAfterFirst, Is.EqualTo(0f).Within(Tolerance));
        Assert.That(overlapAfterFirst, Is.GreaterThan(0f));

        for (int frame = 0; frame < 8; frame++)
        {
            RunJob(heightmap, passBStart, passBEnd, radius, rate: 1f, FrameDt);
        }

        Assert.That(heightmap.GetHeight(uniqueAIx, uniqueAIz), Is.EqualTo(uniqueAAfterFirst).Within(Tolerance));
        Assert.That(heightmap.GetHeight(uniqueBIx, uniqueBIz), Is.GreaterThan(0f));
        Assert.That(heightmap.GetHeight(overlapIx, overlapIz), Is.GreaterThanOrEqualTo(overlapAfterFirst));
    }

    [Test]
    public void LongSegment_CoversMidpointNotOnlyEndpoints()
    {
        using var heightmap = CreateGrid();
        var start = new float2(-4f, 0f);
        var end = new float2(4f, 0f);

        RunJob(heightmap, start, end, radius: 0.6f, rate: 10f, deltaTime: 1f);

        heightmap.LocalToGridClamped(float2.zero, out int midIx, out int midIz);
        Assert.That(heightmap.GetHeight(midIx, midIz), Is.GreaterThan(0f));

        heightmap.LocalToGridClamped(new float2(0f, 4f), out int farIx, out int farIz);
        Assert.That(heightmap.GetHeight(farIx, farIz), Is.EqualTo(0f).Within(Tolerance));
    }

    [Test]
    public void ZoneOverhangingCorner_WritesOnlyInGrid()
    {
        using var heightmap = CreateGrid();
        var corner = new float2(-4f, -4f);

        Assert.DoesNotThrow(() =>
            RunJob(heightmap, corner, corner, radius: 2f, rate: 10f, deltaTime: 1f));

        heightmap.LocalToGridClamped(corner, out int cornerIx, out int cornerIz);
        Assert.That(heightmap.GetHeight(cornerIx, cornerIz), Is.GreaterThan(0f));

        heightmap.LocalToGridClamped(new float2(-3f, -4f), out int edgeIx, out int edgeIz);
        Assert.That(heightmap.GetHeight(edgeIx, edgeIz), Is.GreaterThan(0f));

        heightmap.LocalToGridClamped(new float2(4f, 4f), out int farIx, out int farIz);
        Assert.That(heightmap.GetHeight(farIx, farIz), Is.EqualTo(0f).Within(Tolerance));
    }

    [Test]
    public void Reset_ClearsHistory_NextTickStartsFromZero()
    {
        using var heightmap = CreateGrid();
        var segment = float2.zero;
        const float radius = 2f;
        const float rate = 1f;
        const int framesBeforeReset = 8;

        for (int frame = 0; frame < framesBeforeReset; frame++)
        {
            RunJob(heightmap, segment, segment, radius, rate, FrameDt);
        }

        heightmap.LocalToGridClamped(segment, out int cx, out int cz);
        float beforeReset = heightmap.GetHeight(cx, cz);
        Assert.That(beforeReset, Is.GreaterThan(0f));

        heightmap.Reset();
        Assert.That(heightmap.GetHeight(cx, cz), Is.EqualTo(0f).Within(Tolerance));

        RunJob(heightmap, segment, segment, radius, rate, FrameDt);
        float afterResetOneTick = heightmap.GetHeight(cx, cz);

        Assert.That(afterResetOneTick, Is.GreaterThan(0f));
        Assert.That(afterResetOneTick, Is.LessThan(beforeReset));
        Assert.That(afterResetOneTick, Is.EqualTo(rate * FrameDt).Within(Tolerance));
    }

    private static MaterialHeightmap CreateGrid()
    {
        return new MaterialHeightmap(9, 9, new float2(-4f, -4f), new float2(1f, 1f));
    }

    private static void RunJob(
        MaterialHeightmap heightmap,
        float2 segmentStart,
        float2 segmentEnd,
        float radius,
        float rate,
        float deltaTime)
    {
        var job = new AccumulationJob
        {
            Origin = heightmap.Origin,
            CellSize = heightmap.CellSize,
            Width = heightmap.Width,
            SegmentStart = segmentStart,
            SegmentEnd = segmentEnd,
            Radius = radius,
            Rate = rate,
            DeltaTime = deltaTime,
            Heights = heightmap.Heights,
        };

        NativeArray<float> heights = heightmap.Heights;
        for (int i = 0; i < heights.Length; i++)
        {
            job.Execute(i);
        }
    }

    private static NativeArray<float> CopyHeights(MaterialHeightmap heightmap)
    {
        var copy = new NativeArray<float>(heightmap.Heights.Length, Allocator.Temp);
        copy.CopyFrom(heightmap.Heights);
        return copy;
    }
}
