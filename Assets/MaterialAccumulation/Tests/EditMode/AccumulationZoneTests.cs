using MaterialAccumulation;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

public sealed class AccumulationZoneTests
{
    private const float Tolerance = 1e-4f;

    private static AccumulationZone CreateZone(float2 start = default)
        => new AccumulationZone(start, new float2(-10f, -10f), new float2(10f, 10f));

    private static void AssertPosition(float2 actual, float2 expected)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance));
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance));
    }

    [Test]
    public void Tick_MovesPositionByInputTimesSpeedTimesDeltaTime()
    {
        var zone = CreateZone();
        zone.Tick(new float2(1f, 0f), moveSpeed: 2f, deltaTime: 0.5f,
            radiusBase: 1f, radiusAmplitude: 0f, radiusFrequency: 0f, radiusCurve: null);

        AssertPosition(zone.LocalPosition, new float2(1f, 0f));
    }

    [Test]
    public void Tick_ZeroInput_PositionUnchanged()
    {
        var zone = CreateZone(new float2(3f, -2f));
        zone.Tick(float2.zero, moveSpeed: 5f, deltaTime: 1f,
            radiusBase: 1f, radiusAmplitude: 0f, radiusFrequency: 0f, radiusCurve: null);

        AssertPosition(zone.LocalPosition, new float2(3f, -2f));
    }

    [Test]
    public void Tick_UpdatesPrevPositionBeforeMoving()
    {
        var zone = CreateZone(new float2(0f, 0f));
        zone.Tick(new float2(1f, 0f), moveSpeed: 1f, deltaTime: 1f,
            radiusBase: 1f, radiusAmplitude: 0f, radiusFrequency: 0f, radiusCurve: null);

        AssertPosition(zone.PrevLocalPosition, new float2(0f, 0f));
        AssertPosition(zone.LocalPosition, new float2(1f, 0f));
    }

    [Test]
    public void Tick_ClampsPositionToBounds()
    {
        var zone = CreateZone(new float2(9f, 9f));
        zone.Tick(new float2(1f, 1f), moveSpeed: 100f, deltaTime: 1f,
            radiusBase: 1f, radiusAmplitude: 0f, radiusFrequency: 0f, radiusCurve: null);

        AssertPosition(zone.LocalPosition, new float2(10f, 10f));
    }

    [Test]
    public void Tick_DiagonalInput_DoesNotMoveFasterThanAxisAligned()
    {
        var zone = CreateZone(float2.zero);
        zone.Tick(new float2(1f, 1f), moveSpeed: 1f, deltaTime: 1f,
            radiusBase: 1f, radiusAmplitude: 0f, radiusFrequency: 0f, radiusCurve: null);

        float length = math.length(zone.LocalPosition);
        Assert.That(length, Is.EqualTo(1f).Within(Tolerance));
    }

    [Test]
    public void Constructor_ClampsOutOfBoundsStartPosition()
    {
        var zone = CreateZone(new float2(50f, -50f));

        AssertPosition(zone.LocalPosition, new float2(10f, -10f));
        AssertPosition(zone.PrevLocalPosition, new float2(10f, -10f));
    }

    [Test]
    public void Constructor_DefaultRadius_IsMinRadius()
    {
        var zone = CreateZone();

        Assert.That(zone.Radius, Is.EqualTo(AccumulationZone.MinRadius).Within(Tolerance));
    }

    [Test]
    public void EvaluateRadius_ReturnsBasePlusAmplitudeTimesCurveAtPhase()
    {
        var curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        float radius = AccumulationZone.EvaluateRadius(
            baseRadius: 2f, amplitude: 4f, frequency: 1f, curve: curve, time: 0.25f);

        Assert.That(radius, Is.EqualTo(2f + 4f * 0.25f).Within(Tolerance));
    }

    [Test]
    public void EvaluateRadius_WrapsPhaseCyclically()
    {
        var curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        float radius = AccumulationZone.EvaluateRadius(
            baseRadius: 0f, amplitude: 1f, frequency: 2f, curve: curve, time: 0.625f);

        Assert.That(radius, Is.EqualTo(0.25f).Within(Tolerance));
    }

    [Test]
    public void EvaluateRadius_ClampsToMinimum()
    {
        var curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));

        float radius = AccumulationZone.EvaluateRadius(
            baseRadius: -5f, amplitude: 0f, frequency: 0f, curve: curve, time: 0f);

        Assert.That(radius, Is.EqualTo(AccumulationZone.MinRadius).Within(Tolerance));
    }

    [Test]
    public void EvaluateRadius_ZeroFrequency_IsStableAcrossTime_NoNaN()
    {
        var curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

        float radiusAtT0 = AccumulationZone.EvaluateRadius(
            baseRadius: 1f, amplitude: 2f, frequency: 0f, curve: curve, time: 0f);
        float radiusAtT10 = AccumulationZone.EvaluateRadius(
            baseRadius: 1f, amplitude: 2f, frequency: 0f, curve: curve, time: 10f);

        Assert.That(float.IsNaN(radiusAtT10), Is.False);
        Assert.That(radiusAtT10, Is.EqualTo(radiusAtT0).Within(Tolerance));
    }
}
