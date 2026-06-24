# Sprint 4 — Survival

**Goal question:** Does death feel fair?

**Sprint status:** Not started

---

### Ticket 5.1 - Implement hidden player health system

**Goal:** Track survivability internally without UI complexity.

**Dependencies:** Ticket 4.3.

**Files:**
- `PlayerHealth`
- damage event definitions or health interface

**Research task for Opus:** Define a minimal health architecture that supports collision damage, instant-death events, and delayed regeneration.

**Subagent tasks:**
- Research subagent: write the health model and data flow.
- Coding subagent A: implement the health component.
- Coding subagent B: implement regeneration delay and damage event handling.

**Acceptance criteria:**
- The player has current and max health.
- Collision damage applies correctly.
- Health regeneration starts after a no-damage delay.
- The system supports instant death from monster contact later.

---

### Ticket 5.2 - Hook collision damage into health

**Goal:** Turn crash severity into meaningful damage.

**Dependencies:** Tickets 4.3 and 5.1.

**Files:**
- `BikeCollisionEvaluator`
- `PlayerHealth`
- damage config/settings file

**Research task for Opus:** Define a prototype damage curve from crash severity to health loss.

**Subagent tasks:**
- Research subagent: propose thresholds or curves for impact damage.
- Coding subagent A: integrate severity output into health damage calls.
- Coding subagent B: expose config for tuning damage values.

**Acceptance criteria:**
- Damaging crashes reduce health.
- Fatal crashes can force immediate death.
- Damage values are tunable without code changes.

---

### Ticket 5.3 - Implement death and quick restart loop

**Goal:** Make failed runs restart fast enough for repeated testing.

**Dependencies:** Ticket 5.1.

**Files:**
- `PlayerDeathHandler`
- `RunResetManager`

**Research task for Opus:** Specify the fastest prototype restart flow that preserves iteration speed and avoids menu overhead.

**Subagent tasks:**
- Research subagent: define death flow, delays, and reset requirements.
- Coding subagent A: implement death handling and input lockout.
- Coding subagent B: implement level/player reset behavior.

**Acceptance criteria:**
- Death transitions the player into a fail state.
- The run can restart quickly from a known spawn state.
- Core player systems reset cleanly.

---

## Sprint exit criteria

Sprint 4 is complete when:
- The player accumulates hidden health damage from crashes, dies when health reaches zero, and restarts the run quickly from spawn.
- Damage values and regen delay are tunable without code changes.
- A playtester can answer: does death feel fair given the visible risk taken?
