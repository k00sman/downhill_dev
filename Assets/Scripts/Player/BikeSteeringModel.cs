using UnityEngine;

namespace Downhill.Player
{
    /// Pure, tunable yaw steering math for the downhill bike (Ticket 3.1).
    /// The controller owns Rigidbody and Transform changes; this model only
    /// returns a bounded yaw delta for the current physics step.
    [System.Serializable]
    public class BikeSteeringModel
    {
        [Tooltip("Baseline low-to-medium speed turn rate (degrees/second).")]
        public float turnRateDegreesPerSecond = 100f;

        [Tooltip("Maximum yaw change allowed in one physics step (degrees).")]
        public float maxYawDeltaDegrees = 8f;

        [Tooltip("Minimum world speed before normal player steering and camber steering engage (m/s).")]
        public float minSpeedForSteering = 0.5f;

        [Tooltip("Absolute turn input below this value is ignored.")]
        public float turnDeadzone = 0.15f;

        [Tooltip("How much side-slope tilts the bike (turns away from steeper terrain) (degrees/second per unit tilt).")]
        public float slopeInfluence = 24f;

        [Tooltip("How quickly automatic side-slope steering reacts to terrain changes. Higher values snap faster.")]
        public float slopeSteerResponse = 6f;

        [Tooltip("Minimum horizontal travel speed before heading alignment uses actual velocity (m/s).")]
        public float velocityAlignmentMinSpeed = 0.5f;

        [Tooltip("Yaw rate used to align heading toward ground velocity (degrees/second).")]
        public float velocityAlignmentYawRateDegreesPerSecond = 45f;

        [Tooltip("Yaw rate used to align a near-stopped bike toward the downhill fall line (degrees/second).")]
        public float lowSpeedDownhillAlignmentYawRateDegreesPerSecond = 70f;

        [Tooltip("Minimum slope angle before low-speed downhill heading alignment engages.")]
        public float downhillAlignmentMinSlopeDegrees = 5f;

        [Tooltip("Heading alignment error below this angle is ignored.")]
        public float headingAlignmentDeadzoneDegrees = 3f;

        private float _smoothedSlopeSteer;

        public float StepYawDeltaDegrees(Vector3 currentForward, Vector3 velocity, float turnInput, float dt)
        {
            return StepYawDeltaDegrees(currentForward, velocity, turnInput, Vector3.up, dt);
        }

        public float StepYawDeltaDegrees(Vector3 currentForward, Vector3 velocity, float turnInput, Vector3 groundNormal, float dt)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(currentForward, Vector3.up);
            if (flatForward.sqrMagnitude < 1e-6f)
            {
                return 0f;
            }

            flatForward.Normalize();

            float input = Mathf.Clamp(turnInput, -1f, 1f);
            bool hasTurnInput = Mathf.Abs(input) > Mathf.Max(0f, turnDeadzone);
            if (!hasTurnInput)
            {
                input = 0f;
            }

            float horizontalSpeed = Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude;
            bool canSteerNormally = horizontalSpeed >= Mathf.Max(0f, minSpeedForSteering);

            Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward).normalized;
            float slopeRoll = Vector3.Dot(groundNormal, flatRight);

            // Slope steer: turn away from steeper terrain (towards lower terrain).
            // slopeRoll is negative when slope is higher to the right, positive when higher to the left.
            // So we add slopeRoll * slopeInfluence to turn towards lower terrain.
            float targetSlopeSteer = canSteerNormally ? slopeRoll * slopeInfluence : 0f;
            float slopeSteer = SmoothSlopeSteer(targetSlopeSteer, dt);
            float playerSteer = canSteerNormally ? input * Mathf.Max(0f, turnRateDegreesPerSecond) : 0f;
            float alignmentYaw = HeadingAlignmentYaw(flatForward, velocity, input, groundNormal, dt);

            float unclampedYaw = ((playerSteer + slopeSteer) * dt) + alignmentYaw;

            float maxDelta = Mathf.Max(0f, maxYawDeltaDegrees);
            return Mathf.Clamp(unclampedYaw, -maxDelta, maxDelta);
        }

        private float SmoothSlopeSteer(float targetSlopeSteer, float dt)
        {
            float response = Mathf.Max(0f, slopeSteerResponse);
            if (response <= 0f || dt <= 0f)
            {
                _smoothedSlopeSteer = targetSlopeSteer;
                return _smoothedSlopeSteer;
            }

            float t = 1f - Mathf.Exp(-response * dt);
            _smoothedSlopeSteer = Mathf.Lerp(_smoothedSlopeSteer, targetSlopeSteer, t);
            return _smoothedSlopeSteer;
        }

        private float HeadingAlignmentYaw(
            Vector3 flatForward,
            Vector3 velocity,
            float turnInput,
            Vector3 groundNormal,
            float dt)
        {
            Vector3 desiredDirection = Vector3.zero;
            float yawRate = 0f;

            Vector3 flatVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            if (flatVelocity.magnitude >= Mathf.Max(0f, velocityAlignmentMinSpeed))
            {
                desiredDirection = flatVelocity.normalized;
                yawRate = velocityAlignmentYawRateDegreesPerSecond;
            }
            else if (Vector3.Angle(groundNormal, Vector3.up) >= Mathf.Max(0f, downhillAlignmentMinSlopeDegrees))
            {
                desiredDirection = DownhillFlat(groundNormal);
                yawRate = lowSpeedDownhillAlignmentYawRateDegreesPerSecond;
            }

            if (desiredDirection.sqrMagnitude < 1e-6f)
            {
                return 0f;
            }

            float signedAngle = SignedAngleWithInputTieBreak(flatForward, desiredDirection, turnInput);
            if (Mathf.Abs(signedAngle) < Mathf.Max(0f, headingAlignmentDeadzoneDegrees))
            {
                return 0f;
            }

            float maxStep = Mathf.Max(0f, yawRate) * dt;
            return Mathf.Clamp(signedAngle, -maxStep, maxStep);
        }

        private static float SignedAngleWithInputTieBreak(Vector3 from, Vector3 to, float turnInput)
        {
            float signedAngle = Vector3.SignedAngle(from, to, Vector3.up);
            if (Mathf.Abs(Mathf.Abs(signedAngle) - 180f) < 0.1f && Mathf.Abs(turnInput) > 0.01f)
            {
                return Mathf.Sign(turnInput) * 180f;
            }

            return signedAngle;
        }

        private static Vector3 DownhillFlat(Vector3 groundNormal)
        {
            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, groundNormal);
            Vector3 downhillFlat = Vector3.ProjectOnPlane(downhill, Vector3.up);
            if (downhillFlat.sqrMagnitude < 1e-6f)
            {
                return Vector3.zero;
            }

            return downhillFlat.normalized;
        }
    }
}
