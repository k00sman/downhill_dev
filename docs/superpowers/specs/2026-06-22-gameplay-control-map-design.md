# Gameplay Control Map — Design (Ticket 1.1)

**Date:** 2026-06-22
**Ticket:** `docs/TICKETS.md` → Phase 1, Ticket 1.1 — Create gameplay control map
**Status:** Approved

## Goal

Define the prototype's gameplay control bindings in one place, on the Unity Input
System, and provide a thin wrapper so gameplay scripts query named actions without
hardcoded key checks. Scope is bindings + access only — no movement, no player prefab
wiring (that is Ticket 1.2).

## Context (current project state)

- Unity **6000.3.17f1** (Unity 6.3), URP 17.3.
- Project is currently on the **legacy Input Manager** (`activeInputHandler: 0`); the
  Input System package is **not** installed.
- A stock `Assets/InputSystem_Actions.inputactions` asset already exists (default Unity 6
  FPS-style actions) but is inert without the package. It is left untouched by this ticket
  and reserved for UI/menu navigation later.
- No `asmdef` files exist under `Assets/Scripts` — gameplay code compiles into
  `Assembly-CSharp`, which auto-references the Input System package once installed.
- `com.unity.test-framework` 1.4.6 is installed (EditMode tests available).

## Decisions

| Decision | Choice |
|---|---|
| Input backend | New Input System (`com.unity.inputsystem`) |
| Active input handler | **Both (`2`)** — keeps legacy `Input.GetAxis` working so the third-party Retro Shaders demo scripts don't break |
| Asset layout | New dedicated `DownhillControls.inputactions` with one `Bike` map; stock asset left for UI later |
| Access pattern | "Generate C# Class" on the asset + a thin `PlayerInputReader` MonoBehaviour wrapper |

## Architecture

Three layers, each independently understandable:

1. **`DownhillControls.inputactions`** — the binding data. One `Bike` action map.
2. **`DownhillControls.cs`** — auto-generated strongly-typed accessor (codegen from the
   asset; regenerated when the asset changes). Namespace `Downhill.Input`.
3. **`PlayerInputReader.cs`** — the thin wrapper MonoBehaviour (the ticket's
   "PlayerInputBindings"). Owns a `DownhillControls` instance, enables the `Bike` map in
   `OnEnable` and disables it in `OnDisable`, and exposes a small queryable surface.
   Gameplay scripts depend only on this class.

Data flow: device → Input System → `DownhillControls` (generated) → `PlayerInputReader`
→ gameplay scripts (later tickets).

## Action set — `Bike` map

| Action | Type | Rationale |
|---|---|---|
| `PedalLeft` | Button (digital) | Discrete press; feeds alternating-cadence mechanic (Ticket 2.2) |
| `PedalRight` | Button (digital) | Same |
| `FrontBrake` | Value / float 0–1 (analog) | Trigger pressure on gamepad; 0/1 on keyboard |
| `RearBrake` | Value / float 0–1 (analog) | Same |
| `Turn` | Value / float −1..1 (analog axis) | Steering needs proportional input |
| `Jump` | Button | Discrete |
| `Freelook` | Value / Vector2 | Look delta / right stick |

## Default bindings

Rough by intent — the ticket prefers stable action names over final keybinds. Two control
schemes: **Keyboard&Mouse** and **Gamepad**.

| Action | Keyboard & Mouse | Gamepad |
|---|---|---|
| Turn | A / D (1D axis composite) | Left stick X |
| PedalLeft | Left Arrow | Left bumper (LB) |
| PedalRight | Right Arrow | Right bumper (RB) |
| FrontBrake | Right Mouse | Right trigger |
| RearBrake | Left Shift | Left trigger |
| Jump | Space | South button (A) |
| Freelook | Mouse delta | Right stick |

Rationale: alternating bumpers map intuitively to alternating pedals; analog triggers to
brake levers; steering on A/D leaves the arrow keys free for pedal cadence.

## `PlayerInputReader` surface

Polled (read every frame by consumers):
- `float Turn`
- `float FrontBrake`
- `float RearBrake`
- `Vector2 Freelook`

Edge events (fire once on press):
- `event Action PedalLeftPressed`
- `event Action PedalRightPressed`
- `event Action Jumped`
- `bool JumpedThisFrame` — poll-friendly alternative to the `Jumped` event

Lifecycle: instantiate `DownhillControls` in `Awake`; enable `Bike` map and subscribe to
the relevant `performed` callbacks in `OnEnable`; unsubscribe and disable in `OnDisable`;
`Dispose` the controls in `OnDestroy`.

## Files

Created:
- `Assets/Scripts/Input/DownhillControls.inputactions` (+ `.meta`)
- `Assets/Scripts/Input/DownhillControls.cs` (generated from the asset)
- `Assets/Scripts/Input/PlayerInputReader.cs`
- `Assets/Tests/EditMode/DownhillControlsTests.cs` (+ EditMode asmdef if not present)

Modified (project-wide prerequisite, forces a one-time editor restart):
- `Packages/manifest.json` — add `com.unity.inputsystem`
- `ProjectSettings/ProjectSettings.asset` — `activeInputHandler: 2`

Untouched: the stock `Assets/InputSystem_Actions.inputactions`, `PFB_Player.prefab`
(wiring is Ticket 1.2).

## Error handling / edge cases

- Wrapper guards against missing/disabled actions: enable on `OnEnable`, disable on
  `OnDisable`, dispose on `OnDestroy` to avoid leaked callbacks and duplicate input.
- Analog brake/turn values are read through the action's `ReadValue<float>()`; default 0
  when no device present.
- Codegen: if the asset and generated class drift, the class is regenerated from the asset
  (the asset is the source of truth).

## Verification

- The Unity editor cannot run headless in the dev environment, so the
  package-install + restart + in-editor compile check is performed by the user.
- Automated structural guarantee via an **EditMode test** that loads
  `DownhillControls.inputactions` and asserts:
  - the `Bike` map exists;
  - all seven named actions exist with the expected types;
  - each action has at least one Keyboard&Mouse binding and one Gamepad binding.

## Acceptance criteria (from ticket)

- [x] Inputs for pedal left, pedal right, front brake, rear brake, turn, jump, and
  freelook are defined.
- [x] Gameplay scripts can query named actions without hardcoded key checks (via
  `PlayerInputReader`).
- [x] Keyboard and controller mappings both exist, even if rough.

## Out of scope

Movement logic, pedal cadence evaluation, steering model, player prefab wiring, UI/menu
input.
