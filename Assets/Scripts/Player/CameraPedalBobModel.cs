using UnityEngine;

namespace Downhill.Player
{
    /// Small, camera-only lens shift driven by pedal activity. This stays
    /// pure so the camera controller only has to wire the offset to its pivot.
    [System.Serializable]
    public class CameraPedalBobModel
    {
        [Tooltip("Maximum vertical camera lens shift at full pedal power.")]
        public float verticalLensShiftAmplitude = 0.008f;

        [Tooltip("Pedal bob oscillation frequency while pedaling (cycles/second).")]
        public float frequencyHz = 1.35f;

        [Tooltip("How quickly the bob ramps in and out as pedal power changes.")]
        public float activityResponse = 4f;

        private float _phaseRadians;
        private float _activity;

        public Vector2 StepLensShift(float pedalPower01, float dt)
        {
            float targetActivity = Mathf.Clamp01(pedalPower01);
            float safeDt = Mathf.Max(0f, dt);
            float response = Mathf.Max(0f, activityResponse);

            if (safeDt <= 0f || response <= 0f)
            {
                _activity = targetActivity;
            }
            else
            {
                float t = 1f - Mathf.Exp(-response * safeDt);
                _activity = Mathf.Lerp(_activity, targetActivity, t);
            }

            if (_activity < 0.001f && targetActivity <= 0f)
            {
                _activity = 0f;
                return Vector2.zero;
            }

            _phaseRadians += Mathf.Max(0f, frequencyHz) * Mathf.PI * 2f * safeDt;
            float y = Mathf.Sin(_phaseRadians) * Mathf.Max(0f, verticalLensShiftAmplitude) * _activity;
            return new Vector2(0f, y);
        }

        public void Reset()
        {
            _phaseRadians = 0f;
            _activity = 0f;
        }
    }
}
