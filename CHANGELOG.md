# Changelog

All notable changes to **Downhill** are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and dates use ISO 8601
(`YYYY-MM-DD`).

This project is pre-release and in active prototyping, so there are no tagged
versions yet. All entries currently live under [Unreleased].

## [Unreleased]

### Added

- **Steering input pipeline (Ticket 3.1)** — 2026-06-25
  - Added a pure `BikeSteeringModel` for bounded, tunable yaw steering with
    terrain camber self-turning away from raised elevation.
  - Wired `PlayerBikeController` to keep Rigidbody physics rotation frozen while
    applying code-owned yaw before movement.
  - Changed terrain steering to use explicit left/right height probes instead
    of raw averaged ground normals, reducing random self-turning on noisy
    terrain.
  - Added EditMode steering model coverage and PlayMode steering integration
    coverage for prefab constraints, input-driven heading changes, and
    banked-ground heading influence without pitch-only yaw drift.

- **Unity MCP safety policy + setup prep (Sprint 0, T0.5)** — 2026-06-25
  - Added `docs/playbooks/unity-mcp-allowlist.md` (read-only vs. confirm-gated
    tool policy + human setup steps) and an `AGENTS.md` Unity-MCP safety note.
    Selected the official `com.unity.ai.assistant` MCP server (Editor-open) over
    third-party bridges; install + client-config are prepped for a human to apply
    (needs a running Editor) and are not committed.
  - Added an `AGENTS.md` rule to prefer first-party packages and avoid new
    third-party dependencies (skills/agents excepted).

- **Workflow tooling polish (Sprint 0 follow-up)** — 2026-06-26
  - Added the `workflow-review` cross-tool skill — periodically reviews the dev
    workflow itself (`AGENTS.md`, playbooks, toolchain) against first-party best
    practices and proposes changes for approval. Guardrails: untrusted web,
    propose-never-apply, high bar for change. Distinct from `session-self-eval`
    (compliance) — this one is evolution.
  - Removed the throwaway `example-cross-tool-check` reference entry (its role is
    covered by `docs/playbooks/README.md` + the real skills) and the now-distilled
    source `docs/agenticdev.md`, trimming per-session skill context.

- **Cross-tool authoring standard (Sprint 0, T0.1)** — 2026-06-24
  - Added `docs/playbooks/` with the canonical-playbook + thin-per-tool-wrapper
    standard (`README.md`) and a reference entry (`example-cross-tool-check`),
    proven with both wrappers: `.claude/skills/...` (Claude Code) and
    `.agents/skills/...` (Codex).
  - Corrected the `AGENTS.md` cross-tool section to the verified Codex **skills**
    path (`.agents/skills/`); the deprecated `~/.codex/prompts/` custom-prompt
    path is not used.

- **Cross-tool agent skills (Sprint 0: T0.2–T0.4, T0.6)** — 2026-06-24
  - `unity-monobehaviour-template` — generate a convention-compliant MonoBehaviour.
  - `unity-test-gen` — scaffold EditMode/PlayMode tests (InputTestFixture-aware).
  - `unity-research` / `unity-refactor` — structured codebase research; allowlist-scoped, diff-only refactors.
  - `session-self-eval` — audit a session against `AGENTS.md` + the backlog.
  - Each authored as a `docs/playbooks/` playbook with both `.claude/skills/` and
    `.agents/skills/` wrappers, per the T0.1 standard.

- **Model-tiered delegation rule** — 2026-06-24
  - `AGENTS.md` now directs agents to classify task difficulty and delegate
    mechanical work to smaller, cheaper models (Haiku / Sonnet) to save tokens.

- **Agentic workflow & tooling (Sprint 0) backlog** — 2026-06-24
  - Added `Sprint 0 — Agentic Workflow & Tooling` (`docs/sprints/sprint-0-best-practices.md`),
    reconciling `docs/agenticdev.md` recommendations with the existing AGENTS.md workflow.
  - Added cross-tool (Claude Code + Codex) skill/command authoring rules, a
    context-hygiene rule, and a pre-refactor git-safety rule to `AGENTS.md`.
  - Registered Sprint 0 in `docs/TICKETS.md`.

- **C# linter for gameplay code** — 2026-06-24
  - Self-contained `tools/lint/` project + `scripts/lint.{sh,ps1}`: Phase 1
    auto-fixes whitespace plus a safe allowlist of style fixes (explicit types,
    accessibility modifiers, `new()`, parentheses, braces, block bodies) —
    excluding any code-deleting fixes; Phase 2 reports code-quality via
    `dotnet build` with `Microsoft.Unity.Analyzers` + built-in analyzers, where
    `error`-severity findings block.
  - Root `.editorconfig` encoding the project's conventions (4-space, Allman,
    block namespaces, `camelCase` serialized fields kept) and severity tiers.
  - Opt-in `.githooks/pre-commit` hook (`git config core.hooksPath .githooks`).
  - Linting is scoped to `Assets/{Scripts,Editor,Tests}`; vendor code is excluded.

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

### Fixed

- **Input reader PlayMode initial-state callback** — 2026-06-25
  - Changed polled controls (`FrontBrake`, `RearBrake`, `Turn`, `Freelook`) to
    `PassThrough` actions in both the input asset and generated wrapper, avoiding
    the Input System's automatic `Value`-action initial-state callback path in
    PlayMode tests.
  - Updated PlayMode input tests to register keyboard, mouse, and gamepad
    devices under `InputTestFixture` before enabling the shipped action map, and
    enabled input actions explicitly in `PlayerInputReader`.

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
