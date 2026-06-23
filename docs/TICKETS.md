# Downhill Gameplay Prototype Tickets for Claude Opus-Assisted Development

This document breaks the current gameplay work into small, implementation-oriented tickets sized for Claude Opus to delegate quickly across research and coding subagents. The current state is assumed to be: one playable terrain segment exists, but the player has no controls yet. The objective is to reach a minimal horizontal slice of the core loop before attempting a broader vertical slice.

The ticket structure favors small, file-bounded work items because isolated input handling, movement logic, and feature-specific scripts are easier to research, implement, and test independently.

## Working rules

Each top-level ticket should be small enough that Opus can coordinate a short research pass, then assign implementation subtasks per script or system boundary.

Recommended execution pattern for each ticket:

1. Opus creates a short research/spec note for the ticket.
2. Opus identifies exact files to create or modify.
3. Opus spins up one subagent per file or tightly related file pair.
4. Opus requests a final integration pass only after all per-file tasks are complete.
5. Opus validates the build against acceptance criteria before moving on.

## Phase order

The work should be done in this order so that dependencies stay clear and each step produces a testable result.

| Phase | Goal | Why first |
|---|---|---|
| 1 | Input and player scaffolding | Establish clean separation between controls and movement logic. |
| 2 | Bicycle locomotion | Makes the level traversable at all. |
| 3 | Turning, braking, and camera | Produces the first playable downhill feel. |
| 4 | Jumping and crash basics | Adds risk and technical terrain support. |
| 5 | Health and fail states | Makes mistakes meaningful. |
| 6 | Monster chase stub | Tests the pressure loop central to the game concept. |
| 7 | Playtest instrumentation | Captures whether the prototype is actually working. |

## Phase 1: Input and player scaffolding

### Ticket 1.1 - Create gameplay control map

**Goal:** Define the prototype control bindings in one place.

**Dependencies:** None.

**Files:**
- `InputActions` asset or equivalent input config file
- `PlayerInputBindings` script or config wrapper

**Research task for Opus:** Determine the cleanest way in the current engine stack to centralize mouse, keyboard, and controller bindings for prototype gameplay input.

**Subagent tasks:**
- Research subagent: produce a short spec for control mapping structure, action names, and analog versus digital inputs.
- Coding subagent A: create or update the main input action asset/config.
- Coding subagent B: create a thin wrapper for reading named actions in gameplay code.

**Acceptance criteria:**
- Inputs for pedal left, pedal right, front brake, rear brake, turn, jump, and freelook are defined.
- Gameplay scripts can query named actions without hardcoded key checks.
- Keyboard and controller mappings both exist, even if rough.

**Notes:**
- Keep this ticket limited to bindings and access, not movement.
- Prefer stable action names over perfect final keybinds.

### Ticket 1.2 - Create player bicycle root actor

**Goal:** Establish the object hierarchy for the controllable bicycle and rider.

**Dependencies:** Ticket 1.1.

**Files:**
- `PlayerBikeController`
- player prefab/scene/blueprint
- optional `PlayerBikeReferences` helper

**Research task for Opus:** Define the minimum object structure needed for movement, camera anchor, ground checks, and future crash states.

**Subagent tasks:**
- Research subagent: specify scene hierarchy and required transforms.
- Coding subagent A: create the controller script shell and serialized references.
- Coding subagent B: wire the prefab/scene object hierarchy.

**Acceptance criteria:**
- A single player bicycle object can be spawned or placed in the level.
- Required transforms exist for bike body, camera pivot, ground check, and crash recovery hooks.
- The scene runs without null-reference errors from missing player links.

## Phase 2: Bicycle locomotion

### Ticket 2.1 - Implement downhill forward movement model

**Goal:** Make the bicycle move forward using gravity plus pedal input.

**Dependencies:** Tickets 1.1 and 1.2.

**Files:**
- `PlayerBikeController`
- `BikeMovementModel`

**Research task for Opus:** Propose a simple prototype movement model for downhill biking that combines gravity influence, forward drive, drag, and speed caps without requiring full simulation fidelity.

**Subagent tasks:**
- Research subagent: define the movement math and update loop responsibilities.
- Coding subagent A: implement the reusable movement model.
- Coding subagent B: connect player input to the controller and expose tuning parameters.

**Acceptance criteria:**
- The bicycle can move downhill without steering.
- Pedaling increases or sustains speed.
- The bike slows on flatter ground or when input stops.
- Max speed and acceleration values are tunable in inspector/config.

**Notes:**
- Do not implement turning here.
- Favor feel and tuning speed over realism.

### Ticket 2.2 - Implement alternating pedal input logic

**Goal:** Support left/right pedaling as a distinct mechanic rather than a single accelerate button.

**Dependencies:** Ticket 2.1.

**Files:**
- `PedalInputEvaluator`
- `PlayerBikeController`

**Research task for Opus:** Determine a simple rule set for alternating pedal presses that feels intentional, avoids spam exploits, and can degrade gracefully into a simpler fallback if needed.

**Subagent tasks:**
- Research subagent: write pedal cadence rules and edge-case handling.
- Coding subagent A: implement pedal state tracking and cadence evaluation.
- Coding subagent B: integrate cadence output into forward drive.

**Acceptance criteria:**
- Alternating left and right inputs provides forward drive.
- Repeating the same side only is less effective or ignored, based on the spec.
- The system exposes tuning values for cadence window and drive bonus.

**Notes:**
- This is a high-risk mechanic; keep the logic isolated so it can be replaced later.

## Phase 3: Turning, braking, and camera

### Ticket 3.1 - Implement steering input pipeline

**Goal:** Make the bicycle respond to turn input at low risk before speed-sensitive handling is added.

**Dependencies:** Ticket 2.1.

**Files:**
- `BikeSteeringModel`
- `PlayerBikeController`

**Research task for Opus:** Define a basic steering model suitable for a prototype on sloped terrain, including limits to prevent instant snapping.

**Subagent tasks:**
- Research subagent: specify steering behavior and whether it rotates velocity, heading, or both.
- Coding subagent A: implement steering model code.
- Coding subagent B: integrate input and expose tuning parameters.

**Acceptance criteria:**
- Left/right input turns the bicycle reliably.
- Steering feels controllable at low-to-medium speed.
- No extreme oscillation or instant 180-degree snapping occurs.

### Ticket 3.2 - Add speed-sensitive steering

**Goal:** Make steering more dangerous and less forgiving at higher speed.

**Dependencies:** Ticket 3.1.

**Files:**
- `BikeSteeringModel`
- tuning config file or scriptable settings object

**Research task for Opus:** Specify how steering authority and instability should scale with speed for the prototype.

**Subagent tasks:**
- Research subagent: propose a speed-to-steering response curve.
- Coding subagent A: implement speed scaling and optional wobble hooks.
- Coding subagent B: expose editable response curves or scalar values.

**Acceptance criteria:**
- Steering response changes with speed.
- High speed makes sharp corrections harder or riskier.
- Designers can tune the behavior without touching core code.

### Ticket 3.3 - Implement split braking

**Goal:** Add separate front and rear brake behavior.

**Dependencies:** Ticket 2.1 and Ticket 3.1.

**Files:**
- `BikeBrakeModel`
- `PlayerBikeController`

**Research task for Opus:** Define a simple prototype distinction between front and rear brakes, including what is deferred until later.

**Subagent tasks:**
- Research subagent: write brake behavior rules and failure cases.
- Coding subagent A: implement front/rear brake force handling.
- Coding subagent B: connect bindings and tuning settings.

**Acceptance criteria:**
- Front and rear brakes both reduce speed.
- They can be tuned separately.
- Combined braking is possible.

**Notes:**
- Do not add crash-from-front-brake yet unless trivial.

### Ticket 3.4 - Implement chase camera and freelook

**Goal:** Make the bike readable and playable through a basic downhill camera.

**Dependencies:** Ticket 1.2 and Ticket 3.1.

**Files:**
- `BikeCameraController`
- player prefab/scene camera rig

**Research task for Opus:** Specify a minimal chase camera with freelook that supports downhill readability, not cinematic polish.

**Subagent tasks:**
- Research subagent: define camera anchor points, smoothing, and freelook constraints.
- Coding subagent A: implement the chase camera script.
- Coding subagent B: wire the rig and tune default offsets.

**Acceptance criteria:**
- The camera follows the bicycle smoothly.
- The player can freelook without losing control clarity.
- The trail ahead stays readable in typical downhill motion.

## Phase 4: Jumping and crash basics

### Ticket 4.1 - Implement jump input and airtime state

**Goal:** Allow the bicycle to leave the ground intentionally on jumpable terrain.

**Dependencies:** Ticket 2.1 and Ticket 3.1.

**Files:**
- `BikeJumpModel`
- `BikeGroundingProbe`
- `PlayerBikeController`

**Research task for Opus:** Define a minimal jump model for the prototype, including grounded checks and airborne restrictions.

**Subagent tasks:**
- Research subagent: document grounded state rules and jump impulse behavior.
- Coding subagent A: implement ground detection.
- Coding subagent B: implement jump state and impulse.
- Coding subagent C: integrate jump restrictions into the controller.

**Acceptance criteria:**
- Jump only works when grounded.
- The bicycle can enter and leave an airborne state cleanly.
- Basic landing detection works.

### Ticket 4.2 - Add landing instability hook

**Goal:** Make jumps carry risk by destabilizing control on landing.

**Dependencies:** Ticket 4.1 and Ticket 3.2.

**Files:**
- `BikeJumpModel`
- `BikeSteeringModel`

**Research task for Opus:** Specify a short post-landing penalty model that increases tension without instantly causing failure.

**Subagent tasks:**
- Research subagent: define landing penalty timing and scaling inputs.
- Coding subagent A: implement a temporary landing penalty signal.
- Coding subagent B: consume the penalty in steering or stability code.

**Acceptance criteria:**
- Landings briefly affect handling.
- The effect is tunable.
- The effect is visible in play but not overwhelmingly punishing.

### Ticket 4.3 - Implement basic collision severity detection

**Goal:** Distinguish harmless bumps from dangerous crashes.

**Dependencies:** Ticket 2.1.

**Files:**
- `BikeCollisionEvaluator`
- `PlayerBikeController`

**Research task for Opus:** Define how collision speed, angle, and impacted object type should map to severity tiers in the prototype.

**Subagent tasks:**
- Research subagent: define severity thresholds and object filtering.
- Coding subagent A: implement collision data capture and evaluation.
- Coding subagent B: emit standardized crash events for other systems.

**Acceptance criteria:**
- Collisions are categorized into at least safe, damaging, and severe.
- Other systems can react to standardized crash events.
- Tree and rock collisions can be tuned differently if desired.

### Ticket 4.4 - Implement crash and recovery state

**Goal:** Add a recoverable non-fatal crash outcome.

**Dependencies:** Ticket 4.3.

**Files:**
- `BikeCrashStateMachine`
- `PlayerBikeController`
- optional `BikeRecoveryQTE`

**Research task for Opus:** Define the simplest crash state flow that interrupts movement, creates vulnerability, and can later support the QTE idea.

**Subagent tasks:**
- Research subagent: document crash state transitions and lockouts.
- Coding subagent A: implement crash state machine.
- Coding subagent B: add movement/input lockouts and recovery timing.
- Coding subagent C: add a placeholder recovery interaction or timed auto-recover.

**Acceptance criteria:**
- Severe but non-fatal crashes interrupt normal riding.
- The player can recover and return to control.
- Recovery timing is long enough to matter for chase pressure.

## Phase 5: Health and fail states

### Ticket 5.1 - Implement hidden player health system

**Goal:** Track survivability internally without UI complexity.

**Dependencies:** Ticket 4.3.

**Files:**
- `PlayerHealth`
- damage event definitions or health interface

**Research task for Opus:** Define a minimal health architecture that supports collision damage, instant-death events, and delayed regeneration.

**Subagent tasks:**
- Research subagent: write the health model and data flow.
- Coding subagent A: implement the health component.
- Coding subagent B: implement regeneration delay and damage event handling.

**Acceptance criteria:**
- The player has current and max health.
- Collision damage applies correctly.
- Health regeneration starts after a no-damage delay.
- The system supports instant death from monster contact later.

### Ticket 5.2 - Hook collision damage into health

**Goal:** Turn crash severity into meaningful damage.

**Dependencies:** Tickets 4.3 and 5.1.

**Files:**
- `BikeCollisionEvaluator`
- `PlayerHealth`
- damage config/settings file

**Research task for Opus:** Define a prototype damage curve from crash severity to health loss.

**Subagent tasks:**
- Research subagent: propose thresholds or curves for impact damage.
- Coding subagent A: integrate severity output into health damage calls.
- Coding subagent B: expose config for tuning damage values.

**Acceptance criteria:**
- Damaging crashes reduce health.
- Fatal crashes can force immediate death.
- Damage values are tunable without code changes.

### Ticket 5.3 - Implement death and quick restart loop

**Goal:** Make failed runs restart fast enough for repeated testing.

**Dependencies:** Ticket 5.1.

**Files:**
- `PlayerDeathHandler`
- `RunResetManager`

**Research task for Opus:** Specify the fastest prototype restart flow that preserves iteration speed and avoids menu overhead.

**Subagent tasks:**
- Research subagent: define death flow, delays, and reset requirements.
- Coding subagent A: implement death handling and input lockout.
- Coding subagent B: implement level/player reset behavior.

**Acceptance criteria:**
- Death transitions the player into a fail state.
- The run can restart quickly from a known spawn state.
- Core player systems reset cleanly.

## Phase 6: Monster chase stub

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

## Phase 7: Readability and instrumentation

### Ticket 7.1 - Implement headlamp gameplay light

**Goal:** Improve trail readability and support the night-horror tone.

**Dependencies:** Ticket 1.2.

**Files:**
- `PlayerHeadlampController`
- player prefab/scene light setup

**Research task for Opus:** Define the simplest headlamp setup that improves navigation without introducing heavy rendering complexity.

**Subagent tasks:**
- Research subagent: specify headlamp behavior, intensity, and attachment rules.
- Coding subagent A: implement light control and anchor behavior.
- Coding subagent B: wire the light into the player setup.

**Acceptance criteria:**
- The headlamp illuminates the trail ahead.
- The light follows the player's view or bike-facing direction according to the chosen spec.
- Reflective trail markers are visually useful when present.

### Ticket 7.2 - Add gameplay debug HUD

**Goal:** Surface invisible prototype state for tuning and testing.

**Dependencies:** Tickets 2.1, 5.1, and 6.2.

**Files:**
- `GameplayDebugHUD`
- optional `PrototypeMetricsCollector`

**Research task for Opus:** Determine the smallest useful on-screen debug readout for balancing movement and chase pressure.

**Subagent tasks:**
- Research subagent: specify which values should be shown.
- Coding subagent A: implement the debug HUD.
- Coding subagent B: expose movement, health, and monster-distance metrics.

**Acceptance criteria:**
- The HUD can display current speed, health, grounded state, crash state, and monster distance.
- The HUD can be toggled on and off.
- The data is sufficient for rapid tuning.

### Ticket 7.3 - Add run metrics logging

**Goal:** Capture enough data to tell whether the prototype is fun or just functional.

**Dependencies:** Tickets 5.3 and 6.3.

**Files:**
- `PrototypeRunMetrics`
- optional log exporter or console reporter

**Research task for Opus:** Define the minimum set of run-level metrics needed to judge pace, difficulty, and control friction in early playtests.

**Subagent tasks:**
- Research subagent: specify tracked metrics and reset rules.
- Coding subagent A: implement per-run data collection.
- Coding subagent B: implement summary output on death or run end.

**Acceptance criteria:**
- Each run records duration, crashes, cause of death, average speed, and monster catch events.
- Metrics reset correctly on restart.
- Designers can read the summary without digging through engine internals.

## Suggested first sprint

The fastest route to a playable downhill prototype is a narrow horizontal slice, not a polished vertical slice.

Recommended first sprint:

1. Ticket 1.1 - Create gameplay control map
2. Ticket 1.2 - Create player bicycle root actor
3. Ticket 2.1 - Implement downhill forward movement model
4. Ticket 3.1 - Implement steering input pipeline
5. Ticket 3.4 - Implement chase camera and freelook
6. Ticket 3.3 - Implement split braking
7. Ticket 7.2 - Add gameplay debug HUD

That sprint should answer the first crucial question: is simply riding the existing segment already interesting before adding monster pressure, crashes, or health systems?

## Opus orchestration prompts

### Generic top-level prompt

```text
You are coordinating implementation of one gameplay ticket for the Downhill prototype.

Current project state:
- One terrain segment exists.
- No player controls are implemented yet.
- The goal is a fast gameplay prototype, not production-quality architecture.

Your job:
1. Read the ticket.
2. Produce a short implementation spec.
3. Identify exact files to create or modify.
4. Split work into small per-file subagent tasks.
5. Integrate the results.
6. Validate against the acceptance criteria.

Constraints:
- Prefer simple, testable solutions.
- Isolate risky mechanics into their own files.
- Avoid mixing unrelated systems in one ticket.
- Leave clear tuning parameters exposed.
```

### Research subagent prompt

```text
Research this gameplay implementation ticket for the Downhill prototype.

Output only:
- recommended approach
- files to modify
- data flow between files
- edge cases
- acceptance test plan

Keep the recommendation lightweight and prototype-oriented.
```

### Per-file coding subagent prompt

```text
Implement only the assigned file changes for this gameplay ticket.

Requirements:
- Follow the provided spec.
- Do not redesign the feature.
- Keep tuning values exposed.
- Add concise comments only where needed.
- Report any integration assumptions clearly.
```

## Definition of done

A gameplay ticket is done when:

- The feature works in the existing level.
- The files touched match the planned scope.
- Acceptance criteria are met.
- Debug output is sufficient to tune the feature if needed.
- The ticket does not silently introduce unrelated systems.

The prototype should aim to prove that the game is worth making before it tries to prove full production readiness.
