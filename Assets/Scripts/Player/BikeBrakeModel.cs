using UnityEngine;

namespace Downhill.Player
{
    /// Pure, tunable split braking math for the downhill bike (Ticket 1.5).
    [System.Serializable]
    public class BikeBrakeModel
    {
        [Tooltip("Maximum deceleration for the front brake (m/s^2).")]
        public float frontBrakePower = 12f;

        [Tooltip("Maximum deceleration for the rear brake (m/s^2).")]
        public float rearBrakePower = 8f;

        /// Returns the total braking deceleration force (m/s^2) for the given normalized inputs.
        public float GetTotalBrakeDecel(float frontInput, float rearInput)
        {
            float front = Mathf.Clamp01(frontInput) * Mathf.Max(0f, frontBrakePower);
            float rear = Mathf.Clamp01(rearInput) * Mathf.Max(0f, rearBrakePower);
            return front + rear;
        }
    }
}
