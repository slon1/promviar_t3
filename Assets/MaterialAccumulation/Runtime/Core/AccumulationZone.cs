using Unity.Mathematics;
using UnityEngine;

namespace MaterialAccumulation
{
    /// <summary>
    /// Pure state/logic for the material accumulation zone: position (surface-local,
    /// WASD-driven, clamped to surface bounds) and pulsating radius (AnimationCurve-driven).
    /// Knows nothing about Transform, Mesh, or Unity's global Time — all inputs are
    /// passed explicitly to keep this deterministic and unit-testable.
    /// </summary>
    public sealed class AccumulationZone
    {
        // Single source of truth for the radius floor. Keep the inspector attribute
        // [Min(AccumulationZone.MinRadius)] on MaterialAccumulationController._radiusBase
        // in sync with this value.
        public const float MinRadius = 0.01f;

        private readonly float2 _boundsMin;
        private readonly float2 _boundsMax;
        private float _elapsedTime;

        // Surface-local (XZ) position this frame / previous frame. PrevLocalPosition is
        // the pre-move position of the frame that just ran Tick — needed in M3 for the
        // swept-capsule accumulation segment (prevPos -> currPos).
        public float2 LocalPosition { get; private set; }
        public float2 PrevLocalPosition { get; private set; }
        public float Radius { get; private set; } = MinRadius;

        public AccumulationZone(float2 startLocalPosition, float2 boundsMin, float2 boundsMax)
        {
            _boundsMin = boundsMin;
            _boundsMax = boundsMax;
            LocalPosition = math.clamp(startLocalPosition, boundsMin, boundsMax);
            PrevLocalPosition = LocalPosition;
        }

        /// <summary>
        /// Advances position by <paramref name="moveInput"/> and re-evaluates the pulsating
        /// radius. Call once per frame regardless of Accumulate being held — the radius must
        /// keep animating even when nothing is being deposited (ADR-0001).
        /// </summary>
        public void Tick(
            float2 moveInput,
            float moveSpeed,
            float deltaTime,
            float radiusBase,
            float radiusAmplitude,
            float radiusFrequency,
            AnimationCurve radiusCurve)
        {
            PrevLocalPosition = LocalPosition;

            float inputLength = math.length(moveInput);
            float2 clampedInput = inputLength > 1f ? moveInput / inputLength : moveInput;

            float2 next = LocalPosition + clampedInput * moveSpeed * deltaTime;
            LocalPosition = math.clamp(next, _boundsMin, _boundsMax);

            _elapsedTime += deltaTime;
            Radius = EvaluateRadius(radiusBase, radiusAmplitude, radiusFrequency, radiusCurve, _elapsedTime);
        }

        /// <summary>
        /// base + amplitude * curve(frac(frequency * time)) — phase wraps into [0,1) so the
        /// curve loops indefinitely regardless of curve length/WrapMode settings.
        /// </summary>
        public static float EvaluateRadius(
            float baseRadius,
            float amplitude,
            float frequency,
            AnimationCurve curve,
            float time)
        {
            float phase = frequency * time;
            phase -= math.floor(phase);
            float curveValue = curve != null ? curve.Evaluate(phase) : 0f;
            return math.max(MinRadius, baseRadius + amplitude * curveValue);
        }
    }
}
