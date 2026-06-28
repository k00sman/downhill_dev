using Downhill.Input;
using UnityEngine;

namespace Downhill.Player
{
    public enum BikeState
    {
        Riding,
        Crashed,
        Recovering,
    }

    /// Player bicycle actor. Holds wiring/validation (Ticket 1.2) and the
    /// downhill movement loop: a grounded raycast feeds pure movement, steering,
    /// and cadence models that drive Rigidbody velocity and heading.
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInputReader))]
    public class PlayerBikeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody _body;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private Transform _bikeBody;
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private Transform _recoveryAnchor;

        [Header("Movement (Ticket 2.1)")]
        [SerializeField] private BikeMovementModel _movement = new();

        [Header("Steering (Ticket 3.1)")]
        [SerializeField] private BikeSteeringModel _steering = new();

        [Header("Braking (Ticket 1.5)")]
        [SerializeField] private BikeBrakeModel _brake = new();

        [Header("Ground probe")]
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private float _groundProbeDistance = 1.2f;
        [SerializeField] private float _groundProbeForwardOffset = 0.75f;
        [SerializeField] private float _groundProbeRearOffset = 0.75f;

        [Header("Visual terrain alignment")]
        [SerializeField] private float _terrainPitchLerp = 12f;
        [SerializeField] private float _maxVisualPitchDegrees = 55f;

        [Header("Pedal drive")]
        [SerializeField] private PedalInputEvaluator _pedalInput = new();
        [SerializeField] private float _pedalPowerMax = 1f;
        [SerializeField] private float _pedalDecayPerSec = 0.8f;

        public Rigidbody Body => _body;
        public PlayerInputReader Input => _input;
        public Transform BikeBody => _bikeBody;
        public Transform CameraPivot => _cameraPivot;
        public Transform GroundCheck => _groundCheck;
        public Transform RecoveryAnchor => _recoveryAnchor;

        public BikeState State { get; private set; } = BikeState.Riding;
        public bool IsGrounded { get; private set; }
        public float PedalPower { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public Vector3 DownhillDirection { get; private set; }
        public float ForwardSpeed { get; private set; }
        public float LateralSpeed { get; private set; }
        public float FallLineAlignmentDegrees { get; private set; }

        private void OnValidate()
        {
            if (_body == null)
            {
                _body = GetComponent<Rigidbody>();
            }

            if (_input == null)
            {
                _input = GetComponent<PlayerInputReader>();
            }
        }

        private void Awake()
        {
            RequireRef(_body, nameof(_body));
            RequireRef(_input, nameof(_input));
            RequireRef(_bikeBody, nameof(_bikeBody));
            RequireRef(_cameraPivot, nameof(_cameraPivot));
            RequireRef(_groundCheck, nameof(_groundCheck));
            RequireRef(_recoveryAnchor, nameof(_recoveryAnchor));

            // The narrow, high-COM bike body would topple as a free Rigidbody.
            // Physics rotation stays locked; yaw is set explicitly by the
            // steering model so contact impulses cannot turn the bike.
            if (_body != null)
            {
                _body.constraints |= RigidbodyConstraints.FreezeRotation;
            }

            // BikeMovementModel is authoritative over speed: it commands the
            // body's velocity every FixedUpdate. Ground contact friction would
            // fight that command and stick the body, so the collider must be
            // frictionless (Minimum combine so it wins on any surface).
            if (TryGetComponent<Collider>(out Collider col))
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

        private void OnEnable()
        {
            if (_input == null)
            {
                return;
            }

            _input.PedalLeftPressed += OnPedalLeftPressed;
            _input.PedalRightPressed += OnPedalRightPressed;
        }

        private void OnDisable()
        {
            if (_input == null)
            {
                return;
            }

            _input.PedalLeftPressed -= OnPedalLeftPressed;
            _input.PedalRightPressed -= OnPedalRightPressed;
            _pedalInput?.Reset();
        }

        private void OnPedalLeftPressed()
        {
            AddCadencePedalPower(PedalSide.Left);
        }

        private void OnPedalRightPressed()
        {
            AddCadencePedalPower(PedalSide.Right);
        }

        private void AddCadencePedalPower(PedalSide side)
        {
            if (_pedalInput == null)
            {
                return;
            }

            AddPedalPower(_pedalInput.EvaluatePress(side, Time.time));
        }

        /// Adds pedal power (clamped). Public so PlayMode tests and later
        /// tickets can drive the accumulator without synthesizing input events.
        public void AddPedalPower(float amount)
        {
            PedalPower = Mathf.Min(PedalPower + amount, _pedalPowerMax);
        }

        private void FixedUpdate()
        {
            if (_body == null || _groundCheck == null)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            PedalPower = Mathf.MoveTowards(PedalPower, 0f, _pedalDecayPerSec * dt);

            IsGrounded = TryProbeGround(out _, out Vector3 groundNormal);
            GroundNormal = IsGrounded ? groundNormal : Vector3.up;

            if (!IsGrounded)
            {
                ClearGroundMovementTelemetry();
                UpdateVisualTerrainPitch(Vector3.up, _body.rotation, dt);
                return; // airborne: Rigidbody gravity owns the arc
            }

            Quaternion bikeRotation = ApplySteering(dt, groundNormal);
            UpdateVisualTerrainPitch(groundNormal, bikeRotation, dt);

            float pedal01 = _pedalPowerMax > 0f ? PedalPower / _pedalPowerMax : 0f;
            float frontBrakeInput = _input != null ? _input.FrontBrake : 0f;
            float rearBrakeInput = _input != null ? _input.RearBrake : 0f;
            float brakeDecel = _brake != null ? _brake.GetTotalBrakeDecel(frontBrakeInput, rearBrakeInput) : 0f;

            BikeMovementResult movementResult = _movement.StepDetailed(
                _body.linearVelocity, bikeRotation * Vector3.forward, groundNormal, pedal01, brakeDecel, dt);
            _body.linearVelocity = movementResult.Velocity;
            UpdateGroundMovementTelemetry(movementResult);
        }

        private Quaternion ApplySteering(float dt, Vector3 groundNormal)
        {
            Quaternion currentRotation = _body != null ? _body.rotation : transform.rotation;
            if (_steering == null)
            {
                return currentRotation;
            }

            float turn = _input != null ? _input.Turn : 0f;
            float yawDelta = _steering.StepYawDeltaDegrees(
                currentRotation * Vector3.forward, _body.linearVelocity, turn, groundNormal, dt);
            if (Mathf.Approximately(yawDelta, 0f))
            {
                return currentRotation;
            }

            Quaternion targetRotation = Quaternion.AngleAxis(yawDelta, Vector3.up) * currentRotation;
            _body.MoveRotation(targetRotation);
            return targetRotation;
        }

        private bool TryProbeGround(out RaycastHit bestHit, out Vector3 averagedNormal)
        {
            bestHit = default;
            averagedNormal = Vector3.zero;

            int hitCount = 0;
            float bestDistance = float.PositiveInfinity;
            Vector3 origin = _groundCheck.position;
            Vector3 forward = transform.forward;

            AccumulateGroundProbe(origin, ref bestHit, ref averagedNormal, ref hitCount, ref bestDistance);
            AccumulateGroundProbe(origin + (forward * _groundProbeForwardOffset),
                ref bestHit, ref averagedNormal, ref hitCount, ref bestDistance);
            AccumulateGroundProbe(origin - (forward * _groundProbeRearOffset),
                ref bestHit, ref averagedNormal, ref hitCount, ref bestDistance);

            if (hitCount == 0)
            {
                return false;
            }

            averagedNormal.Normalize();
            return true;
        }

        private void AccumulateGroundProbe(Vector3 origin, ref RaycastHit bestHit, ref Vector3 averagedNormal,
                                   ref int hitCount, ref float bestDistance)
        {
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                    _groundProbeDistance, _groundMask, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            hitCount++;
            averagedNormal += hit.normal;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestHit = hit;
            }
        }

        private void UpdateVisualTerrainPitch(Vector3 groundNormal, Quaternion bikeRotation, float dt)
        {
            if (_bikeBody == null)
            {
                return;
            }

            Vector3 bikeForward = bikeRotation * Vector3.forward;
            Vector3 slopeForward = Vector3.ProjectOnPlane(bikeForward, groundNormal);
            if (slopeForward.sqrMagnitude < 1e-6f)
            {
                return;
            }

            slopeForward.Normalize();
            float signedPitch = Vector3.SignedAngle(bikeForward, slopeForward, bikeRotation * Vector3.right);
            signedPitch = Mathf.Clamp(signedPitch, -_maxVisualPitchDegrees, _maxVisualPitchDegrees);

            Quaternion target = Quaternion.Euler(signedPitch, 0f, 0f);
            float t = 1f - Mathf.Exp(-Mathf.Max(0f, _terrainPitchLerp) * dt);
            _bikeBody.localRotation = Quaternion.Slerp(_bikeBody.localRotation, target, t);
        }

        private void UpdateGroundMovementTelemetry(BikeMovementResult movementResult)
        {
            DownhillDirection = movementResult.DownhillDirection;
            ForwardSpeed = movementResult.ForwardSpeed;
            LateralSpeed = movementResult.LateralSpeed;
            FallLineAlignmentDegrees = movementResult.FallLineAlignmentDegrees;
        }

        private void ClearGroundMovementTelemetry()
        {
            DownhillDirection = Vector3.zero;
            ForwardSpeed = 0f;
            LateralSpeed = 0f;
            FallLineAlignmentDegrees = 0f;
        }

        private void RequireRef(Object reference, string fieldName)
        {
            if (reference == null)
            {
                Debug.LogError($"{nameof(PlayerBikeController)}: '{fieldName}' reference is not wired.", this);
            }
        }
    }
}
