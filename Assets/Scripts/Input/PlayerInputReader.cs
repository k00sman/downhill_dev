using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

namespace Downhill.Input
{
    /// Thin wrapper over the generated DownhillControls asset. Gameplay reads input
    /// through this component and never touches raw keys or InputActions directly.
    public class PlayerInputReader : MonoBehaviour
    {
        private DownhillControls _controls;
        private bool _pedalLeftWasPressed;
        private bool _pedalRightWasPressed;
        private bool _jumpWasPressed;

        public float Turn { get; private set; }
        public float FrontBrake { get; private set; }
        public float RearBrake { get; private set; }
        public Vector2 Freelook { get; private set; }
        public bool JumpedThisFrame { get; private set; }

        public event Action PedalLeftPressed;
        public event Action PedalRightPressed;
        public event Action Jumped;

        private void Awake()
        {
            _controls = new DownhillControls();
        }

        private void OnDisable()
        {
            Turn = 0f;
            FrontBrake = 0f;
            RearBrake = 0f;
            Freelook = Vector2.zero;
            JumpedThisFrame = false;
            _pedalLeftWasPressed = false;
            _pedalRightWasPressed = false;
            _jumpWasPressed = false;
        }

        private void OnDestroy()
        {
            _controls?.Dispose();
        }

        private void Update()
        {
            DownhillControls.BikeActions bike = _controls.Bike;
            bool pedalLeftPressed = ReadButton(bike.PedalLeft);
            bool pedalRightPressed = ReadButton(bike.PedalRight);
            bool jumpPressed = ReadButton(bike.Jump);

            if (pedalLeftPressed && !_pedalLeftWasPressed)
            {
                PedalLeftPressed?.Invoke();
            }

            if (pedalRightPressed && !_pedalRightWasPressed)
            {
                PedalRightPressed?.Invoke();
            }

            JumpedThisFrame = jumpPressed && !_jumpWasPressed;
            if (JumpedThisFrame)
            {
                Jumped?.Invoke();
            }

            _pedalLeftWasPressed = pedalLeftPressed;
            _pedalRightWasPressed = pedalRightPressed;
            _jumpWasPressed = jumpPressed;

            Turn = ReadAxis(bike.Turn);
            FrontBrake = ReadAxis(bike.FrontBrake);
            RearBrake = ReadAxis(bike.RearBrake);
            Freelook = ReadVector2(bike.Freelook);
        }

        private static bool ReadButton(InputAction action)
        {
            foreach (InputBinding binding in action.bindings)
            {
                if (binding.isComposite || string.IsNullOrEmpty(binding.effectivePath))
                {
                    continue;
                }

                using InputControlList<InputControl> controls = InputSystem.FindControls(binding.effectivePath);
                foreach (InputControl control in controls)
                {
                    if (control.device == null || !control.device.added)
                    {
                        continue;
                    }

                    if (control is ButtonControl button && button.isPressed)
                    {
                        return true;
                    }

                    if (control is AxisControl axis && axis.ReadValue() > 0.5f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static float ReadAxis(InputAction action)
        {
            float value = 0f;

            foreach (InputBinding binding in action.bindings)
            {
                if (binding.isComposite || string.IsNullOrEmpty(binding.effectivePath))
                {
                    continue;
                }

                float sign = 1f;
                if (binding.isPartOfComposite)
                {
                    if (string.Equals(binding.name, "negative", StringComparison.OrdinalIgnoreCase))
                    {
                        sign = -1f;
                    }
                    else if (!string.Equals(binding.name, "positive", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                using InputControlList<InputControl> controls = InputSystem.FindControls(binding.effectivePath);
                foreach (InputControl control in controls)
                {
                    if (control.device == null || !control.device.added)
                    {
                        continue;
                    }

                    if (control is AxisControl axis)
                    {
                        value += axis.ReadValue() * sign;
                    }
                    else if (control is ButtonControl button && button.isPressed)
                    {
                        value += sign;
                    }
                }
            }

            return Mathf.Clamp(value, -1f, 1f);
        }

        private static Vector2 ReadVector2(InputAction action)
        {
            Vector2 value = Vector2.zero;

            foreach (InputBinding binding in action.bindings)
            {
                if (binding.isComposite || binding.isPartOfComposite || string.IsNullOrEmpty(binding.effectivePath))
                {
                    continue;
                }

                using InputControlList<InputControl> controls = InputSystem.FindControls(binding.effectivePath);
                foreach (InputControl control in controls)
                {
                    if (control.device == null || !control.device.added)
                    {
                        continue;
                    }

                    if (control is Vector2Control vector)
                    {
                        value += vector.ReadValue();
                    }
                }
            }

            return value;
        }
    }
}
