using Unity.Mathematics;

namespace MaterialAccumulation
{
    /// <summary>
    /// Finite-difference surface normal at a grid cell, from four neighboring heights
    /// and their sampling spacing. Caller (SurfaceNormalJob) is responsible for
    /// clamping neighbor indices at grid boundaries and passing the resulting spacing
    /// (1 cell at an edge, 2 cells in the interior) — this function only does the math.
    /// </summary>
    public static class SurfaceNormalMath
    {
        public static float3 ComputeNormal(
            float heightLeft, float heightRight,
            float heightDown, float heightUp,
            float spacingX, float spacingZ)
        {
            var tangentX = new float3(spacingX, heightRight - heightLeft, 0f);
            var tangentZ = new float3(0f, heightUp - heightDown, spacingZ);

            // cross(tangentZ, tangentX) gives +Y for a flat field — same winding/
            // orientation convention as SurfaceMeshGenerator's triangle winding (ADR-0003).
            return math.normalize(math.cross(tangentZ, tangentX));
        }
    }
}
