# Sprint 4 — Survival

**Goal question:** Does death feel fair?

**Sprint status:** Not started

---

### Ticket 4.1 - Implement hidden player health system

**Goal:** Track survivability internally without UI complexity.

**Dependencies:** Ticket 3.3.

**Files:**
- `PlayerHealth`
- damage event definitions or health interface

**Research task for Opus:** Define a minimal health architecture that supports collision damage, instant-death events, and delayed regeneration.

**Subagent tasks:**
- Research subagent: write the health model and data flow.
- Coding subagent A: implement the health component.
- Coding subagent B: implement regeneration delay and damage event handling.

**Acceptance criteria:**
- The player has current and max health and **starts at full** (README: health is a hidden internal value, never shown to the player).
- Collision damage applies correctly, scaled by impact severity and speed.
- Health regeneration starts after **5 seconds** without taking damage (README value).
- The system supports an instant-death event (health → 0 immediately) for monster contact later.
- Reaching 0 health triggers death.

**Notes:**
- Health is **not** shown in any player-facing UI — it is internal/hidden per the README. Surface it only on the debug HUD (Ticket 1.7).

---

### Ticket 4.2 - Hook collision damage into health

**Goal:** Turn crash severity into meaningful damage.

**Dependencies:** Tickets 3.3 and 4.1.

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
- Damaging crashes reduce health, scaled by impact severity and speed (README).
- Fatal (severe) crashes can force immediate death.
- Damage values are tunable without code changes.

**Notes:**
- Consumes the severity tiers from Ticket 3.3 — map tiers → damage here, don't re-evaluate collisions.

---

### Ticket 4.3 - Implement hidden-health damage feedback

**Goal:** Let the player sense damage and danger without a health bar, since health is hidden.

**Dependencies:** Tickets 4.1 and 4.2.

**Files:**
- `HealthFeedbackController`
- post-process volume / screen-effect material on the player camera

**Research task for Opus:** Specify a screen effect driven by current (hidden) health — grayscaling/desaturation plus a light vignette that intensifies as health drops and eases back as health regenerates.

**Subagent tasks:**
- Research subagent: define the health→effect mapping and how it reads health without exposing a number.
- Coding subagent A: implement the desaturation + light vignette post-process driven by health.
- Coding subagent B: wire it to the health system and tune thresholds and easing.

**Acceptance criteria:**
- As hidden health drops, the screen **desaturates toward grayscale with a light vignette**; it recovers as health regenerates.
- The effect communicates "hurt / in danger" without ever showing a numeric or bar.
- Intensity thresholds and easing are tunable without code changes.

**Notes:**
- This is the player's only ambient damage signal — keep it readable but restrained (light vignette) so it doesn't fight night-trail readability.
- Reads from the same hidden health value surfaced numerically only on the debug HUD (Ticket 1.7).

---

## Sprint exit criteria

Sprint 4 is complete when:
- The player accumulates hidden health damage from crashes (scaled by impact severity and speed), senses it through screen desaturation and a light vignette, and dies when health reaches zero — emitting the death event handled by Sprint 10.
- Health regeneration starts after 5 seconds without damage; damage values, regen delay, and feedback thresholds are all tunable without code changes.
- A playtester can answer: does death feel fair given the visible risk taken?
