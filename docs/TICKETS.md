# Downhill Gameplay Prototype — Ticket Index

Full ticket details (goal, acceptance criteria, subagent tasks) live in the sprint files under `docs/sprints/`. This file is a navigable index only.

## Sprint index

| Sprint | File | Goal question | Status |
|--------|------|---------------|--------|
| Sprint 0 — Agentic Workflow & Tooling | [`docs/sprints/sprint-0-best-practices.md`](sprints/sprint-0-best-practices.md) | Are our agent workflows fast, safe, and cross-tool? | In progress |
| Sprint 1 — Core Ride | [`docs/sprints/sprint-1-core-ride.md`](sprints/sprint-1-core-ride.md) | Can we ride the trail at all? | Completed |
| Sprint 2 — Advanced Input | [`docs/sprints/sprint-2-advanced-input.md`](sprints/sprint-2-advanced-input.md) | Does input feel distinctive and risky? | In progress |
| Sprint 3 — Crashes | [`docs/sprints/sprint-3-crashes.md`](sprints/sprint-3-crashes.md) | Do mistakes feel meaningful? | Not started |
| Sprint 4 — Survival | [`docs/sprints/sprint-4-survival.md`](sprints/sprint-4-survival.md) | Does death feel fair? | Not started |
| Sprint 5 — The Chase | [`docs/sprints/sprint-5-the-chase.md`](sprints/sprint-5-the-chase.md) | Does the monster create pressure? | Not started |
| Sprint 6 — Readability | [`docs/sprints/sprint-6-readability.md`](sprints/sprint-6-readability.md) | Can we tune and read the prototype? | Not started |
| Sprint 7 — Segments & Run Composition | [`docs/sprints/sprint-7-segments.md`](sprints/sprint-7-segments.md) | Do modular segments connect cleanly into replayable runs? | Not started |
| Sprint 8 — Audio & Atmosphere | [`docs/sprints/sprint-8-audio.md`](sprints/sprint-8-audio.md) | Does audio sell speed, danger, and dread? | Not started |
| Sprint 9 — Surface & Terrain Handling | [`docs/sprints/sprint-9-surface.md`](sprints/sprint-9-surface.md) | Does the trail surface change how the bike handles? | Not started |
| Sprint 10 — Run End & Flow | [`docs/sprints/sprint-10-run-end.md`](sprints/sprint-10-run-end.md) | Does a run end cleanly and tell the player how they did? | Not started |

> **Sprint 0** is a meta/tooling sprint (agent workflow), not part of the gameplay
> phase order below. Its tickets live in its own file and are not duplicated in the
> gameplay phase/roster tables.

## Phase order

| Phase | Goal | Why first |
|-------|------|-----------|
| 1 | Input and player scaffolding | Establish clean separation between controls and movement logic. |
| 2 | Bicycle locomotion | Makes the level traversable at all. |
| 3 | Turning, braking, and camera | Produces the first playable downhill feel. |
| 4 | Jumping and crash basics | Adds risk and technical terrain support. |
| 5 | Health and fail states | Makes mistakes meaningful. |
| 6 | Monster chase stub | Tests the pressure loop central to the game concept. |
| 7 | Playtest instrumentation | Captures whether the prototype is actually working. |
| 8 | Segments and run composition | Stitches modular segments into varied, replayable runs. |
| 9 | Audio and atmosphere | Sells speed, danger, and dread through sound. |
| 10 | Surface and terrain handling | Makes the trail surface change grip, speed, and stability. |

> Phases 8–10 cover README systems (level loading / segment composition; audio;
> surface-driven handling) that the original phase list omitted. Phases 1–7 stay
> the validation-first handling/chase core; 8–10 follow once the ride and chase
> hold up.

> **Ticket numbering:** ticket numbers follow the pattern **X.Y** where X is the
> sprint number and Y is the ticket's position within that sprint. A single sprint
> may span several thematic phases — the phase concept above remains valid for
> understanding build order; the ticket number tells you which sprint to open.

## Ticket roster

### Sprint 1 — Core Ride

| Ticket | Title | Status |
|--------|-------|--------|
| 1.1 | Create gameplay control map | ✅ Completed |
| 1.2 | Create player bicycle root actor | ✅ Completed |
| 1.3 | Implement downhill forward movement model | ✅ Completed |
| 1.4 | Implement steering input pipeline | ✅ Completed |
| 1.5 | Implement split braking | ✅ Completed |
| 1.6 | Implement first-person camera and freelook | ✅ Completed |
| 1.7 | Add gameplay debug HUD | ✅ Completed |

### Sprint 2 — Advanced Input

| Ticket | Title | Status |
|--------|-------|--------|
| 2.1 | Implement alternating pedal input logic | Implemented; needs Unity validation |
| 2.2 | Add speed-sensitive steering | Not started |

### Sprint 3 — Crashes

| Ticket | Title | Status |
|--------|-------|--------|
| 3.1 | Implement jump input and airtime state | Not started |
| 3.2 | Add landing instability hook | Not started |
| 3.3 | Implement basic collision severity detection | Not started |
| 3.4 | Implement crash and recovery state | Not started |
| 3.5 | Add death wobble at excess speed | Not started |
| 3.6 | Add front-brake-throw at high speed | Not started |

### Sprint 4 — Survival

| Ticket | Title | Status |
|--------|-------|--------|
| 4.1 | Implement hidden player health system | Not started |
| 4.2 | Hook collision damage into health | Not started |
| 4.3 | Implement death and quick restart loop | Not started |
| 4.4 | Implement hidden-health damage feedback | Not started |

### Sprint 5 — The Chase

| Ticket | Title | Status |
|--------|-------|--------|
| 5.1 | Create monster chase actor stub | Not started |
| 5.2 | Implement delayed speed-follow behavior | Not started |
| 5.3 | Implement monster kill contact | Not started |

### Sprint 6 — Readability

| Ticket | Title | Status |
|--------|-------|--------|
| 6.1 | Implement headlamp gameplay light | Not started |

### Sprint 7 — Segments & Run Composition

| Ticket | Title | Status |
|--------|-------|--------|
| 7.1 | Define segment schema and metadata | Not started |
| 7.2 | Implement segment socket connection | Not started |
| 7.3 | Implement level loading and sequencing system | Not started |
| 7.4 | Implement no-repeat run shuffle | Not started |
| 7.5 | Implement run completion and summary | Not started |

### Sprint 8 — Audio & Atmosphere

| Ticket | Title | Status |
|--------|-------|--------|
| 8.1 | Set up audio foundation | Not started |
| 8.2 | Speed and wind audio | Not started |
| 8.3 | Surface and impact audio | Not started |
| 8.4 | Monster pursuit audio cues | Not started |
| 8.5 | Tension music hooks | Not started |

### Sprint 9 — Surface & Terrain Handling

| Ticket | Title | Status |
|--------|-------|--------|
| 9.1 | Define surface type system | Not started |
| 9.2 | Apply surface effects to handling | Not started |
| 9.3 | Surface feedback hooks | Not started |

### Sprint 10 — Run End & Flow

| Ticket | Title | Status |
|--------|-------|--------|
| 10.1 | Run-end statistics screen | Not started |

## Working rules

Each ticket is small enough for one short research pass followed by per-file implementation tasks. Recommended execution pattern:

1. Read the sprint file for the ticket.
2. Produce a short implementation spec.
3. Identify exact files to create or modify.
4. Split work into per-file subagent tasks.
5. Integrate results.
6. Validate against acceptance criteria before moving on.

## Orchestration prompts

### Generic top-level

```text
You are coordinating implementation of one gameplay ticket for the Downhill prototype.
Read the sprint file. Produce a short spec. Identify exact files. Split into subagent tasks. Integrate. Validate.
Prefer simple, testable solutions. Isolate risky mechanics. Leave tuning parameters exposed.
```

### Research subagent

```text
Research this ticket. Output: recommended approach, files to modify, data flow, edge cases, acceptance test plan.
Keep it lightweight and prototype-oriented.
```

### Coding subagent

```text
Implement only the assigned file. Follow the spec. Don't redesign. Keep tuning values exposed.
Report integration assumptions clearly.
```

## Definition of done

A ticket is done when: feature works in the existing level; touched files match planned scope; acceptance criteria met; debug output is sufficient to tune; no unrelated systems introduced.
