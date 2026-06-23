# Ticket 1.2 — Player Bicycle Root Actor — Design Spec

**Date:** 2026-06-22
**Ticket:** 1.2 (Phase 1 — Input & player scaffolding)
**Depends on:** Ticket 1.1 (gameplay control map) — done.

## Goal

Establish the object hierarchy and a controller *shell* for the controllable
bicycle: required child transforms, a dynamic-Rigidbody physics base, wired
serialized references, and validation so the scene runs with no null-reference
errors. **No movement, steering, braking, or camera logic** — those are later
tickets. This ticket only makes the actor exist and be correctly wired.

## Acceptance criteria (from TICKETS.md)

- A single player bicycle object can be spawned or placed in the level.
- Required transforms exist for bike body, camera pivot, ground check, and crash
  recovery hooks.
- The scene runs without null-reference errors from missing player links.

## Current state

- `Assets/PFB_Player.prefab` exists and is placed in `Assets/Scenes/SCN_Tutorial.unity`
  at the origin. It currently contains only:
  - Root transform — `Untagged`, **no Rigidbody, no scripts**.
  - A nested bike-mesh prefab instance (`MSH_BikeDefault`) carrying an **added**
    `BoxCollider` (size `0.12 × 1 × 2.16`, center `y 0.51`).
  - A `Point Light` (spot) acting as a placeholder headlamp.
- `Main Camera` is a **standalone** scene object, not parented to the player.
- `PlayerInputReader` (Ticket 1.1, assembly `Downhill.Input`) is **not** on the
  prefab yet.

## Design decisions

These four forks were resolved during brainstorming:

1. **Physics base — Dynamic Rigidbody.** The root carries a `Rigidbody` + collider
   so gravity, collisions, crash impacts, and impulse forces come from the physics
   engine. Ground checks will raycast/spherecast from a foot transform (later
   tickets).
2. **Lean pivot — separate `BikeBody`.** Hierarchy is
   `Root (physics) → BikeBody (lean pivot) → mesh`, so later steering/landing
   wobble can tilt the visual without rotating the physics body.
3. **References — direct serialized fields + validation.** `PlayerBikeController`
   holds `[SerializeField]` references directly; no separate references component
   (YAGNI). `OnValidate` auto-wires same-GameObject components; `Awake` validates
   and logs a clear, named error for any missing link.
4. **Crash hooks — `RecoveryAnchor` transform + read-only `BikeState` stub.** A
   child anchor transform plus a public `BikeState` enum
   (`Riding`/`Crashed`/`Recovering`) defaulting to `Riding`, exposed read-only. No
   transitions or logic this ticket — just a stable hook for Tickets 4.4 / 6.x.

## Target hierarchy (`PFB_Player.prefab`)

```
PFB_Player          tag: Player
                    [Rigidbody, BoxCollider, PlayerInputReader, PlayerBikeController]
├── BikeBody         empty lean pivot (identity transform)
│   └── MSH_BikeDefault   existing bike mesh; its added BoxCollider REMOVED
├── CameraPivot      empty, ~chest height — follow target for Ticket 3.4
├── GroundCheck      empty, at tire-contact height — raycast origin for 2.1 / 4.1
├── RecoveryAnchor   empty — rider/recovery hook for Ticket 4.4
└── Point Light      existing headlamp spot — untouched (Ticket 7.1 formalizes it)
```

### Component details

- **Root tag:** `Player` (create the tag if absent). Lets the monster (6.1) and
  ground raycasts identify/exclude the player.
- **Rigidbody (root):** prototype-tunable starting values — `mass ≈ 80`,
  `interpolation = Interpolate`, `collisionDetection = Continuous`
  (fast downhill, avoids tunneling), `useGravity = true`, no constraints yet.
- **BoxCollider (root):** moved up from the mesh. Reuse the existing silhouette
  `size 0.12 × 1 × 2.16`, `center y 0.51`. The mesh's added BoxCollider is removed
  so the upright physics body and the leanable visual are decoupled.
- **BikeBody / CameraPivot / GroundCheck / RecoveryAnchor:** empty GameObjects.
  Local positions are rough placeholders, tunable in-editor; nothing reads their
  exact values this ticket.

## Code

### New assembly `Downhill.Player`

`Assets/Scripts/Player/Downhill.Player.asmdef`
- `name`: `Downhill.Player`
- `references`: `["Downhill.Input"]`
- `autoReferenced`: `true`

Gameplay code gets its own focused assembly (per CLAUDE.md), and this keeps the
controller testable from the test assemblies without referencing
`Assembly-CSharp`.

### `PlayerBikeController.cs` (namespace `Downhill.Player`)

A reference/validation shell — **no movement logic**.

- `public enum BikeState { Riding, Crashed, Recovering }`
- `[RequireComponent(typeof(Rigidbody))]` on the class.
- `[SerializeField]` private references, each surfaced as a read-only property:
  - `Rigidbody Body`
  - `PlayerInputReader Input`
  - `Transform BikeBody`
  - `Transform CameraPivot`
  - `Transform GroundCheck`
  - `Transform RecoveryAnchor`
- `public BikeState State { get; private set; } = BikeState.Riding;`
- `OnValidate()` — if null, auto-wire the same-GameObject components
  (`Body = GetComponent<Rigidbody>()`, `Input = GetComponent<PlayerInputReader>()`).
  The four child transforms are wired by hand in the prefab.
- `Awake()` — validate every reference; for any null, `Debug.LogError` a clear
  message naming the missing link (with `this` as context object). This is what
  satisfies "runs without null-reference errors": mis-wiring fails loud and
  named, not as a downstream NRE.

## Data flow

No runtime behavior beyond validation this ticket. `PlayerInputReader` already
self-enables its actions (Ticket 1.1). `PlayerBikeController` only *holds* the
wiring and exposes the `BikeState` stub so later tickets read stable hooks. The
standalone `Main Camera` is **not** rewired here — Ticket 3.4 will point it at
`CameraPivot`.

## Testing

Mirrors the Ticket 1.1 approach (asset-structure EditMode test + behavioural
PlayMode test). Both test assemblies add a reference to `Downhill.Player`.

### EditMode — `PlayerBikePrefabTests.cs`

Load the prefab asset at `Assets/PFB_Player.prefab` and assert:
- Root has `PlayerBikeController`, `Rigidbody`, `PlayerInputReader`.
- Root has a `BoxCollider`; the mesh child does not add its own collider.
- Child transforms `BikeBody`, `CameraPivot`, `GroundCheck`, `RecoveryAnchor`
  exist by name.
- Root tag is `Player`.
- Via `SerializedObject`, every serialized reference field on
  `PlayerBikeController` is non-null (catches a mis-wired prefab without play
  mode).

### PlayMode — `PlayerBikeControllerTests.cs`

- Instantiate the prefab, step one frame.
- `LogAssert.NoUnexpectedReceived()` — no errors/exceptions during Awake/enable.
- `controller.State == BikeState.Riding`.
- All reference properties (`Body`, `Input`, `BikeBody`, `CameraPivot`,
  `GroundCheck`, `RecoveryAnchor`) are non-null after `Awake`.

## Files

**Create**
- `Assets/Scripts/Player/Downhill.Player.asmdef` (+ `.meta`)
- `Assets/Scripts/Player/PlayerBikeController.cs` (+ `.meta`)
- `Assets/Tests/EditMode/PlayerBikePrefabTests.cs` (+ `.meta`)
- `Assets/Tests/PlayMode/PlayerBikeControllerTests.cs` (+ `.meta`)

**Modify**
- `Assets/PFB_Player.prefab` — restructure hierarchy, add components, set tag.
- `Assets/Tests/EditMode/Downhill.Tests.EditMode.asmdef` — add `Downhill.Player`
  reference.
- `Assets/Tests/PlayMode/Downhill.Tests.PlayMode.asmdef` — add `Downhill.Player`
  reference.
- `CHANGELOG.md` — log under `[Unreleased] → Added`.

## Execution note

The prefab restructuring (reparenting the mesh under a new `BikeBody`, adding
the Rigidbody/collider/empties, setting the tag, wiring serialized references) is
safest done **in the Unity editor with a walkthrough** — hand-editing prefab YAML
risks GUID/fileID breakage. All scripts, asmdefs, and tests are authored directly.

## Out of scope (later tickets)

- Movement / gravity drive / speed caps — Ticket 2.1.
- Steering — 3.1. Braking — 3.3. Chase camera + freelook wiring — 3.4.
- Jump / grounding probe logic — 4.1. Crash state transitions — 4.4.
- Headlamp behaviour — 7.1.
