using UnityEngine;

namespace Downhill.Player
{
    public enum PedalSide
    {
        Left,
        Right,
    }

    /// Pure cadence evaluator for the left/right pedal mechanic. It consumes
    /// input edges and timestamps only; PlayerBikeController decides how the
    /// returned drive value feeds the bike's movement accumulator.
    [System.Serializable]
    public class PedalInputEvaluator
    {
        [Tooltip("Maximum time between opposite-side presses that counts as cadence.")]
        public float cadenceWindowSeconds = 0.45f;

        [Tooltip("Drive added by a first press or a press after the cadence window expires.")]
        public float basePressDrive = 0.15f;

        [Tooltip("Extra drive added when the rider alternates pedals within the cadence window.")]
        public float alternatingDriveBonus = 0.5f;

        [Tooltip("Drive added by repeated same-side presses within the cadence window.")]
        public float sameSideDrive = 0.03f;

        [Tooltip("When true, any pedal press gives fallbackDrive so testers can disable alternation quickly.")]
        public bool useSinglePedalFallback;

        [Tooltip("Drive added per press when single-pedal fallback is enabled.")]
        public float fallbackDrive = 0.35f;

        private bool _hasLastPress;
        private PedalSide _lastSide;
        private float _lastPressTimeSeconds;

        public float EvaluatePress(PedalSide side, float timeSeconds)
        {
            if (useSinglePedalFallback)
            {
                RecordPress(side, timeSeconds);
                return Mathf.Max(0f, fallbackDrive);
            }

            bool hasFreshPrevious = HasFreshPreviousPress(timeSeconds);
            bool alternates = hasFreshPrevious && side != _lastSide;
            bool repeatsSameSide = hasFreshPrevious && side == _lastSide;

            float drive;
            if (alternates)
            {
                drive = Mathf.Max(0f, basePressDrive) + Mathf.Max(0f, alternatingDriveBonus);
            }
            else if (repeatsSameSide)
            {
                drive = Mathf.Max(0f, sameSideDrive);
            }
            else
            {
                drive = Mathf.Max(0f, basePressDrive);
            }

            RecordPress(side, timeSeconds);
            return drive;
        }

        public void Reset()
        {
            _hasLastPress = false;
            _lastSide = default;
            _lastPressTimeSeconds = 0f;
        }

        private bool HasFreshPreviousPress(float timeSeconds)
        {
            if (!_hasLastPress)
            {
                return false;
            }

            float elapsed = timeSeconds - _lastPressTimeSeconds;
            return elapsed >= 0f && elapsed <= Mathf.Max(0f, cadenceWindowSeconds);
        }

        private void RecordPress(PedalSide side, float timeSeconds)
        {
            _hasLastPress = true;
            _lastSide = side;
            _lastPressTimeSeconds = timeSeconds;
        }
    }
}
