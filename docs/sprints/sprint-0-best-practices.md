# Sprint 0 — Agentic Workflow & Tooling

**Goal question:** Are our agent workflows fast, safe, and cross-tool (Claude Code + Codex)?

**Sprint status:** In progress

> Meta/tooling sprint — **not** part of the gameplay phase order. Derived from a
> best-practices review, distilled into the gap analysis in
> `docs/superpowers/specs/2026-06-24-agentic-workflow-sprint-design.md`. Items the
> repo already satisfies (the `CLAUDE.md`→`AGENTS.md` contract, brainstorm→spec→
> plan→execute, the small-patch definition of done) are intentionally **not**
> re-ticketed here.

---

### Ticket 0.1 - Cross-tool skill/command authoring standard

**Status:** Implemented — Claude side verified in-repo; live Codex slash-command run pending a human (can't drive Codex from here).

**Goal:** Define and prove one way to author a skill/command once and run it under both Claude Code and OpenAI Codex.

**Dependencies:** None. Gates Tickets 0.2, 0.3, 0.4, and 0.6.

**Files:**
- `docs/playbooks/README.md` — the pattern + per-tool wrapper conventions (new)
- _(proof entry `example-cross-tool-check` — playbook + both wrappers — was invoked live in Claude Code to verify wiring, then removed; the five real skills are the living examples)_
- `AGENTS.md` — "Agent skills & commands (cross-tool)" section (corrected to the verified Codex skills path)

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

**Status:** Implemented — playbook + both wrappers authored; conventions verified against the codebase. Not yet exercised on a live input.

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

**Status:** Implemented — playbook + both wrappers authored; conventions verified against the codebase. Not yet exercised on a live input.

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

**Status:** Implemented — both playbooks + all four wrappers authored; conventions verified against the codebase. Not yet exercised on a live input.

**Goal:** Formalize the existing orchestration prompts into two reusable cross-tool skills.

**Dependencies:** Ticket 0.1.

**Files:**
- `docs/playbooks/unity-research.md`, `docs/playbooks/unity-refactor.md` (new)
- `.claude/skills/unity-research/SKILL.md`, `.claude/skills/unity-refactor/SKILL.md` (new) — invoked as `/unity-research` and `/unity-refactor`
- `.agents/skills/unity-research/SKILL.md`, `.agents/skills/unity-refactor/SKILL.md` — Codex wrappers (new)

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

**Status:** In progress — docs + live setup done; live acceptance checks A/B not yet exercised. Verified live (2026-06-27): package `2.13.0-pre.2` installed, bridge running (relay `relay_linux`), Claude Code + Codex auto-configured via Unity **Integrations** (`unity-mcp ✔ Connected`), tools loaded into Claude Code after `claude --continue`. Checks A (read-only scene query) and B (PlayMode tests) deferred to a live Editor session. **Scope discovery:** the official package exposes **no test-runner tool**, so acceptance criterion 3 needs a custom `TestRunnerApi` MCP tool — otherwise tests stay on the manual Test Runner path. Allowlist playbook updated with the verified three-layer safety model (Unity's per-tool toggle is a third layer and ships non-default-deny) + the `Manage*`-GATED rule.

**Goal:** Let agents inspect a running Editor's scenes/GameObjects and run PlayMode tests via MCP, behind safety guardrails.

**Dependencies:** None. Can run parallel to Ticket 0.1.

**Files:**
- `docs/playbooks/unity-mcp-allowlist.md` — read-only vs. confirm-gated tool policy + human setup steps (new)
- `AGENTS.md` — "Unity MCP (optional, Editor-open)" safety note + first-party-dependency rule
- MCP client config (Claude Code auto via Integrations; Codex manual `~/.codex/config.toml`) and the `com.unity.ai.assistant` package install — applied by a human in-Editor, **not committed** (machine-specific paths)

**Research task:** Select a Unity MCP bridge and enumerate its tools for the allowlist. **Resolved:** chose the **official** `com.unity.ai.assistant` Unity MCP server (Editor-open; built-in per-client Accept/Deny gate; Claude Code auto-configures via Integrations, Codex is wired manually to the `~/.unity/relay/` binary with `--mcp`) over third-party `com.ivanmurzak.unity.mcp`, per the first-party-dependency preference. **Verified 2026-06-27 (2.13.0-pre.2): both Claude Code *and* Codex auto-configure via Unity Integrations now; the manual Codex `config.toml` path is a fallback.**

**Subagent tasks:**
- Research subagent: evaluate the candidate bridge, list its tools, classify each read-only vs. mutating.
- Coding subagent A: wire MCP config for both tools.
- Coding subagent B: write the allowlist playbook + the AGENTS.md safety note.

**Acceptance criteria:**
- An MCP entry exists for both tools; a read-only command (e.g. "list active scenes and root GameObjects") succeeds against a running Editor.
- A documented allowlist marks list/query/read-log tools **safe**, and scene-mutation / build / delete / play-mode / test-run tools as **requiring an explicit plan step + confirmation** (consistent with the user's global risky-operations rule).
- Running PlayMode tests via MCP returns a structured pass/fail summary. **(Blocked — verified 2026-06-27: the official `com.unity.ai.assistant` exposes no test-runner tool. Needs a custom `TestRunnerApi` MCP tool; until then tests run from the manual Unity Test Runner window.)**

**Notes:**
- Editor-open only. **Not** a headless/CI path — unattended runs stay out of scope (same constraint as the linter).
- **Live findings (2026-06-27):** no `run_tests` tool in the official package (criterion 3 above); Unity's per-tool enable/disable toggle is a third safety layer that ships non-default-deny (`RunCommand` + `GenerateAsset` enabled); `Manage*` tools bundle read+write → GATED even for reads; Linux relay binary is `relay_linux`; both Claude Code and Codex auto-configure via Unity Integrations in 2.13. Details in `docs/playbooks/unity-mcp-allowlist.md`.

---

### Ticket 0.6 - Session self-evaluation meta-skill

**Status:** Implemented — playbook + both wrappers authored; checklist derived from `AGENTS.md`. Not yet exercised on a live input.

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

**Status: Built (merged to `main`, `ef96daa`).** Spec (`docs/superpowers/specs/2026-06-23-unity-csharp-linter-design.md`) and plan (`docs/superpowers/plans/2026-06-24-unity-csharp-linter.md`) exist, and the implementation has landed (`tools/lint/`, `scripts/lint.{sh,ps1}`, `.githooks/`, `.editorconfig`, plus an `AGENTS.md` `## Linting` section). Covers draft items 4.2 (editor-vs-runtime) and 4.4 (best-practices review). Listed here so the sprint is a complete picture of agentic tooling; it is tracked by its own spec/plan, not duplicated as a Sprint 0 ticket.

---

## Sprint exit criteria

Sprint 0 is complete when:
- **T0.1** standard is documented in `docs/playbooks/README.md` and proven with one reference playbook invocable from **both** Claude Code and Codex.
- The skills (**T0.2**, **T0.3**, **T0.6**) and commands (**T0.4**) are authored via that standard and run under both tools.
- **T0.5** MCP connects read-only to a running Editor, ships a documented allowlist, and returns a PlayMode test summary.
- The linter is built and `scripts/lint.sh --check` runs clean on current gameplay code.
- `AGENTS.md` reflects the cross-tool authoring rule, the context-hygiene rule, and the pre-refactor git-safety rule.
