# Agentic Workflow & Tooling Sprint — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the approved design spec into the actual docs: a house-format `Sprint 0 — Agentic Workflow & Tooling`, the cross-tool/context-hygiene/git-safety rules folded into `AGENTS.md`, and Sprint 0 registered in `docs/TICKETS.md` + `CHANGELOG.md`.

**Architecture:** Pure documentation change. It transcribes the gap-analysis result from `docs/superpowers/specs/2026-06-24-agentic-workflow-sprint-design.md` into four files. No code, no assets, no build. The Sprint 0 *tickets* (T0.1–T0.6) describe future work and are authored here but **not executed** by this plan.

**Tech Stack:** Markdown only. Verification is link/heading/consistency checks (grep, ls), not unit tests.

## Global Constraints

- **Source of truth:** `docs/superpowers/specs/2026-06-24-agentic-workflow-sprint-design.md` (committed on this branch). Content below is the assembled final state; where it differs from the spec, the difference is intentional and noted (linter status is now "in progress," not "spec done").
- **Cross-tool:** every reusable artifact must run under **both Claude Code and OpenAI Codex**. `AGENTS.md` is the shared contract; `CLAUDE.md` is only a pointer.
- **House ticket format:** match `docs/sprints/sprint-1-core-ride.md` — `Status / Goal / Dependencies / Files / Research task / Subagent tasks / Acceptance criteria / Notes`. The "Research task" line is **tool-neutral** ("Research task:", not "for Opus") to fit the cross-tool theme.
- **Sprint file location:** keep at `docs/sprints/sprint-0-best-practices.md` (preserves the existing reference in `docs/agenticdev.md`).
- **Do NOT touch:** gameplay sprints 1–6, the gameplay phase order, or any in-flight linter files — `tools/lint/`, `scripts/lint.{sh,ps1}`, `.githooks/`, `.editorconfig`, `.gitignore`, `docs/superpowers/plans/2026-06-24-unity-csharp-linter.md`, `docs/superpowers/specs/2026-06-23-unity-csharp-linter-design.md`.
- **No code changed → no codemap regeneration** required for this plan.
- **Commits require explicit user confirmation** (user's global rule), even though commit steps appear below. Confirm before running them.

---

### Task 1: Rewrite `docs/sprints/sprint-0-best-practices.md` into house-format Sprint 0

**Files:**
- Modify (full rewrite): `docs/sprints/sprint-0-best-practices.md`

**Cross-references:**
- Produces: the sprint file that `docs/TICKETS.md` (Task 3) links to at path `sprints/sprint-0-best-practices.md`.
- Consumes: the linter spec/plan paths (must exist — they do, untracked) and the design spec path.

- [ ] **Step 1: Overwrite the file with the exact content below**

````markdown
# Sprint 0 — Agentic Workflow & Tooling

**Goal question:** Are our agent workflows fast, safe, and cross-tool (Claude Code + Codex)?

**Sprint status:** In progress

> Meta/tooling sprint — **not** part of the gameplay phase order. Derived from
> `docs/agenticdev.md` via the gap analysis in
> `docs/superpowers/specs/2026-06-24-agentic-workflow-sprint-design.md`. Items the
> repo already satisfies (the `CLAUDE.md`→`AGENTS.md` contract, brainstorm→spec→
> plan→execute, the small-patch definition of done) are intentionally **not**
> re-ticketed here.

---

### Ticket 0.1 - Cross-tool skill/command authoring standard

**Status:** Not started.

**Goal:** Define and prove one way to author a skill/command once and run it under both Claude Code and OpenAI Codex.

**Dependencies:** None. Gates Tickets 0.2, 0.3, 0.4, and 0.6.

**Files:**
- `docs/playbooks/README.md` — the pattern + per-tool wrapper conventions (new)
- `docs/playbooks/<example>.md` — one reference playbook (new)
- `.claude/skills/<example>/SKILL.md` — Claude Code wrapper (new)
- Codex project custom-prompt wrapper — exact path from research
- `AGENTS.md` — "Agent skills & commands (cross-tool)" section (already added in this sprint's setup)

**Research task:** Confirm Codex CLI's project-level custom-prompt mechanism and path, and confirm Claude Code resolves a skill whose body defers to an external markdown playbook. Output the minimal wrapper boilerplate for each tool.

**Subagent tasks:**
- Research subagent: determine the two wrapper mechanisms and their exact file locations.
- Coding subagent A: write `docs/playbooks/README.md` + the reference playbook.
- Coding subagent B: add both wrappers; verify each tool resolves the same playbook.

**Acceptance criteria:**
- `docs/playbooks/README.md` documents the canonical-playbook + per-tool-wrapper pattern.
- One reference playbook exists with both wrappers and can be invoked from **both** Claude Code and Codex, each resolving the same file.
- `AGENTS.md` has the cross-tool authoring section.
- No tool-specific behavior is introduced that the other tool cannot follow.

**Notes:**
- Keep wrappers thin — all logic lives in the playbook, never duplicated.

---

### Ticket 0.2 - Unity MonoBehaviour template skill

**Status:** Not started.

**Goal:** Given a script name and responsibility, generate a convention-compliant MonoBehaviour.

**Dependencies:** Ticket 0.1.

**Files:**
- `docs/playbooks/unity-monobehaviour-template.md` (new)
- `.claude/skills/unity-monobehaviour-template/SKILL.md` + Codex wrapper (new)

**Research task:** Extract the concrete conventions already used in `Assets/Scripts/` (serialized-field grouping, naming, asmdef placement) so the template matches reality instead of fighting it.

**Subagent tasks:**
- Research subagent: read a representative gameplay script and the linter `.editorconfig`; list the conventions to encode.
- Coding subagent A: write the playbook (the template + the questions it asks).
- Coding subagent B: add both wrappers.

**Acceptance criteria:**
- Invoking the skill with a description (e.g. "enemy patrol AI") produces a MonoBehaviour using `[SerializeField] private` fields grouped at the top, Allman braces, camelCase serialized fields, and the correct asmdef/folder.
- The generated file is paired with a reminder to commit its `.meta`.
- Output avoids heavy logic in `Update()`; suggests events/coroutines where apt.

**Notes:**
- Generator skill, not game code — touches no existing gameplay.

---

### Ticket 0.3 - Unity test-generation skill

**Status:** Not started.

**Goal:** Scaffold EditMode/PlayMode tests for a target class or behavior.

**Dependencies:** Ticket 0.1.

**Files:**
- `docs/playbooks/unity-test-gen.md` (new)
- `.claude/skills/unity-test-gen/SKILL.md` + Codex wrapper (new)

**Research task:** Capture the repo's test conventions — `Assets/Tests/EditMode` and `Assets/Tests/PlayMode`, each with its own asmdef; PlayMode input tests use `InputTestFixture`.

**Subagent tasks:**
- Research subagent: read both test asmdefs and one existing test; list the scaffolding rules.
- Coding subagent A: write the playbook.
- Coding subagent B: add both wrappers.

**Acceptance criteria:**
- Given a target class + expected behavior, the skill produces a compiling test in the correct assembly (EditMode for pure math, PlayMode for runtime).
- PlayMode input tests are scaffolded with `InputTestFixture`.
- Output reminds the user to run the Unity Test Runner (agents can't run it here, per `AGENTS.md`).

---

### Ticket 0.4 - Research + Refactor agent commands

**Status:** Not started.

**Goal:** Formalize the existing orchestration prompts into two reusable cross-tool commands.

**Dependencies:** Ticket 0.1.

**Files:**
- `docs/playbooks/unity-research.md`, `docs/playbooks/unity-refactor.md` (new)
- `.claude/commands/unity-research.md`, `.claude/commands/unity-refactor.md` + Codex wrappers (new)

**Research task:** Lift the "Research subagent" / "Coding subagent" prompts from `docs/TICKETS.md` ("Orchestration prompts" section) and adapt them; define the refactor command's file-allowlist contract.

**Subagent tasks:**
- Research subagent: extract and adapt the existing prompts.
- Coding subagent A: write both playbooks (research summary format; refactor allowlist + diff-only output).
- Coding subagent B: add the Claude commands + Codex wrappers.

**Acceptance criteria:**
- The research command takes a target (feature/folder/script) and returns a structured summary (files, responsibilities, key patterns), not raw dumps.
- The refactor command operates **only** on an explicit file allowlist and returns diffs, respecting Unity patterns (no MonoBehaviour constructors, correct serialization).
- Both run under Claude Code and Codex.

**Notes:**
- The "testing subagent" (draft 3.4) is intentionally folded into Ticket 0.5 — it needs the MCP bridge + a running Editor.

---

### Ticket 0.5 - Unity MCP bridge + safe allowlist

**Status:** Not started.

**Goal:** Let agents inspect a running Editor's scenes/GameObjects and run PlayMode tests via MCP, behind safety guardrails.

**Dependencies:** None. Can run parallel to Ticket 0.1.

**Files:**
- MCP config for Claude Code and Codex
- `Packages/manifest.json` (if the bridge ships as a UPM package)
- `docs/playbooks/unity-mcp-allowlist.md` — the read-only vs. confirm-gated mapping (new)
- `AGENTS.md` — short MCP-safety note

**Research task:** Select and install a Unity MCP bridge (candidate: `com.ivanmurzak.unity.mcp` / IvanMurzak/Unity-MCP). Confirm it works against a running Editor and enumerate its tools for the allowlist.

**Subagent tasks:**
- Research subagent: evaluate the candidate bridge, list its tools, classify each read-only vs. mutating.
- Coding subagent A: wire MCP config for both tools.
- Coding subagent B: write the allowlist playbook + the AGENTS.md safety note.

**Acceptance criteria:**
- An MCP entry exists for both tools; a read-only command (e.g. "list active scenes and root GameObjects") succeeds against a running Editor.
- A documented allowlist marks list/query/read-log tools **safe**, and scene-mutation / build / delete / play-mode / test-run tools as **requiring an explicit plan step + confirmation** (consistent with the user's global risky-operations rule).
- Running PlayMode tests via MCP returns a structured pass/fail summary.

**Notes:**
- Editor-open only. **Not** a headless/CI path — unattended runs stay out of scope (same constraint as the linter).

---

### Ticket 0.6 - Session self-evaluation meta-skill

**Status:** Not started.

**Goal:** Audit a recent session against this backlog + `AGENTS.md` and report which practices were followed vs. skipped.

**Dependencies:** Ticket 0.1.

**Files:**
- `docs/playbooks/session-self-eval.md` (new)
- `.claude/skills/session-self-eval/SKILL.md` + Codex wrapper (new)

**Research task:** Decide what signals are observable to each tool (transcript, git diff, files touched) and define the checklist (plan present? tests requested? patches small? codemap regenerated? changelog updated?).

**Subagent tasks:**
- Research subagent: define the observable signals + checklist.
- Coding subagent A: write the playbook.
- Coding subagent B: add both wrappers.

**Acceptance criteria:**
- Running the skill after a task yields concrete observations (e.g. "plan step present; Test Runner not requested; codemap not regenerated").
- Output is short and actionable enough for the next session to course-correct.
- Runs under both Claude Code and Codex.

---

### Reference (not a new ticket) — Unity C# linter

**Status: In progress.** Spec (`docs/superpowers/specs/2026-06-23-unity-csharp-linter-design.md`) and plan (`docs/superpowers/plans/2026-06-24-unity-csharp-linter.md`) exist, and implementation is underway (`tools/lint/`, `scripts/lint.{sh,ps1}`, `.githooks/`, `.editorconfig`). Covers draft items 4.2 (editor-vs-runtime) and 4.4 (best-practices review). Listed here so the sprint is a complete picture of agentic tooling; it is tracked by its own spec/plan, not duplicated as a Sprint 0 ticket.

---

## Sprint exit criteria

Sprint 0 is complete when:
- **T0.1** standard is documented in `docs/playbooks/README.md` and proven with one reference playbook invocable from **both** Claude Code and Codex.
- The skills (**T0.2**, **T0.3**, **T0.6**) and commands (**T0.4**) are authored via that standard and run under both tools.
- **T0.5** MCP connects read-only to a running Editor, ships a documented allowlist, and returns a PlayMode test summary.
- The linter is built and `scripts/lint.sh --check` runs clean on current gameplay code.
- `AGENTS.md` reflects the cross-tool authoring rule, the context-hygiene rule, and the pre-refactor git-safety rule.
````

- [ ] **Step 2: Verify structure**

Run: `grep -nE "^### Ticket 0\.[1-6] -|^### Reference|^## Sprint exit criteria" docs/sprints/sprint-0-best-practices.md`
Expected: 6 ticket headings (0.1–0.6) + the Reference heading + the exit-criteria heading (8 lines).

---

### Task 2: Fold the new rules into `AGENTS.md`

**Files:**
- Modify: `AGENTS.md`

**Cross-references:**
- Produces: the "Agent skills & commands (cross-tool)" section that Ticket 0.1 references.

- [ ] **Step 1: Add the cross-tool section + context-hygiene bullet** (after the codemap bullet, before `## Input`)

Replace this exact text:

```
- **Before grepping or exploring `Assets/Scripts/` to understand the codebase, READ `docs/codemap.md` first.** It is a Mermaid class diagram of all gameplay types, their fields, public methods, and cross-type dependencies — faster than grepping. Regenerate it if it looks stale (see Workflow section for commands).

## Input
```

with:

```
- **Before grepping or exploring `Assets/Scripts/` to understand the codebase, READ `docs/codemap.md` first.** It is a Mermaid class diagram of all gameplay types, their fields, public methods, and cross-type dependencies — faster than grepping. Regenerate it if it looks stale (see Workflow section for commands).
- **Context hygiene:** rely on `.gitignore` (both tools honor it) to keep `Library/`, `Logs/`, `obj/`, and build output out of context — do not add a tool-specific ignore file.

## Agent skills & commands (cross-tool)

This repo is driven by **both Claude Code and OpenAI Codex**. Anything reusable
(a skill, command, or template) is authored **once, tool-neutrally**, then
exposed to each tool through a thin wrapper:

- **Canonical playbook** — the real procedure/template lives in
  `docs/playbooks/<name>.md`, written in terms of actions (read a file, create a
  file, run a command), never a single tool's API.
- **Claude Code wrapper** — `.claude/skills/<name>/SKILL.md` (or a
  `.claude/commands/<name>.md` slash command), body = "Follow the playbook."
- **Codex wrapper** — the project custom-prompt entry pointing at the same playbook.
- `AGENTS.md` is the shared contract both tools read (`CLAUDE.md` only points to
  it). Put rules here, not in a tool-specific file.

Never add a Claude-only or Codex-only behavior the other tool can't follow.

## Input
```

- [ ] **Step 2: Add the git-safety bullet** (end of the Workflow list, before `## Changelog`)

Replace this exact text:

```
  - Any platform (fallback): `dotnet run --project tools/codemap/codemap.csproj -- <repo-root>`

## Changelog — read and update it
```

with:

```
  - Any platform (fallback): `dotnet run --project tools/codemap/codemap.csproj -- <repo-root>`
- Before a large or structural refactor, commit or stash first so there is a clean rollback point, and note this in the plan.

## Changelog — read and update it
```

- [ ] **Step 3: Verify**

Run: `grep -nE "Agent skills & commands \(cross-tool\)|Context hygiene:|commit or stash first" AGENTS.md`
Expected: 3 matches (the section heading, the context-hygiene bullet, the git-safety bullet).

---

### Task 3: Register Sprint 0 in `docs/TICKETS.md`

**Files:**
- Modify: `docs/TICKETS.md`

**Cross-references:**
- Consumes: the sprint file produced by Task 1 (`sprints/sprint-0-best-practices.md`).

- [ ] **Step 1: Add the Sprint 0 index row** (before the Sprint 1 row)

Replace this exact text:

```
| Sprint | File | Goal question | Status |
|--------|------|---------------|--------|
| Sprint 1 — Core Ride | [`docs/sprints/sprint-1-core-ride.md`](sprints/sprint-1-core-ride.md) | Can we ride the trail at all? | In progress |
```

with:

```
| Sprint | File | Goal question | Status |
|--------|------|---------------|--------|
| Sprint 0 — Agentic Workflow & Tooling | [`docs/sprints/sprint-0-best-practices.md`](sprints/sprint-0-best-practices.md) | Are our agent workflows fast, safe, and cross-tool? | In progress |
| Sprint 1 — Core Ride | [`docs/sprints/sprint-1-core-ride.md`](sprints/sprint-1-core-ride.md) | Can we ride the trail at all? | In progress |
```

- [ ] **Step 2: Add the meta-sprint note** (after the index table, before `## Phase order`)

Replace this exact text:

```
| Sprint 6 — Readability | [`docs/sprints/sprint-6-readability.md`](sprints/sprint-6-readability.md) | Can we tune and read the prototype? | Not started |

## Phase order
```

with:

```
| Sprint 6 — Readability | [`docs/sprints/sprint-6-readability.md`](sprints/sprint-6-readability.md) | Can we tune and read the prototype? | Not started |

> **Sprint 0** is a meta/tooling sprint (agent workflow), not part of the gameplay
> phase order below. Its tickets live in its own file and are not duplicated in the
> gameplay phase/roster tables.

## Phase order
```

- [ ] **Step 3: Verify the link target exists**

Run: `ls docs/sprints/sprint-0-best-practices.md && grep -n "Sprint 0 — Agentic Workflow & Tooling" docs/TICKETS.md`
Expected: the file lists, and one grep match for the index row.

---

### Task 4: Add the `CHANGELOG.md` entry

**Files:**
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Add a 2026-06-24 entry at the top of `### Added`**

Replace this exact text:

```
## [Unreleased]

### Added

- **Bike terrain contact stability** — 2026-06-23
```

with:

```
## [Unreleased]

### Added

- **Agentic workflow & tooling (Sprint 0) backlog** — 2026-06-24
  - Added `Sprint 0 — Agentic Workflow & Tooling` (`docs/sprints/sprint-0-best-practices.md`),
    reconciling `docs/agenticdev.md` recommendations with the existing AGENTS.md workflow.
  - Added cross-tool (Claude Code + Codex) skill/command authoring rules, a
    context-hygiene rule, and a pre-refactor git-safety rule to `AGENTS.md`.
  - Registered Sprint 0 in `docs/TICKETS.md`.

- **Bike terrain contact stability** — 2026-06-23
```

- [ ] **Step 2: Verify**

Run: `grep -n "Agentic workflow & tooling (Sprint 0) backlog" CHANGELOG.md`
Expected: 1 match.

---

### Task 5: Consistency check + commit

**Files:** none modified — verification + commit only.

- [ ] **Step 1: Confirm the four files agree**

Run:
```bash
ls docs/sprints/sprint-0-best-practices.md
grep -c "^### Ticket 0\.[1-6] -" docs/sprints/sprint-0-best-practices.md   # expect 6
grep -n "bestpractices/sprint.md" docs/TICKETS.md                        # expect the index row link
grep -n "Agent skills & commands (cross-tool)" AGENTS.md                 # expect 1
grep -n "Sprint 0" CHANGELOG.md                                          # expect the changelog entry
```
Expected: sprint file present; 6 tickets; the TICKETS.md link; the AGENTS.md section; the CHANGELOG entry.

- [ ] **Step 2: Confirm no in-flight linter files were touched**

Run: `git status --short`
Expected: changes limited to `docs/sprints/sprint-0-best-practices.md` (new), `AGENTS.md`, `docs/TICKETS.md`, `CHANGELOG.md`. The linter files (`tools/lint/`, `scripts/lint.*`, `.githooks/`, `.editorconfig`, `.gitignore`, linter spec/plan) remain untracked/unstaged and unchanged.

- [ ] **Step 3: Commit** — *requires user confirmation first (global rule)*

```bash
git add docs/sprints/sprint-0-best-practices.md AGENTS.md docs/TICKETS.md CHANGELOG.md
git commit -m "docs: add Sprint 0 (agentic workflow & tooling) and fold cross-tool rules into AGENTS.md" \
  -m "Rewrites bestpractices/sprint.md into house-format Sprint 0 (T0.1 cross-tool authoring standard, T0.2 MonoBehaviour template, T0.3 test-gen, T0.4 research/refactor commands, T0.5 Unity MCP bridge, T0.6 self-eval; linter referenced as in-progress). Adds cross-tool authoring, context-hygiene, and pre-refactor git-safety rules to AGENTS.md, and registers Sprint 0 in TICKETS.md + CHANGELOG.md." \
  -m "Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Self-Review

**1. Spec coverage** — every spec section maps to a task:
- Gap-analysis classification → lives in the committed spec; the sprint (Task 1) carries only the resulting tickets. ✔
- Cross-tool authoring pattern → Ticket 0.1 body (Task 1) + AGENTS.md section (Task 2). ✔
- Sprint 0 ticket bodies T0.1–T0.6 + linter reference → Task 1. ✔ (Linter status upgraded to "in progress" per working-tree discovery — intentional deviation.)
- AGENTS.md additions (cross-tool / context-hygiene / git-safety) → Task 2. ✔
- TICKETS.md registration (row + note) → Task 3. ✔
- "Update CHANGELOG.md" (AGENTS.md mandate) → Task 4. ✔
- Verification ("done") criteria → Task 5. ✔

**2. Placeholder scan** — `<example>`, `<name>`, and "path from research" inside the Ticket 0.1 body are intentional: they describe the future ticket's naming convention and its one bounded research item, not gaps in *this* plan. Every edit in Tasks 1–4 contains the literal final text. No "TBD/TODO" in executable steps. ✔

**3. Type consistency** — file paths are identical across tasks and the spec: `docs/sprints/sprint-0-best-practices.md`, `docs/playbooks/`, `.claude/skills/<name>/SKILL.md`, `.claude/commands/<name>.md`. Ticket IDs T0.1–T0.6 match the spec roster and the exit criteria. The TICKETS.md row path matches the file Task 1 creates. ✔
