using UnityEngine;

namespace Downhill.Player
{
    /// Pure, tunable forward-movement math for the downhill bike (Ticket 2.1).
    /// No Unity components: given the current velocity, heading, ground normal,
    /// normalized pedal power, and dt, it returns the new world velocity. The
    /// caller (PlayerBikeController) decides grounded-ness and owns the Rigidbody.
    [System.Serializable]
    public class BikeMovementModel
    {
        [Tooltip("Forward speed cap (m/s).")]
        public float maxSpeed = 16f;

        [Tooltip("Multiplier on the gravity-along-slope drive.")]
        public float slopeDriveGain = 0.4f;

        [Tooltip("Forward acceleration at full pedal power (m/s^2).")]
        public float pedalAccel = 6.4f;

        [Tooltip("Linear drag coefficient (per second).")]
        public float drag = 0.02f;

        [Tooltip("Gravity magnitude used for the slope term (m/s^2).")]
        public float gravity = 9.81f;

        [Tooltip("Maximum upward grounded velocity allowed when following terrain (m/s).")]
        public float maxGroundedUpSpeed = 0.75f;

        /// Returns the new world velocity. Assumes the caller has decided the bike
        /// is grounded; works in 1-D along the horizontal heading so there is no
        /// sideways slide while steering does not exist yet.
        public Vector3 Step(Vector3 velocity, Vector3 facing, Vector3 groundNormal,
                            float pedalPower01, float dt)
        {
            return Step(velocity, facing, groundNormal, pedalPower01, 0f, dt);
        }

        public Vector3 Step(Vector3 velocity, Vector3 facing, Vector3 groundNormal,
                            float pedalPower01, float brakeDecel, float dt)
        {
            Vector3 forwardFlat = Vector3.ProjectOnPlane(facing, Vector3.up);
            if (forwardFlat.sqrMagnitude < 1e-6f)
            {
                return velocity; // heading vertical — avoid NaN
            }

            forwardFlat.Normalize();

            Vector3 forwardOnSlope = ProjectHeadingOntoGroundPitch(forwardFlat, groundNormal);
            if (forwardOnSlope.sqrMagnitude < 1e-6f)
            {
                return velocity; // heading parallel to the normal — avoid NaN
            }

            forwardOnSlope.Normalize();

            Vector3 gravityVec = Vector3.down * gravity;
            float slopeAccel = Vector3.Dot(
                Vector3.ProjectOnPlane(gravityVec, groundNormal), forwardOnSlope)
                * slopeDriveGain;

            float pedalAccelTerm = Mathf.Clamp01(pedalPower01) * pedalAccel;

            float speed = Vector3.Dot(velocity, forwardFlat);
            speed += (slopeAccel + pedalAccelTerm) * dt;

            // Apply braking deceleration
            speed -= Mathf.Max(0f, brakeDecel) * dt;

            speed -= speed * drag * dt;
            speed = Mathf.Clamp(speed, 0f, maxSpeed);

            float horizontalComponent = Vector3.Dot(forwardOnSlope, forwardFlat);
            if (horizontalComponent < 0.1f)
            {
                return forwardFlat * speed;
            }

            Vector3 result = forwardOnSlope * (speed / horizontalComponent);
            if (result.y > maxGroundedUpSpeed)
            {
                result.y = maxGroundedUpSpeed;
            }

            return result;
        }

        private static Vector3 ProjectHeadingOntoGroundPitch(Vector3 forwardFlat, Vector3 groundNormal)
        {
            if (Mathf.Abs(groundNormal.y) < 1e-4f)
            {
                return forwardFlat;
            }

            float verticalPerHorizontalMeter = -Vector3.Dot(groundNormal, forwardFlat) / groundNormal.y;
            return forwardFlat + (Vector3.up * verticalPerHorizontalMeter);
        }
    }
}
