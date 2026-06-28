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

        [Tooltip("Minimum world speed before normal steering engages (m/s). Stopped uphill recovery turns can still engage.")]
        public float minSpeedForSteering = 0.5f;

        [Tooltip("Absolute turn input below this value is ignored.")]
        public float turnDeadzone = 0.15f;

        [Tooltip("How much side-slope tilts the bike (turns away from steeper terrain) (degrees/second per unit tilt).")]
        public float slopeInfluence = 30f;

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

            float speed = velocity.magnitude;
            bool canRecoverFromUphillStall = hasTurnInput && IsFacingUphill(flatForward, groundNormal);
            if (speed < Mathf.Max(0f, minSpeedForSteering) && !canRecoverFromUphillStall)
            {
                return 0f;
            }

            Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward).normalized;
            float slopeRoll = Vector3.Dot(groundNormal, flatRight);

            // Slope steer: turn away from steeper terrain (towards lower terrain).
            // slopeRoll is negative when slope is higher to the right, positive when higher to the left.
            // So we add slopeRoll * slopeInfluence to turn towards lower terrain.
            float slopeSteer = slopeRoll * slopeInfluence;

            float unclampedYaw = ((input * Mathf.Max(0f, turnRateDegreesPerSecond)) + slopeSteer) * dt;

            return Mathf.Clamp(unclampedYaw, -maxYawDeltaDegrees, maxYawDeltaDegrees);
        }

        private static bool IsFacingUphill(Vector3 flatForward, Vector3 groundNormal)
        {
            if (Mathf.Abs(groundNormal.y) < 1e-4f)
            {
                return false;
            }

            float verticalPerHorizontalMeter = -Vector3.Dot(groundNormal, flatForward) / groundNormal.y;
            return verticalPerHorizontalMeter > 0.01f;
        }
    }
}
