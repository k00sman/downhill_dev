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

## Sprint exit criteria

Sprint 2 is complete when:
- Alternating left/right pedals drives the bike forward and same-side spam is penalized.
- The shipped input action asset matches the intended pedal scheme: LMB/RMB on
  keyboard+mouse.
- Steering becomes measurably harder to control at high speed.
- A playtester can answer: does the input feel distinctive and risky compared to a simple hold-to-go mechanic?
