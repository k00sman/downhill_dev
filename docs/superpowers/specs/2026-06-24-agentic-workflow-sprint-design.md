# Agentic Workflow & Tooling Sprint — Design Spec

**Date:** 2026-06-24
**Topic:** Reconcile the `agenticdev.md` best-practice recommendations with this
repo's real workflow, and turn the genuinely-new items into a house-format sprint.
**Status:** Approved (brainstorm complete) — ready for implementation plan.

## Context

`docs/agenticdev.md` was a generic best-practices report for indie Unity devs
(since removed — its content is fully captured in the gap analysis below).
`docs/sprints/sprint-0-best-practices.md` is a first-pass ticket breakdown generated
straight from that report (Epics 1–7). The note at the top of `agenticdev.md`
asks: *what can we apply to our existing workflow as defined in the agents file,
and insert into our sprint system?*

The honest answer is that **~half the draft is already enforced** by `AGENTS.md`
plus the Superpowers workflow, and one chunk (best-practices review + editor-vs-
runtime detection) is already being built as the **Unity C# linter**
(`docs/superpowers/specs/2026-06-23-unity-csharp-linter-design.md`). So this is a
reconciliation job, not a blind import.

### Cross-tool constraint (shapes everything)

This repo is driven by **both Claude Code (CLI) and OpenAI Codex (CLI)**. Every
reusable artifact produced by this sprint — skills, commands, templates — must
run under **both** tools. The repo is already half-built for this: `AGENTS.md` is
the shared contract Codex reads natively, and `CLAUDE.md` is only a pointer to it.
This constraint reframes the draft's "Copilot/Codex coordination" epic from
documentation into a **foundational ticket** (T0.1) that the skill tickets depend
on.

## Goal

1. Classify every draft recommendation as **Done**, **Rule** (→ `AGENTS.md`),
   **Ticket** (→ build it), or **Drop**, with a reason.
2. Fold the always-on rules into `AGENTS.md`.
3. Rewrite `docs/sprints/sprint-0-best-practices.md` into a real house-format sprint
   (`Sprint 0 — Agentic Workflow & Tooling`) containing only the genuinely-new
   tickets.
4. Register Sprint 0 in `docs/TICKETS.md`.

## Approach (decided)

**Gap-analysis + split.** Chosen over "annotate the draft in place" and "tickets
only, no AGENTS.md edits" because the draft conflates always-on rules (which
belong in the contract every session reads) with build-once tooling (which
belongs in a sprint). Splitting them puts each kind where it actually gets used
and removes the busywork tickets for things already done.

Optional/heavy epics, per explicit user selection: **Unity MCP (Epic 2) is in**
(corrected understanding: it drives a *running* Editor, so it buys interactive
scene inspection + PlayMode test runs; the "no headless" limit only kills
unattended CI). **Self-eval meta-skill (Epic 7.1) is in.** **Epic 6 is reframed**
into the cross-tool authoring standard (T0.1) rather than dropped.

## Gap-analysis classification

Legend: ✅ Done (cite) · 📋 Rule→`AGENTS.md` · 🎫 Build as ticket · ✂️ Drop.

| Draft item | Disposition | Where it goes / why |
|---|---|---|
| 1.1 Create CLAUDE.md | ✅ Done | `AGENTS.md` is richer; `CLAUDE.md` is the cross-tool pointer. |
| 1.2 `.claudeignore` + rules | 📋 Rule | A Claude-only ignore violates "works for both." Use `.gitignore` (both tools honor it) + a context-hygiene note. Verify it covers `Library/ Logs/ obj/ build`. |
| 2.1 Unity MCP server | 🎫 T0.5 | User-selected. Editor-open, not headless. |
| 2.2 MCP allowlist/guardrails | 🎫 → T0.5 | Folded in: read-only vs. confirm-gated tools. |
| 3.1 plan→execute→review | ✅ Done | = brainstorm→spec→plan→execute in `AGENTS.md`. |
| 3.2 research subagent | 🎫 T0.4 | Formalize the existing `TICKETS.md` prompt as a cross-tool command. |
| 3.3 refactor subagent + allowlist | 🎫 → T0.4 | Combined with 3.2. |
| 3.4 testing subagent via MCP | 🎫 → T0.5 | Becomes feasible once MCP + Editor are up. |
| 4.1 MonoBehaviour template | 🎫 T0.2 | Encodes repo conventions (camelCase serialized fields, Allman, asmdef, `.meta`). |
| 4.2 editor-vs-runtime | ✅ Covered | Linter (`Microsoft.Unity.Analyzers`) catches this. |
| 4.3 test-gen skill | 🎫 T0.3 | EditMode/PlayMode + `InputTestFixture`-aware. |
| 4.4 review vs best practices | ✅ Covered | = the linter spec (2026-06-23). |
| 5.1 small-patch default | ✅ Done | = DoD + working rules. |
| 5.2 git safety | 📋 Rule | Add "commit/stash before large refactor"; risky-op confirm already in the user's global rules. |
| 6.1/6.2 IDE-AI coordination | 🎫 T0.1 | Reframed → cross-tool authoring standard (Claude Code + Codex). Foundational. |
| 7.1 self-eval meta-skill | 🎫 T0.6 | User-selected. |

Notably **nothing landed in ✂️ Drop**: every Epic 1–7 item was either already
satisfied, foldable into a rule, or worth building — even Epic 6 was salvageable
by reframing it as the cross-tool standard rather than discarding it.

## Cross-tool authoring pattern (foundational design)

Every reusable artifact is authored **once, tool-neutrally**, then exposed to
each tool through a thin wrapper. Three parts:

1. **Canonical playbook** — the real procedure/checklist/template lives in
   `docs/playbooks/<name>.md`, written in terms of *actions* ("read a file",
   "create a file", "run a command"), never one tool's API. This mirrors how
   Superpowers skills "speak in actions."
2. **Per-tool wrappers** — thin entry points that only point at the playbook:
   - **Claude Code:** `.claude/skills/<name>/SKILL.md` (frontmatter `name` +
     `description`, body = "Follow `docs/playbooks/<name>.md`"). For
     orchestration-style entries, a `.claude/commands/<name>.md` slash command is
     the better fit than a skill.
   - **Codex:** a skill at `.agents/skills/<name>/SKILL.md` (the same `SKILL.md`
     format) pointing at the same playbook.
     **Resolved in T0.1:** the candidate `.codex/prompts/<name>.md` was the
     *custom-prompt* feature — home-directory-only and now deprecated — so it is
     not used. Codex's repo-committed mechanism is skills (`.agents/skills/`,
     scanned CWD→repo-root); both tools therefore share the identical `SKILL.md`
     artifact and differ only by directory.
3. **Contract** — `AGENTS.md` documents the convention so both tools and humans
   know playbooks are the source of truth and where wrappers live.

T0.1 establishes this pattern and proves it with one trivial reference wrapper
invoked from both tools. T0.2/T0.3/T0.4/T0.6 then consume the pattern, so any
divergence is caught once, early.

## Sprint 0 — ticket bodies

These are the proposed contents of the rewritten
`docs/sprints/sprint-0-best-practices.md`. House ticket format, adapted: the
"Research task" line is tool-neutral (not "for Opus") to match this sprint's
cross-tool theme.

**Suggested execution order:** T0.1 first (gates T0.2/T0.3/T0.4/T0.6); T0.5 and
the linter build can proceed in parallel.

---

### Ticket 0.1 - Cross-tool skill/command authoring standard

**Status:** Not started.

**Goal:** Define and prove a single way to author a skill/command once and run it
under both Claude Code and OpenAI Codex.

**Dependencies:** None.

**Files:**
- `docs/playbooks/` (new) and `docs/playbooks/README.md` — the pattern + wrapper conventions
- `.claude/skills/<example>/SKILL.md` — proof wrapper (Claude Code)
- Codex project custom-prompt wrapper (path determined by research)
- `AGENTS.md` — the cross-tool authoring rule

**Research task:** Confirm Codex CLI's project-level custom-prompt mechanism and
path, and confirm Claude Code resolves a skill whose body defers to an external
markdown file. Output the minimal wrapper boilerplate for each tool.

**Acceptance criteria:**
- `docs/playbooks/README.md` documents the canonical-playbook + per-tool-wrapper
  pattern.
- One reference playbook exists with both wrappers, and it can be invoked from
  **both** Claude Code and Codex, each resolving the same playbook.
- `AGENTS.md` has a short "Agent skills & commands (cross-tool)" section stating
  the rule.
- No tool-specific behavior is introduced that the other tool cannot follow.

**Notes:**
- Keep wrappers truly thin — all logic lives in the playbook, never duplicated.

---

### Ticket 0.2 - Unity MonoBehaviour template skill

**Status:** Not started.

**Goal:** Given a script name and responsibility, generate a convention-compliant
MonoBehaviour.

**Dependencies:** Ticket 0.1.

**Files:**
- `docs/playbooks/unity-monobehaviour-template.md`
- `.claude/skills/unity-monobehaviour-template/SKILL.md` + Codex wrapper

**Research task:** Extract the concrete conventions already used in
`Assets/Scripts/` (serialized-field grouping, naming, asmdef placement) so the
template matches reality.

**Acceptance criteria:**
- Invoking the skill with a description (e.g. "enemy patrol AI") produces a
  MonoBehaviour using `[SerializeField] private` fields grouped at the top,
  Allman braces, camelCase serialized fields, and the correct asmdef/folder.
- The generated file is paired with a reminder to commit its `.meta`.
- Output avoids heavy logic in `Update()`; suggests events/coroutines where apt.

**Notes:**
- This is a generator skill, not game code — it touches no existing gameplay.

---

### Ticket 0.3 - Unity test-generation skill

**Status:** Not started.

**Goal:** Scaffold EditMode/PlayMode tests for a target class or behavior.

**Dependencies:** Ticket 0.1.

**Files:**
- `docs/playbooks/unity-test-gen.md`
- `.claude/skills/unity-test-gen/SKILL.md` + Codex wrapper

**Research task:** Capture the repo's test conventions — `Assets/Tests/EditMode`
and `Assets/Tests/PlayMode`, each with its own asmdef; PlayMode input tests use
`InputTestFixture`.

**Acceptance criteria:**
- Given a target class + expected behavior, the skill produces a compiling test
  in the correct test assembly (EditMode for pure math, PlayMode for runtime).
- PlayMode input tests are scaffolded with `InputTestFixture`.
- Output reminds the user to run the Unity Test Runner (per `AGENTS.md`, agents
  can't run it here).

---

### Ticket 0.4 - Research + Refactor agent commands

**Status:** Not started.

**Goal:** Formalize the existing orchestration prompts into two reusable
cross-tool commands.

**Dependencies:** Ticket 0.1.

**Files:**
- `docs/playbooks/unity-research.md`, `docs/playbooks/unity-refactor.md`
- `.claude/commands/unity-research.md`, `.claude/commands/unity-refactor.md` + Codex wrappers

**Research task:** Lift the "Research subagent" / "Coding subagent" prompts from
`docs/TICKETS.md` and adapt them; define the refactor command's file-allowlist
contract.

**Acceptance criteria:**
- A research command takes a target (feature/folder/script) and returns a
  structured summary (files, responsibilities, key patterns) instead of raw dumps.
- A refactor command operates **only** on an explicit file allowlist and returns
  diffs, respecting Unity patterns (no MonoBehaviour constructors, correct
  serialization).
- Both run under Claude Code and Codex.

**Notes:**
- The "testing subagent" (draft 3.4) is intentionally folded into T0.5, since it
  needs the MCP bridge + a running Editor.

---

### Ticket 0.5 - Unity MCP bridge + safe allowlist

**Status:** Not started.

**Goal:** Let agents inspect a running Editor's scenes/GameObjects and run
PlayMode tests via MCP, behind safety guardrails.

**Dependencies:** None (can run parallel to T0.1).

**Files:**
- MCP config for Claude Code and Codex
- `Packages/manifest.json` (if the bridge ships as a UPM package)
- `docs/playbooks/unity-mcp-allowlist.md` — the read-only vs. confirm-gated mapping
- `AGENTS.md` — short MCP-safety note

**Research task:** Select and install a Unity MCP bridge (candidate:
`com.ivanmurzak.unity.mcp` / IvanMurzak/Unity-MCP). Confirm it works against a
running Editor and enumerate its tools for the allowlist.

**Acceptance criteria:**
- An MCP entry exists for both tools; a read-only command (e.g. "list active
  scenes and root GameObjects") succeeds against a running Editor.
- A documented allowlist marks list/query/read-log tools **safe**, and
  scene-mutation / build / delete / play-mode / test-run tools as **requiring an
  explicit plan step + confirmation** (consistent with the user's global
  risky-operations rule).
- Running PlayMode tests via MCP returns a structured pass/fail summary.

**Notes:**
- Editor-open only. This is **not** a headless/CI path — unattended runs remain
  out of scope (see linter spec for the same constraint).

---

### Ticket 0.6 - Session self-evaluation meta-skill

**Status:** Not started.

**Goal:** Audit a recent session against this backlog + `AGENTS.md` and report
which practices were followed vs. skipped.

**Dependencies:** Ticket 0.1.

**Files:**
- `docs/playbooks/session-self-eval.md`
- `.claude/skills/session-self-eval/SKILL.md` + Codex wrapper

**Research task:** Decide what signals are observable to each tool (transcript,
git diff, files touched) and define the checklist (plan present? tests requested?
patches small? codemap regenerated? changelog updated?).

**Acceptance criteria:**
- Running the skill after a task yields concrete observations (e.g. "plan step
  present; Test Runner not requested; codemap not regenerated").
- Output is short and actionable enough for the next session to correct course.
- Runs under both Claude Code and Codex.

---

### Reference (not a new ticket) — Unity C# linter

Already specced and approved in
`docs/superpowers/specs/2026-06-23-unity-csharp-linter-design.md` (covers draft
4.2 editor-vs-runtime + 4.4 best-practices review). **Status in Sprint 0:** spec
done → needs implementation plan + build. Listed in the roster so the sprint is a
complete picture of agentic tooling.

## AGENTS.md additions (proposed text)

A new section (placed after "Project structure & conventions"):

```markdown
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
```

Appended to "Project structure & conventions":

```markdown
- **Context hygiene:** rely on `.gitignore` (both tools honor it) to keep
  `Library/`, `Logs/`, `obj/`, and build output out of context — do not add a
  tool-specific ignore file.
```

Appended to the "Workflow (specs / plans / tickets)" list:

```markdown
- Before a large or structural refactor, commit or stash first so there is a
  clean rollback point, and note this in the plan.
```

## TICKETS.md registration

Add a row to the **Sprint index** table:

```markdown
| Sprint 0 — Agentic Workflow & Tooling | [`docs/sprints/sprint-0-best-practices.md`](sprints/sprint-0-best-practices.md) | Are our agent workflows fast, safe, and cross-tool? | In progress |
```

Add a one-line note under the index clarifying Sprint 0 is a **meta/tooling**
sprint (not part of the gameplay phase order); its tickets live in its own file
and are not duplicated in the gameplay phase/roster tables.

## File changes summary

| File | Change |
|---|---|
| `docs/sprints/sprint-0-best-practices.md` | Rewrite into house-format `Sprint 0` with tickets T0.1–T0.6 + linter reference + exit criteria |
| `AGENTS.md` | Add cross-tool authoring section; add context-hygiene + git-safety rules |
| `docs/TICKETS.md` | Add Sprint 0 index row + meta-sprint note |
| `docs/superpowers/specs/2026-06-24-agentic-workflow-sprint-design.md` | This spec (new) |

No code or assets change in this sprint-authoring work itself; individual tickets
produce playbooks/wrappers/config when they are later executed.

## Verification ("done")

This is a docs-reconciliation deliverable, so "done" for the authoring work is:

1. The three files above are updated and internally consistent (the classification
   table, the sprint roster, and the `TICKETS.md` row all agree).
2. Every cross-reference resolves (links to the linter spec, the sprint file, the
   playbooks dir convention).
3. `AGENTS.md` additions are present and don't contradict existing rules.
4. The draft's Epic 1–7 items are each accounted for by exactly one disposition.

Each Sprint 0 ticket carries its own acceptance criteria (above) for when it is
later built.

## Out of scope / deferred

- Unattended/CI test runs (no headless Editor) — same constraint as the linter.
- A custom Unity MCP server of our own — use an existing bridge.
- Re-ticketing the linter — it has its own approved spec.
- Any change to gameplay sprints (1–6) or the gameplay phase order.
