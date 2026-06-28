# Sprint 7 — Segments & Run Composition

**Goal question:** Do modular segments connect cleanly into replayable runs?

**Sprint status:** Not started

> Covers the README's "Level structure" + "Segment schema" requirements and the
> "Level loading and segment sequencing system" core system. The prototype scope
> includes **1 region, 5 playable segments, and segment shuffling with no repeated
> segment in a run** — this sprint builds all of it. Segment *art* (terrain,
> props, marker placement) is Kam's level-design work; this sprint owns the
> data/code that stitches segments together and composes a run.

---

### Ticket 7.1 - Define segment schema and metadata

**Goal:** Give every segment a small, queryable metadata record so run composition can reason about it.

**Dependencies:** None.

**Files:**
- `SegmentDefinition` (ScriptableObject) in a new `Downhill.Level` asmdef under `Assets/Scripts/Level/`
- enum/constant definitions for hazard, visibility, and route-type tags

**Research task for Opus:** Confirm the cleanest Unity-native way to attach per-segment metadata to a segment prefab (ScriptableObject referenced by the prefab vs. a component on the prefab root) and how the loader will discover the set of available segments.

**Subagent tasks:**
- Research subagent: decide ScriptableObject-vs-component, define the field set and tag enums.
- Coding subagent A: implement the `SegmentDefinition` data type with the README schema fields.
- Coding subagent B: author placeholder definitions for the 5 prototype segments + intro/transition/ending.

**Acceptance criteria:**
- A segment record carries: region, segment ID, entry socket, exit socket, difficulty rating, expected speed band, hazard tags (jumps, hairpins, narrow trail, poor terrain, dense trees), visibility/readability tags, and primary route type (safe / fast / technical).
- Metadata is editable in the inspector without touching code.
- The 5 prototype segments plus intro, region transition, and ending each have a definition.

**Notes:**
- This is data scaffolding — no runtime stitching here. Keep it isolated in the `Downhill.Level` asmdef.
- Per README: metadata exists to control run composition and prevent broken or repetitive combinations.

---

### Ticket 7.2 - Implement segment socket connection

**Goal:** Let two segments snap together at compatible entry/exit sockets at runtime.

**Dependencies:** Ticket 7.1.

**Files:**
- `SegmentSocket` (marks an entry or exit transform on a segment prefab)
- `SegmentConnector` (aligns a segment's entry socket to the previous segment's exit socket)

**Research task for Opus:** Define the transform-alignment math for snapping a new segment so its entry socket coincides (position + orientation) with the prior segment's exit socket, and how downward slope continuity is preserved across the seam.

**Subagent tasks:**
- Research subagent: specify socket representation and the align/parent strategy.
- Coding subagent A: implement `SegmentSocket` and entry/exit lookup.
- Coding subagent B: implement `SegmentConnector` alignment and verify two test segments seam without a visible gap or step.

**Acceptance criteria:**
- A segment can be instantiated and aligned so its entry socket matches the previous segment's exit socket in position and heading.
- The trail remains continuous and downward-sloping across the seam.
- Connection works for any ordering of valid same-region segments (per README: segments must support seamless connection to any other valid segment in the region).

**Notes:**
- README calls for a shared convergence/connection area before each transition — keep exit sockets consistent enough that any segment can follow any segment.
- Existing trail geometry may use the vendor PathCreator asset; do **not** modify vendor code — build socket/connection logic as new `Downhill.Level` code on top.

---

### Ticket 7.3 - Implement level loading and sequencing system

**Goal:** Load an ordered list of segments and stitch them into one playable run.

**Dependencies:** Tickets 7.1 and 7.2.

**Files:**
- `LevelSequenceLoader`
- run/level config asset describing the intro → segments → transitions → ending order

**Research task for Opus:** Specify how the loader consumes an ordered segment list (like the README `load_levels` example), instantiates each, connects it via sockets, and where the player spawns relative to the intro segment.

**Subagent tasks:**
- Research subagent: define the load/sequence data flow and intro/transition/ending handling.
- Coding subagent A: implement the loader that instantiates + connects segments in order.
- Coding subagent B: wire player spawn at the intro and verify an end-to-end multi-segment run loads without gaps.

**Acceptance criteria:**
- Given an ordered list (intro, N region segments, region transition, ending), the loader builds a connected, rideable run.
- Region transition segments sit between regions; intro and ending bookend the run (matches the README `load_levels` example).
- The player spawns on the intro segment **already in control (instant ride, no menu/countdown)** and can ride through to the ending with no seams broken.

**Notes:**
- Keep the ordered-list input separate from the shuffle logic (Ticket 7.4) so a fixed, debuggable order can be loaded during development.

---

### Ticket 7.4 - Implement no-repeat run shuffle

**Goal:** Compose a fresh, non-repeating segment order for each run.

**Dependencies:** Ticket 7.3.

**Files:**
- `RunComposer`
- shuffle/selection tuning config

**Research task for Opus:** Define the selection rule that picks and orders segments within a region so that no segment repeats in a single run, and decide whether every available segment must appear or a subset is chosen for shorter runs.

**Subagent tasks:**
- Research subagent: specify the selection/ordering algorithm and its constraints.
- Coding subagent A: implement `RunComposer` producing an ordered, non-repeating segment list per region.
- Coding subagent B: feed the composed order into `LevelSequenceLoader` and expose a seed for reproducible test runs.

**Acceptance criteria:**
- Each run produces an ordered segment list with **no segment repeated within the run** (README rule).
- Re-running produces a different order (substantial variation between runs), and a fixed seed reproduces a given run for testing.
- Composition respects region grouping and inserts intro / transition / ending around the shuffled segments.

**Notes:**
- Per README, the exact composition formula is not the point — ordered, non-repeating selection that creates variation is. Keep it simple and seedable.
- The death/restart loop (Ticket 4.3) re-rolls a new composition on each restart by calling back into `RunComposer`.

---

### Ticket 7.5 - Implement run completion and summary

**Goal:** Give a run an explicit win condition — reaching the end — and close the loop back to a fresh run.

**Dependencies:** Tickets 7.3, 5.2, and 10.1.

**Files:**
- `RunCompletionHandler`
- run-summary readout (reuses the metrics from Ticket 10.1)

**Research task for Opus:** Specify how reaching the ending segment finishes the run: stop the monster, freeze fail conditions, present a short run summary, and route into a fresh re-rolled run.

**Subagent tasks:**
- Research subagent: define the completion trigger, monster/fail shutdown, and summary contents.
- Coding subagent A: implement the ending-reached trigger and win state.
- Coding subagent B: present the run summary and wire restart into a re-rolled run.

**Acceptance criteria:**
- Reaching the ending segment ends the run as a **win** (README gameplay-loop step 5: "reach the end of the run").
- On completion the monster stops pursuing and crash/death can no longer trigger.
- A short run summary appears (duration, average speed, crashes, cause-of-death N/A on a win, monster-distance trend) using the Ticket 10.1 metrics, then the player can start a fresh re-rolled run.

**Notes:**
- This is the only success/end state in the prototype — failure paths (crash/monster death) are covered in Sprints 3–5. Keep the summary lightweight and fast to dismiss (the README wants rapid repeated runs).

---

## Sprint exit criteria

Sprint 7 is complete when:
- The 5 prototype segments carry queryable metadata, connect via sockets into one continuous downhill run, and the run is composed by a no-repeat shuffle that varies between plays (and is seed-reproducible for testing).
- Reaching the ending segment wins the run, shows a short summary, and leads into a fresh re-rolled run; a fixed ordered list can still be loaded for debugging.
- A playtester can answer: do modular segments connect cleanly enough to support replayability?
