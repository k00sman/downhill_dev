# Sprint 5 — The Chase

**Goal question:** Does the monster create pressure?

**Sprint status:** Not started

---

### Ticket 6.1 - Create monster chase actor stub

**Goal:** Spawn a monster actor that can track player position with minimal presentation.

**Dependencies:** Ticket 1.2.

**Files:**
- `MonsterChaseController`
- monster prefab/scene/blueprint

**Research task for Opus:** Define the minimum monster representation needed to test pursuit pressure before full AI exists.

**Subagent tasks:**
- Research subagent: specify movement simplifications and follow rules.
- Coding subagent A: implement the chase controller shell.
- Coding subagent B: wire spawn and player reference acquisition.

**Acceptance criteria:**
- A monster can exist in the scene and target the player.
- The monster updates consistently without pathfinding.
- The controller is ready for speed-follow logic.

---

### Ticket 6.2 - Implement delayed speed-follow behavior

**Goal:** Make the monster loosely mirror the player's pace with lag.

**Dependencies:** Ticket 6.1 and Ticket 2.1.

**Files:**
- `MonsterSpeedModel`
- `MonsterChaseController`

**Research task for Opus:** Define a delayed velocity-follow model with configurable floor and cap speeds.

**Subagent tasks:**
- Research subagent: specify the smoothing or lag behavior mathematically.
- Coding subagent A: implement monster speed tracking logic.
- Coding subagent B: connect it to chase movement and tuning config.

**Acceptance criteria:**
- The monster speeds up and slows down in response to the player's pace with visible delay.
- Minimum and maximum monster speed caps work.
- Slowing down or crashing makes the monster catch up.

---

### Ticket 6.3 - Implement monster kill contact

**Goal:** End the run when the monster reaches the player.

**Dependencies:** Tickets 5.1 and 6.2.

**Files:**
- `MonsterChaseController`
- `PlayerDeathHandler`
- `PlayerHealth`

**Research task for Opus:** Define the cleanest prototype contact rule for monster kills.

**Subagent tasks:**
- Research subagent: specify collision/contact detection and kill timing.
- Coding subagent A: implement contact detection.
- Coding subagent B: route contact into instant-death handling.

**Acceptance criteria:**
- Monster contact kills the player reliably.
- The same death/restart loop works for monster kills and crash deaths.

---

## Sprint exit criteria

Sprint 5 is complete when:
- A monster spawns, tracks the player with lagged speed-following, and kills the player on contact using the same death/restart loop.
- Slowing down or crashing causes the monster to visibly close the gap.
- A playtester can answer: does the monster create real pressure or does it feel ignorable?
