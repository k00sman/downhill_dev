# Changelog

All notable changes to **Downhill** are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and dates use ISO 8601
(`YYYY-MM-DD`).

This project is pre-release and in active prototyping, so there are no tagged
versions yet. All entries currently live under [Unreleased].

## [Unreleased]

### Added

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
