# Sprint 1 — Core Ride

**Goal question:** Can we ride the trail at all?

**Sprint status:** In progress

---

### Ticket 1.1 - Create gameplay control map

**Status:** Completed.

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

---

### Ticket 1.2 - Create player bicycle root actor

**Status:** Completed.

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

---

### Ticket 2.1 - Implement downhill forward movement model

**Status:** Completed.

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
- Terrain-driven left/right curve following is intentionally deferred to Ticket
  3.1. Ticket 2.1 keeps the bike root rotation frozen and only owns forward
  speed/contact stability; curved or banked slope steering needs an explicit
  heading/yaw model so terrain normals do not create noisy implicit turns.

---

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
- Banked or curved downhill terrain can influence heading through the steering
  model, so the bike naturally follows readable slope curves instead of driving
  straight through them.

---

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

---

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

---

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

---

## Sprint exit criteria

Sprint 1 is complete when:
- The player can spawn on the trail, pedal forward, steer left and right, brake, and view the action through the chase camera without null errors.
- The debug HUD is readable and shows speed and grounded state.
- A playtester can ride the existing terrain segment end-to-end and answer: is simply riding the trail already interesting?
