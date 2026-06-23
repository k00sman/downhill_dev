# Downhill Forward Movement Model Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the player bicycle roll forward down the trail using a tunable, model-driven blend of gravity-along-slope, pedal acceleration, drag, and a speed cap — no steering, braking, or cadence rule.

**Architecture:** A pure `[System.Serializable]` `BikeMovementModel` class owns the movement math and tuning (vectors/floats only, no Unity components). `PlayerBikeController` owns physics and input wiring: it raycasts the ground from `GroundCheck`, maintains a decaying pedal-power accumulator fed by `PlayerInputReader` pedal-press events, and in `FixedUpdate` writes `model.Step(...)` to `Rigidbody.velocity` while grounded (leaving the body to Unity gravity while airborne).

**Tech Stack:** Unity 6000.3.17f1, URP 17.3.0, C#, Unity Test Framework (NUnit EditMode + `[UnityTest]` PlayMode), assembly `Downhill.Player` (references `Downhill.Input`).

## Global Constraints

- **No git commits/add/push.** Do not commit, stage, or push at any point. The user commits manually. (Overrides the TDD "commit" step in every task.)
- **The user runs all tests.** The Unity editor cannot run headless here. Never claim tests passed that you did not see the user confirm. Each task ends by asking the user to run the named tests in the Unity Test Runner and report results.
- **`.meta` files MUST accompany every new asset/script.** Unity generates them on import; they are committed alongside (by the user). A new `.cs` without its `.meta` is incomplete.
- **Third-party content under `Assets/` is off-limits** (PathCreator, Retro Shaders Pro, Adrift Team, vendor folders) — do not modify.
- **Input is read only through `PlayerInputReader`** — never raw key checks in gameplay code.
- **Active Input Handling stays "Both"** — do not change project input settings.
- **No new assemblies.** Reuse `Downhill.Player`. Both test assemblies already reference `Downhill.Player` and `Downhill.Input`.
- **Update `CHANGELOG.md`** under `## [Unreleased] → ### Added` when the feature lands (Task 4).

---

## File Structure

- **Create** `Assets/Scripts/Player/BikeMovementModel.cs` — pure movement math + serialized tuning. Namespace `Downhill.Player`.
- **Create** `Assets/Tests/EditMode/BikeMovementModelTests.cs` — pure-math unit tests (no physics, no play mode).
- **Create** `Assets/Tests/PlayMode/BikeMovementControllerTests.cs` — controller↔physics integration tests.
- **Modify** `Assets/Scripts/Player/PlayerBikeController.cs` — add the model field, ground/pedal tuning, pedal subscription, `FixedUpdate` loop, `IsGrounded`, and an `internal` test seam for adding pedal power.
- **Modify** `CHANGELOG.md` — log under `[Unreleased] → Added`.

Tasks are ordered so the pure model (and its cheap EditMode tests) lands first, then the controller integration, then the PlayMode tests that exercise both together, then docs.

---

### Task 1: `BikeMovementModel` — pure movement math

**Files:**
- Create: `Assets/Scripts/Player/BikeMovementModel.cs`
- Test: `Assets/Tests/EditMode/BikeMovementModelTests.cs`

**Interfaces:**
- Consumes: nothing (leaf class).
- Produces: `Downhill.Player.BikeMovementModel`, a `[System.Serializable]` class with public serialized tuning fields `maxSpeed`, `slopeDriveGain`, `pedalAccel`, `drag`, `gravity`, and the method
  `public Vector3 Step(Vector3 velocity, Vector3 facing, Vector3 groundNormal, float pedalPower01, float dt)`.

- [ ] **Step 1: Write the failing EditMode tests**

Create `Assets/Tests/EditMode/BikeMovementModelTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using Downhill.Player;

public class BikeMovementModelTests
{
    // Known, simple tuning so expected directions are obvious.
    static BikeMovementModel MakeModel() => new BikeMovementModel
    {
        maxSpeed = 20f,
        slopeDriveGain = 1f,
        pedalAccel = 8f,
        drag = 0.4f,
        gravity = 9.81f,
    };

    // A ground normal for a plane that descends along +Z (downhill ahead).
    // Tilt the up-vector backward (toward -Z) by `angleDeg`.
    static Vector3 DownhillNormal(float angleDeg)
    {
        float r = angleDeg * Mathf.Deg2Rad;
        return new Vector3(0f, Mathf.Cos(r), -Mathf.Sin(r)).normalized;
    }

    [Test]
    public void Flat_NoPedal_WithSpeed_Slows()
    {
        var m = MakeModel();
        var v = Vector3.forward * 5f;
        var result = m.Step(v, Vector3.forward, Vector3.up, 0f, 0.02f);
        Assert.Less(Vector3.Dot(result, Vector3.forward), 5f);
        Assert.Greater(Vector3.Dot(result, Vector3.forward), 0f);
    }

    [Test]
    public void Downhill_NoPedal_FromRest_SpeedsUp()
    {
        var m = MakeModel();
        var n = DownhillNormal(20f);
        var result = m.Step(Vector3.zero, Vector3.forward, n, 0f, 0.02f);
        Assert.Greater(Vector3.Dot(result, Vector3.forward), 0f);
    }

    [Test]
    public void Flat_Pedalling_FromRest_SpeedsUp()
    {
        var m = MakeModel();
        var result = m.Step(Vector3.zero, Vector3.forward, Vector3.up, 1f, 0.02f);
        Assert.Greater(Vector3.Dot(result, Vector3.forward), 0f);
    }

    [Test]
    public void SpeedCap_Respected_OverManySteps()
    {
        var m = MakeModel();
        var n = DownhillNormal(45f);
        var v = Vector3.zero;
        for (int i = 0; i < 2000; i++)
            v = m.Step(v, Vector3.forward, n, 1f, 0.02f);
        Assert.LessOrEqual(v.magnitude, m.maxSpeed + 0.01f);
    }

    [Test]
    public void Uphill_LowSpeed_DoesNotReverse()
    {
        var m = MakeModel();
        // Uphill ahead: descends along -Z, so facing +Z climbs.
        var n = new Vector3(0f, Mathf.Cos(20f * Mathf.Deg2Rad),
                            Mathf.Sin(20f * Mathf.Deg2Rad)).normalized;
        var result = m.Step(Vector3.zero, Vector3.forward, n, 0f, 0.02f);
        Assert.GreaterOrEqual(Vector3.Dot(result, Vector3.forward), 0f);
    }

    [Test]
    public void DegenerateHeading_ReturnsInputVelocity()
    {
        var m = MakeModel();
        var v = Vector3.forward * 3f;
        // facing parallel to the normal -> projected heading is ~zero.
        var result = m.Step(v, Vector3.up, Vector3.up, 1f, 0.02f);
        Assert.AreEqual(v, result);
    }
}
```

- [ ] **Step 2: Ask the user to run the tests and confirm they FAIL**

Ask the user to open the Unity Test Runner → EditMode and run `BikeMovementModelTests`.
Expected: compile error / all fail — `BikeMovementModel` does not exist yet. (This is the RED step; confirm before proceeding.)

- [ ] **Step 3: Implement `BikeMovementModel`**

Create `Assets/Scripts/Player/BikeMovementModel.cs`:

```csharp
using UnityEngine;

namespace Downhill.Player
{
    /// Pure, tunable forward-movement math for the downhill bike (Ticket 2.1).
    /// No Unity components: given the current velocity, heading, ground normal,
    /// normalized pedal power, and dt, it returns the new world velocity. The
    /// caller (PlayerBikeController) decides grounded-ness and owns the Rigidbody.
    [System.Serializable]
    public class BikeMovementModel
    {
        [Tooltip("Forward speed cap (m/s).")]
        public float maxSpeed = 20f;

        [Tooltip("Multiplier on the gravity-along-slope drive.")]
        public float slopeDriveGain = 1f;

        [Tooltip("Forward acceleration at full pedal power (m/s^2).")]
        public float pedalAccel = 8f;

        [Tooltip("Linear drag coefficient (per second).")]
        public float drag = 0.4f;

        [Tooltip("Gravity magnitude used for the slope term (m/s^2).")]
        public float gravity = 9.81f;

        /// Returns the new world velocity. Assumes the caller has decided the bike
        /// is grounded; works in 1-D along the projected heading so there is no
        /// sideways slide while steering does not exist yet.
        public Vector3 Step(Vector3 velocity, Vector3 facing, Vector3 groundNormal,
                            float pedalPower01, float dt)
        {
            Vector3 forwardOnSlope = Vector3.ProjectOnPlane(facing, groundNormal);
            if (forwardOnSlope.sqrMagnitude < 1e-6f)
                return velocity; // heading parallel to the normal — avoid NaN
            forwardOnSlope.Normalize();

            Vector3 gravityVec = Vector3.down * gravity;
            float slopeAccel = Vector3.Dot(
                Vector3.ProjectOnPlane(gravityVec, groundNormal), forwardOnSlope)
                * slopeDriveGain;

            float pedalAccelTerm = Mathf.Clamp01(pedalPower01) * pedalAccel;

            float speed = Vector3.Dot(velocity, forwardOnSlope);
            speed += (slopeAccel + pedalAccelTerm) * dt;
            speed -= speed * drag * dt;
            speed = Mathf.Clamp(speed, 0f, maxSpeed);

            return forwardOnSlope * speed;
        }
    }
}
```

- [ ] **Step 4: Ask the user to run the tests and confirm they PASS**

Ask the user to re-run `BikeMovementModelTests` in the EditMode Test Runner.
Expected: all 6 tests PASS. If any fail, report the failing assertion and stop — do not tune values to force a pass without understanding the cause.

---

### Task 2: Wire movement into `PlayerBikeController`

**Files:**
- Modify: `Assets/Scripts/Player/PlayerBikeController.cs`

**Interfaces:**
- Consumes: `BikeMovementModel.Step(Vector3, Vector3, Vector3, float, float)` (Task 1); `PlayerInputReader.PedalLeftPressed` / `PedalRightPressed` events; the existing serialized `_body` (`Rigidbody`), `_input` (`PlayerInputReader`), `_groundCheck` (`Transform`).
- Produces: `public bool IsGrounded { get; }`; `public void AddPedalPower(float amount)` (drive seam, also usable by later tickets/debug); runtime `FixedUpdate` movement behavior.

- [ ] **Step 1: Add fields, pedal subscription, and the FixedUpdate loop**

Edit `Assets/Scripts/Player/PlayerBikeController.cs`. Keep the existing references/validation intact; add the movement members. The full file after editing:

```csharp
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

            _grounded = Physics.Raycast(_groundCheck.position, Vector3.down,
                out RaycastHit hit, _groundProbeDistance, _groundMask,
                QueryTriggerInteraction.Ignore);

            if (!_grounded) return; // airborne: Rigidbody gravity owns the arc

            float pedal01 = _pedalPowerMax > 0f ? _pedalPower / _pedalPowerMax : 0f;
            _body.velocity = _movement.Step(
                _body.velocity, transform.forward, hit.normal, pedal01, dt);
        }

        void RequireRef(Object reference, string fieldName)
        {
            if (reference == null)
                Debug.LogError($"{nameof(PlayerBikeController)}: '{fieldName}' reference is not wired.", this);
        }
    }
}
```

- [ ] **Step 2: Ask the user to confirm the project compiles and 1.2 tests still pass**

Ask the user to let Unity recompile, then run the existing `PlayerBikePrefabTests` (EditMode) and `PlayerBikeControllerTests` (PlayMode).
Expected: compiles with no errors; both 1.2 suites still PASS (the controller's references/validation/`State` are unchanged). If `Rigidbody.velocity` raises an obsolete-API error on this Unity version, rename it to `linearVelocity` here and everywhere in the plan, then recompile. Report results before proceeding.

---

### Task 3: PlayMode integration tests

**Files:**
- Create: `Assets/Tests/PlayMode/BikeMovementControllerTests.cs`

**Interfaces:**
- Consumes: the player prefab at `Assets/PFB_Player.prefab`; `PlayerBikeController.Body`, `.IsGrounded`, `.AddPedalPower(float)`; `BikeState` from `Downhill.Player`.
- Produces: nothing (leaf tests).

- [ ] **Step 1: Write the PlayMode tests**

Create `Assets/Tests/PlayMode/BikeMovementControllerTests.cs`:

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Downhill.Player;

public class BikeMovementControllerTests
{
    const string PrefabPath = "Assets/PFB_Player.prefab";

    GameObject _ground;
    GameObject _player;

    [TearDown]
    public void TearDown()
    {
        if (_player != null) Object.Destroy(_player);
        if (_ground != null) Object.Destroy(_ground);
    }

    // Big flat or tilted ground slab the player can rest on.
    GameObject MakeGround(Quaternion rotation)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.transform.rotation = rotation;
        g.transform.position = Vector3.zero;
        g.transform.localScale = new Vector3(50f, 1f, 50f);
        return g;
    }

    IEnumerator SpawnPlayerAbove(GameObject ground, float height)
    {
#if UNITY_EDITOR
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab, $"Player prefab not found at {PrefabPath}");
        _player = Object.Instantiate(prefab);
        _player.transform.position = ground.transform.position + Vector3.up * height;
        _player.transform.rotation = Quaternion.identity;
        // Settle onto the ground for a few physics frames.
        for (int i = 0; i < 25; i++) yield return new WaitForFixedUpdate();
#else
        Assert.Ignore("PlayMode movement tests require the editor (AssetDatabase).");
        yield break;
#endif
    }

    static float ForwardSpeed(PlayerBikeController c) =>
        Vector3.Dot(c.Body.velocity, c.transform.forward);

    [UnityTest]
    public IEnumerator FlatGround_BleedsInitialSpeed()
    {
        _ground = MakeGround(Quaternion.identity);
        yield return SpawnPlayerAbove(_ground, 1.0f);
        var c = _player.GetComponent<PlayerBikeController>();

        c.Body.velocity = c.transform.forward * 5f;
        for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();

        Assert.Less(ForwardSpeed(c), 5f, "Drag should bleed speed on flat ground");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator DownhillGround_GainsSpeed()
    {
        // Tilt so the surface descends toward +Z (the player's forward).
        _ground = MakeGround(Quaternion.Euler(-20f, 0f, 0f));
        yield return SpawnPlayerAbove(_ground, 1.0f);
        var c = _player.GetComponent<PlayerBikeController>();

        float before = ForwardSpeed(c);
        for (int i = 0; i < 60; i++) yield return new WaitForFixedUpdate();

        Assert.Greater(ForwardSpeed(c), before + 0.5f, "Should accelerate downhill");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator Pedalling_IncreasesSpeed_VsControl()
    {
        // Control run: flat, no pedal.
        _ground = MakeGround(Quaternion.identity);
        yield return SpawnPlayerAbove(_ground, 1.0f);
        var c = _player.GetComponent<PlayerBikeController>();
        for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
        float control = ForwardSpeed(c);

        // Pedalled run: keep topping up pedal power.
        for (int i = 0; i < 40; i++)
        {
            c.AddPedalPower(1f);
            yield return new WaitForFixedUpdate();
        }
        Assert.Greater(ForwardSpeed(c), control + 0.5f, "Pedalling should add speed");
        LogAssert.NoUnexpectedReceived();
    }
}
```

- [ ] **Step 2: Ask the user to run the PlayMode tests and confirm they PASS**

Ask the user to open the Unity Test Runner → PlayMode and run `BikeMovementControllerTests`.
Expected: all 3 tests PASS, no errors logged. Common adjustments if a test is flaky: the settle-height (`1.0f`) or `_groundProbeDistance` may need nudging so the spawned bike actually rests within probe range of the slab — if `DownhillGround_GainsSpeed` reports the bike never grounded, raise `_groundProbeDistance` slightly or lower the spawn height, then re-run. Report results before proceeding.

---

### Task 4: Changelog

**Files:**
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: nothing.
- Produces: nothing.

- [ ] **Step 1: Add the Ticket 2.1 entry**

Edit `CHANGELOG.md`. Under `## [Unreleased]` → `### Added`, insert this entry **above** the existing "Player bicycle root actor (Ticket 1.2)" entry (newest first):

```markdown
- **Downhill forward movement** (Ticket 2.1) — 2026-06-22
  - Added `BikeMovementModel`, a pure, tunable class that blends
    gravity-along-slope drive, pedal acceleration, linear drag, and a speed cap
    into a forward velocity. No steering or braking yet.
  - Extended `PlayerBikeController` with a `FixedUpdate` movement loop: it
    raycasts the ground from `GroundCheck`, keeps a decaying pedal-power
    accumulator fed by `PlayerInputReader` pedal-press events, and drives
    `Rigidbody` velocity along the slope while grounded (letting real gravity
    arc it while airborne). Exposes `IsGrounded`.
  - Added EditMode (pure movement math) and PlayMode (controller + physics on a
    test slope) tests.
```

- [ ] **Step 2: Ask the user to confirm the changelog reads correctly**

Ask the user to glance at the `## [Unreleased]` section and confirm the entry is accurate and well-placed. No tests for this step.

---

## Done criteria (Ticket 2.1)

- `BikeMovementModelTests` (EditMode) and `BikeMovementControllerTests` (PlayMode) pass, confirmed by the user; the 1.2 suites still pass.
- Acceptance criteria met: bike rolls downhill without steering; pedalling sustains/increases speed; it slows on flat / when input stops; `maxSpeed` and accel fields are inspector-tunable.
- Touched files match scope: `BikeMovementModel.cs`, `PlayerBikeController.cs`, the two test files, `CHANGELOG.md` — no unrelated systems introduced.
- All work left uncommitted for the user.
