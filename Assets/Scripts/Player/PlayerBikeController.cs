using UnityEngine;
using Downhill.Input;

namespace Downhill.Player
{
    public enum BikeState
    {
        Riding,
        Crashed,
        Recovering,
    }

    /// Player bicycle actor. Holds wiring/validation (Ticket 1.2) and the
    /// downhill forward-movement loop (Ticket 2.1): a grounded raycast feeds a
    /// pure BikeMovementModel that drives Rigidbody velocity. No steering,
    /// braking, or cadence rule yet (Tickets 2.2 / 3.x).
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerBikeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Rigidbody _body;
        [SerializeField] PlayerInputReader _input;
        [SerializeField] Transform _bikeBody;
        [SerializeField] Transform _cameraPivot;
        [SerializeField] Transform _groundCheck;
        [SerializeField] Transform _recoveryAnchor;

        [Header("Movement (Ticket 2.1)")]
        [SerializeField] BikeMovementModel _movement = new BikeMovementModel();

        [Header("Ground probe")]
        [SerializeField] LayerMask _groundMask = ~0;
        [SerializeField] float _groundProbeDistance = 0.6f;
        [SerializeField] float _groundProbeForwardOffset = 0.75f;
        [SerializeField] float _groundProbeRearOffset = 0.75f;

        [Header("Visual terrain alignment")]
        [SerializeField] float _terrainPitchLerp = 12f;
        [SerializeField] float _maxVisualPitchDegrees = 55f;

        [Header("Pedal drive")]
        [SerializeField] float _pedalImpulse = 0.5f;
        [SerializeField] float _pedalPowerMax = 1f;
        [SerializeField] float _pedalDecayPerSec = 0.8f;

        float _pedalPower;
        bool _grounded;

        public Rigidbody Body => _body;
        public PlayerInputReader Input => _input;
        public Transform BikeBody => _bikeBody;
        public Transform CameraPivot => _cameraPivot;
        public Transform GroundCheck => _groundCheck;
        public Transform RecoveryAnchor => _recoveryAnchor;

        public BikeState State { get; private set; } = BikeState.Riding;
        public bool IsGrounded => _grounded;

        void OnValidate()
        {
            if (_body == null) _body = GetComponent<Rigidbody>();
            if (_input == null) _input = GetComponent<PlayerInputReader>();
        }

        void Awake()
        {
            RequireRef(_body, nameof(_body));
            RequireRef(_input, nameof(_input));
            RequireRef(_bikeBody, nameof(_bikeBody));
            RequireRef(_cameraPivot, nameof(_cameraPivot));
            RequireRef(_groundCheck, nameof(_groundCheck));
            RequireRef(_recoveryAnchor, nameof(_recoveryAnchor));

            // The narrow, high-COM bike body would topple as a free Rigidbody.
            // Freeze all rotation until steering owns yaw intentionally
            // (Ticket 3.1). Visual lean lives on BikeBody, not here.
            if (_body != null)
                _body.constraints |= RigidbodyConstraints.FreezeRotation;

            // BikeMovementModel is authoritative over speed: it commands the
            // body's velocity every FixedUpdate. Ground contact friction would
            // fight that command and stick the body, so the collider must be
            // frictionless (Minimum combine so it wins on any surface).
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.sharedMaterial = new PhysicsMaterial("BikeFrictionless")
                {
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounciness = 0f,
                    bounceCombine = PhysicsMaterialCombine.Minimum,
                };
            }
        }

        void OnEnable()
        {
            if (_input == null) return;
            _input.PedalLeftPressed += OnPedalPressed;
            _input.PedalRightPressed += OnPedalPressed;
        }

        void OnDisable()
        {
            if (_input == null) return;
            _input.PedalLeftPressed -= OnPedalPressed;
            _input.PedalRightPressed -= OnPedalPressed;
        }

        void OnPedalPressed() => AddPedalPower(_pedalImpulse);

        /// Adds pedal power (clamped). Public so PlayMode tests and later
        /// tickets can drive the accumulator without synthesizing input events.
        public void AddPedalPower(float amount)
        {
            _pedalPower = Mathf.Min(_pedalPower + amount, _pedalPowerMax);
        }

        void FixedUpdate()
        {
            if (_body == null || _groundCheck == null) return;

            float dt = Time.fixedDeltaTime;
            _pedalPower = Mathf.MoveTowards(_pedalPower, 0f, _pedalDecayPerSec * dt);

            _grounded = TryProbeGround(out _, out Vector3 groundNormal);

            if (!_grounded)
            {
                UpdateVisualTerrainPitch(Vector3.up, dt);
                return; // airborne: Rigidbody gravity owns the arc
            }

            UpdateVisualTerrainPitch(groundNormal, dt);

            float pedal01 = _pedalPowerMax > 0f ? _pedalPower / _pedalPowerMax : 0f;
            _body.linearVelocity = _movement.Step(
                _body.linearVelocity, transform.forward, groundNormal, pedal01, dt);
        }

        bool TryProbeGround(out RaycastHit bestHit, out Vector3 averagedNormal)
        {
            bestHit = default;
            averagedNormal = Vector3.zero;

            int hitCount = 0;
            float bestDistance = float.PositiveInfinity;
            Vector3 origin = _groundCheck.position;
            Vector3 forward = transform.forward;

            AccumulateGroundProbe(origin, ref bestHit, ref averagedNormal, ref hitCount, ref bestDistance);
            AccumulateGroundProbe(origin + forward * _groundProbeForwardOffset,
                ref bestHit, ref averagedNormal, ref hitCount, ref bestDistance);
            AccumulateGroundProbe(origin - forward * _groundProbeRearOffset,
                ref bestHit, ref averagedNormal, ref hitCount, ref bestDistance);

            if (hitCount == 0)
                return false;

            averagedNormal.Normalize();
            return true;
        }

        void AccumulateGroundProbe(Vector3 origin, ref RaycastHit bestHit, ref Vector3 averagedNormal,
                                   ref int hitCount, ref float bestDistance)
        {
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                    _groundProbeDistance, _groundMask, QueryTriggerInteraction.Ignore))
                return;

            hitCount++;
            averagedNormal += hit.normal;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestHit = hit;
            }
        }

        void UpdateVisualTerrainPitch(Vector3 groundNormal, float dt)
        {
            if (_bikeBody == null)
                return;

            Vector3 slopeForward = Vector3.ProjectOnPlane(transform.forward, groundNormal);
            if (slopeForward.sqrMagnitude < 1e-6f)
                return;

            slopeForward.Normalize();
            float signedPitch = Vector3.SignedAngle(transform.forward, slopeForward, transform.right);
            signedPitch = Mathf.Clamp(signedPitch, -_maxVisualPitchDegrees, _maxVisualPitchDegrees);

            Quaternion target = Quaternion.Euler(signedPitch, 0f, 0f);
            float t = 1f - Mathf.Exp(-Mathf.Max(0f, _terrainPitchLerp) * dt);
            _bikeBody.localRotation = Quaternion.Slerp(_bikeBody.localRotation, target, t);
        }

        void RequireRef(Object reference, string fieldName)
        {
            if (reference == null)
                Debug.LogError($"{nameof(PlayerBikeController)}: '{fieldName}' reference is not wired.", this);
        }
    }
}
