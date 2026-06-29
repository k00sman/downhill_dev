# Playbook: session-self-eval

**Purpose:** Audit a recent session against `AGENTS.md` and the Sprint 0 backlog
(`docs/sprints/bestpractices/sprint.md`). Reports which practices were followed vs.
skipped, with concrete evidence and up to three course-corrections for the next
session. It reports observations, not a passing grade.

## When to use

Run at the end of any work session — after completing a ticket, a bug fix, or a
meaningful batch of changes — to surface gaps before the next session begins.

## Signals to gather

All signals are observable to either tool using only the conversation/transcript,
file reads, and read-only shell `git` commands. No special APIs or editor access
required.

1. **Plan file present?**
   Check whether a plan exists under `docs/superpowers/plans/` for the current
   work. Run: `ls docs/superpowers/plans/` and look for a file matching the
   session's ticket or feature name.

2. **Spec file present?**
   Check whether a spec exists under `docs/superpowers/specs/` for the same scope.
   Run: `ls docs/superpowers/specs/`.

3. **Patch size and scope**
   Run `git diff --stat HEAD` (or against the base branch if on a feature branch).
   Inspect whether changed files match the planned scope. Flag patches that touch
   files outside the plan or that are very large (> ~300 lines net).

4. **Codemap regenerated?**
   After meaningful code changes under `Assets/Scripts/`, `docs/codemap.md` must
   be regenerated. Check: `git diff --name-only HEAD | grep -q 'Assets/Scripts'`
   — if true, then `git diff --name-only HEAD | grep -q 'docs/codemap.md'`. If
   scripts changed but codemap did not, flag it.

5. **CHANGELOG updated?**
   Read `CHANGELOG.md` (or run `git diff HEAD -- CHANGELOG.md`) and check for a
   matching entry under `## [Unreleased]`. Flag if missing for notable changes.

6. **Linter run for C# changes?**
   Check whether `scripts/lint.sh --check` (or `.ps1` on Windows) was mentioned
   in the session transcript. If C# files changed (`git diff --name-only HEAD |
   grep -qE '\\.cs$'`) but linting was not run, flag it.

7. **`.meta` files committed beside new `Assets/` files?**
   Run: `git diff --name-only HEAD | grep -E '^Assets/' | grep -v '\.meta$'`.
   For each new asset/script, verify a matching `.meta` also appears in the diff.
   Flag any asset without a committed `.meta`.

8. **Cross-tool rule respected?**
   If the session authored a new skill, command, or reusable procedure, verify it
   was written as a playbook in `docs/playbooks/` with both wrappers
   (`.claude/skills/<name>/SKILL.md` and `.agents/skills/<name>/SKILL.md`).
   Check: `ls docs/playbooks/` and the two skills directories.

9. **Model-tiered delegation applied?**
   Review the session transcript for subagent use. Mechanical tasks (file moves,
   boilerplate, single-file scaffolds) should use the smallest model (Haiku/small).
   Bounded spec-following work should use Sonnet/mid. Design or ambiguous work
   should use Opus/top. Flag obvious mismatches (e.g. Opus used for a grep).

10. **Unity Test Runner requested/reported for test-relevant changes?**
    If code under `Assets/Scripts/` or `Assets/Tests/` changed, check whether the
    session transcript shows a request to the user to run the Unity Test Runner
    (EditMode + PlayMode) and report results. Agents cannot run it themselves.

## Checklist

Evaluate each item and assign one of: **Followed**, **Skipped**, or **N/A**.
Provide a single line of evidence for each.

| # | Practice | Result | Evidence |
|---|----------|--------|----------|
| 1 | Spec present for feature work | — | path or "no spec file found" |
| 2 | Plan present for feature work | — | path or "no plan file found" |
| 3 | Patches small and scoped to planned files | — | `git diff --stat` summary |
| 4 | Codemap regenerated after code changes | — | `docs/codemap.md` in diff or "not changed" |
| 5 | `CHANGELOG.md` updated under `[Unreleased]` | — | entry found or "no matching entry" |
| 6 | Linter run/clean for C# changes | — | mentioned in transcript or "not run" |
| 7 | `.meta` committed beside new `Assets/` files | — | all present or list missing |
| 8 | Cross-tool rule respected (playbook + both wrappers) | — | files present or "skipped" |
| 9 | Model-tiered delegation applied | — | observation or "N/A (no subagents)" |
| 10 | Unity Test Runner requested for test-relevant changes | — | mentioned or "not requested" |

## Procedure

1. Read `AGENTS.md` to confirm the current rules (the checklist above is derived
   from it, but the file is authoritative).
2. Gather each signal listed in "Signals to gather" using read-only file reads and
   `git` commands. Do not stage, commit, or modify any file.
3. Fill in the checklist table with Followed/Skipped/N/A and one line of evidence.
4. Identify the top 1–3 items that were Skipped and that would have the highest
   impact if corrected in the next session.
5. Output the report (see format below). Stop — do not make any changes.

## Report format

```
## Session self-eval — <date or branch name>

| # | Practice | Result | Evidence |
|---|----------|--------|----------|
| 1 | ...      | Followed / Skipped / N/A | ... |
...

**Top course-corrections for next session:**
1. <most impactful skipped item — one sentence>
2. <second — one sentence>
3. <third — one sentence, or omit if fewer than three>

_Reports observations only — not a passing grade._
```

Keep the entire output short enough to read in 30 seconds.

## Done check

The skill is complete when:
- Every checklist item has a Followed/Skipped/N/A result with evidence.
- The report lists at most three course-corrections.
- No files were modified (read-only run).
