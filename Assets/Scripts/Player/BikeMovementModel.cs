using UnityEngine;

namespace Downhill.Player
{
    public readonly struct BikeMovementResult
    {
        public BikeMovementResult(
            Vector3 velocity,
            Vector3 horizontalVelocity,
            Vector3 downhillDirection,
            float forwardSpeed,
            float lateralSpeed,
            float fallLineAlignmentDegrees)
        {
            Velocity = velocity;
            HorizontalVelocity = horizontalVelocity;
            DownhillDirection = downhillDirection;
            ForwardSpeed = forwardSpeed;
            LateralSpeed = lateralSpeed;
            FallLineAlignmentDegrees = fallLineAlignmentDegrees;
        }

        public Vector3 Velocity { get; }
        public Vector3 HorizontalVelocity { get; }
        public Vector3 DownhillDirection { get; }
        public float ForwardSpeed { get; }
        public float LateralSpeed { get; }
        public float FallLineAlignmentDegrees { get; }
    }

    /// Pure, tunable grounded movement math for the downhill bike (Ticket 2.1).
    /// No Unity components: given the current velocity, heading, ground normal,
    /// normalized pedal power, and dt, it returns the new world velocity. The
    /// caller (PlayerBikeController) decides grounded-ness and owns the Rigidbody.
    [System.Serializable]
    public class BikeMovementModel
    {
        [Tooltip("Horizontal grounded speed cap (m/s).")]
        public float maxSpeed = 10.88f;

        [Tooltip("Multiplier on the gravity-along-slope drive.")]
        public float slopeDriveGain = 0.272f;

        [Tooltip("Forward acceleration at full pedal power (m/s^2).")]
        public float pedalAccel = 2.56f;

        [Tooltip("Linear drag coefficient (per second).")]
        public float drag = 0.02f;

        [Tooltip("How quickly sideways slip damps back toward the bike's wheel track (per second).")]
        public float lateralGrip = 6f;

        [Tooltip("Gravity magnitude used for the slope term (m/s^2).")]
        public float gravity = 9.81f;

        [Tooltip("Maximum upward grounded velocity allowed when following terrain (m/s).")]
        public float maxGroundedUpSpeed = 0.75f;

        /// Returns the new world velocity. Assumes the caller has decided the bike is grounded.
        public Vector3 Step(Vector3 velocity, Vector3 facing, Vector3 groundNormal,
                            float pedalPower01, float dt)
        {
            return Step(velocity, facing, groundNormal, pedalPower01, 0f, dt);
        }

        public Vector3 Step(Vector3 velocity, Vector3 facing, Vector3 groundNormal,
                            float pedalPower01, float brakeDecel, float dt)
        {
            return StepDetailed(velocity, facing, groundNormal, pedalPower01, brakeDecel, dt).Velocity;
        }

        public BikeMovementResult StepDetailed(Vector3 velocity, Vector3 facing, Vector3 groundNormal,
                                               float pedalPower01, float brakeDecel, float dt)
        {
            Vector3 forwardFlat = Vector3.ProjectOnPlane(facing, Vector3.up);
            if (forwardFlat.sqrMagnitude < 1e-6f)
            {
                return MakeResult(velocity, Vector3.forward, groundNormal);
            }

            forwardFlat.Normalize();

            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            Vector3 downhillFlat = DownhillFlat(groundNormal);

            if (downhillFlat.sqrMagnitude > 1e-6f)
            {
                float slopeAngleRadians = Vector3.Angle(groundNormal, Vector3.up) * Mathf.Deg2Rad;
                Vector3 slopeAcceleration = downhillFlat
                    * (Mathf.Max(0f, gravity) * Mathf.Sin(slopeAngleRadians) * Mathf.Max(0f, slopeDriveGain));
                horizontalVelocity += slopeAcceleration * dt;
            }

            Vector3 pedalAcceleration = forwardFlat * (Mathf.Clamp01(pedalPower01) * Mathf.Max(0f, pedalAccel));
            horizontalVelocity += pedalAcceleration * dt;

            horizontalVelocity = ApplyBrake(horizontalVelocity, brakeDecel, dt);
            horizontalVelocity *= Mathf.Max(0f, 1f - (Mathf.Max(0f, drag) * dt));
            horizontalVelocity = ApplyLateralGrip(horizontalVelocity, forwardFlat, dt);

            float speed = horizontalVelocity.magnitude;
            if (speed > Mathf.Max(0f, maxSpeed))
            {
                horizontalVelocity = horizontalVelocity.normalized * Mathf.Max(0f, maxSpeed);
            }

            Vector3 result = FollowGroundPlane(horizontalVelocity, groundNormal);
            if (result.y > maxGroundedUpSpeed)
            {
                result.y = maxGroundedUpSpeed;
            }

            return MakeResult(result, forwardFlat, groundNormal);
        }

        private Vector3 ApplyLateralGrip(Vector3 horizontalVelocity, Vector3 forwardFlat, float dt)
        {
            Vector3 lateralVelocity = horizontalVelocity - (forwardFlat * Vector3.Dot(horizontalVelocity, forwardFlat));
            float damping = Mathf.Clamp01(Mathf.Max(0f, lateralGrip) * dt);
            return horizontalVelocity - (lateralVelocity * damping);
        }

        private static Vector3 ApplyBrake(Vector3 horizontalVelocity, float brakeDecel, float dt)
        {
            float speed = horizontalVelocity.magnitude;
            if (speed < 1e-6f)
            {
                return horizontalVelocity;
            }

            float newSpeed = Mathf.Max(0f, speed - (Mathf.Max(0f, brakeDecel) * dt));
            return horizontalVelocity.normalized * newSpeed;
        }

        private static Vector3 FollowGroundPlane(Vector3 horizontalVelocity, Vector3 groundNormal)
        {
            if (horizontalVelocity.sqrMagnitude < 1e-8f || Mathf.Abs(groundNormal.y) < 1e-4f)
            {
                return horizontalVelocity;
            }

            float verticalVelocity = -((groundNormal.x * horizontalVelocity.x) + (groundNormal.z * horizontalVelocity.z))
                / groundNormal.y;
            return horizontalVelocity + (Vector3.up * verticalVelocity);
        }

        private static BikeMovementResult MakeResult(Vector3 velocity, Vector3 forwardFlat, Vector3 groundNormal)
        {
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            Vector3 flatRight = Vector3.Cross(Vector3.up, forwardFlat).normalized;
            Vector3 downhillFlat = DownhillFlat(groundNormal);
            float fallLineAlignment = downhillFlat.sqrMagnitude > 1e-6f
                ? Vector3.SignedAngle(forwardFlat, downhillFlat, Vector3.up)
                : 0f;

            return new BikeMovementResult(
                velocity,
                horizontalVelocity,
                downhillFlat,
                Vector3.Dot(horizontalVelocity, forwardFlat),
                Vector3.Dot(horizontalVelocity, flatRight),
                fallLineAlignment);
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
