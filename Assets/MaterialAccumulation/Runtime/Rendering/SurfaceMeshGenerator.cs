using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace MaterialAccumulation
{
    public static class SurfaceMeshGenerator
    {
        public static Mesh Generate(
            int width,
            int depth,
            float2 origin,
            float2 cellSize,
            string name = "MaterialSurface")
        {
            Validate(width, depth, cellSize);

            var mesh = new Mesh
            {
                name = name,
                hideFlags = HideFlags.DontSave,
                indexFormat = IndexFormat.UInt32
            };
            mesh.MarkDynamic();

            int vertexCount = width * depth;
            NativeArray<float3> vertices = new NativeArray<float3>(vertexCount, Allocator.Temp);
            NativeArray<float2> uvs = new NativeArray<float2>(vertexCount, Allocator.Temp);
            NativeArray<int> indices = new NativeArray<int>((width - 1) * (depth - 1) * 6, Allocator.Temp);
            try
            {
                SurfaceGridMath.WriteGridPositions(vertices, width, depth, origin, cellSize);

                float invWidth = 1f / (width - 1);
                float invDepth = 1f / (depth - 1);
                for (int iz = 0; iz < depth; iz++)
                {
                    for (int ix = 0; ix < width; ix++)
                    {
                        uvs[ix + iz * width] = new float2(ix * invWidth, iz * invDepth);
                    }
                }

                int triangle = 0;
                for (int iz = 0; iz < depth - 1; iz++)
                {
                    for (int ix = 0; ix < width - 1; ix++)
                    {
                        int i = ix + iz * width;
                        int w = width;
                        indices[triangle++] = i;
                        indices[triangle++] = i + w;
                        indices[triangle++] = i + 1;
                        indices[triangle++] = i + w;
                        indices[triangle++] = i + w + 1;
                        indices[triangle++] = i + 1;
                    }
                }

                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uvs);
                mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            }
            finally
            {
                if (vertices.IsCreated)
                {
                    vertices.Dispose();
                }

                if (uvs.IsCreated)
                {
                    uvs.Dispose();
                }

                if (indices.IsCreated)
                {
                    indices.Dispose();
                }
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void Validate(int width, int depth, float2 cellSize)
        {
            if (width < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be at least 2.");
            }

            if (depth < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(depth), depth, "Depth must be at least 2.");
            }

            if (cellSize.x <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize.x, "CellSize.x must be greater than 0.");
            }

            if (cellSize.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize.y, "CellSize.y must be greater than 0.");
            }
        }
    }
}
