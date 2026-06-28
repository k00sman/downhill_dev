# Sprint 3 — Crashes

**Goal question:** Do mistakes feel meaningful?

**Sprint status:** Not started

> Unity physics guardrail: keep the bike an authored gameplay controller, not a
> free bicycle simulation. `PlayerBikeController` owns Rigidbody integration;
> pure model classes own tunable decisions. Add state and event seams before
> adding new effects so jump, crash, health, audio, and chase systems all consume
> the same facts instead of re-detecting them.

---

### Ticket 3.1 - Implement jump input and airtime state

**Goal:** Allow the bicycle to leave the ground intentionally on jumpable terrain.

**Dependencies:** Ticket 1.3 and Ticket 1.4.

**Files:**
- `BikeJumpModel`
- `BikeGroundingProbe`
- `PlayerBikeController`

**Research task for Opus:** Define a minimal jump model for the prototype, including grounded checks and airborne restrictions.

**Subagent tasks:**
- Research subagent: document grounded state rules and jump impulse behavior.
- Coding subagent A: implement ground detection.
- Coding subagent B: implement jump state and impulse.
- Coding subagent C: integrate jump restrictions into the controller.

**Acceptance criteria:**
- Jump only works when grounded, triggered by the `Jump` action (README: Space or the south face button).
- The bicycle can enter and leave an airborne state cleanly.
- Basic landing detection works.
- **No air control** — steering input is ignored while airborne, so takeoff commits the landing (reinforces reading the trail before committing).
- Grounded movement and airborne movement have explicit ownership: the grounded
  bike model owns velocity while grounded, and Rigidbody gravity owns the arc
  while airborne.
- Grounding output is queryable as one reusable result: grounded flag, normal,
  hit point/distance, current collider, and landing impact data.

**Notes:**
- Jumps are committed by design. Don't add mid-air steering/lean; the only post-jump handling effect is the landing instability hook (Ticket 3.2).
- Extract only as much `BikeGroundingProbe` as this ticket needs. The existing
  multi-point probe behavior should be preserved so steep slopes and smooth
  bumps do not regress into false airborne states.
- Use `FixedUpdate`/physics-step data for jump and landing decisions; avoid
  mixing render-frame camera/input smoothing with physics state.

---

### Ticket 3.2 - Add landing instability hook

**Goal:** Make jumps carry risk by destabilizing control on landing.

**Dependencies:** Ticket 3.1 and Ticket 2.2.

**Files:**
- `BikeJumpModel`
- `BikeSteeringModel`

**Research task for Opus:** Specify a short post-landing penalty model that increases tension without instantly causing failure.

**Subagent tasks:**
- Research subagent: define landing penalty timing and scaling inputs.
- Coding subagent A: implement a temporary landing penalty signal.
- Coding subagent B: consume the penalty in steering or stability code.

**Acceptance criteria:**
- Landings briefly affect handling.
- The effect is tunable.
- The effect is visible in play but not overwhelmingly punishing.
- The landing penalty is emitted as a short-lived signal that steering, wobble,
  and debug HUD code can read without duplicating landing detection.

**Notes:**
- Scale instability from landing severity (for example downward velocity or
  landing impulse), then clamp it. This should feel like destabilized control,
  not a random failure roll.

---

### Ticket 3.3 - Implement basic collision severity detection

**Goal:** Distinguish harmless bumps from dangerous crashes.

**Dependencies:** Ticket 1.3.

**Files:**
- `BikeCollisionEvaluator`
- `PlayerBikeController`

**Research task for Opus:** Define how collision speed, angle, and impacted object type should map to severity tiers in the prototype.

**Subagent tasks:**
- Research subagent: define severity thresholds and object filtering.
- Coding subagent A: implement collision data capture and evaluation.
- Coding subagent B: emit standardized crash events for other systems.

**Acceptance criteria:**
- Collisions are categorized into at least safe, damaging, and severe.
- Other systems can react to standardized crash events.
- Tree and rock collisions can be tuned differently if desired.
- Collision output includes enough context for later systems: severity tier,
  impact speed, approximate impact direction/normal, impacted object category,
  and whether the crash is recoverable or fatal.

**Notes:**
- `BikeCollisionEvaluator` should be the single crash-severity source. Health,
  audio, recovery, and death handling should consume its event output instead of
  re-evaluating collisions independently.
- Keep object filtering data-driven and Unity-friendly: layers/tags or small
  marker components are preferable to hardcoded names.

---

### Ticket 3.4 - Implement crash and recovery state

**Goal:** Add a recoverable non-fatal crash outcome.

**Dependencies:** Ticket 3.3.

**Files:**
- `BikeCrashStateMachine`
- `PlayerBikeController`
- optional `BikeRecoveryQTE`

**Research task for Opus:** Define the simplest crash state flow that interrupts movement, creates vulnerability, and can later support the QTE idea.

**Subagent tasks:**
- Research subagent: document crash state transitions, lockouts, and the recover-in-place reposition.
- Coding subagent A: implement crash state machine.
- Coding subagent B: add movement/input lockouts and recovery timing.
- Coding subagent C: implement the minimal recovery QTE and the remount reposition.

**Acceptance criteria:**
- Severe but non-fatal crashes interrupt normal riding (moderate-to-high-speed impacts throw the rider off the bike, per the README crash outcomes).
- The player recovers **in place** via a **minimal QTE** (a simple timed/button-mash interaction) and remounts to return to control.
- On crash, the bike comes to rest where the rider lands; a short recovery spline runs from the fallen bike back onto the rider. Completing the QTE returns the player to the bike, **repositioned/re-oriented so they are not pointed straight back at the obstacle they just hit**.
- Recovery timing is long enough to matter for chase pressure — the delay is exactly what lets the monster close in.
- Severity above the fatal threshold skips recovery and routes to instant death (handled in Sprint 4); this ticket owns only the recoverable path.

**Notes:**
- Recovery is a **minimal QTE** by decision (README allows QTE or timed auto-recover; we commit to the QTE to test whether it adds tension). Keep the interaction isolated so it can be swapped for a timed auto-recover if it hurts pacing.
- The remount reposition exists so the player doesn't immediately re-collide with the same hazard on recovery — face them down-trail along a clear line.
- Crash recovery is an open design risk (README): if it triggers too often it breaks pacing — make the trigger threshold and recovery duration tunable.
- Crash/recovery should lock normal movement ownership cleanly: no pedal drive,
  steering, braking, jump, or death-wobble updates while the state machine is in
  crashed/recovering states.

---

### Ticket 3.5 - Add death wobble at excess speed

**Goal:** Make excessive speed visibly punish control through a destabilizing wobble.

**Dependencies:** Ticket 2.2.

**Files:**
- `BikeSteeringModel`
- speed/wobble tuning config or scriptable settings object

**Research task for Opus:** Specify a "death wobble" that grows as speed exceeds a threshold — how it perturbs heading/steering, how it scales with overspeed, and how it resolves when the player slows.

**Subagent tasks:**
- Research subagent: define the overspeed threshold and the wobble growth/decay model.
- Coding subagent A: implement the wobble perturbation on the steering/heading signal.
- Coding subagent B: expose threshold, amplitude, and frequency for tuning.

**Acceptance criteria:**
- Above a tunable speed threshold the bike develops an increasing wobble that makes holding a line harder.
- The wobble eases off when the player drops back under the threshold.
- Threshold, amplitude, and frequency are tunable without code changes.

**Notes:**
- This realizes the README handling goal "excessive speed should create a death wobble effect," promoted from the optional wobble hook noted in Ticket 2.2.
- Keep it a perturbation on the steering model, not a separate physics system — it should compose with speed-sensitive steering (2.2).
- Make wobble deterministic and inspectable for tuning: threshold, amplitude,
  frequency, growth, and decay should be serialized; debug HUD should be able to
  show current wobble intensity.
- Avoid adding procedural Rigidbody torque for wobble at this stage. Perturb
  steering/heading output so the existing root-rotation policy stays coherent.

---

### Ticket 3.6 - Add front-brake-throw at high speed

**Goal:** Punish over-reliance on the front brake at speed by throwing the rider forward.

**Dependencies:** Ticket 1.5 and Ticket 3.4.

**Files:**
- `BikeBrakeModel`
- `BikeCrashStateMachine`
- front-brake risk tuning config

**Research task for Opus:** Define when heavy front-brake input at high speed should pitch the rider over the bars — the speed/brake-force threshold and how it routes into the existing crash state.

**Subagent tasks:**
- Research subagent: specify the front-brake-force × speed threshold and the resulting throw/crash outcome.
- Coding subagent A: detect the over-braking condition in the brake model.
- Coding subagent B: route it into the crash state machine (throw + recovery, or death if severe enough).

**Acceptance criteria:**
- Holding the front brake hard at high speed can throw the player forward into the crash state (README handling goal).
- The rear brake and light front-brake use at lower speed do not trigger it.
- The threshold and consequence severity are tunable without code changes.

**Notes:**
- This is the front-brake crash deferred from Ticket 1.5 ("Do not add crash-from-front-brake yet"). It reuses the crash state machine from Ticket 3.4 — don't build a parallel crash path.
- Separate front/rear braking being "too demanding" is an open design risk (README); keep the punishment tunable so it can be softened.
- Treat the front-brake throw as a tunable risk signal from speed x front-brake
  input, routed into the standard crash event/state-machine path. Do not model
  full rider pitch-over physics unless playtesting proves the authored signal is
  unreadable.

---

## Sprint exit criteria

Sprint 3 is complete when:
- The player can jump, land with a visible handling penalty, hit obstacles at varying speeds, and enter a recoverable crash state with a minimal QTE.
- Excess speed produces a death wobble, and over-using the front brake at speed can throw the rider.
- Crash events are categorized and emittable to downstream systems.
- A playtester can answer: do mistakes feel meaningful and recoverable rather than arbitrary?
