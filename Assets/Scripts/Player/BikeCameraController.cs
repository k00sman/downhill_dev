using System.IO;
using Downhill.Input;
using UnityEngine;

namespace Downhill.Player
{
    /// First-person helmet/handlebar camera controller with smooth follow and freelook (Ticket 1.6).
    public class BikeCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _targetPivot;
        [SerializeField] private PlayerInputReader _input;

        [Header("Follow Settings")]
        [SerializeField] private float _positionLerpSpeed = 30f;
        [SerializeField] private float _rotationLerpSpeed = 15f;

        [Header("Freelook Settings")]
        [SerializeField] private float _lookSensitivity = 1.5f; // Decreased default sensitivity for smooth mouse look
        [SerializeField] private float _minPitch = -60f;
        [SerializeField] private float _maxPitch = 60f;
        [SerializeField] private float _minYaw = -90f;
        [SerializeField] private float _maxYaw = 90f;

        private float _freelookYaw;
        private float _freelookPitch;

        [System.Serializable]
        private class CameraConfig
        {
            public float lookSensitivity = 1.5f;
        }

        public Quaternion FreelookRotation => Quaternion.Euler(_freelookPitch, _freelookYaw, 0f);

        private void Start()
        {
            LoadConfig();

            if (_targetPivot == null || _input == null)
            {
                PlayerBikeController controller = FindFirstObjectByType<PlayerBikeController>();
                if (controller != null)
                {
                    if (_targetPivot == null)
                    {
                        _targetPivot = controller.CameraPivot;
                    }

                    if (_input == null)
                    {
                        _input = controller.Input;
                    }
                }
            }

            // Lock and hide cursor for a proper gameplay feel
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LoadConfig()
        {
            string path = Path.Combine(Application.dataPath, "../config.json");
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    CameraConfig config = JsonUtility.FromJson<CameraConfig>(json);
                    if (config != null)
                    {
                        _lookSensitivity = config.lookSensitivity;
                        Debug.Log($"[BikeCameraController] Loaded lookSensitivity: {_lookSensitivity} from config.json");
                    }
                }
                else
                {
                    CameraConfig defaultConfig = new() { lookSensitivity = _lookSensitivity };
                    string json = JsonUtility.ToJson(defaultConfig, true);
                    File.WriteAllText(path, json);
                    Debug.Log($"[BikeCameraController] Created default config.json at {path}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BikeCameraController] Failed to load config.json: {ex.Message}");
            }
        }

        private void LateUpdate()
        {
            if (_targetPivot == null)
            {
                return;
            }

            // Position follow (smoothly interpolate to target pivot position)
            transform.position = Vector3.Lerp(transform.position, _targetPivot.position, _positionLerpSpeed * Time.deltaTime);

            // Handle freelook input
            Vector2 lookInput = _input != null ? _input.Freelook : Vector2.zero;

            if (lookInput.sqrMagnitude > 1e-4f)
            {
                // Accumulate freelook based on sensitivity and dt
                _freelookYaw += lookInput.x * _lookSensitivity * Time.deltaTime * 10f;
                _freelookPitch -= lookInput.y * _lookSensitivity * Time.deltaTime * 10f;

                _freelookYaw = Mathf.Clamp(_freelookYaw, _minYaw, _maxYaw);
                _freelookPitch = Mathf.Clamp(_freelookPitch, _minPitch, _maxPitch);
            }

            // Combine base bike orientation with freelook offsets
            Quaternion baseRotation = _targetPivot.rotation;
            Quaternion targetRotation = baseRotation * FreelookRotation;

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationLerpSpeed * Time.deltaTime);
        }
    }
}
