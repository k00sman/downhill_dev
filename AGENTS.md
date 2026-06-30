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
- **Prefer first-party (Unity / official) packages; avoid adding new third-party code or dependencies** unless there is no official option (then flag it for sign-off). Agent skills/commands and external agents are exempt — they are tooling, not shipped game code. This governs *new* deps; existing vendor assets stay (and stay unmodified, per the rule above).
- **Before grepping or exploring `Assets/Scripts/` to understand the codebase, READ `docs/codemap.md` first.** It is a Mermaid class diagram of all gameplay types, their fields, public methods, and cross-type dependencies — faster than grepping. Regenerate it if it looks stale (see Workflow section for commands).
- **Context hygiene:** rely on `.gitignore` (both tools honor it) to keep `Library/`, `Logs/`, `obj/`, and build output out of context — do not add a tool-specific ignore file.

## Agent skills & commands (cross-tool)

This repo is driven by **both Claude Code and OpenAI Codex**. Anything reusable
(a skill, command, or template) is authored **once, tool-neutrally**, then
exposed to each tool through a thin wrapper:

- **Canonical playbook** — the real procedure/template lives in
  `docs/playbooks/<name>.md`, written in terms of actions (read a file, create a
  file, run a command), never a single tool's API.
- **Claude Code wrapper** — `.claude/skills/<name>/SKILL.md`, body = "Follow the playbook." Invoked as `/<name>`.
- **Codex wrapper** — `.agents/skills/<name>/SKILL.md` (the **same** `SKILL.md`
  format), body = "Follow the playbook." Invoked via `/skills`. Both wrappers
  are skills; Codex's deprecated `~/.codex/prompts/` custom prompts are not used
  (home-only, not repo-shared).
- `AGENTS.md` is the shared contract both tools read (`CLAUDE.md` only points to
  it). Put rules here, not in a tool-specific file. See `docs/playbooks/README.md`
  for the full standard.

Never add a Claude-only or Codex-only behavior the other tool can't follow.

## Model-tiered delegation (cost discipline)

Match the model to the task — don't burn a heavy reasoning model on mechanical
work. When delegating to a subagent, classify the task and pick the **cheapest
model that can do it well**:

- **Trivial / mechanical → smallest model** (Claude **Haiku**; Codex's small
  tier): boilerplate, file moves, mechanical or templated edits, running a
  documented command, single-file scaffolds from a clear template,
  grep-and-summarize.
- **Moderate → mid model** (Claude **Sonnet**): well-scoped work against a clear
  spec, tests from a known pattern, multi-file but bounded changes, authoring a
  playbook/skill once the pattern exists.
- **Complex / design / ambiguous → top model** (Claude **Opus**): architecture,
  brainstorming, cross-cutting refactors, anything with real design decisions or
  unclear requirements.

The orchestrator stays cheap by **handing mechanical subtasks down**, not doing
them itself. When torn between two tiers, pick the smaller and escalate only if
it actually struggles. Tool-neutral: each tool maps these tiers to its own model
menu (Claude Code sets a subagent's model; Codex selects per session).

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

## Unity MCP (optional, Editor-open)

An optional **official Unity MCP** server (`com.unity.ai.assistant`) lets agents
inspect a running Editor — scenes, GameObjects, console, screenshots. It is
**Editor-open only** — not a headless/CI path. The official package exposes no
*dedicated* test-runner tool, but `Unity_RunCommand` can compile and invoke
`TestRunnerApi` to run the EditMode suite and read results back from
`TestResults.xml` (used successfully this session — see Session learnings). For
PlayMode (it enters Play mode) and whenever MCP is unavailable, run tests from
the Unity Test Runner window (above). Three safety layers apply: Unity's per-client connection approval (Accept
under `Edit > Project Settings > AI > Unity MCP`), Unity's per-tool enable/disable
toggle (it ships with a command-runner and a mutator **on** — curate down to
read-only), and our per-call gating in `docs/playbooks/unity-mcp-allowlist.md`
(read/query/log tools are free; scene/asset/script/package mutation, code
generation, and the combined-verb `Manage*` tools need an explicit plan step +
confirmation each time). Setup is not committed by default — see
`docs/playbooks/unity-mcp-allowlist.md` and Ticket 0.5. Prefer this official
package over third-party Unity-MCP bridges.

## Workflow (specs / plans / tickets)

- Feature work follows **brainstorm → spec → plan → execute**.
- Prefer the Superpowers workflow for feature work and bug fixes. Use the
  relevant Superpowers skills when they apply, and keep generated specs/plans in
  `docs/superpowers/` so future agents can resume with context.
- Specs go in `docs/superpowers/specs/`; plans go in `docs/superpowers/plans/`.
- The roadmap lives in **`docs/TICKETS.md`** — the source of work. Phase order:
  1. Input & player scaffolding → 2. Bicycle locomotion → 3. Turning, braking & camera → 4. Jumping & crash basics → 5. Health & fail states → 6. Monster chase stub → 7. Readability & instrumentation → 8. Segments & run composition → 9. Audio & atmosphere → 10. Surface & terrain handling → 11. Run end & flow.
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
- Before a large or structural refactor, commit or stash first so there is a clean rollback point, and note this in the plan.

## Changelog — read and update it

- **At the start of any work session, READ `CHANGELOG.md`** to understand recent changes.
- **Whenever you make a notable change, UPDATE `CHANGELOG.md`** under the `## [Unreleased]` section, following the **Keep a Changelog** format (Added / Changed / Fixed / Removed).

## Session learnings

- For Ticket 1.3 bike locomotion, keep responsibilities split: `BikeMovementModel`
  owns scalar forward-speed math and controlled grounded velocity, while
  `PlayerBikeController` owns ground probing, contact stability, Rigidbody
  wiring, and visual-only bike body pitch.
- Keep the root Rigidbody rotation-frozen until Ticket 1.4 introduces an
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
- **Slope-driven steering math**: Banked/curved slope curve following is calculated by projecting the ground normal onto the bike's flat right vector (`slopeRoll = Vector3.Dot(groundNormal, flatRight)`). Adding this multiplied by a tunable `slopeInfluence` to the yaw delta (`slopeSteer = slopeRoll * slopeInfluence`) naturally steers the bike away from steeper terrain (uphill walls) towards lower ground.
- **Smoothing code-owned yaw**: Raw terrain-normal slope steering can visibly jitter when normals change frame-to-frame. Smooth the automatic slope/camber steering contribution before applying yaw, and apply code-owned Rigidbody yaw with `MoveRotation` while feeding the predicted rotation into same-tick movement math. Avoid direct `transform.rotation` assignment in `FixedUpdate` when Rigidbody interpolation is expected to smooth the visible bike.
- **Steering safety checks under extreme angles**: Governing steering availability based on the flat forward projection of velocity (`Vector3.Dot(velocity, flatForward)`) locks up steering when the bike faces uphill or turns sharply away from its sliding momentum. Use travel speed that includes sideways/backward motion (for current grounded steering, horizontal velocity magnitude) for steering minimum speed checks.
- **Option 3 ground-plane bike movement**: `BikeMovementModel` intentionally allows downhill/backward/lateral slip from slope force and damps sideways motion with `lateralGrip`; `BikeSteeringModel` aligns heading toward horizontal velocity or the downhill fall line at near-stop. Do not reintroduce forward-only/no-side-slip tests as a default invariant. Future surface handling should layer onto `lateralGrip`, drag, max speed, and steering alignment rather than adding a parallel tire simulation.
- **Brake-aware heading alignment**: Velocity/fall-line heading alignment helps
  drift and uphill recovery, but active front braking should not implicitly
  steer or "center" the bike. Pass front-brake state into the steering model and
  suppress only automatic heading alignment while preserving manual steering and
  slope/camber steering.
- **Pedal camera feedback**: Keep pedal view bob as camera-only feedback, not
  bike physics or bike-rig movement. Drive it from normalized/published pedal
  power through a pure tunable model, and prefer applying it through the actual
  `Camera` lens shift rather than offsetting a world transform parented under
  the bike. Coasting downhill should not bob.
- **Continuous ground contact on slopes**: On steep slope gradients or uphill climbs, the offset front ground probe sits higher. Ensure ground probe raycast distances are set deep enough (e.g., `1.2m` instead of `0.6m`) to prevent false "airborne" states that disable turning controls.
- **URP Post-Processing & IMGUI conflicts**: Custom stylized blit passes (such as retro CRT/pixel-art shader scripts) can completely clear or obscure immediate-mode `OnGUI` graphics. For robust prototype HUDs, construct a `ScreenSpaceOverlay` Canvas with a high sorting order (`9999`) to guarantee UI is rendered on top of post-processing layers.
- **External Configuration Files**: Expose player variables (like mouse/freelook `lookSensitivity`) in an external config file (e.g., `config.json` in the project root) that auto-creates itself with safe defaults on first launch. This allows testers to adjust handling without requiring Editor access.
- **Safe Object Lookups**: Do not use `GameObject.FindWithTag` on tags that are planned but not yet officially created in Editor settings (e.g., `"Monster"`). It throws blocking exceptions that crash the script's update loop. Use safe name-based searches (`GameObject.Find`) until the tag is defined.
- **Pedal cadence isolation**: Keep alternating-pedal timing in the pure
  `PedalInputEvaluator` and let `PlayerBikeController` only translate input
  events into cadence drive for the existing `PedalPower` accumulator. Trigger
  depth remains ignored for now; the mechanic is timing-only unless a future
  pressure-modulation ticket explicitly changes it.
- **Input binding normalization**: When changing `DownhillControls.inputactions`,
  update the generated `DownhillControls.cs` JSON too, then cover both the asset
  and generated wrapper with binding assertions so runtime polling and checked-in
  code cannot drift.
- **Generated InputAction wrapper cleanup in EditMode tests**: Do not wrap
  `DownhillControls` in `using` from EditMode tests. Its generated `Dispose()`
  calls `UnityEngine.Object.Destroy`, which logs EditMode errors. Destroy the
  generated in-memory asset with `UnityEngine.Object.DestroyImmediate` in a
  `finally` block instead.
- **Project-wide Input System actions in PlayMode tests**: Unity 6 project-wide
  actions (`InputSystem.actions`, configured in `EditorBuildSettings.asset`) are
  enabled during PlayMode test setup and may include default `Value` actions
  that schedule initial-state callbacks. Physics tests that inherit
  `InputTestFixture` but do not exercise project-wide actions should disable
  them in `Setup()` with an explicit Unity-object null check before spawning
  gameplay prefabs. This applies to **any** `InputTestFixture` fixture that does
  not itself enable those actions — **not just physics tests**. Device-polling
  input tests qualify too: `PlayerInputReaderTests` polls controls via
  `InputSystem.FindControls` and never enables the actions, so it hit the same
  `ArgumentNullException: statePtr` in `InputActionState.OnBeforeInitialUpdate`
  (symptom surfaced on `FrontBrake_ReflectsLeftShoulder`). Mirror the
  `InputSystem.actions?.Disable()` guard into every such fixture's `Setup()`.
- **Tuning-only number changes**: Do not add new tests just to lock numeric
  tuning changes for existing behavior (for example bike speed, acceleration, or
  scalar feel tweaks). Update existing tests if their expectations need to follow
  the new tuning. Add new tests only when the tuning change introduces or fixes
  a behavior rule. (The Ticket 2.1 low-speed pedal boost added tests because it
  introduced a *new rule* — speed-dependent acceleration that fades above a
  threshold — not merely a scalar tweak.)
- **Running EditMode tests via Unity MCP**: With the Editor open and MCP
  connected, `Unity_RunCommand` can compile and invoke `TestRunnerApi` to run
  the EditMode suite, writing NUnit results to
  `~/.config/unity3d/<Company>/<Project>/TestResults.xml` (Linux) to read back.
  Confirmed this session (16/16 EditMode). PlayMode enters Play mode and
  disrupts a live Editor, so prefer asking the user for those; never claim a
  pass you did not actually observe in the results file.
- **New serialized fields on embedded `[Serializable]` models**: Adding a public
  field to a pure model like `BikeMovementModel` (held as a `[SerializeField]`
  on a prefab/scene component, e.g. `PFB_Player.prefab`) does **not** write into
  the existing prefab/scene YAML until that asset is re-opened and saved in the
  Editor. At runtime Unity backfills the missing field from the C# initializer,
  so play uses the right value — but the prefab Inspector/YAML won't reflect it
  until re-saved. After adding a tunable, verify it in the Inspector (and save
  the prefab if you want the value overridable there).

## Game design summary

- **Pillars:** momentum is survival; fear from pursuit (not jump scares); readability over realism; trail reading over tricks; stylized look supports gameplay.
- **Bike handling (the key prototype risk):** separate left/right pedaling for drive; separate front/rear brakes; speed-sensitive steering (unstable at high speed, death wobble); front-brake-at-speed throws the rider; landings briefly destabilize handling.
- **Health:** hidden internal value (no UI). Monster contact = instant death. Collisions damage by impact/speed. Regen starts after 5s without damage; 0 health = death.
- **Crashes:** severe = instant death; otherwise throw rider into a recoverable state (placeholder QTE/timed recover) that lets the monster close in. Death → fast restart.
- **Monster:** a pressure system, not a character. No real pathfinding (can float). Loosely mirrors player pace but **lags** acceleration/deceleration, with min/max speed caps — that lag is what lets it catch up when you slow or crash.
- **Levels:** modular downhill segments, shuffled with no repeat per run; all slope downward; readable trails, telegraphed hazards, reflective markers that catch the headlamp.
