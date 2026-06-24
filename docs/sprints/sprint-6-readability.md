# Sprint 6 — Readability

**Goal question:** Can we tune and read the prototype?

**Sprint status:** Not started

---

### Ticket 7.1 - Implement headlamp gameplay light

**Goal:** Improve trail readability and support the night-horror tone.

**Dependencies:** Ticket 1.2.

**Files:**
- `PlayerHeadlampController`
- player prefab/scene light setup

**Research task for Opus:** Define the simplest headlamp setup that improves navigation without introducing heavy rendering complexity.

**Subagent tasks:**
- Research subagent: specify headlamp behavior, intensity, and attachment rules.
- Coding subagent A: implement light control and anchor behavior.
- Coding subagent B: wire the light into the player setup.

**Acceptance criteria:**
- The headlamp illuminates the trail ahead.
- The light follows the player's view or bike-facing direction according to the chosen spec.
- Reflective trail markers are visually useful when present.

---

### Ticket 7.3 - Add run metrics logging

**Goal:** Capture enough data to tell whether the prototype is fun or just functional.

**Dependencies:** Tickets 5.3 and 6.3.

**Files:**
- `PrototypeRunMetrics`
- optional log exporter or console reporter

**Research task for Opus:** Define the minimum set of run-level metrics needed to judge pace, difficulty, and control friction in early playtests.

**Subagent tasks:**
- Research subagent: specify tracked metrics and reset rules.
- Coding subagent A: implement per-run data collection.
- Coding subagent B: implement summary output on death or run end.

**Acceptance criteria:**
- Each run records duration, crashes, cause of death, average speed, and monster catch events.
- Metrics reset correctly on restart.
- Designers can read the summary without digging through engine internals.

---

## Sprint exit criteria

Sprint 6 is complete when:
- The headlamp illuminates the trail and reflective markers are useful at night.
- Per-run metrics (duration, crashes, cause of death, average speed, monster catch events) are logged and reset on restart.
- A playtester can answer: can we tune the prototype from what we can see and read?
