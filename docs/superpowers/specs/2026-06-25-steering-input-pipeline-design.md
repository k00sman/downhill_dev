# Ticket 3.1 - Steering Input Pipeline - Design Spec

**Date:** 2026-06-25
**Ticket:** 3.1 (Phase 3 - Turning, braking, and camera)
**Depends on:** Ticket 2.1 (downhill forward movement model) - done.

## Goal

Make the bicycle respond reliably to left/right turn input at low-to-medium
speed, without speed-sensitive handling, death wobble, braking, or crash logic.
This ticket introduces an explicit heading/yaw model so the bike can follow the
trail intentionally instead of driving straight through curves.

## Acceptance Criteria

- Left/right input turns the bicycle reliably.
- Steering feels controllable at low-to-medium speed.
- No extreme oscillation or instant 180-degree snapping occurs.
- Banked or curved downhill terrain can influence heading through the steering
  model, so the bike naturally follows readable slope curves instead of driving
  straight through them.

## Current State

- `PlayerInputReader` already exposes a polled `Turn` float from the `Bike`
  action map. Gameplay code should continue to read turn input only through this
  component.
- `PlayerBikeController` owns ground probing, pedal accumulation, Rigidbody
  setup, and visual terrain pitch. While grounded, it calls
  `BikeMovementModel.Step` using `transform.forward` as the facing direction.
- The root Rigidbody currently freezes all rotation. This was intentional until
  Ticket 3.1 added explicit steering ownership.
- `BikeMovementModel` is a pure, serializable forward-speed model. It should not
  gain steering responsibility.

## Chosen Approach

Use an explicit root-heading model. Add a pure, serializable
`BikeSteeringModel` that computes a bounded yaw change from player turn input,
current heading, velocity, a signed side-to-side terrain bank angle, and `dt`.
`PlayerBikeController` applies that yaw to the root transform before calling
`BikeMovementModel.Step`.

Terrain influence is explicit and bounded:

- terrain can turn the bike even with no player turn input;
- terrain yaw turns away from the raised side of banked ground, simulating the
  way a bicycle naturally falls/turns downhill across camber;
- `PlayerBikeController` measures side-to-side height with left/right terrain
  probes; raw averaged ground normals are not used as steering input because
  ordinary bump/contact noise can look like roll;
- small bank angles are ignored so minor side-height noise does not become
  steering drift;
- terrain yaw is capped separately from player yaw rate;
- Rigidbody physics rotation remains frozen, while yaw is assigned explicitly by
  controller code so contact impulses cannot turn the bike.

This gives future camera, braking, crash, and debug systems a real bike heading
without letting physics contacts own rotation.

## Alternatives Considered

1. **Velocity-only steering, root yaw frozen.** Lower physics risk, but the bike
   would point one way while moving another. That mismatch makes camera follow,
   braking direction, crash checks, and debug readouts harder to reason about.
2. **Manual root yaw only, no terrain assist.** Simple and robust, but it would
   knowingly miss the terrain-influence acceptance criterion.

## `BikeSteeringModel`

Create `Assets/Scripts/Player/BikeSteeringModel.cs` in namespace
`Downhill.Player`.

`BikeSteeringModel` is a `[System.Serializable]` class with one public pure
method. It does not touch `Rigidbody`, transforms, raycasts, or input actions.

Starting tuning fields:

| Field | Start | Meaning |
|---|---:|---|
| `turnRateDegreesPerSecond` | `100` | Baseline low-to-medium speed turn rate. |
| `maxYawDeltaDegrees` | `8` | Per-step clamp against snapping or stalls. |
| `minSpeedForSteering` | `0.5` | Minimum forward speed before steering engages. |
| `turnDeadzone` | `0.15` | Ignore tiny input noise and common stick drift. |
| `terrainBankDeadzoneDegrees` | `8` | Ignore minor side-height noise before terrain steering engages. |
| `terrainTurnStrength` | `0.35` | How much banked terrain turns the bike away from the raised side. |
| `terrainTurnMaxDegreesPerSecond` | `35` | Hard cap for terrain yaw. |

Method shape:

```csharp
public float StepYawDeltaDegrees(
    Vector3 currentForward,
    Vector3 velocity,
    float terrainBankDegrees,
    float turnInput,
    float dt)
```

Rules:

1. Flatten `currentForward` onto the horizontal plane. If degenerate, return 0.
2. Clamp `turnInput` to `[-1, 1]` and apply `turnDeadzone`; input inside the
   deadzone contributes no player yaw, but terrain yaw can still act.
3. Compute horizontal forward speed with
   `Vector3.Dot(velocity, flatForward)`. If below `minSpeedForSteering`, return
   0 so the bike does not spin in place.
4. Compute player yaw:
   `turnInput * turnRateDegreesPerSecond * dt`.
5. Compute terrain yaw from `terrainBankDegrees`, where positive means the
   terrain is higher on the bike's right side. Ignore bank angles inside
   `terrainBankDeadzoneDegrees`; otherwise turn opposite the bank sign, which
   turns the bike away from the raised side of the terrain. The result is capped
   by `terrainTurnMaxDegreesPerSecond * dt`.
6. Add player yaw and terrain yaw, then clamp the final per-step delta to
   `[-maxYawDeltaDegrees, maxYawDeltaDegrees]`.

This deliberately avoids a speed-sensitive steering curve. Ticket 3.2 owns
speed-sensitive steering, instability, and death wobble.

## `PlayerBikeController` Integration

Modify `Assets/Scripts/Player/PlayerBikeController.cs`.

Add serialized field:

```csharp
[Header("Steering (Ticket 3.1)")]
[SerializeField] private BikeSteeringModel _steering = new();
```

Add a side-bank probe field:

```csharp
[SerializeField] private float _terrainTurnProbeHalfWidth = 0.45f;
```

Update Rigidbody setup:

- keep `FreezeRotationX`, `FreezeRotationY`, and `FreezeRotationZ` enabled so
  physics contacts cannot rotate the bike;
- do not use physics torque or angular velocity for steering;
- apply controller-owned yaw by directly assigning the root/Rigidbody rotation.

Grounded `FixedUpdate` flow becomes:

1. Decay pedal power.
2. Probe ground.
3. If airborne, update visual pitch toward neutral and let Rigidbody gravity own
   the arc.
4. If grounded, measure side-to-side terrain bank with two downward probes at
   `_groundCheck.position +/- transform.right * _terrainTurnProbeHalfWidth`.
5. Ask `_steering` for a yaw delta using `_input.Turn`, `_body.linearVelocity`,
   the current `transform.forward`, the measured bank angle, and `dt`.
6. Apply yaw to the root/Rigidbody rotation with a yaw-only quaternion.
7. Update visual terrain pitch from the averaged ground normal.
8. Call `_movement.Step` with the updated `transform.forward`.

Ordering matters: steering first, movement second. The updated heading should be
the direction that forward movement uses during the same physics step.

## Testing

### EditMode - `BikeSteeringModelTests.cs`

Add pure tests for the model:

- **Right input returns positive yaw.** With enough forward speed on flat ground,
  right turn input produces positive yaw.
- **Left input returns negative yaw.** Same setup, left input produces negative
  yaw.
- **No spin at rest.** With zero velocity, non-zero turn input returns zero.
- **Deadzone suppresses tiny input.** Input smaller than `turnDeadzone` returns
  zero.
- **Yaw delta is clamped.** Very high turn-rate tuning cannot exceed
  `maxYawDeltaDegrees`.
- **Terrain steers on camber.** Banked ground with zero turn input produces yaw
  away from the raised side.
- **Small bank does not steer.** Minor side-height bank values inside the
  terrain deadzone return zero.
- **Terrain can help or resist input.** A bank that agrees with turn input
  increases magnitude within the terrain cap; the opposite bank reduces the
  player's turn without overpowering full input at default tuning.

### PlayMode - Controller/Prefab Tests

Update or add tests around the prefab/controller:

- Existing yaw-freeze test changes to assert physics rotation remains fully
  frozen while controller-owned steering can still change root yaw.
- On flat ground at forward speed, setting turn input or using a small test seam
  causes the root yaw to change over fixed updates without unexpected logs.
- Forward velocity follows the updated heading after steering, rather than
  continuing along the original world-forward line.
- On banked ground at forward speed with no turn input, heading rotates away
  from the raised side while Rigidbody physics rotation remains frozen.
- On downhill pitch without side-to-side bank and no turn input, heading does
  not rotate, protecting against random self-steer on ordinary slopes.
- PlayMode input tests register keyboard, mouse, and gamepad devices under
  `InputTestFixture`, because the shipped action map resolves all three device
  families.

If direct input synthesis is awkward, add a minimal controller test seam rather
than weakening the behavior assertion. Do not add raw key checks.

## Files

**Create**

- `Assets/Scripts/Player/BikeSteeringModel.cs` (+ `.meta`)
- `Assets/Tests/EditMode/BikeSteeringModelTests.cs` (+ `.meta`)

**Modify**

- `Assets/Scripts/Player/PlayerBikeController.cs`
- `Assets/Tests/PlayMode/PlayerBikeControllerTests.cs`
- `Assets/Tests/PlayMode/BikeMovementControllerTests.cs` if existing movement
  assertions need to account for yaw being code-owned.
- `CHANGELOG.md`
- `docs/codemap.md` after implementation

No new assembly definitions are needed. Reuse `Downhill.Player` and the existing
test assemblies.

## Out Of Scope

- Speed-sensitive steering, high-speed instability, and death wobble - Ticket
  3.2.
- Front/rear braking - Ticket 3.3.
- Chase camera/freelook behavior - Ticket 3.4.
- Crash, health, and monster interactions - later phases.

## Verification

- Run `./scripts/lint.sh` if the local Unity-generated project files are
  available.
- Ask the user to run Unity Test Runner EditMode and PlayMode tests because the
  Unity editor generally cannot run headless in this environment.
- Regenerate the codemap after implementation with
  `bash scripts/generate-codemap.sh`.
