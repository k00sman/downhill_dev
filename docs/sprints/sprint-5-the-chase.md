# Sprint 5 — The Chase

**Goal question:** Does the monster create pressure?

**Sprint status:** Not started

---

### Ticket 5.1 - Create monster chase actor stub

**Goal:** Spawn a monster actor that can track player position with a placeholder visible form.

**Dependencies:** Ticket 1.2.

**Files:**
- `MonsterChaseController`
- monster prefab/scene/blueprint
- spawn-distance tuning value

**Research task for Opus:** Define the minimum monster representation needed to test pursuit pressure before full AI exists, including a tunable initial spawn distance behind the player.

**Subagent tasks:**
- Research subagent: specify movement simplifications, follow rules, and the spawn-behind-player rule.
- Coding subagent A: implement the chase controller shell with a placeholder visible mesh.
- Coding subagent B: wire spawn at a configurable distance behind the player and player reference acquisition.

**Acceptance criteria:**
- A monster can exist in the scene with a **placeholder visible form** and target the player.
- The monster **spawns a tunable distance behind the player** and is **visible when the player looks back** (via freelook) — it is meant to be seen, not purely audio.
- The monster updates consistently **without real pathfinding** — it may move toward the player in a simplified manner, **including floating** if necessary (README: the monster is a pressure system first, a character second).
- The controller is ready for speed-follow logic.

**Notes:**
- Decision: the monster is **visible behind the player** (placeholder art), giving direct pressure on freelook; audio cues (Sprint 8, T8.4) layer on top. Keep the start distance tunable.
- Advanced monster navigation / obstacle avoidance is explicitly out of scope (README). Keep movement deliberately simple.

---

### Ticket 5.2 - Implement delayed speed-follow behavior

**Goal:** Make the monster loosely mirror the player's pace with lag.

**Dependencies:** Ticket 5.1 and Ticket 1.3.

**Files:**
- `MonsterSpeedModel`
- `MonsterChaseController`

**Research task for Opus:** Define a delayed velocity-follow model with configurable floor and cap speeds.

**Subagent tasks:**
- Research subagent: specify the smoothing or lag behavior mathematically.
- Coding subagent A: implement monster speed tracking logic.
- Coding subagent B: connect it to chase movement and tuning config.

**Acceptance criteria:**
- The monster's speed broadly mirrors the player's pace but **reacts more slowly** to acceleration and deceleration — that lag is the core mechanic (README).
- Configurable **minimum and maximum** monster speed caps work.
- Slowing down or crashing makes the monster visibly catch up; pulling ahead lets the player open the gap.

**Notes:**
- Monster speed logic must be readable, not arbitrary — an open design risk (README): "the monster may feel unfair if its speed logic is not readable." Keep the lag and caps tunable and observable on the debug HUD.
- **Region-based escalation:** pressure ramps with the run's region structure (Sprint 7). Region 2 raises the monster's acceleration/floor relative to region 1, so late-run tension builds at region transitions rather than via a generic timer. Expose the per-region escalation as tunable values; keep per-segment coupling out (region-level only).

---

### Ticket 5.3 - Implement monster kill contact

**Goal:** End the run when the monster reaches the player.

**Dependencies:** Tickets 4.1 and 5.2.

**Files:**
- `MonsterChaseController`
- `PlayerHealth`

**Research task for Opus:** Define the cleanest prototype contact rule for monster kills.

**Subagent tasks:**
- Research subagent: specify collision/contact detection and kill timing.
- Coding subagent A: implement contact detection.
- Coding subagent B: route contact into instant-death (health → 0) and emit the death event.

**Acceptance criteria:**
- Monster contact kills the player reliably — physical contact sets health to 0 immediately (README) and emits the death event.
- The death event routes into **Sprint 10's** run-end flow — the same path fatal crashes use. This ticket owns contact detection + instant-death emission only, **not** the restart/run-end.

---

## Sprint exit criteria

Sprint 5 is complete when:
- A monster spawns, tracks the player with lagged speed-following, and kills the player on contact by setting health to 0 and emitting the death event (the restart and run-end flow are Sprint 10's scope).
- Slowing down or crashing causes the monster to visibly close the gap; pulling ahead opens it.
- Monster speed, lag, floor, and cap are tunable and observable on the debug HUD.
- A playtester can answer: does the monster create real pressure or does it feel ignorable?
