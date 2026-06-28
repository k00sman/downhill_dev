# Sprint 10 — Run End & Flow

**Goal question:** Does a run end cleanly and tell the player how they did?

**Sprint status:** Not started

This sprint owns the end-of-run flow: death & quick restart, run completion & win, and the run-end stats screen shown to the player. Two more tickets — **death & quick restart** and **run completion / win** — will be moved here next by the orchestrator; this sprint currently holds only the run-end stats ticket as **10.1** and the others are incoming.

---

### Ticket 10.1 - Run-end statistics screen

**Goal:** Show the player a simple summary screen when a run ends (death or win) displaying how far they rode and how long it took.

**Dependencies:** Death & quick restart ticket (incoming to Sprint 10), Run completion & win ticket (incoming to Sprint 10).

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
- A run ends (death or win) and the player sees distance ridden and run time before returning to a fresh run.
- Stats reset cleanly on each new run.
- A playtester can answer: does a run end cleanly and tell the player how they did?
