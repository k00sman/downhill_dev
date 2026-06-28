# Uphill Fall-Line Handling Handoff

Date: 2026-06-28

## Implementation Status

Option 3 was selected and implemented on 2026-06-28. The bike no longer uses a
strict forward-only grounded velocity model:

- `BikeMovementModel` now builds horizontal ground velocity from downhill slope
  acceleration, pedal acceleration, drag, braking, and tunable `lateralGrip`.
- Slope acceleration can create backward or lateral downhill slip when the bike
  is not pointed down the fall line.
- `BikeMovementResult` exposes velocity telemetry: forward speed, lateral speed,
  downhill direction, and fall-line alignment.
- `BikeSteeringModel` now aligns heading toward actual horizontal velocity when
  the bike is moving, and falls back to downhill fall-line alignment when the
  bike is nearly stopped on a meaningful slope.
- `GameplayDebugHUD` displays forward/lateral speed and fall-line alignment for
  tuning.

The Option 2 section remains below as historical context and as a lower-risk
alternative if this broader handling pass is rolled back.

## Purpose

This handoff originally documented two candidate solutions for the bike getting
stuck when stopped while facing uphill. It compares:

- Option 1: keep things as is.
- Option 2: low-speed fall-line yaw on pedal drive.
- Option 3: broader downhill lateral drift and velocity alignment.

The goal is to help the next implementer choose the right handling model before
writing more code. This is not an implementation plan; it is a design handoff
with enough detail to write one.

## Current System

The current bike controller is intentionally authored and simple:

- `PlayerBikeController` owns Unity components, ground probing, input wiring,
  Rigidbody velocity assignment, and visual pitch.
- `BikeMovementModel` owns grounded horizontal velocity, downhill/lateral slip,
  lateral grip damping, terrain-follow vertical velocity, and movement
  telemetry.
- `BikeSteeringModel` owns yaw delta. It currently considers player turn input,
  speed gating, side-slope/camber steering, velocity alignment, and low-speed
  fall-line alignment.
- `PedalInputEvaluator` turns alternating pedal events into `PedalPower`;
  `PlayerBikeController` normalizes that into `pedalPower01` for movement.

The previous limitation was that movement was effectively one-dimensional along
the bike's current forward heading. If the bike pointed uphill from rest,
`BikeMovementModel` could clamp forward speed to zero. If steering was also
gated by speed, the bike had no way to rotate back downhill. The implemented
Option 3 model addresses that by letting slope force create downhill slip and
letting heading alignment rotate the bike toward that movement or the fall line.

## Shared Concepts

Both options need the same terrain math.

### Fall Line

The downhill fall line is the steepest downhill direction on the current ground
plane:

```csharp
Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, groundNormal);
```

If `downhill.sqrMagnitude` is near zero, the ground is flat enough that no
fall-line behavior should apply.

For yaw-only bike rotation, use a flat version:

```csharp
Vector3 downhillFlat = Vector3.ProjectOnPlane(downhill, Vector3.up).normalized;
Vector3 forwardFlat = Vector3.ProjectOnPlane(currentForward, Vector3.up).normalized;
float signedAngleToDownhill = Vector3.SignedAngle(forwardFlat, downhillFlat, Vector3.up);
```

Positive `signedAngleToDownhill` means yaw right toward downhill; negative means
yaw left toward downhill.

### Trigger Conditions

Do not apply fall-line behavior all the time. It should be gated by:

- Grounded state.
- Meaningful slope angle.
- Low speed or near-stall state for Option 2.
- Pedal/drive intent for Option 2.
- No airborne state.
- A dead zone around already-facing-downhill to avoid twitch.

The most direct drive-intent signal in the current code is normalized
`pedalPower01`. This is not raw input; it is the accumulated output of the
alternating cadence mechanic. That is good enough for a prototype because the
fall-line assist should respond to actual drive effort, not incidental mouse or
trigger state.

## Option 2: Low-Speed Fall-Line Yaw On Pedal Drive

### Summary

When the grounded bike is nearly stopped, the ground is sloped, and the rider is
applying pedal drive, add a tunable yaw delta toward the downhill fall line. This
means pedaling while pointed uphill naturally starts the bike turning across the
slope and back down it. The bike still uses the existing forward-only movement
model once it has turned enough for gravity and pedal drive to move it.

This option keeps the current arcade handling model and adds a small, targeted
recovery force.

### Behavior

At low speed:

- If the bike is facing uphill and the rider pedals, yaw toward the fall line.
- If the bike is facing across the slope and the rider pedals, gently bias it
  toward downhill.
- If the bike is already facing downhill, do nothing.
- If the rider is not pedaling, do nothing beyond existing slope/camber steering.
- If the bike is moving faster than a tunable low-speed threshold, do not apply
  this assist.

The player experience should be: "I tried to pedal uphill from a dead stop, the
bike started to turn away from the climb, and then gravity let me continue."

### How It Fits The Current System

Best fit is inside `BikeSteeringModel`, not `BikeMovementModel`.

Reasoning:

- The output is yaw, and `BikeSteeringModel` already owns yaw.
- `PlayerBikeController` already calls steering before movement in
  `FixedUpdate`, which is the right order: first rotate toward downhill, then
  let movement compute velocity from the new facing direction.
- `BikeMovementModel` can stay one-dimensional and does not need lateral
  velocity.

The current `BikeSteeringModel.StepYawDeltaDegrees(...)` signature does not
receive drive intent. To implement Option 2 cleanly, extend the ground-normal
overload to include a drive-intent value:

```csharp
public float StepYawDeltaDegrees(
    Vector3 currentForward,
    Vector3 velocity,
    float turnInput,
    Vector3 groundNormal,
    float driveIntent01,
    float dt)
```

Keep the existing overloads as wrappers so older tests and callers stay simple.

`PlayerBikeController.FixedUpdate` already computes `pedal01`; pass that value
into steering before movement:

```csharp
float pedal01 = _pedalPowerMax > 0f ? PedalPower / _pedalPowerMax : 0f;
ApplySteering(dt, groundNormal, pedal01);
```

Then `ApplySteering` forwards `pedal01` to the steering model.

### Suggested Tuning Fields

Add serialized public fields to `BikeSteeringModel`:

```csharp
[Tooltip("Maximum speed where fall-line pedal recovery can yaw the bike.")]
public float fallLineAssistMaxSpeed = 1.0f;

[Tooltip("Minimum slope angle before fall-line pedal recovery engages.")]
public float fallLineAssistMinSlopeDegrees = 5f;

[Tooltip("Minimum normalized pedal drive before fall-line pedal recovery engages.")]
public float fallLineAssistMinDrive = 0.05f;

[Tooltip("Yaw rate toward downhill fall line at full uphill misalignment.")]
public float fallLineAssistYawRateDegreesPerSecond = 70f;

[Tooltip("Angle around downhill direction where fall-line assist stops correcting.")]
public float fallLineAssistAngleDeadzoneDegrees = 8f;
```

The exact numbers should be tuned in Editor, not locked by numeric-only tests.

### Suggested Algorithm

Inside `BikeSteeringModel`:

1. Compute normal player input yaw and side-slope yaw as today.
2. Compute optional fall-line yaw:
   - If `velocity.magnitude > fallLineAssistMaxSpeed`, return zero assist.
   - If `driveIntent01 < fallLineAssistMinDrive`, return zero assist.
   - Compute slope angle with `Vector3.Angle(groundNormal, Vector3.up)`.
   - If slope angle is below `fallLineAssistMinSlopeDegrees`, return zero assist.
   - Compute downhill fall line.
   - Compute signed yaw angle from current forward to downhill.
   - If absolute angle is below deadzone, return zero assist.
   - Scale yaw rate by how misaligned the bike is from downhill.
   - Clamp by `maxYawDeltaDegrees`.
3. Add player yaw, side-slope yaw, and fall-line yaw before final clamp.

Pseudo-code:

```csharp
private float FallLineAssistYaw(
    Vector3 flatForward,
    Vector3 groundNormal,
    Vector3 velocity,
    float driveIntent01,
    float dt)
{
    if (velocity.magnitude > fallLineAssistMaxSpeed)
    {
        return 0f;
    }

    if (driveIntent01 < fallLineAssistMinDrive)
    {
        return 0f;
    }

    float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
    if (slopeAngle < fallLineAssistMinSlopeDegrees)
    {
        return 0f;
    }

    Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, groundNormal);
    Vector3 downhillFlat = Vector3.ProjectOnPlane(downhill, Vector3.up);
    if (downhillFlat.sqrMagnitude < 1e-6f)
    {
        return 0f;
    }

    downhillFlat.Normalize();

    float angle = Vector3.SignedAngle(flatForward, downhillFlat, Vector3.up);
    if (Mathf.Abs(angle) < fallLineAssistAngleDeadzoneDegrees)
    {
        return 0f;
    }

    float alignment01 = Mathf.Clamp01(Mathf.Abs(angle) / 180f);
    float yawRate = fallLineAssistYawRateDegreesPerSecond * alignment01;
    return Mathf.Sign(angle) * yawRate * dt;
}
```

### Tests

Add tests for behavior, not exact tuning constants:

- EditMode: stopped, facing uphill, with drive intent returns yaw toward downhill.
- EditMode: stopped, facing uphill, no drive intent returns zero assist.
- EditMode: stopped on flat ground, with drive intent returns zero assist.
- EditMode: moving above assist speed does not receive fall-line assist.
- EditMode: already near downhill direction does not twitch.
- Existing tests should continue covering normal turn input, deadzone, and
  side-slope steering.

PlayMode/manual checks:

- Put the bike on a slope, face uphill, stop, alternate pedals. It should turn
  toward downhill and begin moving.
- Repeat without pedaling. It should not spin by itself.
- Face across a slope and pedal. It may gently bias downhill, but should not
  seize control.
- Ride normal downhill trail. Assist should be invisible at normal speed.

### Benefits

- Minimal change to the current architecture.
- Preserves the authored arcade feel.
- Directly addresses the real player complaint.
- Keeps behavior tunable on `BikeSteeringModel`.
- Easy to test with pure EditMode tests.
- Low risk to crash, terrain contact, and future monster/chase systems.
- The mechanic feels plausible: drive effort on a slope causes the bike to
  rotate toward the fall line.

### Negatives

- It is still an authored assist, not a physical model.
- If tuned too high, it can feel like hidden steering.
- If it engages while the player is trying to climb intentionally, it can fight
  the player.
- It depends on `pedalPower01`, which is an accumulator. Assist may continue for
  a short time after the last pedal press unless tuned around decay.
- It will not solve broader sideways sliding or off-camber drift; it only solves
  low-speed fall-line recovery.

### Risks

- False positives on small terrain noise: avoid by using minimum slope angle and
  angle deadzone.
- Control fight near switchbacks: avoid by limiting to low speeds and drive
  intent.
- Oscillation when almost aligned downhill: avoid by deadzone and low yaw rate.
- Overlap with current side-slope `slopeInfluence`: keep fall-line assist
  separate and additive, then tune both in Editor.
- The current temporary "stopped uphill steering escape" may become redundant.
  If Option 2 is implemented, review and likely remove that special case so the
  model has one recovery rule instead of two.

### Reasons To Pick Option 2

Pick this if the goal is to keep the prototype simple, readable, and tunable
while fixing the uphill-stall behavior quickly. This option fits the current
Sprint 2/Sprint 3 architecture: authored handling models, focused tests, no
large physics rewrite.

This is the recommended next implementation.

## Option 3: Downhill Lateral Drift And Velocity Alignment

### Summary

Instead of only yawing the bike at low speed, model the slope as a downhill
force that can create lateral velocity and gradually align the bike with its
actual downhill movement. The bike would not be constrained to move only along
its forward vector. When stopped uphill, gravity produces downhill slip; the
bike's heading then rotates toward the movement direction.

This option is closer to physical intuition, but it changes the core movement
model substantially.

### Behavior

On sloped ground:

- Gravity along the ground contributes to world-space velocity, not only
  forward speed.
- If the bike points uphill from rest, downhill velocity begins to build even if
  the bike's forward speed is zero.
- The bike yaw gradually aligns toward the direction of travel or downhill
  slip.
- Pedaling adds forward drive, but slope-driven movement can include lateral or
  backward/downhill components.
- Sideways drift can become a real handling factor on off-camber terrain.

The player experience should be: "The bike is a body on a slope; if I point it
the wrong way, it slips or rolls downhill and the heading follows."

### How It Fits The Current System

Option 3 would require a broader redesign of `BikeMovementModel` and
`BikeSteeringModel`.

`BikeMovementModel` would no longer return velocity only from scalar speed along
`forwardFlat` / `forwardOnSlope`. It would need to preserve and update a
world-space velocity vector with at least these components:

- Forward drive from pedaling.
- Ground-plane gravity acceleration.
- Longitudinal drag.
- Lateral grip/slide damping.
- Braking projected against current motion or wheel direction.
- Speed cap applied to planar velocity, not just forward scalar speed.

`BikeSteeringModel` would need to consider velocity heading:

- Player steering still controls yaw.
- At low speed or high slip, the bike can align toward velocity/downhill.
- At speed, alignment should be slower so the rider still feels in control.

`PlayerBikeController` would likely need to pass more context into both models:

- Ground normal.
- Current velocity.
- Forward vector.
- Pedal drive.
- Brake input.
- Maybe a lateral-grip/surface modifier later.

This could still stay inside the existing pure-model architecture, but it is no
longer a narrow bug fix.

### Suggested Model Shape

Keep pure logic, but split responsibilities more clearly:

- `BikeMovementModel`: computes new velocity.
- `BikeHeadingModel` or expanded `BikeSteeringModel`: computes yaw from input,
  terrain, and velocity alignment.
- `PlayerBikeController`: probes ground and applies returned velocity/rotation.

Possible `BikeMovementModel.Step` shape:

```csharp
public BikeMovementResult Step(
    Vector3 velocity,
    Vector3 forward,
    Vector3 groundNormal,
    float pedalPower01,
    float frontBrake,
    float rearBrake,
    float dt)
```

Where:

```csharp
public readonly struct BikeMovementResult
{
    public Vector3 Velocity { get; }
    public Vector3 HorizontalVelocity { get; }
    public Vector3 DownhillDirection { get; }
    public float ForwardSpeed { get; }
    public float LateralSpeed { get; }
    public float FallLineAlignmentDegrees { get; }
}
```

That result gives the steering/alignment model better information without
making `PlayerBikeController` derive everything itself.

### Suggested Movement Algorithm

1. Compute a normalized forward direction projected onto the ground pitch.
2. Compute downhill acceleration:

```csharp
Vector3 slopeAccel = Vector3.ProjectOnPlane(Vector3.down * gravity, groundNormal) * slopeDriveGain;
```

3. Compute pedal acceleration along the bike's ground-projected forward:

```csharp
Vector3 pedalAccelVec = forwardOnGround * (pedalPower01 * pedalAccel);
```

4. Split current velocity into ground-plane components:

```csharp
Vector3 groundVelocity = Vector3.ProjectOnPlane(velocity, groundNormal);
float forwardSpeed = Vector3.Dot(groundVelocity, forwardOnGround);
Vector3 lateralVelocity = groundVelocity - (forwardOnGround * forwardSpeed);
```

5. Apply acceleration:

```csharp
groundVelocity += (slopeAccel + pedalAccelVec) * dt;
```

6. Apply damping:

- Longitudinal drag against full ground velocity or forward component.
- Lateral grip damping against lateral velocity.
- Braking against forward or motion direction depending on brake model.

7. Clamp planar speed:

```csharp
groundVelocity = Vector3.ClampMagnitude(groundVelocity, maxSpeed);
```

8. Rebuild world velocity with controlled vertical terrain-following, preserving
   the existing upward velocity cap.

### Suggested Heading Alignment Algorithm

Add velocity alignment to steering:

- Compute desired movement direction from ground-plane velocity if magnitude is
  above a small threshold.
- If velocity is too small, fall back to downhill fall line.
- Compute signed angle from bike forward to desired direction.
- Apply a tunable alignment yaw rate.
- Scale by slip amount or low-speed state.
- Add player steering yaw.
- Clamp final yaw delta.

Tuning fields might include:

```csharp
public float velocityAlignmentMinSpeed = 0.5f;
public float velocityAlignmentYawRateDegreesPerSecond = 45f;
public float lowSpeedDownhillAlignmentYawRateDegreesPerSecond = 70f;
public float lateralGrip = 6f;
public float downhillSlipGain = 1f;
```

### Tests

Option 3 needs broader test coverage because it changes movement contracts:

- EditMode: facing uphill from rest on a slope builds downhill ground-plane
  velocity.
- EditMode: lateral velocity damps on flat ground.
- EditMode: side slope creates lateral/downhill drift but respects speed cap.
- EditMode: pedal input still adds forward acceleration on flat ground.
- EditMode: braking reduces motion direction consistently.
- EditMode: heading alignment turns toward downhill velocity at low speed.
- PlayMode: bike remains grounded on continuous slopes without popping.
- PlayMode: steering still rotates heading and velocity coherently.
- Manual: off-camber sections are readable and do not feel like loss of input.

### Benefits

- More physically plausible.
- Solves uphill stall as a natural consequence of slope velocity.
- Makes side slopes, off-camber turns, surface types, and future terrain handling
  richer.
- Creates a better foundation for Sprint 9 surface handling.
- Gives debug HUD more meaningful values: forward speed, lateral slip, downhill
  slip, alignment error.
- May reduce the number of special-case steering assists over time.

### Negatives

- Larger implementation with more failure modes.
- Changes the feel of the entire bike, not just uphill recovery.
- Risks making the prototype harder to tune before the core ride is locked.
- Can make the bike feel slippery or unresponsive if lateral damping is wrong.
- Braking and crash severity become more complex because velocity may not align
  with forward.
- Existing PlayMode tests for velocity following heading may need redesign.
- More interaction with collision, ground probing, terrain noise, and future
  crash states.

### Risks

- Terrain normals can be noisy. The implemented drift model exposes that noise
  more than the previous forward-only model.
- The root Rigidbody rotation is frozen. Heading alignment remains code-owned,
  so all physical-looking yaw must still be authored.
- Downhill drift can fight narrow trail readability if the bike slides sideways
  off intended paths.
- The player may feel punished for exploring uphill or stopping.
- Existing debug HUD values may become misleading unless expanded to show
  lateral slip and fall-line alignment.
- This can become a stealth full-bike-simulation rewrite if not scoped tightly.

### Reasons To Pick Option 3

Pick this if the team wants slope, surface, and off-camber handling to become a
core feel pillar soon, and is willing to spend a full ticket or more reshaping
movement. It is appropriate if a forward-only model keeps producing edge cases
that require special assists.

Do not pick this merely to fix uphill stall. It is a system-level movement pass.

## Comparison

| Criterion | Option 2: Fall-Line Pedal Yaw | Option 3: Lateral Drift + Alignment |
|---|---|---|
| Scope | Small, focused | Large, systemic |
| Architecture impact | Extends `BikeSteeringModel` | Redesigns movement and steering contracts |
| Uphill-stall fix | Direct and tunable | Emergent from slope velocity |
| Physics plausibility | Medium | Higher |
| Risk to current feel | Low | High |
| Test burden | Moderate | High |
| Tuning burden | Low to medium | High |
| Surface-system foundation | Limited | Strong |
| Best timing | Now / Sprint 2-3 | Later / Sprint 9 or dedicated handling pass |

## Recommendation

Implement Option 2 first.

The prototype's immediate need is not a full physical bike model; it is to make
the current authored handling avoid a dead-end state and feel plausible. Option
2 does that while preserving the current separation of responsibilities:
`PlayerBikeController` probes ground and applies results, `BikeSteeringModel`
owns yaw, and `BikeMovementModel` remains simple.

Option 3 should remain a documented future path. Revisit it if:

- Low-speed assists keep stacking up.
- Off-camber handling becomes central to the game loop.
- Surface handling needs real lateral grip.
- Playtesters describe the bike as "on rails" or physically incoherent.

## Handoff Notes For Next Implementer

If implementing Option 2:

1. Review any current stopped-uphill manual steering escape in
   `BikeSteeringModel`. Decide whether to remove it or fold it into fall-line
   assist. Avoid keeping two overlapping low-speed recovery rules.
2. Extend `BikeSteeringModel.StepYawDeltaDegrees` to accept `driveIntent01`
   while preserving existing overloads.
3. Pass normalized `PedalPower` from `PlayerBikeController` into steering before
   movement.
4. Add fall-line assist tuning fields to `BikeSteeringModel`.
5. Add behavior tests for drive-gated uphill recovery and no-drive no-spin.
6. Do not add numeric-only tests for chosen tuning values.
7. Ask for Unity Test Runner results and an in-Editor playtest on a slope.

If implementing Option 3:

1. Treat it as a new handling ticket, not a patch.
2. Write a short spec first because it changes the movement contract.
3. Build tests around pure movement math before touching the controller.
4. Expect to update PlayMode movement tests that currently assume velocity
   follows bike forward.
5. Add debug HUD fields for lateral slip and fall-line alignment before tuning.
6. Keep root Rigidbody rotation code-owned unless crash/air-control work changes
   that architecture.

## Open Decision

The main design decision is whether uphill recovery should be:

- An authored low-speed yaw assist triggered by pedal drive, preserving the
  current arcade model.
- A consequence of a broader ground-plane velocity model with lateral drift and
  heading alignment.

The pragmatic path is Option 2 now, Option 3 only if the prototype needs richer
slope and surface handling after the core ride/chase loop is validated.
