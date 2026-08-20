using MaterialAccumulation;
using NUnit.Framework;
using Unity.Mathematics;

public sealed class SurfaceNormalMathTests
{
    private const float Tolerance = 1e-4f;

    [Test]
    public void ComputeNormal_FlatHeights_PointsStraightUp()
    {
        float3 normal = SurfaceNormalMath.ComputeNormal(
            heightLeft: 1f, heightRight: 1f, heightDown: 1f, heightUp: 1f,
            spacingX: 1f, spacingZ: 1f);

        Assert.That(normal.x, Is.EqualTo(0f).Within(Tolerance));
        Assert.That(normal.y, Is.EqualTo(1f).Within(Tolerance));
        Assert.That(normal.z, Is.EqualTo(0f).Within(Tolerance));
    }

    [Test]
    public void ComputeNormal_IsUnitLength()
    {
        float3 normal = SurfaceNormalMath.ComputeNormal(
            heightLeft: 0f, heightRight: 2f, heightDown: 0.5f, heightUp: 1.5f,
            spacingX: 1f, spacingZ: 1f);

        Assert.That(math.length(normal), Is.EqualTo(1f).Within(Tolerance));
    }

    [Test]
    public void ComputeNormal_SlopeAlongX_TiltsTowardsLowerSide()
    {
        // Rising towards +X (heightRight > heightLeft) — the normal must tilt
        // towards -X (away from the uphill direction), same convention as a
        // standard heightmap normal.
        float3 normal = SurfaceNormalMath.ComputeNormal(
            heightLeft: 0f, heightRight: 2f, heightDown: 0f, heightUp: 0f,
            spacingX: 1f, spacingZ: 1f);

        Assert.That(normal.x, Is.LessThan(0f));
        Assert.That(normal.y, Is.GreaterThan(0f));
        Assert.That(normal.z, Is.EqualTo(0f).Within(Tolerance));
    }
}
