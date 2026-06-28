# Sprint 10 — Run End & Flow

**Goal question:** Does the run feel conclusive — win or loss — and does it get you back on the trail fast enough to want another try?

**Sprint status:** Not started

This sprint owns the end-of-run flow: death/end-state handling (+ a brief death stinger), quick restart and re-roll into a fresh run, the run completion/win trigger, and the player-facing run-end stats screen. Failure *detection* lives in Sprints 3–5 (crash severity, health, monster contact) and the win *trigger* fires from the segment loader (Sprint 7); this sprint consumes those signals and closes the loop.

---

### Ticket 10.1 - Death, restart & re-roll

**Goal:** End a failed run, play a brief death stinger, and restart fast into a fresh re-rolled run.

**Dependencies:** Ticket 4.1 (health emits the death event). Re-roll depends on Sprint 7 `RunComposer`.

**Files:**
- `PlayerDeathHandler`
- `RunResetManager`

**Research task for Opus:** Specify the fastest prototype death→restart flow that preserves iteration speed, plays a short death stinger, and re-rolls a new run with no menu overhead.

**Subagent tasks:**
- Research subagent: define death flow, the stinger beat, delays, and reset requirements.
- Coding subagent A: implement death handling, the death stinger, and input lockout.
- Coding subagent B: implement level/player reset + re-roll behavior.

**Acceptance criteria:**
- Death from **any cause** (fatal crash severity or monster contact) transitions into a fail state and plays a brief **death stinger** — a ~0.5–1s freeze / desaturate-to-black; on a monster catch the monster fills the view — then restarts.
- The run restarts into an **instant ride** — the player spawns already in control on the trail, no menu/countdown (the monster's lag provides the natural head start).
- Restart **re-rolls a new run** via the Sprint 7 `RunComposer` (fresh shuffled segment order); a debug seed can force a fixed run for tuning.
- Core player systems (health, crash state, speed, damage-feedback effect) reset cleanly.

**Notes:**
- Moved here from Sprint 4 ("death and quick restart loop") so all run-end flow lives together. Sprints 3–5 only *emit* the death event; this ticket owns the response.
- Re-roll depends on Sprint 7. Until the loader exists, restart resets the single existing segment; wire the re-roll once `RunComposer` lands.
- Keep the stinger isolated and short — the README wants rapid repeated runs.
- **Known deferred gap:** falling off the trail / out-of-bounds has no handling yet (out of scope for the prototype). The test trail is assumed bounded enough; revisit if playtests show players riding off edges.

---

### Ticket 10.2 - Run completion and win

**Goal:** Give a run an explicit win condition — reaching the end — and close the loop back to a fresh run.

**Dependencies:** Tickets 7.3, 5.2, 10.1, and 10.3.

**Files:**
- `RunCompletionHandler`

**Research task for Opus:** Specify how reaching the ending segment finishes the run: stop the monster, suspend fail conditions, present the run-end stats screen, and route into a fresh re-rolled run.

**Subagent tasks:**
- Research subagent: define the completion trigger, monster/fail shutdown, and hand-off to the stats screen.
- Coding subagent A: implement the ending-reached trigger and win state.
- Coding subagent B: route the win into the stats screen (10.3) and then a re-rolled run (10.1).

**Acceptance criteria:**
- Reaching the ending segment ends the run as a **win** (README gameplay-loop step 5: "reach the end of the run").
- On completion the monster stops pursuing and crash/death can no longer trigger.
- The run-end stats screen (Ticket 10.3) is shown, then the player starts a fresh re-rolled run via Ticket 10.1.

**Notes:**
- Moved here from Sprint 7 ("run completion and summary"). The ending-reached signal is emitted by the Sprint 7 loader; this ticket owns the win response.
- This is the only success/end state in the prototype — failure paths (crash/monster death) route through Ticket 10.1.

---

### Ticket 10.3 - Run-end statistics screen

**Goal:** Show the player a simple summary screen when a run ends (death or win) displaying how far they rode and how long it took.

**Dependencies:** None — the stats tracker runs every run; the screen is invoked by Ticket 10.1 (death) and Ticket 10.2 (win).

**Files:**
- `RunEndStatsUI`
- `RunStatsTracker` (tracks distance and elapsed time per run)

**Research task for Opus:** Define the simplest Unity UI approach for a run-end overlay that displays distance ridden (ft/m, unit-switchable) and run time, dismisses quickly, and resets cleanly on restart.

**Subagent tasks:**
- Research subagent: specify data tracked, unit display rules, and dismiss/reset flow.
- Coding subagent A: implement `RunStatsTracker` — accumulates distance from player position delta each frame and tracks elapsed time; resets on run start.
- Coding subagent B: implement `RunEndStatsUI` — shown on death or win, displays distance and time, provides a dismiss/restart action.

**Acceptance criteria:**
- When a run ends (death or win), a screen appears showing distance ridden (ft/m) and run time.
- The screen dismisses promptly and all tracked values reset for the next run.
- Richer designer metrics (crashes, cause of death, average speed, monster distance) remain live on the debug HUD (Sprint 1, Ticket 1.7) and are **not** part of this player-facing screen.

**Notes:**
- Keep it lightweight — one or two numbers, fast to read and dismiss. The README wants rapid repeated runs.
- Unit display (ft vs. m) can default to ft for a North American playtesting context; expose as a tuning constant.

---

## Sprint exit criteria

Sprint 10 is complete when:
- Death from any cause (crash severity or monster contact) plays a brief death stinger, then immediately starts a fresh re-rolled run — the player spawns already in control with no menu or countdown.
- Reaching the ending segment triggers a win: the monster stops pursuing, fail conditions are suspended, and a stats screen showing distance and run time is shown before routing into a fresh re-rolled run.
- On every restart, health, crash state, speed, and damage-feedback reset cleanly; a debug seed can force a fixed run for tuning.
- A playtester can answer: does the end of a run — win or loss — feel conclusive and get you back into a fresh run fast enough to want one more try?
