# Ticket 2.1 — Downhill Forward Movement Model — Design Spec

**Date:** 2026-06-22
**Ticket:** 2.1 (Phase 2 — Bicycle locomotion)
**Depends on:** Tickets 1.1 (control map) and 1.2 (player root actor) — both done.

## Goal

Make the bicycle move forward down the trail by combining gravity influence,
pedal-driven acceleration, drag, and a speed cap — without full simulation
fidelity. Movement is **straight-ahead only**: no steering, no braking, no
cadence rule. The result is a tunable, predictable forward roll that proves the
level is traversable.

## Acceptance criteria (from TICKETS.md)

- The bicycle can move downhill without steering.
- Pedaling increases or sustains speed.
- The bike slows on flatter ground or when input stops.
- Max speed and acceleration values are tunable in inspector/config.

## Current state

- `PlayerBikeController` (assembly `Downhill.Player`) is a wired shell: it holds
  serialized references to `Rigidbody _body`, `PlayerInputReader _input`,
  `Transform _bikeBody`, `_cameraPivot`, `_groundCheck`, `_recoveryAnchor`, and a
  read-only `BikeState State` (default `Riding`). It validates references in
  `Awake` and contains **no movement logic**.
- The root carries a dynamic `Rigidbody` (`mass 80`, `useGravity = true`,
  `Interpolate`, `Continuous` collision) and a `BoxCollider`.
- `GroundCheck` is an empty child at tire-contact height, intended as a downward
  raycast origin.
- `PlayerInputReader` exposes pedalling as **button events** —
  `event Action PedalLeftPressed` and `PedalRightPressed` — fired on
  `performed`. There is **no held pedal axis**. `FrontBrake`/`RearBrake`/`Turn`
  are polled but out of scope here.

## Design decisions

Four forks were resolved during brainstorming:

1. **Drive model — model-driven velocity.** Each `FixedUpdate`, raycast the
   ground for its normal, project the bike's facing onto that plane, and compute
   the new velocity ourselves: a tunable gravity-along-slope term + pedal
   acceleration − drag, clamped to max speed. The `Rigidbody` is retained for
   collisions and airborne gravity, but while grounded we set its velocity
   directly. Predictable and highly tunable; physics realism is intentionally
   sacrificed.
2. **Pedal drive — decaying drive accumulator.** Each pedal press (either side)
   adds an impulse to a `pedalPower` value that decays over time; that value
   feeds forward acceleration. Side-agnostic for this ticket. **Ticket 2.2
   replaces only the press→power rule** with the alternating-cadence evaluator;
   the movement math below is untouched by that change.
3. **Ground check — minimal raycast, gated in the controller.** A single short
   downward raycast from `GroundCheck` each `FixedUpdate` yields `grounded` +
   `groundNormal`. Grounded: apply the model. Airborne: leave the `Rigidbody`
   alone so Unity's real gravity arcs it (no double gravity). This is the seed
   that Ticket 4.1 will formalize/extend (spherecast, snapping, coyote time).
4. **Model shape — pure plain-C# class.** `BikeMovementModel` is a
   `[System.Serializable]` class holding tuning fields with one pure method;
   no MonoBehaviour, no `Rigidbody`/raycast access inside. The controller owns
   physics and calls the model. Fully unit-testable in EditMode without play
   mode; tuning fields surface in the inspector through a `[SerializeField]`
   instance on the controller.

## Architecture

```
PlayerInputReader ──(PedalLeftPressed / PedalRightPressed events)──► PlayerBikeController
                                                                      │
PlayerBikeController (FixedUpdate):                                   │
  1. raycast GroundCheck ▼ groundMask ─► (grounded, groundNormal)     │
  2. _pedalPower: bump on press, decay each step ─► pedalPower01      │
  3. if grounded:  _body.velocity = model.Step(...)  ◄───────────────►│ BikeMovementModel.Step (pure)
     else:         leave _body alone (real gravity)
```

- **`BikeMovementModel`** — owns the *math* and the *tuning*. Knows nothing about
  Unity components, only vectors and floats.
- **`PlayerBikeController`** — owns the *physics and input wiring*: the
  `Rigidbody`, the raycast, the pedal accumulator, and the FixedUpdate cadence.

## Components

### `BikeMovementModel` (new — `Assets/Scripts/Player/BikeMovementModel.cs`)

A `[System.Serializable]` class in namespace `Downhill.Player`.

**Tuning fields** (serialized, with starting values; all inspector-editable):

| Field | Start | Meaning |
|---|---|---|
| `maxSpeed` | `20` | Forward speed cap (m/s). |
| `slopeDriveGain` | `1.0` | Multiplier on the gravity-along-slope drive. |
| `pedalAccel` | `8` | Forward acceleration at full pedal power (m/s²). |
| `drag` | `0.4` | Linear drag coefficient (per second). |
| `gravity` | `9.81` | Gravity magnitude used for the slope term (m/s²). |

**Method:**

```
public Vector3 Step(Vector3 velocity, Vector3 facing, Vector3 groundNormal,
                    float pedalPower01, float dt)
```

Pure. Assumes the caller has already decided the bike is grounded (the controller
gates it). Returns the new world velocity. Algorithm:

1. `forwardOnSlope = Vector3.ProjectOnPlane(facing, groundNormal).normalized`
   - Guard: if its magnitude is ~0 (facing parallel to the normal), return
     `velocity` unchanged to avoid a NaN heading.
2. `gravityVec = Vector3.down * gravity`
3. `slopeAccel = Vector3.Dot(Vector3.ProjectOnPlane(gravityVec, groundNormal),
   forwardOnSlope) * slopeDriveGain` — positive pointing downhill along the
   heading, negative uphill.
4. `pedalAccelTerm = Mathf.Clamp01(pedalPower01) * pedalAccel`
5. `speed = Vector3.Dot(velocity, forwardOnSlope)`
6. `speed += (slopeAccel + pedalAccelTerm) * dt`
7. `speed -= speed * drag * dt` (linear drag)
8. `speed = Mathf.Clamp(speed, 0f, maxSpeed)` — floor of 0 means no reverse roll
   (prototype feel choice; uphill simply bleeds speed to a stop).
9. `return forwardOnSlope * speed`

Working in 1-D along the heading (rather than a free 2-D ground velocity) is
deliberate: with no steering yet, it prevents sideways sliding and keeps the
bike pointed where it's placed.

### `PlayerBikeController` (modify — `Assets/Scripts/Player/PlayerBikeController.cs`)

Add to the existing shell (references and validation stay):

**New serialized fields:**

- `[SerializeField] BikeMovementModel _movement = new BikeMovementModel();`
- `[SerializeField] LayerMask _groundMask = ~0;` — what counts as ground
  (default: everything; tightened in-editor).
- `[SerializeField] float _groundProbeDistance = 0.6f;` — ray length from
  `GroundCheck`.
- `[SerializeField] float _pedalImpulse = 0.5f;` — power added per pedal press.
- `[SerializeField] float _pedalPowerMax = 1f;` — accumulator clamp.
- `[SerializeField] float _pedalDecayPerSec = 0.8f;` — decay rate toward 0.

**New runtime state:** `float _pedalPower;` and `bool _grounded;` (the latter
exposed read-only as `public bool IsGrounded => _grounded;` for later
tickets/debug HUD).

**Lifecycle:**

- `OnEnable` / `OnDisable` — subscribe/unsubscribe
  `_input.PedalLeftPressed` and `_input.PedalRightPressed` to a single
  `OnPedalPressed()` handler. (Guard against a null `_input` so a mis-wired
  prefab still fails via the existing `Awake` error, not an exception here.)
- `OnPedalPressed()` — `_pedalPower = Mathf.Min(_pedalPower + _pedalImpulse,
  _pedalPowerMax);`
- `FixedUpdate()`:
  1. `float dt = Time.fixedDeltaTime;`
  2. Decay: `_pedalPower = Mathf.MoveTowards(_pedalPower, 0f,
     _pedalDecayPerSec * dt);`
  3. Ground sample:
     `_grounded = Physics.Raycast(_groundCheck.position, Vector3.down,
     out RaycastHit hit, _groundProbeDistance, _groundMask,
     QueryTriggerInteraction.Ignore);`
  4. If `_grounded`:
     `float pedal01 = _pedalPowerMax > 0f ? _pedalPower / _pedalPowerMax : 0f;`
     `_body.velocity = _movement.Step(_body.velocity, transform.forward,
     hit.normal, pedal01, dt);`
  5. Else: do nothing — the `Rigidbody`'s own gravity integrates the arc.

No movement is applied to `BikeBody`/visual lean here (that's 3.x); the physics
root moves, the visual follows as its child.

> Note: in Unity 6 `Rigidbody.velocity` is the current API surface used elsewhere
> in this project. If the project's Unity version surfaces the
> `linearVelocity`-only deprecation, the implementer swaps the property name
> consistently in both the controller and the PlayMode test — the design is
> unaffected.

## Data flow

1. Player presses a pedal → `PlayerInputReader` fires `PedalLeftPressed` /
   `PedalRightPressed` → controller bumps `_pedalPower`.
2. Each physics step the controller decays `_pedalPower`, samples the ground,
   and (when grounded) hands the current velocity + heading + ground normal +
   normalized pedal power to `BikeMovementModel.Step`, writing the result back
   to `_body.velocity`.
3. When airborne the controller writes nothing; `Rigidbody` gravity owns the arc.

## Testing

Two assemblies, mirroring 1.1/1.2. The pure model gives cheap, deterministic
EditMode coverage; PlayMode covers the controller↔physics integration.

### EditMode — `BikeMovementModelTests.cs` (no play mode, no physics)

Construct a `BikeMovementModel` with known tuning and call `Step` directly:

- **Flat, no pedal, with initial speed → slows.** `groundNormal = up`,
  `facing = forward`, `pedalPower01 = 0`, `velocity = forward * 5`. New forward
  speed `< 5`. (Drag bleeds speed on flat / when input stops.)
- **Downhill, no pedal, from rest → speeds up.** `groundNormal` tilted so the
  plane drops along `facing`, `velocity = 0`, `pedalPower01 = 0`. New forward
  speed `> 0`. (Gravity influence drives downhill roll.)
- **Flat, pedalling, from rest → speeds up.** `groundNormal = up`,
  `pedalPower01 = 1`, `velocity = 0`. New forward speed `> 0`. (Pedalling builds
  speed.)
- **Speed cap respected.** Iterate `Step` many times on a steep slope with
  `pedalPower01 = 1`; assert forward speed never exceeds `maxSpeed` (within a
  small epsilon).
- **No reverse roll.** Facing *uphill* (slope rises along `facing`) at low/zero
  speed; assert returned forward speed is clamped at `0`, never negative.
- **Degenerate heading guard.** `facing` parallel to `groundNormal`; assert
  `Step` returns the input velocity unchanged (no NaN).

### PlayMode — `BikeMovementControllerTests.cs`

Build a minimal world in the test (a ground collider + the player prefab),
step `FixedUpdate` via `new WaitForFixedUpdate()` across several frames:

- **Flat ground bleeds speed.** Place the player on a flat `BoxCollider`, set
  `Body.velocity = transform.forward * 5`, step ~30 physics frames; assert
  forward speed decreased and no errors logged.
- **Downhill gains speed.** Tilt the ground collider (or place the player on a
  ramp) so the heading points downhill; from rest, step physics; assert the bike
  gained forward speed/displacement.
- **Pedalling increases speed.** On flat ground, raise `_pedalPower` (via a
  pedal press / exposed test hook), step physics; assert speed increased versus
  an un-pedalled control.
- **No errors.** `LogAssert.NoUnexpectedReceived()` after each scenario.

> The PlayMode tests construct their own ground rather than depending on
> `SCN_Tutorial` geometry, so they stay deterministic and scene-independent.
> If raising `_pedalPower` from a test proves awkward through events, add a
> minimal internal test seam (e.g. an `internal` method to add pedal power) on
> the controller rather than making physics assertions fragile.

## Files

**Create**
- `Assets/Scripts/Player/BikeMovementModel.cs` (+ `.meta`)
- `Assets/Tests/EditMode/BikeMovementModelTests.cs` (+ `.meta`)
- `Assets/Tests/PlayMode/BikeMovementControllerTests.cs` (+ `.meta`)

**Modify**
- `Assets/Scripts/Player/PlayerBikeController.cs` — add movement model field,
  ground/pedal tuning, pedal subscription, `FixedUpdate` loop, `IsGrounded`.
- `CHANGELOG.md` — log under `[Unreleased] → Added`.

No new assemblies (reuse `Downhill.Player`); the test assemblies already
reference `Downhill.Player` and `Downhill.Input`.

## Execution note

All code in this ticket is authored directly (no prefab restructuring required —
the 1.2 hierarchy already provides `GroundCheck`, the `Rigidbody`, and the
collider). The user runs the Unity Test Runner (EditMode + PlayMode) and reports
results; the editor cannot run headless here. After the model lands, the
`maxSpeed` / `slopeDriveGain` / `pedalAccel` / `drag` values are tuned in-play.

## Out of scope (later tickets)

- Turning / steering — 3.1, speed-sensitive steering — 3.2.
- Braking (front/rear) — 3.3.
- Alternating-pedal cadence rule — 2.2 (replaces only the press→power step).
- Formal grounding probe, snapping, coyote time, jump — 4.1.
- Visual lean on `BikeBody`, camera — 3.x.
- Debug HUD readout of speed/grounded — 7.2.
