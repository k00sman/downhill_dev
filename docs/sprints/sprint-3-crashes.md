# Sprint 3 — Crashes

**Goal question:** Do mistakes feel meaningful?

**Sprint status:** Not started

---

### Ticket 4.1 - Implement jump input and airtime state

**Goal:** Allow the bicycle to leave the ground intentionally on jumpable terrain.

**Dependencies:** Ticket 2.1 and Ticket 3.1.

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
- Jump only works when grounded.
- The bicycle can enter and leave an airborne state cleanly.
- Basic landing detection works.

---

### Ticket 4.2 - Add landing instability hook

**Goal:** Make jumps carry risk by destabilizing control on landing.

**Dependencies:** Ticket 4.1 and Ticket 3.2.

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

---

### Ticket 4.3 - Implement basic collision severity detection

**Goal:** Distinguish harmless bumps from dangerous crashes.

**Dependencies:** Ticket 2.1.

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

---

### Ticket 4.4 - Implement crash and recovery state

**Goal:** Add a recoverable non-fatal crash outcome.

**Dependencies:** Ticket 4.3.

**Files:**
- `BikeCrashStateMachine`
- `PlayerBikeController`
- optional `BikeRecoveryQTE`

**Research task for Opus:** Define the simplest crash state flow that interrupts movement, creates vulnerability, and can later support the QTE idea.

**Subagent tasks:**
- Research subagent: document crash state transitions and lockouts.
- Coding subagent A: implement crash state machine.
- Coding subagent B: add movement/input lockouts and recovery timing.
- Coding subagent C: add a placeholder recovery interaction or timed auto-recover.

**Acceptance criteria:**
- Severe but non-fatal crashes interrupt normal riding.
- The player can recover and return to control.
- Recovery timing is long enough to matter for chase pressure.

---

## Sprint exit criteria

Sprint 3 is complete when:
- The player can jump, land with a visible handling penalty, hit obstacles at varying speeds, and enter a recoverable crash state.
- Crash events are categorized and emittable to downstream systems.
- A playtester can answer: do mistakes feel meaningful and recoverable rather than arbitrary?
