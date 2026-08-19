using System;
using MaterialAccumulation;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

public sealed class SurfaceMeshGeneratorTests
{
    [Test]
    public void Generate_VertexLayout_MatchesGrid()
    {
        var origin = new float2(-1.5f, -1.5f);
        var cellSize = new float2(1f, 1f);
        Mesh mesh = SurfaceMeshGenerator.Generate(4, 4, origin, cellSize);
        try
        {
            Assert.AreEqual(16, mesh.vertexCount);

            Vector3[] vertices = mesh.vertices;
            Assert.AreEqual(new Vector3(origin.x, 0f, origin.y), vertices[0]);
            Assert.AreEqual(new Vector3(origin.x + 3f, 0f, origin.y + 3f), vertices[15]);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mesh);
        }
    }

    [Test]
    public void Generate_Throws_OnInvalidSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SurfaceMeshGenerator.Generate(1, 4, float2.zero, new float2(1f, 1f)));
    }
}
