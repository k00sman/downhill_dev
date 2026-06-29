# Cross-tool playbooks

This repo is driven by **both Claude Code and OpenAI Codex**. Anything reusable —
a skill, generator, command, or template — is authored **once, tool-neutrally**,
as a *playbook* in this directory, then exposed to each tool through a thin
wrapper. This file is the standard (Ticket 0.1); `AGENTS.md` carries the
one-paragraph summary, the detail lives here.

## The pattern

Three parts:

1. **Canonical playbook — `docs/playbooks/<name>.md`.** The real procedure,
   checklist, or template. Written in terms of *actions* ("read a file", "create
   a file", "run a command"), never one tool's API or tool-call syntax. Single
   source of truth — all logic lives here.
2. **Per-tool wrappers.** A thin skill per tool whose body only defers to the
   playbook. Both tools use the **identical `SKILL.md` format** — YAML
   frontmatter (`name`, `description`) plus a one-line body — and differ *only*
   in which directory the file lives in:

   | Tool | Wrapper path | Invoked as |
   |------|--------------|-----------|
   | Claude Code | `.claude/skills/<name>/SKILL.md` | `/<name>` |
   | OpenAI Codex | `.agents/skills/<name>/SKILL.md` | `/skills` → pick `<name>`, or implicitly by description |
3. **Contract — `AGENTS.md`.** The "Agent skills & commands (cross-tool)" section
   states the rule so every session (and human) knows playbooks are the source of
   truth and where wrappers live.

```
            docs/playbooks/<name>.md          ← procedure (write once)
              ▲                  ▲
              │ defers           │ defers
   .claude/skills/<name>/   .agents/skills/<name>/
        SKILL.md (Claude)        SKILL.md (Codex)
```

## Why skills for both (not custom prompts or commands)

- **Codex:** repo-committed, shareable reusable instructions are **skills**,
  discovered by scanning `.agents/skills/` from the working directory up to the
  repo root. The older custom-prompt mechanism (`~/.codex/prompts/<name>.md`,
  invoked `/prompts:<name>`) is **home-directory-only and deprecated** — not
  shared through the repo, so we don't use it.
- **Claude Code:** project skills are discovered only under `.claude/skills/` (it
  does **not** read `.agents/skills/`). Slash commands
  (`.claude/commands/<name>.md`) are now equivalent to skills — both produce
  `/<name>` — so we standardize on skills for one consistent pattern.

## Wrapper boilerplate

Both wrapper files are byte-identical apart from their directory. Body = defer to
the playbook; nothing else.

`.claude/skills/<name>/SKILL.md` **and** `.agents/skills/<name>/SKILL.md`:

```markdown
---
name: <name>
description: <one line — what it does and when to use it. Codex uses this to
  decide implicit invocation, so make it specific.>
---

Follow the playbook at `docs/playbooks/<name>.md`: read it and do exactly what it
says.
```

## Conventions

- **`<name>` is kebab-case** and identical across all three files: playbook
  `<name>.md`, and each wrapper directory `<name>/`.
- **Keep wrappers thin.** No procedure text in a wrapper — if you're tempted to
  add a step to a `SKILL.md`, it belongs in the playbook. Duplicated logic is the
  one thing this pattern exists to prevent.
- **Write playbooks in actions, not tool calls.** "Create the file at X", not
  "use the Write tool". Each tool maps the action to its own toolset.
- **Tool-specific behavior is forbidden.** Never add a step only one tool can
  follow. If a step needs a capability the other tool lacks, redesign it.

## Adding a new cross-tool entry

1. Write `docs/playbooks/<name>.md` — the full procedure, tool-neutral.
2. Create `.claude/skills/<name>/SKILL.md` with the boilerplate above.
3. Create `.agents/skills/<name>/SKILL.md` with the same boilerplate.
4. If it's a standing workflow worth advertising, mention it in `AGENTS.md`.
5. Verify (below).

## Verifying an entry

- **Claude Code:** run `/<name>` (reload the session if the skill was just
  added). Confirm it reads the playbook and follows it.
- **Codex:** run it via `/skills` (or let description-based implicit invocation
  pick it). Confirm it resolves the same playbook.
- **Cross-tool limit:** one tool can't drive the other. As with the Unity Test
  Runner, when you can't run a tool yourself, **ask a human to run the other
  tool's side** and report — don't claim both verified from one tool.

## Starting from an example

Copy an existing entry as your template — e.g. `session-self-eval` or
`unity-research` — then reduce its wrappers to the boilerplate above. Each entry
is a `docs/playbooks/<name>.md` plus two byte-identical `SKILL.md` wrappers under
`.claude/skills/<name>/` and `.agents/skills/<name>/`.
