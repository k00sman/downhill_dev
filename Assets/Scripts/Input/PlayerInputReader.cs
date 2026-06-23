using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Downhill.Input
{
    /// Thin wrapper over the generated DownhillControls asset. Gameplay reads input
    /// through this component and never touches raw keys or InputActions directly.
    public class PlayerInputReader : MonoBehaviour
    {
        DownhillControls _controls;

        public float Turn { get; private set; }
        public float FrontBrake { get; private set; }
        public float RearBrake { get; private set; }
        public Vector2 Freelook { get; private set; }
        public bool JumpedThisFrame { get; private set; }

        public event Action PedalLeftPressed;
        public event Action PedalRightPressed;
        public event Action Jumped;

        void Awake() => _controls = new DownhillControls();

        void OnEnable()
        {
            var bike = _controls.Bike;
            bike.Enable();
            bike.PedalLeft.performed += OnPedalLeft;
            bike.PedalRight.performed += OnPedalRight;
            bike.Jump.performed += OnJump;
        }

        void OnDisable()
        {
            var bike = _controls.Bike;
            bike.PedalLeft.performed -= OnPedalLeft;
            bike.PedalRight.performed -= OnPedalRight;
            bike.Jump.performed -= OnJump;
            bike.Disable();
        }

        void OnDestroy() => _controls?.Dispose();

        void Update()
        {
            var bike = _controls.Bike;
            Turn = bike.Turn.ReadValue<float>();
            FrontBrake = bike.FrontBrake.ReadValue<float>();
            RearBrake = bike.RearBrake.ReadValue<float>();
            Freelook = bike.Freelook.ReadValue<Vector2>();
            JumpedThisFrame = bike.Jump.WasPerformedThisFrame();
        }

        void OnPedalLeft(InputAction.CallbackContext _) => PedalLeftPressed?.Invoke();
        void OnPedalRight(InputAction.CallbackContext _) => PedalRightPressed?.Invoke();
        void OnJump(InputAction.CallbackContext _) => Jumped?.Invoke();
    }
}
