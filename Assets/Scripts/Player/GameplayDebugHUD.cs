using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Downhill.Player
{
    /// On-screen debug readout for balancing movement, health, and monster distance (Ticket 1.7).
    /// Uses Canvas-based UI to ensure visibility over retro shader blits.
    public class GameplayDebugHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerBikeController _player;

        [Header("Settings")]
        [SerializeField] private bool _showHUD = true;

        private Canvas _canvas;
        private Text _text;
        private GameObject _backgroundPanel;
        private RectTransform _panelRect;

        private void Start()
        {
            ResolvePlayer();
            CreateRuntimeHUD();
            UpdateVisibility();
        }

        private void ResolvePlayer()
        {
            if (_player != null)
            {
                return;
            }

            _player = FindFirstObjectByType<PlayerBikeController>();
        }

        private void CreateRuntimeHUD()
        {
            // 1. Create Canvas GameObject
            GameObject canvasObj = new("DebugHUD_Canvas");
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999; // Ensure it's on top of everything

            // Add CanvasScaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // 2. Create Background Panel
            _backgroundPanel = new GameObject("BackgroundPanel");
            _backgroundPanel.transform.SetParent(canvasObj.transform, false);

            Image panelImage = _backgroundPanel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.75f); // 75% dark black

            _panelRect = _backgroundPanel.GetComponent<RectTransform>();
            _panelRect.anchorMin = new Vector2(0f, 1f); // Top-Left anchor
            _panelRect.anchorMax = new Vector2(0f, 1f);
            _panelRect.pivot = new Vector2(0f, 1f);
            _panelRect.anchoredPosition = new Vector2(15f, -15f);

            // 3. Create Text GameObject
            GameObject textObj = new("DebugText");
            textObj.transform.SetParent(_backgroundPanel.transform, false);

            _text = textObj.AddComponent<Text>();

            // Safe, exception-proof font loading
            try
            {
                _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameplayDebugHUD] Legacy font loading threw: {ex.Message}");
            }

            if (_text.font == null)
            {
                Font[] projectFonts = Resources.FindObjectsOfTypeAll<Font>();
                if (projectFonts != null && projectFonts.Length > 0)
                {
                    _text.font = projectFonts[0];
                }
            }

            _text.fontSize = 14;
            _text.lineSpacing = 1.2f;
            _text.color = Color.green; // High readability green
            _text.alignment = TextAnchor.UpperLeft;
            _text.supportRichText = true;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-20f, -20f); // 10px padding
            textRect.anchoredPosition = Vector2.zero;

            // Parent to Player so it cleans up when player is destroyed
            canvasObj.transform.SetParent(transform);
        }

        private void OnDestroy()
        {
            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
            }
        }

        private void Update()
        {
            // Toggle HUD using New Input System direct access for 100% reliability
            if (Keyboard.current != null)
            {
                if (Keyboard.current[Key.F3].wasPressedThisFrame || Keyboard.current[Key.Backquote].wasPressedThisFrame)
                {
                    _showHUD = !_showHUD;
                    UpdateVisibility();
                }
            }

            UpdateHUDText();
        }

        private void UpdateVisibility()
        {
            if (_panelRect == null)
            {
                return;
            }

            if (_showHUD)
            {
                _panelRect.sizeDelta = new Vector2(320f, 250f);
            }
            else
            {
                _panelRect.sizeDelta = new Vector2(175f, 35f);
            }
        }

        private void UpdateHUDText()
        {
            if (_text == null)
            {
                return;
            }

            if (!_showHUD)
            {
                _text.text = " <b>[F3]</b> Show Debug HUD";
                return;
            }

            if (_player == null)
            {
                ResolvePlayer();
            }

            if (_player == null)
            {
                _text.text = "<b>DOWNHILL PROTOTYPE DEBUG</b>\nNo PlayerBikeController found!";
                return;
            }

            // Calculations
            float speedMS = _player.Body != null ? _player.Body.linearVelocity.magnitude : 0f;
            float speedKmh = speedMS * 3.6f;
            bool isGrounded = _player.IsGrounded;
            string stateStr = _player.State.ToString();
            float health = 100f; // placeholder for health (Sprint 4)
            float pedalPower = _player.PedalPower;
            Vector3 norm = _player.GroundNormal;

            // Input reads
            float turnInput = 0f;
            float frontBrake = 0f;
            float rearBrake = 0f;
            if (_player.Input != null)
            {
                turnInput = _player.Input.Turn;
                frontBrake = _player.Input.FrontBrake;
                rearBrake = _player.Input.RearBrake;
            }

            // Human readable turn directions
            string turnDirection = "STRAIGHT";
            if (turnInput < -0.15f)
            {
                turnDirection = $"LEFT ({Mathf.Abs(turnInput) * 100f:F0}%)";
            }
            else if (turnInput > 0.15f)
            {
                turnDirection = $"RIGHT ({turnInput * 100f:F0}%)";
            }

            // Brake formatting
            string frontBrakeStr = frontBrake > 0.05f ? $"<color=red>ON ({frontBrake * 100f:F0}%)</color>" : "OFF";
            string rearBrakeStr = rearBrake > 0.05f ? $"<color=red>ON ({rearBrake * 100f:F0}%)</color>" : "OFF";

            // Check monster distance safely by name to avoid undefined tag exceptions
            string monsterDistStr = "N/A (No Pursuit)";
            GameObject monster = GameObject.Find("Monster");
            if (monster == null)
            {
                monster = GameObject.Find("PFB_Monster");
            }

            if (monster != null)
            {
                float dist = Vector3.Distance(_player.transform.position, monster.transform.position);
                monsterDistStr = $"{dist:F1} m";
            }

            _text.text = $"<b>DOWNHILL PROTOTYPE DEBUG</b> (F3 to hide)\n" +
                         $"---------------------------------------------\n" +
                         $"<b>[BIKE STATUS]</b>\n" +
                         $"Speed: {speedMS:F1} m/s ({speedKmh:F1} km/h)\n" +
                         $"State: {stateStr}\n" +
                         $"Grounded: {(isGrounded ? "<color=green>YES</color>" : "<color=red>NO</color>")}\n" +
                         $"Normal: ({norm.x:F2}, {norm.y:F2}, {norm.z:F2})\n" +
                         $"Pedal Power: {pedalPower * 100f:F0}%\n\n" +
                         $"<b>[INPUTS]</b>\n" +
                         $"Turn: {turnDirection}\n" +
                         $"Front Brake [W]: {frontBrakeStr}\n" +
                         $"Rear Brake [S]: {rearBrakeStr}\n\n" +
                         $"<b>[SURVIVAL]</b>\n" +
                         $"Health: {health:F0}/100 | Monster: {monsterDistStr}";
        }
    }
}
