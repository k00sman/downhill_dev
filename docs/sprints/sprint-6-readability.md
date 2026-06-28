# Sprint 6 — Readability

**Goal question:** Can we tune and read the prototype?

**Sprint status:** Not started

---

### Ticket 6.1 - Implement headlamp gameplay light

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
- The light follows the player's **view / freelook direction** (decision: lamp tracks where the player looks, so they can illuminate around corners and toward hazards — accept that freelook can briefly darken the straight-ahead trail).
- Reflective trail markers and tape catch the headlamp and are visually useful at night, helping guide navigation (README).

**Notes:**
- Marker/tape **placement** is level-design/art (Kam); this ticket owns the light that makes them catch. Coordinate so the light's intensity/range actually lights placed markers.
- Don't prioritize atmosphere over readability — an open design risk (README): "dark visuals may hurt trail readability if atmosphere is prioritized too aggressively."

---

## Sprint exit criteria

Sprint 6 is complete when:
- The headlamp illuminates the trail and reflective markers are useful at night.
- A playtester can answer: can we tune the prototype from what we can see and read?
