# Sprint 2 — Advanced Input

**Goal question:** Does input feel distinctive and risky?

**Sprint status:** Not started

---

### Ticket 2.2 - Implement alternating pedal input logic

**Goal:** Support left/right pedaling as a distinct mechanic rather than a single accelerate button.

**Dependencies:** Ticket 2.1.

**Files:**
- `PedalInputEvaluator`
- `PlayerBikeController`

**Research task for Opus:** Determine a simple rule set for alternating pedal presses that feels intentional, avoids spam exploits, and can degrade gracefully into a simpler fallback if needed.

**Subagent tasks:**
- Research subagent: write pedal cadence rules and edge-case handling.
- Coding subagent A: implement pedal state tracking and cadence evaluation.
- Coding subagent B: integrate cadence output into forward drive.

**Acceptance criteria:**
- Alternating left and right inputs provides forward drive.
- Repeating the same side only is less effective or ignored, based on the spec.
- The system exposes tuning values for cadence window and drive bonus.

**Notes:**
- This is a high-risk mechanic; keep the logic isolated so it can be replaced later.

---

### Ticket 3.2 - Add speed-sensitive steering

**Goal:** Make steering more dangerous and less forgiving at higher speed.

**Dependencies:** Ticket 3.1.

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

---

## Sprint exit criteria

Sprint 2 is complete when:
- Alternating left/right pedals drives the bike forward and same-side spam is penalized.
- Steering becomes measurably harder to control at high speed.
- A playtester can answer: does the input feel distinctive and risky compared to a simple hold-to-go mechanic?
