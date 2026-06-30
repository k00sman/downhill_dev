# Sprint 2 — Advanced Input

**Goal question:** Does input feel distinctive and risky?

**Sprint status:** In progress

> Research conclusion: the current bike code is a solid, barebones authored
> arcade model, not a full bicycle simulation. Do not pause for a dedicated
> physics rewrite before this sprint. Fold the physics findings into small,
> tunable, testable models here and in Sprint 3: cadence, speed-sensitive
> steering, wobble hooks, grounding state, and crash events.

---

### Ticket 2.1 - Implement alternating pedal input logic

**Goal:** Support left/right pedaling as a distinct mechanic rather than a single accelerate button. Per the README inputs, the left input (LMB / LT) drives the left leg and the right input (RMB / RT) drives the right leg, read via the `PedalLeft` / `PedalRight` actions.

**Dependencies:** Ticket 1.3.

**Status:** In progress

**Files:**
- `PedalInputEvaluator`
- `PlayerBikeController`
- `DownhillControls.inputactions`
- `PlayerInputReader`

**Research task for Opus:** Determine a simple rule set for alternating pedal presses that feels intentional, avoids spam exploits, and can degrade gracefully into a simpler fallback if needed.

**Subagent tasks:**
- Research subagent: write pedal cadence rules and edge-case handling.
- Coding subagent A: implement pedal state tracking and cadence evaluation.
- Coding subagent B: integrate cadence output into forward drive.

**Acceptance criteria:**
- The `PedalLeft` / `PedalRight` bindings are normalized before cadence work:
  LMB/RMB for keyboard+mouse and LT/RT for gamepad. Front/rear brakes stay on
  W/S and LB/RB unless playtests force a later remap.
- Alternating left and right inputs provides forward drive.
- Repeating the same side only is less effective or ignored, based on the spec.
- The system exposes tuning values for cadence window and drive bonus.
- EditMode tests cover cadence timing, same-side spam, stale input timeout,
  and any fallback mode; PlayMode input tests cover the shipped LMB/RMB action
  bindings.

**Notes:**
- Follow-up tuning pass on 2026-06-28 further reduced downhill speed by 20%,
  cut pedal drive by 50%, and moved pedal view bob to camera lens shift so it
  reads as view feedback instead of making the bike rig appear to bounce.
- Follow-up tuning pass on 2026-06-28 reduced pedal acceleration and cadence
  drive by about 20%, added a small pedal-power camera bob, and made front brake
  input suppress automatic velocity/fall-line heading alignment so braking
  should slow the bike without subtly pulling it back toward the center line.
- Implementation pass complete locally as of 2026-06-28; final ticket closure
  still needs Unity Test Runner results and an in-Editor playtest to confirm the
  cadence tuning feels intentional.
- Tuning request 2026-06-29: pedal speed gain ramps up too slowly when starting
  from a standstill (0 speed). Increase the from-zero acceleration by ~15% so
  the bike picks up off the line more responsively. Bias this toward the low-end
  ramp; do not raise top speed. Re-confirm via playtest that it still feels like
  earned cadence drive, not a hold-to-go boost.
- This is a high-risk mechanic (README open design risk: "separate left/right pedaling may be novel but awkward"); keep the logic isolated so it can be swapped for a simpler hold-to-go fallback.
- **Timing only:** drive comes from alternation cadence, not press strength — trigger depth (analog LT/RT) is ignored so controller and mouse behave identically. Leave a hook for layering pressure-modulation later if it improves feel, but don't build it now.
- Keep `PedalInputEvaluator` pure and free of Unity input APIs. It should consume
  `PlayerInputReader` pedal events plus timestamps, then output normalized drive
  for `PlayerBikeController` / `BikeMovementModel`.
- The current Sprint 1 control asset may still reflect rough placeholder
  bindings. Treat the LMB/RMB normalization above as part of this ticket, not as
  a separate controls redesign.

---

### Ticket 2.2 - Add speed-sensitive steering

**Goal:** Make steering more dangerous and less forgiving at higher speed.

**Dependencies:** Ticket 1.4.

**Files:**
- `BikeSteeringModel`
- tuning config file or scriptable settings object

**Research task for Opus:** Specify how steering authority and instability should scale with speed for the prototype.

**Subagent tasks:**
- Research subagent: propose a speed-to-steering response curve.
- Coding subagent A: implement speed scaling and optional wobble hooks.
- Coding subagent B: expose editable response curves or scalar values.

**Acceptance criteria:**
- Steering response changes with speed.
- High speed makes sharp corrections harder or riskier.
- Designers can tune the behavior without touching core code.
- EditMode tests cover the speed response at low, medium, high, and overspeed
  bands, including deadzone behavior and clamp limits.

**Notes:**
- Ticket 2.1 now owns cadence drive via a pure `PedalInputEvaluator` feeding the
  existing `PedalPower` accumulator. Keep speed-sensitive steering independent of
  cadence state unless playtests show a clear need to couple them.
- This ticket owns the speed→steering response curve and may expose an optional wobble hook. The full **death wobble** at excess speed is its own ticket (3.5, Sprint 3) and builds on this — don't fully implement the wobble here.
- Prefer a curve/scalar set that changes steering authority inside the existing
  `BikeSteeringModel` rather than introducing a second steering system. The
  model can stay arcade-authored: real bicycle lean/steer dynamics are too
  large for this prototype stage.
- If the same speed bands will feed death wobble, landing instability, or debug
  readouts, move tuning into a small ScriptableObject or serializable settings
  object. Otherwise keep the settings serialized on `BikeSteeringModel`.

---

### Ticket 2.3 - Bicycle movement refinement

**Goal:** Fix three movement-feel problems that surfaced in playtesting: the bike
cannot climb, turning snaps instantly with no smoothing, and the turn arc is far
too wide. Make the ride feel like a controllable bicycle, not a rail-locked rig.

**Dependencies:** Ticket 2.2.

**Status:** Open

**Files (confirmed 2026-06-29):**
- `Assets/Scripts/Player/BikeMovementModel.cs` — climb cap, slope drive,
  grounding (`maxGroundedUpSpeed`, `FollowGroundPlane`).
- `Assets/Scripts/Player/BikeSteeringModel.cs` — turn rate, per-step yaw clamp,
  steering authority, smoothing (`turnRateDegreesPerSecond`,
  `maxYawDeltaDegrees`, `StepYawDeltaDegrees`).
- `Assets/Scripts/Player/PlayerBikeController.cs` — `FixedUpdate` loop,
  `ApplySteering`, ground probe.
- Tuning lives as serialized fields on the two pure models above.

**Research findings (web subagent, 2026-06-29):**
- A bike turns by leaning, not bar-twist. Steady-turn lean is
  `θ = arctan(v²/(g·R))`, so minimum turn radius grows with **v²**: max
  sustainable yaw rate decays roughly as **1/v** for a fixed lean cap. Real
  bikes are genuinely tight at low speed and wide-arcing at high speed — exactly
  the target feel.
- Most common arcade technique: scale max steer/yaw authority by speed (Unity's
  own WheelCollider docs: "at high speed use only small steer angles"), and
  drive a *target* yaw from input then ease the actual value toward it — never
  snap.
- **Smoothing:** ease the steer/yaw value with `Mathf.SmoothDamp`,
  `smoothTime ≈ 0.10–0.20 s` (≈0.08 s feels twitchy, ≥0.30 s sluggish). Or a
  framerate-independent lerp `current = Lerp(current, target, 1 - exp(-k·dt))`
  with `k ≈ 6–10`. Avoid raw `Lerp(a, b, speed·dt)` (framerate-dependent).
- **Tight-low / wide-high:** make max yaw rate a function of speed via an
  AnimationCurve — full authority at 0–30% top speed (tight, pivoty), dropping
  to ~20–35% of that at 70–100% top speed (wide, committed). "Risky at high
  speed" then emerges for free: low authority + smoothing means high-speed
  corrections overshoot, forcing the player to brake to tighten a line.
- Suggested tunables: `maxYawRateLowSpeed`, `highSpeedYawFactor` (0.2–0.35),
  `steerAuthorityVsSpeed` (AnimationCurve), `steerSmoothTime` (0.10–0.20 s),
  `inputResponseCurve`, optional `maxLeanAngle`.
- Sources: Wikipedia *Bicycle and motorcycle dynamics* & *Countersteering*,
  Bike Gremlin countersteering, Unity `WheelCollider.steerAngle` docs, Unity
  car-steering-vs-speed threads.

This overlaps Ticket 2.2's speed-sensitive steering curve — coordinate so 2.2's
authority curve and 2.3's smoothing/narrowing are one coherent model, not two.

**Sub-items:**

1. **Climbing resistance / derail on uphill.** Elevations at a climbable angle
   currently stop the bike dead and derail it instead of letting it ride up.
   - **Root cause (diagnosed 2026-06-29):** the stall is *not* a slope-angle
     check — it's `BikeMovementModel.maxGroundedUpSpeed` (0.75 m/s, line 57),
     which clamps `result.y` after `FollowGroundPlane` (lines 108–111).
     Climbing the fall line needs `vertical = horizontalSpeed · tan(slope)`;
     once the slope demands more lift than 0.75 m/s the bike keeps full
     horizontal speed but can't rise fast enough to follow the terrain, so it
     drives into the hill and the collider stops it. The de-facto max climbable
     angle is `atan(0.75 / speed)` — ~21° at 2 m/s but only ~4° at 10 m/s, which
     is why it feels "too slow" and worsens with speed.
   - **Fix direction:** keep a cap (it stops the bike rocketing skyward off
     bumps) but make it **angle-based** — derive from a tunable
     `maxClimbAngleDegrees` and current speed (`cap = horizontalSpeed ·
     tan(maxClimbAngle)`), clamped to a sane jump ceiling. Gives a readable
     "max climbable grade" instead of a hidden vertical-speed wall.
   - The bike should climb grades up to that tunable max angle, bleeding speed
     with slope rather than hitting a wall. Above the max it may stall, but must
     not derail / lose grounding on otherwise rideable terrain.

2. **Turn smoothing.** Steering currently turns "on a dime."
   - **Where:** `BikeSteeringModel.StepYawDeltaDegrees` applies player steer as
     `input · turnRateDegreesPerSecond · dt` with no easing — only the *slope*
     steer is smoothed (`SmoothSlopeSteer`, an `exp(-k·dt)` lerp). Mirror that
     pattern for player input: ease the actual yaw rate toward
     `input · maxYawRate(speed)` instead of applying it raw.
   - Use `SmoothDamp` / exp-lerp with a tunable `steerSmoothTime ≈ 0.10–0.20 s`
     (per research). Expose the strength. Keep independent of Ticket 2.2's
     authority curve unless research/playtest shows they should couple.

3. **Turn arc too wide.** Narrow it ~200% (turn ~twice as sharply for the same
   input, strongest at low speed).
   - **Where:** `BikeSteeringModel.turnRateDegreesPerSecond = 100f` sets the
     arc; the per-step clamp `maxYawDeltaDegrees = 8f` (≈400°/s at 0.02 s
     fixed step) leaves headroom to raise it. Increasing the low-speed turn
     rate tightens the arc.
   - Make max yaw rate a function of speed (AnimationCurve / `1/v` falloff per
     research): full authority at low speed (tight, pivoty), dropping to
     ~20–35% at high speed (wide, risky) — consistent with Ticket 2.2's
     speed-sensitive steering.

**Acceptance criteria:**
- The bike climbs rideable uphill grades up to a tunable max angle, bleeding
  speed with slope, and never derails / loses grounding on climbable terrain.
- Turning eases in and out smoothly instead of snapping; smoothing strength is
  designer-tunable.
- Low-speed turn arc is roughly twice as tight as before for the same input,
  while high-speed turning stays wide; turn behavior scales with speed.
- Tuning values (max climb angle, slope speed-loss, turn smoothing, turn rate /
  authority) are editable without touching core logic.
- EditMode tests cover climb behavior across slope bands (flat, climbable,
  too-steep), turn smoothing response, and turn rate at low vs high speed.

**Notes:**
- Keep changes inside the existing arcade-authored models; this is tuning and a
  smoothing layer, not a physics rewrite (consistent with the sprint research
  conclusion above).
- Sub-items 1 and 3 interact with the fall-line / heading-alignment behavior
  touched in Ticket 2.1's brake note — verify climbing and tighter turns don't
  reintroduce unwanted auto-centering.

---

## Sprint exit criteria

Sprint 2 is complete when:
- Alternating left/right pedals drives the bike forward and same-side spam is penalized.
- The shipped input action asset matches the intended pedal scheme: LMB/RMB on
  keyboard+mouse.
- Steering becomes measurably harder to control at high speed.
- The bike climbs rideable grades without derailing, turning is smoothed, and the
  low-speed turn arc is tightened (Ticket 2.3).
- A playtester can answer: does the input feel distinctive and risky compared to a simple hold-to-go mechanic?
