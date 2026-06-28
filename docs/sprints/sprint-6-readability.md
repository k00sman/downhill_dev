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
- Reflective trail markers and tape catch the headlamp and are visually useful at night, helping guide navigation (README). Markers use a bright emissive/spec **art material** (no custom retroreflective shader for the prototype).
- The headlamp **flickers/destabilizes as hidden health drops** (and steadies as health regenerates), layering dread onto the damage signal — kept subtle so it never breaks trail readability.

**Notes:**
- Marker/tape **placement** is level-design/art (Kam); this ticket owns the light that makes them catch. Coordinate so the light's intensity/range actually lights placed markers.
- The flicker reads from the same hidden health value as the damage-feedback effect (Sprint 4); keep it tunable and restrained.
- Don't prioritize atmosphere over readability — an open design risk (README): "dark visuals may hurt trail readability if atmosphere is prioritized too aggressively."

---

### Ticket 6.2 - On-screen control card

**Goal:** Teach the novel control scheme so playtest feedback measures the mechanic, not confusion.

**Dependencies:** Ticket 1.1.

**Files:**
- `ControlCardUI`

**Research task for Opus:** Define the simplest dismissable overlay that lists the control scheme, shows on the intro segment / first launch, and is re-accessible via a hotkey.

**Subagent tasks:**
- Research subagent: specify the card contents, show/dismiss/recall flow.
- Coding subagent A: implement the control-card overlay.
- Coding subagent B: wire show-on-intro + recall hotkey.

**Acceptance criteria:**
- A dismissable control card shows on the intro segment / first launch, covering pedals (L/R), front/rear brakes, jump, turn, and freelook.
- It is re-accessible mid-session via a hotkey.
- It does not block the instant-ride start beyond a single dismiss keypress.

**Notes:**
- The alternating-pedal + split-brake scheme is THE prototype risk (README); everyone should start informed so feedback measures the mechanic, not first-contact confusion.

---

### Ticket 6.3 - Debug & playtest hotkeys

**Goal:** Facilitate playtest sessions beyond the automatic restart-on-death.

**Dependencies:** Tickets 1.7 and 10.1.

**Files:**
- `DebugControls`

**Research task for Opus:** Decide the minimal set of debug hotkeys that keep a playtest moving without a full debug menu.

**Subagent tasks:**
- Research subagent: list the hotkeys and their guards.
- Coding subagent A: implement the hotkey controls.
- Coding subagent B: wire them to pause, force-restart/re-roll (Sprint 10), and the debug HUD toggle (Sprint 1).

**Acceptance criteria:**
- Hotkeys exist for pause, force-restart/re-roll (calls the Sprint 10 restart), and toggling the debug HUD (Sprint 1).
- The hotkeys are debug-only and do not interfere with normal play.

**Notes:**
- Keep it to hotkeys, not a menu — cheap facilitation that keeps the rapid-run loop intact. A fuller debug menu (segment skip, free-cam, monster on/off) is out of scope for now.

---

### Ticket 6.4 - Visual speed feedback

**Goal:** Sell momentum — the lead pillar — visually, not only through wind audio.

**Dependencies:** Ticket 1.6.

**Files:**
- `SpeedCameraFeedback`

**Research task for Opus:** Specify a speed-scaled FOV widening plus a light camera shake above a threshold, tunable and capped to protect readability.

**Subagent tasks:**
- Research subagent: define the speed→FOV/shake curves and caps.
- Coding subagent A: implement the FOV widening driven by current speed.
- Coding subagent B: add light shake above a threshold and expose tuning.

**Acceptance criteria:**
- Camera FOV widens with speed and a light shake kicks in above a tunable threshold, reinforcing the sense of momentum.
- Intensity is **capped** so it never hurts trail readability on the dark trail.
- All curves/thresholds are tunable without code changes.

**Notes:**
- This is camera/visual juice, distinct from the handling-realism / fall-line model. Keep it restrained — readability beats spectacle.

---

## Sprint exit criteria

Sprint 6 is complete when:
- The headlamp illuminates the trail and follows the player's freelook direction; reflective markers and trail tape are visually distinguishable under the lamp at night and help guide navigation.
- An onboarding control card is shown on first launch and re-accessible via a hotkey, covering the full control scheme (pedals, brakes, jump, freelook).
- A debug hotkey surfaces live prototype state (or toggles the debug HUD from Sprint 1) without cluttering normal play, and camera speed feedback (FOV + light shake) sells momentum without hurting readability.
- A playtester can answer: can we read the trail, look up controls when needed, and tune the prototype from what we can see?
