using System;
using MaterialAccumulation;
using NUnit.Framework;
using Unity.Mathematics;

public sealed class MaterialHeightmapTests
{
    [Test]
    public void Reset_ClearsAllHeights()
    {
        using var heightmap = CreateDefaultHeightmap();
        heightmap.SetHeight(0, 0, 1.5f);
        heightmap.SetHeight(1, 2, 0.25f);
        heightmap.SetHeight(3, 3, -0.1f);

        heightmap.Reset();

        for (int iz = 0; iz < heightmap.Depth; iz++)
        {
            for (int ix = 0; ix < heightmap.Width; ix++)
            {
                Assert.AreEqual(0f, heightmap.GetHeight(ix, iz));
            }
        }
    }

    [Test]
    public void SetHeight_Get_RoundTrips()
    {
        using var heightmap = CreateDefaultHeightmap();
        heightmap.SetHeight(1, 2, 0.75f);
        Assert.AreEqual(0.75f, heightmap.GetHeight(1, 2));
    }

    [TestCase(1, 4, 1f, 1f)]
    [TestCase(4, 1, 1f, 1f)]
    [TestCase(4, 4, 0f, 1f)]
    [TestCase(4, 4, 1f, 0f)]
    [TestCase(4, 4, -1f, 1f)]
    public void Constructor_Throws_OnInvalidSize(int width, int depth, float cellX, float cellY)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new MaterialHeightmap(width, depth, new float2(-1.5f, -1.5f), new float2(cellX, cellY)));
    }

    [Test]
    public void HeightsLength_MatchesWidthTimesDepth()
    {
        using var heightmap = CreateDefaultHeightmap();
        Assert.AreEqual(heightmap.Width * heightmap.Depth, heightmap.Heights.Length);
    }

    [Test]
    public void GridToLocal_MatchesOrigin()
    {
        using var heightmap = CreateDefaultHeightmap();
        float3 local = heightmap.GridToLocal(0, 0);
        Assert.AreEqual(heightmap.Origin.x, local.x);
        Assert.AreEqual(0f, local.y);
        Assert.AreEqual(heightmap.Origin.y, local.z);
    }

    [Test]
    public void GridToLocal_MatchesFarCorner()
    {
        using var heightmap = CreateDefaultHeightmap();
        float3 local = heightmap.GridToLocal(heightmap.Width - 1, heightmap.Depth - 1);
        Assert.AreEqual(heightmap.Origin.x + (heightmap.Width - 1) * heightmap.CellSize.x, local.x);
        Assert.AreEqual(0f, local.y);
        Assert.AreEqual(heightmap.Origin.y + (heightmap.Depth - 1) * heightmap.CellSize.y, local.z);
    }

    [Test]
    public void LocalToGrid_CenterMapsToMiddleIndex()
    {
        using var heightmap = CreateDefaultHeightmap();
        float2 grid = heightmap.LocalToGrid(float2.zero);
        float expected = (heightmap.Width - 1) * 0.5f;
        Assert.AreEqual(expected, grid.x, 0.0001f);
        Assert.AreEqual(expected, grid.y, 0.0001f);
    }

    [Test]
    public void LocalToGrid_IsUnclamped_NegativeOutsideOrigin()
    {
        using var heightmap = CreateDefaultHeightmap();
        float2 outside = heightmap.Origin - new float2(heightmap.CellSize.x, heightmap.CellSize.y);
        float2 grid = heightmap.LocalToGrid(outside);
        Assert.Less(grid.x, 0f);
        Assert.Less(grid.y, 0f);
    }

    [Test]
    public void LocalToGridClamped_ClampsOutOfBounds()
    {
        using var heightmap = CreateDefaultHeightmap();
        heightmap.LocalToGridClamped(new float2(-100f, -100f), out int ixMin, out int izMin);
        heightmap.LocalToGridClamped(new float2(100f, 100f), out int ixMax, out int izMax);

        Assert.AreEqual(0, ixMin);
        Assert.AreEqual(0, izMin);
        Assert.AreEqual(heightmap.Width - 1, ixMax);
        Assert.AreEqual(heightmap.Depth - 1, izMax);
    }

    [Test]
    public void IsInside_TrueInsideFalseOutside()
    {
        using var heightmap = CreateDefaultHeightmap();
        float2 farCorner = heightmap.Origin + new float2(
            (heightmap.Width - 1) * heightmap.CellSize.x,
            (heightmap.Depth - 1) * heightmap.CellSize.y);

        Assert.IsTrue(heightmap.IsInside(heightmap.Origin));
        Assert.IsTrue(heightmap.IsInside(farCorner));
        Assert.IsFalse(heightmap.IsInside(farCorner + new float2(heightmap.CellSize.x * 0.5f, 0f)));
    }

    [Test]
    public void Dispose_IsIdempotent()
    {
        var heightmap = CreateDefaultHeightmap();
        heightmap.Dispose();
        Assert.DoesNotThrow(heightmap.Dispose);
    }

    private static MaterialHeightmap CreateDefaultHeightmap()
    {
        return new MaterialHeightmap(4, 4, new float2(-1.5f, -1.5f), new float2(1f, 1f));
    }
}
