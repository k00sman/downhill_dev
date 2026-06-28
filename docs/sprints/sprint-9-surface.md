# Sprint 9 — Surface & Terrain Handling

**Goal question:** Does the trail surface change how the bike handles?

**Sprint status:** Not started

> Realizes the README "poor terrain" hazard tag as an actual handling effect:
> different surfaces (packed trail, loose dirt, gravel, roots/rock, mud) change
> grip, speed, and stability so the player must *read the surface*, not just the
> shape of the trail. Promoted to its own sprint because it cuts across
> locomotion (Sprint 1/2), crashes (Sprint 3), audio (Sprint 8), and readability
> (Sprint 6). Sequenced after the core ride and chase hold up — it adds handling
> depth, it doesn't gate the prototype's central questions.
>
> Bike-physics research note: keep surface handling as modifiers layered onto
> the authored movement/steering models. Do not introduce a separate tire or
> WheelCollider simulation here unless the existing model has already failed
> playtest goals.

---

### Ticket 9.1 - Define surface type system

**Goal:** Give terrain a queryable surface type that the rest of the game can react to.

**Dependencies:** Ticket 1.3.

**Files:**
- `SurfaceType` definition (enum or ScriptableObject set) in a `Downhill.Surface` asmdef under `Assets/Scripts/Surface/`
- `SurfaceTagger` (marks a collider/terrain region with a surface type)

**Research task for Opus:** Decide the cleanest way to attach surface type to terrain in this stack (per-collider tag, terrain layer, or material lookup) and how the grounding probe queries the surface under the bike.

**Subagent tasks:**
- Research subagent: choose the tagging mechanism and define the starting surface set.
- Coding subagent A: implement `SurfaceType` and `SurfaceTagger`.
- Coding subagent B: expose a "current surface under the bike" query from the existing grounding probe.

**Acceptance criteria:**
- A small set of surface types exists (e.g. packed trail, loose dirt/gravel, roots/rock, mud) and can be assigned to terrain in the editor.
- Gameplay can query the surface type currently under the bike each frame.
- No handling change yet — this ticket is the data layer only.

**Notes:**
- Keep it isolated in `Downhill.Surface`. Vendor terrain assets stay unmodified — tag via new components/data, don't edit vendor content.
- Feed surface queries from the same grounding result used by jump/contact code
  once Sprint 3 extracts it. Avoid separate downward raycasts in every surface
  consumer.

---

### Ticket 9.2 - Apply surface effects to handling

**Goal:** Make surface type change grip, speed, and stability so terrain reading matters.

**Dependencies:** Tickets 9.1, 1.4, and 2.2.

**Files:**
- `SurfaceHandlingModifier`
- `BikeMovementModel`
- `BikeSteeringModel`
- per-surface tuning config

**Research task for Opus:** Specify per-surface modifiers — grip/steering authority, rolling drag or max-speed scaling, and added instability — and how they layer onto the existing movement and steering models without fighting speed-sensitive steering (2.2) or the death wobble (3.5).

**Subagent tasks:**
- Research subagent: define the per-surface modifier set and how it composes with existing handling.
- Coding subagent A: implement `SurfaceHandlingModifier` reading the current surface.
- Coding subagent B: apply the modifiers in the movement/steering models and expose tuning.

**Acceptance criteria:**
- Riding onto a different surface measurably changes handling (e.g. loose dirt = less grip/more slide; mud = slower; roots = more instability).
- Effects compose cleanly with speed-sensitive steering and the death wobble — no double-counting or runaway instability.
- Per-surface values are tunable without code changes.

**Notes:**
- This is the payoff for "trail reading matters" — surfaces should reward reading ahead and choosing a line, per the README pillar.
- Compose modifiers in a predictable order: base bike tuning, speed-sensitive
  steering, landing/wobble instability, then surface multipliers/clamps. The
  research task should explicitly prevent double-counting instability.
- Prefer data assets or serializable settings for per-surface values so designers
  can tune grip, drag, max-speed scale, and instability without source edits.

---

### Ticket 9.3 - Surface feedback hooks

**Goal:** Make the current surface readable and audible so its handling effect feels fair.

**Dependencies:** Tickets 9.1, 8.3, and 6.1.

**Files:**
- `SurfaceFeedbackController`
- surface→visual/audio mapping config

**Research task for Opus:** Specify how the current surface is telegraphed — visual distinction (texture/tint the headlamp catches) and the surface audio loop from Ticket 8.3 — so the player can anticipate a handling change before hitting it.

**Subagent tasks:**
- Research subagent: define the surface→visual/audio cue mapping and lead-time for readability.
- Coding subagent A: drive the surface audio loop selection from the current surface type.
- Coding subagent B: ensure surfaces are visually distinguishable under the headlamp and expose tuning.

**Acceptance criteria:**
- The surface the player is on (and ideally the one just ahead) is visually distinguishable under the headlamp.
- The surface audio loop (Ticket 8.3) matches the current surface type.
- Cues give enough lead time that a handling change reads as fair, not random.

**Notes:**
- Reuses the surface audio from Sprint 8 (T8.3) and the headlamp from Sprint 6 (T6.1) — don't build parallel systems; wire surface type into them.

---

## Sprint exit criteria

Sprint 9 is complete when:
- Terrain carries queryable surface types, those types measurably change grip/speed/stability, and the current surface is readable visually and audibly.
- Surface modifiers compose cleanly with speed-sensitive steering and the death wobble, and all per-surface values are tunable.
- A playtester can answer: does reading the surface (not just the trail shape) add meaningful handling depth?
