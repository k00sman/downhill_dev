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

        [Tooltip("Minimum forward speed before steering engages (m/s).")]
        public float minSpeedForSteering = 0.5f;

        [Tooltip("Absolute turn input below this value is ignored.")]
        public float turnDeadzone = 0.15f;

        public float StepYawDeltaDegrees(Vector3 currentForward, Vector3 velocity, float turnInput, float dt)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(currentForward, Vector3.up);
            if (flatForward.sqrMagnitude < 1e-6f)
            {
                return 0f;
            }

            flatForward.Normalize();

            float forwardSpeed = Vector3.Dot(velocity, flatForward);
            if (forwardSpeed < Mathf.Max(0f, minSpeedForSteering))
            {
                return 0f;
            }

            float input = Mathf.Clamp(turnInput, -1f, 1f);
            if (Mathf.Abs(input) <= Mathf.Max(0f, turnDeadzone))
            {
                input = 0f;
            }

            float unclampedYaw = input * Mathf.Max(0f, turnRateDegreesPerSecond) * dt;

            return Mathf.Clamp(unclampedYaw, -maxYawDeltaDegrees, maxYawDeltaDegrees);
        }
    }
}
