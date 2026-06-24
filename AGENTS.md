# AGENTS.md — Downhill

Downhill is a stylized downhill mountain-biking **horror** prototype: the player descends a dark, dangerous trail under a pursuing monster. Survival comes from momentum — ride too slow and the monster catches up, ride too aggressively and a crash kills you. This repo is a **gameplay prototype** whose goal is to validate that the bike handling feels tense and readable, that the chase creates real pressure, and that modular trail segments stitch into replayable runs. Favor simple, tunable, testable solutions over production architecture.

## Maintainers & platforms

This repo is worked on by multiple people across **Linux, macOS, and Windows**. Keep tooling cross-platform:
- Shell scripts live in `scripts/` — always provide a `.sh` (bash) and a `.ps1` (PowerShell) variant side by side.
- The Unity editor itself handles Linux/macOS/Windows transparently.
- `dotnet` (10.x) must be available on the developer's machine to run the codemap tool; it is not bundled.

## Tech stack & engine

- **Unity 6000.3.17f1** (Unity 6.3), **URP 17.3.0**. Platforms: **Linux**, **Windows**, *mMacOS**.
- Render look: stylized **PS1/PS2-era** retro 3D (Retro Shaders Pro asset). This is a deliberate feature — it supports horror atmosphere and trail readability.
- **Input System** `com.unity.inputsystem` 1.19.0. Active Input Handling = **"Both"** (legacy is intentionally left on so third-party demo scripts keep working — do not switch to Input-System-only).

## Project structure & conventions

- New gameplay code lives in focused **assembly definitions** (e.g. `Downhill.Input` asmdef at `Assets/Scripts/Input/`). Keep risky mechanics isolated in their own files so they can be replaced.
- **`.meta` files MUST be committed** alongside their asset/script. Never leave a new file without its meta.
- **Third-party content under `Assets/` is off-limits** — do not modify it: PathCreator, Retro Shaders Pro, Adrift Team, and other vendor folders.
- **Before grepping or exploring `Assets/Scripts/` to understand the codebase, READ `docs/codemap.md` first.** It is a Mermaid class diagram of all gameplay types, their fields, public methods, and cross-type dependencies — faster than grepping. Regenerate it if it looks stale (see Workflow section for commands).

## Input

- Bindings live in `Assets/Scripts/Input/DownhillControls.inputactions` — **"Bike"** action map; codegen wrapper class `Downhill.Input.DownhillControls`.
- Gameplay reads input **only** through `Assets/Scripts/Input/PlayerInputReader.cs`. **Never do raw key checks** in gameplay code.
- Stable action names (prefer these over rebinding): `PedalLeft`, `PedalRight`, `FrontBrake`, `RearBrake`, `Turn`, `Jump`, `Freelook`.
- Both keyboard/mouse and controller mappings exist.

## Testing & verification

- Tests live under `Assets/Tests/EditMode` and `Assets/Tests/PlayMode`, each with **its own asmdef**. PlayMode input tests use `InputTestFixture`.
- The Unity editor generally **cannot run headless here**, so you usually **cannot run the tests yourself**. **Ask the user to run the Unity Test Runner** (EditMode + PlayMode) and report results — never claim tests passed when you didn't run them.
- "Done" = typecheck + tests pass. For gameplay features, also confirm against the ticket's acceptance criteria.

## Linting

- Lint gameplay C# (`Assets/Scripts`, `Assets/Editor`, `Assets/Tests`) with the
  self-contained project in `tools/lint/`. Vendor code under `Assets/` is never
  linted (the project's compile globs *are* the scope). Requires the project to
  have been compiled in Unity once (for `Library/ScriptAssemblies` + a generated
  `.csproj` to harvest the engine path from).
  - Linux / macOS: `./scripts/lint.sh` — auto-fixes formatting, then reports
    code-quality issues. `./scripts/lint.sh --check` verifies without writing.
  - Windows (PowerShell): `pwsh scripts/lint.ps1` (`--check` to verify).
- **Two phases:**
  - **Phase 1 (auto-fix):** `dotnet format whitespace` + `dotnet format style`,
    where `style` is restricted to a **safe allowlist** of diagnostic IDs
    (explicit types, accessibility modifiers, `new()`, parentheses, braces,
    block bodies, inlined out-vars, index operators). It runs in a small
    convergence loop because some fixes are interdependent. It **never** runs the
    member-deleting fixes (IDE0051/IDE0052) — those once stripped real WIP code.
    `IDE0032` (auto-property) and the `UNT*` Unity-perf rules are deliberately
    *excluded* from auto-fix (serialization / behavior risk) and only reported.
  - **Phase 2 (report + block):** `dotnet build` with analyzers
    (`Microsoft.Unity.Analyzers` + built-in). Findings are reported, never
    auto-applied; `error`-severity ones fail the build.
- Rules and severities live in the root `.editorconfig`. Suppress a single line
  with `#pragma warning disable <ID>` / `restore`, or change a rule's severity in
  `.editorconfig`. Public serialized fields stay `camelCase` (Unity convention)
  by design.
- Optional pre-commit hook: `git config core.hooksPath .githooks`. It auto-fixes
  (whitespace) staged in-scope files, re-stages them, and blocks commits with
  `error`-tier findings (bypass with `git commit --no-verify`).

## Workflow (specs / plans / tickets)

- Feature work follows **brainstorm → spec → plan → execute**.
- Prefer the Superpowers workflow for feature work and bug fixes. Use the
  relevant Superpowers skills when they apply, and keep generated specs/plans in
  `docs/superpowers/` so future agents can resume with context.
- Specs go in `docs/superpowers/specs/`; plans go in `docs/superpowers/plans/`.
- The roadmap lives in **`docs/TICKETS.md`** — the source of work. Phase order:
  1. Input & player scaffolding → 2. Bicycle locomotion → 3. Turning, braking & camera → 4. Jumping & crash basics → 5. Health & fail states → 6. Monster chase stub → 7. Readability & instrumentation.
- Sprint files live in `docs/sprints/` — one file per sprint, each containing its tickets in full. **When starting a sprint, read the sprint file.** When a sprint's exit criteria are met, mark it complete at the top of the sprint file.
- **Definition of done** (per ticket): feature works in the existing level; touched files match planned scope; acceptance criteria met; debug output is enough to tune the feature; no unrelated systems introduced silently.
- After completing every ticket, review `AGENTS.md` and append useful lessons
  from the session: development patterns, Unity/Test Runner pitfalls,
  workflow gotchas, and decisions future agents should preserve.
- After completing every ticket, review `docs/TICKETS.md` and update later
  tickets with any discoveries, deferred scope, or acceptance criteria that
  future agents will need to know.
- **After completing a ticket (or any meaningful set of code changes), regenerate the code map.** The tool lives in `tools/codemap/` and uses Roslyn — no Unity Editor required. Run from the repo root:
  - Linux / macOS: `bash scripts/generate-codemap.sh`
  - Windows (PowerShell): `pwsh scripts/generate-codemap.ps1`
  - Any platform (fallback): `dotnet run --project tools/codemap/codemap.csproj -- <repo-root>`

## Changelog — read and update it

- **At the start of any work session, READ `CHANGELOG.md`** to understand recent changes.
- **Whenever you make a notable change, UPDATE `CHANGELOG.md`** under the `## [Unreleased]` section, following the **Keep a Changelog** format (Added / Changed / Fixed / Removed).

## Session learnings

- For Ticket 2.1 bike locomotion, keep responsibilities split: `BikeMovementModel`
  owns scalar forward-speed math and controlled grounded velocity, while
  `PlayerBikeController` owns ground probing, contact stability, Rigidbody
  wiring, and visual-only bike body pitch.
- Keep the root Rigidbody rotation-frozen until Ticket 3.1 introduces an
  explicit steering/yaw model. Terrain-driven left/right curve following should
  not be smuggled into the contact model, because normals alone can create noisy
  implicit turns on bumps, props, or banked surfaces.
- When improving bike contact, avoid blind parameter tuning. Add focused
  EditMode tests for pure movement math, PlayMode tests for grounded behavior,
  and ask the user to verify Unity Test Runner results and playtest metrics in
  the Editor.
- Treat the root `BoxCollider` as a compact contact proxy, not the final crash
  volume. If hazard collision feels too forgiving, add a separate crash/hazard
  trigger later instead of enlarging the contact proxy.
- **Linting is "done" only after reading the linter's output, not on a green
  build.** `./scripts/lint.sh` exiting 0 with warnings is not clean — enumerate
  warnings (`... | grep -oE 'warning [A-Z]+[0-9]+' | sort | uniq -c`), decide
  each consciously, and surface the whole set at once instead of fixing them one
  round at a time.
- **NEVER run `dotnet format style` over the full rule set in apply mode.** Its
  IDE0051/IDE0052 ("unused member") code-fixes *delete* members — it silently
  stripped real WIP lifecycle/wiring methods out of `PlayerInputReader` and
  `PlayerBikeController`. Only apply the scoped allowlist baked into
  `scripts/lint.*`, and review `git diff` (and `git diff -w`) after any format
  run before treating it as done. UNT* perf rules change behavior — report, don't
  auto-fix.
- Validate the exact mode you ship: test `apply` (not just `--verify`), confirm
  idempotency (`./scripts/lint.sh` then `--check` from a clean tree), and read
  the diff for any file-mutating step.

## Game design summary

- **Pillars:** momentum is survival; fear from pursuit (not jump scares); readability over realism; trail reading over tricks; stylized look supports gameplay.
- **Bike handling (the key prototype risk):** separate left/right pedaling for drive; separate front/rear brakes; speed-sensitive steering (unstable at high speed, death wobble); front-brake-at-speed throws the rider; landings briefly destabilize handling.
- **Health:** hidden internal value (no UI). Monster contact = instant death. Collisions damage by impact/speed. Regen starts after 5s without damage; 0 health = death.
- **Crashes:** severe = instant death; otherwise throw rider into a recoverable state (placeholder QTE/timed recover) that lets the monster close in. Death → fast restart.
- **Monster:** a pressure system, not a character. No real pathfinding (can float). Loosely mirrors player pace but **lags** acceleration/deceleration, with min/max speed caps — that lag is what lets it catch up when you slow or crash.
- **Levels:** modular downhill segments, shuffled with no repeat per run; all slope downward; readable trails, telegraphed hazards, reflective markers that catch the headlamp.
