# Changelog

All notable changes to **Downhill** are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and dates use ISO 8601
(`YYYY-MM-DD`).

This project is pre-release and in active prototyping, so there are no tagged
versions yet. All entries currently live under [Unreleased].

## [Unreleased]

### Added

- **Bike terrain contact stability** — 2026-06-23
  - Added controlled downhill tangent-following with capped upward grounded
    velocity to reduce steep-slope pops.
  - Added multi-point ground probing so continuous slopes and smooth bumps
    produce steadier ground normals.
  - Added visual terrain pitch on `BikeBody` while keeping the root Rigidbody
    rotation-frozen.
  - Tightened the root collider into a compact contact proxy so steep-slope
    changes are less likely to pop the bike off terrain.

- **Player bike yaw drift before steering** — 2026-06-23
  - Locked runtime Rigidbody yaw along with pitch and roll until steering owns
    bike rotation, preventing physics contact from turning the bike sideways or
    uphill and causing the forward movement model to clamp speed to zero.
  - Removed leftover movement debug logs from the normal riding path.

- **Bike stopping on small rises** — 2026-06-23
  - Reduced default forward-movement drag and slope-drive gain so lower-speed
    momentum carries over shallow trail undulations without runaway acceleration
    on smooth descents.
  - Changed movement math to preserve horizontal forward momentum across
    changing smooth ground normals, avoiding artificial speed loss or gain from
    minor bumps.
  - Stopped the grounded movement model from injecting vertical velocity on
    steep slopes, reducing terrain-contact pops while still using slope angle
    for forward acceleration.

- **Downhill forward movement model** (Ticket 2.1) — 2026-06-22
  - Added `BikeMovementModel`, a serializable pure movement model that blends
    gravity along the slope, pedal acceleration, drag, and a speed cap for
    straight-ahead downhill rolling.
  - Wired the player bike controller to sample ground, accumulate decaying pedal
    power from left/right pedal input, drive Rigidbody velocity while grounded,
    and expose grounded/pedal seams for tuning and tests.
  - Added EditMode coverage for the movement math and PlayMode coverage for
    flat-ground drag, downhill acceleration, and pedalled acceleration, plus the
    Ticket 2.1 design spec and implementation plan docs.

- **Player bicycle root actor** (Ticket 1.2) — 2026-06-22
  - Restructured `PFB_Player.prefab` into a dynamic-Rigidbody actor: root carries
    `Rigidbody` + `BoxCollider` + `PlayerInputReader` + `PlayerBikeController` and
    is tagged `Player`. The bike mesh now sits under a `BikeBody` lean pivot, with
    `CameraPivot`, `GroundCheck`, and `RecoveryAnchor` anchor transforms for later
    camera, grounding, and crash-recovery work.
  - Added the `Downhill.Player` assembly and `PlayerBikeController`, a wiring/
    validation shell exposing the actor's references and a read-only `BikeState`
    (Riding/Crashed/Recovering) stub. It auto-wires same-object components and
    logs a named error for any missing link, so the scene runs without
    null-reference errors. No movement logic yet.
  - Added EditMode (prefab structure + serialized-reference wiring) and PlayMode
    (instantiate-and-validate) tests.

- **Gameplay control map** (Ticket 1.1) — 2026-06-22
  - Added the Unity Input System package (`com.unity.inputsystem` 1.19.0). Set
    Active Input Handling to "Both" (legacy + new) so existing third-party demo
    scripts keep working.
  - Added `Assets/Scripts/Input/DownhillControls.inputactions`, a dedicated input
    asset with one "Bike" action map exposing the actions PedalLeft, PedalRight,
    FrontBrake, RearBrake, Turn, Jump, and Freelook, with Keyboard&Mouse and
    Gamepad control schemes. C# class generation is enabled
    (`Downhill.Input.DownhillControls`).
  - Added `Assets/Scripts/Input/PlayerInputReader.cs`, a thin MonoBehaviour
    wrapper that exposes the actions to gameplay: polled Turn, FrontBrake,
    RearBrake, and Freelook, plus edge events PedalLeftPressed, PedalRightPressed,
    and Jumped.
  - Added the `Downhill.Input` assembly definition, with EditMode tests (asset
    structure) and PlayMode tests (PlayerInputReader behaviour, using
    `InputTestFixture`).
  - Added a `.gitignore` rule for auto-generated
    `Assets/Resources/PerformanceTestRun*.json` files produced by the
    performance-testing package.

## Conventions

How to add entries to this changelog:

- Keep entries concise and user-facing — describe what changed, not how it was
  implemented internally.
- Group changes under `### Added`, `### Changed`, `### Fixed`, or `### Removed`,
  using only the headings that apply.
- Add new entries to the top of the relevant group under `## [Unreleased]`
  (newest first).
- Reference the ticket number from `docs/TICKETS.md` when an entry corresponds to
  one (e.g. "Ticket 1.1").
- Use ISO dates (`YYYY-MM-DD`).
- When the prototype reaches a tagged release, move the accumulated `[Unreleased]`
  entries under a new dated version heading.

[Unreleased]: https://keepachangelog.com/en/1.1.0/
