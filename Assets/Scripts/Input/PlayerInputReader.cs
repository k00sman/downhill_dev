using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Downhill.Input
{
    /// Thin wrapper over the generated DownhillControls asset. Gameplay reads input
    /// through this component and never touches raw keys or InputActions directly.
    public class PlayerInputReader : MonoBehaviour
    {
        private DownhillControls _controls;

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

        private void OnEnable()
        {
            DownhillControls.BikeActions bike = _controls.Bike;
            bike.Enable();
            bike.PedalLeft.performed += OnPedalLeft;
            bike.PedalRight.performed += OnPedalRight;
            bike.Jump.performed += OnJump;
        }

        private void OnDisable()
        {
            DownhillControls.BikeActions bike = _controls.Bike;
            bike.PedalLeft.performed -= OnPedalLeft;
            bike.PedalRight.performed -= OnPedalRight;
            bike.Jump.performed -= OnJump;
            bike.Disable();
        }

        private void OnDestroy()
        {
            _controls?.Dispose();
        }

        private void Update()
        {
            DownhillControls.BikeActions bike = _controls.Bike;
            Turn = bike.Turn.ReadValue<float>();
            FrontBrake = bike.FrontBrake.ReadValue<float>();
            RearBrake = bike.RearBrake.ReadValue<float>();
            Freelook = bike.Freelook.ReadValue<Vector2>();
            JumpedThisFrame = bike.Jump.WasPerformedThisFrame();
        }

        private void OnPedalLeft(InputAction.CallbackContext _)
        {
            PedalLeftPressed?.Invoke();
        }

        private void OnPedalRight(InputAction.CallbackContext _)
        {
            PedalRightPressed?.Invoke();
        }

        private void OnJump(InputAction.CallbackContext _)
        {
            Jumped?.Invoke();
        }
    }
}
