using MaterialAccumulation;
using NUnit.Framework;
using Unity.Mathematics;

public sealed class AccumulationMathTests
{
    private const float Tolerance = 1e-4f;

    [Test]
    public void PointToSegmentDistanceSquared_PointOnSegment_IsZero()
    {
        float distSq = AccumulationMath.PointToSegmentDistanceSquared(
            new float2(1f, 0f), new float2(0f, 0f), new float2(2f, 0f));

        Assert.That(distSq, Is.EqualTo(0f).Within(Tolerance));
    }

    [Test]
    public void PointToSegmentDistanceSquared_PointPerpendicularToMidpoint()
    {
        float distSq = AccumulationMath.PointToSegmentDistanceSquared(
            new float2(1f, 3f), new float2(0f, 0f), new float2(2f, 0f));

        Assert.That(distSq, Is.EqualTo(9f).Within(Tolerance));
    }

    [Test]
    public void PointToSegmentDistanceSquared_ClampsToSegmentEndpoints()
    {
        float distSq = AccumulationMath.PointToSegmentDistanceSquared(
            new float2(5f, 0f), new float2(0f, 0f), new float2(2f, 0f));

        Assert.That(distSq, Is.EqualTo(9f).Within(Tolerance));
    }

    [Test]
    public void PointToSegmentDistanceSquared_DegenerateSegment_FallsBackToPointDistance()
    {
        float distSq = AccumulationMath.PointToSegmentDistanceSquared(
            new float2(3f, 4f), new float2(0f, 0f), new float2(0f, 0f));

        Assert.That(distSq, Is.EqualTo(25f).Within(Tolerance));
    }

    [Test]
    public void EvaluateCapHeight_InsideRadius_ReturnsHemisphereHeight()
    {
        float cap = AccumulationMath.EvaluateCapHeight(radius: 5f, distanceSquared: 9f);

        Assert.That(cap, Is.EqualTo(4f).Within(Tolerance));
    }

    [Test]
    public void EvaluateCapHeight_OutsideRadius_IsZero()
    {
        float cap = AccumulationMath.EvaluateCapHeight(radius: 2f, distanceSquared: 100f);

        Assert.That(cap, Is.EqualTo(0f).Within(Tolerance));
    }

    [Test]
    public void EvaluateCapHeight_AtExactRadius_IsZero()
    {
        float cap = AccumulationMath.EvaluateCapHeight(radius: 3f, distanceSquared: 9f);

        Assert.That(cap, Is.EqualTo(0f).Within(Tolerance));
    }

    [Test]
    public void AccumulateHeight_GrowsByRateTimesDeltaTime_WhenBelowCap()
    {
        float result = AccumulationMath.AccumulateHeight(currentHeight: 0f, capHeight: 10f, rate: 2f, deltaTime: 0.5f);

        Assert.That(result, Is.EqualTo(1f).Within(Tolerance));
    }

    [Test]
    public void AccumulateHeight_ClampsToCapHeight_WhenGrowthWouldExceedIt()
    {
        float result = AccumulationMath.AccumulateHeight(currentHeight: 0.9f, capHeight: 1f, rate: 10f, deltaTime: 1f);

        Assert.That(result, Is.EqualTo(1f).Within(Tolerance));
    }

    [Test]
    public void AccumulateHeight_NeverDecreases_WhenCapHeightBelowCurrent()
    {
        float result = AccumulationMath.AccumulateHeight(currentHeight: 5f, capHeight: 0f, rate: 1f, deltaTime: 1f);

        Assert.That(result, Is.EqualTo(5f).Within(Tolerance));
    }

    [Test]
    public void AccumulateHeight_ZeroRate_NeverGrows()
    {
        float result = AccumulationMath.AccumulateHeight(currentHeight: 2f, capHeight: 10f, rate: 0f, deltaTime: 1f);

        Assert.That(result, Is.EqualTo(2f).Within(Tolerance));
    }
}
