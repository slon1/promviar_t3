using Unity.Mathematics;

namespace MaterialAccumulation
{
    /// <summary>
    /// Pure, Burst-compatible accumulation math (ADR-0001). No managed types, no
    /// allocations — safe to call both from AccumulationJob.Execute and directly
    /// from EditMode tests without scheduling a Job.
    /// </summary>
    public static class AccumulationMath
    {
        /// <summary>Squared point-to-segment distance in the XZ plane (avoids sqrt when only comparing).</summary>
        public static float PointToSegmentDistanceSquared(float2 point, float2 segmentStart, float2 segmentEnd)
        {
            float2 segment = segmentEnd - segmentStart;
            float lengthSq = math.lengthsq(segment);

            // Degenerate segment (prevPos == currPos, e.g. first frame or zone not moving):
            // fall back to point-to-point distance from segmentStart.
            float t = lengthSq > 1e-12f
                ? math.clamp(math.dot(point - segmentStart, segment) / lengthSq, 0f, 1f)
                : 0f;

            float2 closest = segmentStart + segment * t;
            return math.lengthsq(point - closest);
        }

        /// <summary>
        /// Dome cap height for a cell, anchored to the surface base plane (Y=0 in
        /// surface-local space — see ADR-0001), NOT to the current accumulated height.
        /// Returns 0 for distanceSquared >= radius^2 by construction (sqrt of a
        /// non-positive value clamped to 0).
        /// </summary>
        public static float EvaluateCapHeight(float radius, float distanceSquared)
        {
            float underSqrt = radius * radius - distanceSquared;
            return underSqrt > 0f ? math.sqrt(underSqrt) : 0f;
        }

        /// <summary>
        /// Monotonic height update: never decreases (outer max), grows at most by
        /// rate*deltaTime this call, never exceeds capHeight this call (inner min).
        /// </summary>
        public static float AccumulateHeight(float currentHeight, float capHeight, float rate, float deltaTime)
        {
            float grown = currentHeight + rate * deltaTime;
            float limited = math.min(grown, capHeight);
            return math.max(currentHeight, limited);
        }
    }
}
