# Unity C# Linter — Design Spec

**Date:** 2026-06-23
**Topic:** Code-quality + formatting linter for gameplay C#
**Status:** Approved (brainstorm complete) — ready for implementation plan.

## Goal

Give the Downhill repo a linter that does two jobs for **gameplay C# only**:

1. **Code quality** — catch null-ref patterns, unused members, dead code,
   empty Unity message methods, and other suspicious patterns.
2. **Formatting / style consistency** — indentation, brace style, naming, and
   `using` ordering, codified once so the whole team stays consistent.

It must be cross-platform (Linux/macOS/Windows), runnable without opening the
Unity Editor, and fit the existing tooling shape (a `dotnet`-based tool driven
by paired `scripts/*.sh` + `scripts/*.ps1`, exactly like `tools/codemap/`).

## Non-goals

- No CI gate and no in-Editor (Unity console) integration in this iteration.
  Run modes are **CLI** and a **pre-commit hook** only.
- No custom hand-written analyzers. We rely on off-the-shelf rules now; a custom
  rule can be slotted in later if a project-specific pattern emerges.
- The linter does **not** touch third-party / vendor code under `Assets/`.

## Approach (decided)

**Approach A — standard .NET stack.** Chosen over a custom Roslyn tool because it
gives battle-tested, Unity-aware rules with effectively zero rule-maintenance:

- **`Microsoft.Unity.Analyzers` (v1.26.0)** — Unity-specific Roslyn analyzers.
  Critical because Unity changes what "correct" means: `== null` on a destroyed
  `UnityEngine.Object`, empty `Start()`/`Update()` messages, `GetComponent` in
  `Update`, etc. It also *suppresses* generic rules that misfire in Unity (e.g.
  "serialized field never assigned" — Unity assigns those via the Inspector).
- **Built-in .NET analyzers** (`CAxxxx` quality, `IDExxxx` style) — unused vars,
  dead code, redundant code.
- **`dotnet format` + `.editorconfig`** — formatting, style, naming, and
  analyzer severities, all from one config file. Has an auto-fix mode and a
  verify-only (`--verify-no-changes`) check mode, plus `--include` / `--exclude`
  globs for scoping.

### Why not the alternatives
- **B (custom Roslyn tool like codemap):** high effort, we'd re-implement and
  maintain every rule, and codemap is syntactic-only — it can't do the semantic
  null analysis that is the whole point. Rejected.
- **C (hybrid: A + custom rules):** kept as the future escape hatch, not built
  now (YAGNI).

### Important nuance — semantic analysis needs `UnityEngine` (mechanism, validated by spike)
The high-value Unity rules are **semantic**: they need `UnityEngine` (and for
the editor file, `UnityEditor`) referenced to fire. A feasibility spike
(2026-06-23) established two things:

1. **Unity's generated `.csproj` cannot be used directly.** They go stale (e.g.
   `PathCreator.csproj` references source deleted when a vendor package was
   swapped) and dragging in their transitive `ProjectReference` graph fails the
   build for reasons unrelated to gameplay code. So the original idea of running
   `dotnet format`/`dotnet build` against the Unity csproj is **rejected**.
2. **A self-contained lint project works.** A single SDK-style project
   (`tools/lint/Downhill.Lint.csproj`) that pulls gameplay source via
   `<Compile>` globs and references DLLs directly — the engine managed dir +
   `Library/ScriptAssemblies/*.dll` + NUnit from `Library/PackageCache` — has no
   `ProjectReference` graph to break. The spike compiled the **entire** in-scope
   surface (Scripts + Editor + Tests) with **0 errors** and surfaced 41 analyzer
   warnings on real code. Unity-specific (`UNT*`), `IDE*`, and `CS*` diagnostics
   all fired, and `.editorconfig` severity correctly promoted them to
   build-blocking errors.

Only **one** input is machine-specific: the Unity **engine managed dir** (e.g.
`<UnityHub>/Editor/6000.3.17f1/Editor/Data/Managed/UnityEngine/`). It is
harvested at lint time from any existing Unity `.csproj` `HintPath` and written
to a git-ignored `tools/lint/Local.props`. Everything else is repo-relative and
portable (compile globs, `Library/ScriptAssemblies`, the NUnit `**` glob).

**Prerequisite:** Unity must have compiled the project at least once, so
`Library/ScriptAssemblies/*.dll` exists and at least one `.csproj` exists to
harvest the engine path from. This is a normal side effect of opening the
project in Unity.

## Scope (which code is linted)

**In scope** (gameplay assemblies):
- `Assets/Scripts/` — `Downhill.Input`, `Downhill.Player`
- `Assets/Editor/`
- `Assets/Tests/EditMode`, `Assets/Tests/PlayMode`
  (`Downhill.Tests.EditMode`, `Downhill.Tests.PlayMode`)

**Out of scope** (never modified — per AGENTS.md "third-party content under
`Assets/` is off-limits"):
- `Assets/Retro Shaders Pro/`, `Assets/Adrift Team/`, `Assets/TutorialInfo/`,
  `Assets/Fonts/`, PathCreator, and any other vendor folder.
- `Library/`, `Logs/`, `obj/`, build output.
- `tools/codemap/` is a separate dotnet tool and is not in linter scope.

**Scoping is structural, not glob-based.** The lint project's `<Compile>` globs
include *only* `Assets/Scripts/`, `Assets/Editor/`, and `Assets/Tests/`. Vendor
folders live elsewhere (`Assets/Retro Shaders Pro/`, etc.) and are simply never
compiled, so they can be neither reported on nor reformatted. The project
definition *is* the scope — no `--include`/`--exclude` or vendor opt-out configs
needed. (`Assets/Editor/` contains only the gameplay `MeshImportSettings.cs`;
all vendor editor code lives under vendor folders.)

## Components

| Component | Path | Purpose |
|---|---|---|
| Root config | `.editorconfig` | Single source of truth: formatting, style, naming, severities |
| Lint project | `tools/lint/Downhill.Lint.csproj` | Self-contained SDK project: compiles in-scope source, references engine + package DLLs, pulls in `Microsoft.Unity.Analyzers` |
| Machine props | `tools/lint/Local.props` (git-ignored) | Holds `<UnityManagedDir>`, regenerated each run by the lint script |
| CLI (bash) | `scripts/lint.sh` | Entry point, Linux/macOS: harvest path → format → build |
| CLI (pwsh) | `scripts/lint.ps1` | Entry point, Windows (mirror of `.sh`) |
| Hook | `.githooks/pre-commit` | Auto-fix staged files, re-stage, block on quality errors |
| gitignore | `.gitignore` | Ignore `tools/lint/Local.props` + `tools/lint/{obj,bin}` |
| Docs | AGENTS.md + `README.md` note | How to run, how to suppress a rule, how to enable the hook |

Note: `.gitignore` already un-ignores `tools/**/*.csproj`, so the lint project is
committed despite the global `*.csproj` ignore.

## Behavior — two phases

**Phase 1 — Format (auto-fix always).**
`dotnet format whitespace` + a **scoped** `dotnet format style` (allowlisted
diagnostic IDs only) on the lint project, writing fixes in place to the real
source files (the project compiles them via globs, so only in-scope files are
touched). Covers indentation, brace style, spacing, newlines, plus the safe
style fixes: explicit types (IDE0008), accessibility modifiers (IDE0040),
target-typed `new()` (IDE0090), clarifying parentheses (IDE0048), braces
(IDE0011), block bodies (IDE0022), inlined out-vars (IDE0018), index operators
(IDE0056). Runs in a bounded convergence loop because some fixes are
interdependent (IDE0008 must precede IDE0090 on `var x = new T()`).

> **Correction (2026-06-24, during implementation):** an earlier draft ran the
> *whole* `dotnet format style` rule set in apply mode. That is **unsafe** —
> `style` applies analyzer *code-fixes*, and the fix for IDE0051 ("unused
> member") **deletes the member**. In testing it stripped real WIP
> lifecycle/wiring methods out of `PlayerInputReader` and `PlayerBikeController`.
> The fix: `style` is run only for an explicit safe allowlist that excludes
> IDE0051/IDE0052 (deletion) and IDE0032 (auto-property — Unity serialization
> risk). Behavior-changing Unity-perf rules (UNT0022/0026/0039) are **reported,
> not auto-fixed**. Everything not in the allowlist is report-only via Phase 2.

**Phase 2 — Quality (report + block).**
A `dotnet build` of the lint project with `EnforceCodeStyleInBuild=true` and the
analyzers active. Analyzers
run as part of compilation, so this surfaces **all** `Microsoft.Unity.Analyzers`
+ `CA`/`IDE` findings — including the majority that have no auto-fixer.
`.editorconfig` severity decides the outcome: rules set to `error` become build
errors (non-zero exit, blocks the hook); rules set to `warning` are reported but
don't block. Findings are **not** auto-fixed.

> Note: `dotnet format analyzers` only applies/verifies diagnostics that ship a
> code fix, so it cannot be the reporting mechanism for the non-fixable quality
> rules — hence Phase 2 uses `dotnet build`, not `dotnet format analyzers`. The
> implementation plan resolves the exact invocation (e.g. capturing diagnostics
> and filtering to in-scope files).

This split is the honest reading of the "auto-fix always" preference: formatting
is always fixed; code-quality problems can only be reported.

## Run modes

| Invocation | Behavior |
|---|---|
| `scripts/lint.sh` | Phase 1 auto-fix, then Phase 2 build+report. Default dev workflow. |
| `scripts/lint.sh --check` | Phase 1 `--verify-no-changes` + Phase 2 build; non-zero on needed-changes or quality errors. |
| pre-commit hook | Run Phase 1 `--fix` on **staged** gameplay files, **re-stage** what changed, run Phase 2, **block** commit if quality errors remain. Bypassable with `git commit --no-verify`. |

The hook is **opt-in**: a developer enables it with
`git config core.hooksPath .githooks` (documented). It is not force-installed, to
respect a mixed-OS team and avoid surprising contributors.

## Style/naming conventions to encode

Derived from the existing gameplay code so the config matches reality instead of
fighting it:

- `indent_style = space`, `indent_size = 4`.
- **Allman braces** (`csharp_new_line_before_open_brace = all`).
- **Block-scoped namespaces** (`csharp_style_namespace_declarations = block_scoped`).
- Naming: types & methods `PascalCase`; locals & parameters `camelCase`.
- **Public serialized fields stay `camelCase`** (Unity convention, e.g.
  `maxSpeed`). The default .NET "public members are PascalCase" rule is
  **disabled** so it doesn't flag the whole codebase.
- Brace-requirement on single-line statements is **off / suggestion** — the code
  intentionally uses brace-less `if (...) return ...;`.
- `using` directives sorted, `System.*` first.

## Severity policy (default tiers)

- **Error (blocks the hook):** Unity null-comparison misuse, unused private
  members, unreachable / dead code, empty Unity message methods, obvious
  null-deref patterns.
- **Warning (reported, non-blocking):** style nits, naming drift, minor CA
  suggestions.
- **Disabled:** rules that misfire in Unity — serialized-field-never-assigned,
  public-field-should-be-PascalCase, brace-requirement on single statements.

Tiers are adjustable in `.editorconfig`; this is the starting baseline.

## Error handling / edge cases

- **No `.csproj` present** (project never opened in Unity): the script detects
  the missing files and prints a clear message ("open the project in Unity once
  to generate the .csproj files") and exits non-zero.
- **`dotnet` missing:** same friendly failure path as `codemap`.
- **First run** needs one network `dotnet restore` to fetch the analyzer
  package; documented as a prerequisite.
- **Hook on a partial stage:** only staged hunks are auto-fixed and re-staged;
  unstaged changes in the same file are left untouched.

## Verification ("done")

Per AGENTS.md, "done" = typecheck + tests pass. For this tool specifically:

1. After an initial baseline auto-fix pass, `scripts/lint.sh --check` runs clean
   (no needed changes, no error-severity findings) on current gameplay code.
2. A deliberately-broken sample (an unused local + a Unity `== null` misuse) is
   **caught** by Phase 2 and **blocks** `--check`.
3. `scripts/lint.sh` works on Linux; `scripts/lint.ps1` mirrors it.
4. The pre-commit hook auto-fixes a badly-formatted staged file, re-stages it,
   and blocks a commit that introduces a quality error.
5. Formatting changes shouldn't alter behavior, but per AGENTS.md the user is
   asked to run the Unity Test Runner (EditMode + PlayMode) after the baseline
   format pass and confirm green.

## Open items deferred (not in this iteration)

- CI gate (would need machine-readable SARIF/JSON output + exit-code contract).
- In-Editor live diagnostics.
- Project-specific custom analyzers (Approach C escape hatch).
