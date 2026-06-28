# Sprint 7 — Segments & Run Composition

**Goal question:** Do modular segments connect cleanly into replayable runs?

**Sprint status:** Not started

> Covers the README's "Level structure" + "Segment schema" requirements and the
> "Level loading and segment sequencing system" core system. The prototype run is
> **two difficulty-bucketed regions**: each region draws **5 non-repeating shuffled
> segments** from its own pool, linked by a **bridge segment**, with next region
> composed from higher-difficulty segments (and ramping monster pressure — Sprint
> 5). Segment *art* (terrain, props, marker placement) is Kam's level-design work;
> this sprint owns the data/code that stitches segments together and composes a run.
> For now we only have 1 level from first region.

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
- Coding subagent B: author placeholder definitions across the two region pools + intro / bridge / ending.

**Acceptance criteria:**
- A segment record carries: **region bucket** (1 or 2), segment ID, entry socket, exit socket, difficulty rating, expected speed band, hazard tags (jumps, hairpins, narrow trail, poor terrain, dense trees), visibility/readability tags, and primary route type (safe / fast / technical).
- Metadata is editable in the inspector without touching code.
- Each region's segment pool plus the intro, bridge (region-to-region transition), and ending each have definitions; the prototype ships **two** region pools.

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
- Given an ordered list (intro → region 1's 5 segments → bridge → region 2's 5 segments → ending), the loader builds a connected, rideable run.
- The bridge segment sits between the two regions; intro and ending bookend the run (matches the README `load_levels` example).
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

**Research task for Opus:** Define the selection rule that, **per region**, draws 5 non-repeating shuffled segments from that region's pool (region 2 from the higher-difficulty pool), and orders the run as intro → region 1 → bridge → region 2 → ending.

**Subagent tasks:**
- Research subagent: specify the per-region selection/ordering algorithm and its constraints.
- Coding subagent A: implement `RunComposer` producing, for each of the two regions, an ordered non-repeating list of 5 segments from that region's pool.
- Coding subagent B: feed the composed order into `LevelSequenceLoader` and expose a seed for reproducible test runs.

**Acceptance criteria:**
- Each run produces an ordered list of **two regions × 5 non-repeating shuffled segments**, joined by the bridge and bookended by intro/ending — **no segment repeated within the run** (README rule).
- Region 2's segments are drawn from the higher-difficulty pool, so the run escalates.
- Re-running produces a different order (substantial variation between runs), and a fixed seed reproduces a given run for testing.

**Notes:**
- Per README, the exact composition formula is not the point — ordered, non-repeating selection that creates variation is. Keep it simple and seedable.
- The death/restart loop (Ticket 10.1) re-rolls a new composition on each restart by calling back into `RunComposer`.

---

## Sprint exit criteria

Sprint 7 is complete when:
- Prototype segments carry queryable metadata (region bucket, difficulty rating, hazard tags, route type) and connect via sockets into one continuous downhill run without visible seams or steps.
- Each run is composed of two difficulty-bucketed regions: each region draws 5 non-repeating shuffled segments from its pool, linked by a bridge segment; region 2 is composed from higher-difficulty segments.
- The no-repeat shuffle varies between plays and is seed-reproducible for testing; a fixed ordered list can still be loaded for debugging.
- A playtester can answer: do modular segments connect cleanly and does the two-region structure produce a run that feels varied and escalating in difficulty?

> Win state, run summary, and re-roll on completion are **Sprint 10's** scope (the loader here only emits the "ending reached" signal Sprint 10 consumes).
