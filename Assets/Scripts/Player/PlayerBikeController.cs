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

    /// Scaffolding shell for the player bicycle. Holds the actor's wiring and a
    /// state hook for later crash/chase systems. No movement logic lives here yet
    /// (see Tickets 2.x / 3.x / 4.x).
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

        public Rigidbody Body => _body;
        public PlayerInputReader Input => _input;
        public Transform BikeBody => _bikeBody;
        public Transform CameraPivot => _cameraPivot;
        public Transform GroundCheck => _groundCheck;
        public Transform RecoveryAnchor => _recoveryAnchor;

        public BikeState State { get; private set; } = BikeState.Riding;

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
        }

        void RequireRef(Object reference, string fieldName)
        {
            if (reference == null)
                Debug.LogError($"{nameof(PlayerBikeController)}: '{fieldName}' reference is not wired.", this);
        }
    }
}
