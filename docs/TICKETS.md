# Downhill Gameplay Prototype — Ticket Index

Full ticket details (goal, acceptance criteria, subagent tasks) live in the sprint files under `docs/sprints/`. This file is a navigable index only.

## Sprint index

| Sprint | File | Goal question | Status |
|--------|------|---------------|--------|
| Sprint 1 — Core Ride | [`docs/sprints/sprint-1-core-ride.md`](sprints/sprint-1-core-ride.md) | Can we ride the trail at all? | In progress |
| Sprint 2 — Advanced Input | [`docs/sprints/sprint-2-advanced-input.md`](sprints/sprint-2-advanced-input.md) | Does input feel distinctive and risky? | Not started |
| Sprint 3 — Crashes | [`docs/sprints/sprint-3-crashes.md`](sprints/sprint-3-crashes.md) | Do mistakes feel meaningful? | Not started |
| Sprint 4 — Survival | [`docs/sprints/sprint-4-survival.md`](sprints/sprint-4-survival.md) | Does death feel fair? | Not started |
| Sprint 5 — The Chase | [`docs/sprints/sprint-5-the-chase.md`](sprints/sprint-5-the-chase.md) | Does the monster create pressure? | Not started |
| Sprint 6 — Readability | [`docs/sprints/sprint-6-readability.md`](sprints/sprint-6-readability.md) | Can we tune and read the prototype? | Not started |

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

## Ticket roster

### Phase 1 — Input and player scaffolding

| Ticket | Title | Sprint | Status |
|--------|-------|--------|--------|
| 1.1 | Create gameplay control map | Sprint 1 | ✅ Completed |
| 1.2 | Create player bicycle root actor | Sprint 1 | ✅ Completed |

### Phase 2 — Bicycle locomotion

| Ticket | Title | Sprint | Status |
|--------|-------|--------|--------|
| 2.1 | Implement downhill forward movement model | Sprint 1 | ✅ Completed |
| 2.2 | Implement alternating pedal input logic | Sprint 2 | Not started |

### Phase 3 — Turning, braking, and camera

| Ticket | Title | Sprint | Status |
|--------|-------|--------|--------|
| 3.1 | Implement steering input pipeline | Sprint 1 | Not started |
| 3.2 | Add speed-sensitive steering | Sprint 2 | Not started |
| 3.3 | Implement split braking | Sprint 1 | Not started |
| 3.4 | Implement chase camera and freelook | Sprint 1 | Not started |

### Phase 4 — Jumping and crash basics

| Ticket | Title | Sprint | Status |
|--------|-------|--------|--------|
| 4.1 | Implement jump input and airtime state | Sprint 3 | Not started |
| 4.2 | Add landing instability hook | Sprint 3 | Not started |
| 4.3 | Implement basic collision severity detection | Sprint 3 | Not started |
| 4.4 | Implement crash and recovery state | Sprint 3 | Not started |

### Phase 5 — Health and fail states

| Ticket | Title | Sprint | Status |
|--------|-------|--------|--------|
| 5.1 | Implement hidden player health system | Sprint 4 | Not started |
| 5.2 | Hook collision damage into health | Sprint 4 | Not started |
| 5.3 | Implement death and quick restart loop | Sprint 4 | Not started |

### Phase 6 — Monster chase stub

| Ticket | Title | Sprint | Status |
|--------|-------|--------|--------|
| 6.1 | Create monster chase actor stub | Sprint 5 | Not started |
| 6.2 | Implement delayed speed-follow behavior | Sprint 5 | Not started |
| 6.3 | Implement monster kill contact | Sprint 5 | Not started |

### Phase 7 — Readability and instrumentation

| Ticket | Title | Sprint | Status |
|--------|-------|--------|--------|
| 7.1 | Implement headlamp gameplay light | Sprint 6 | Not started |
| 7.2 | Add gameplay debug HUD | Sprint 1 | Not started |
| 7.3 | Add run metrics logging | Sprint 6 | Not started |

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
