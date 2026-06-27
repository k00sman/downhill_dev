# Playbook: unity-refactor

**Purpose:** Apply a focused refactor to an explicit, user-provided file allowlist, returning diffs for review. Does not silently rewrite anything. Respects Unity serialization conventions, assembly boundaries, and vendor code rules.

## When to use

When you have a clear refactor goal — rename, extract, restructure, split responsibility — and an explicit list of files that are in scope. Do not invoke this to add new features; use the ticket implementation flow for that.

## Inputs to gather

Ask for these before starting if they were not provided:

- **Refactor goal** — what specifically is being changed and why (e.g. "extract speed math out of `PlayerBikeController` into `BikeMovementModel`").
- **File allowlist** — an explicit list of file paths the refactor is permitted to touch. If the user did not provide one, ask. Do not infer or expand it.
- **Constraints** — any tuning values, public APIs, or behaviors that must be preserved.

## Procedure

1. **Confirm the allowlist.** State it back to the user before doing any work. If a needed change would require modifying a file not on the allowlist, name that file and stop — do not touch it without explicit approval.

2. **Read `docs/codemap.md`.** Understand how the in-scope files relate to the rest of the codebase before editing. Note any callers or dependents that the refactor might affect (even if those files are not on the allowlist — you need to know if you're breaking a contract).

3. **Read every file on the allowlist in full.** Pay attention to `[SerializeField]` annotations, `public` fields used for Unity serialization, assembly definition membership, and Unity lifecycle methods (`Awake`, `Start`, `OnEnable`, `OnDisable`, `Update`, `FixedUpdate`).

4. **Apply the refactor.** Rules:
   - **Scope:** modify ONLY files on the allowlist. If a change would touch a file outside it, stop and report which file and why.
   - **Vendor code is off-limits unconditionally.** Never touch anything under `Assets/PathCreator/`, `Assets/Retro Shaders Pro/`, `Assets/Adrift/`, or any other third-party folder under `Assets/`. This rule overrides the allowlist — even if a vendor file is listed, refuse and flag it.
   - **No MonoBehaviour constructors.** Unity does not support them; use `Awake`/`Start` for initialization.
   - **Serialization conventions:**
     - Private fields that Unity should serialize: `[SerializeField] private _camelCase` (leading underscore).
     - Public fields used for serialization: stay `camelCase` (no underscore) — this is Unity convention and the `.editorconfig` enforces it.
     - Do not change a `public` field to `private` if Unity serialization depends on it being `public`, unless you simultaneously add `[SerializeField]` and confirm the Inspector value round-trips.
   - **Keep tuning values exposed.** Do not fold `[SerializeField]` Inspector-visible parameters into constants or private literals. They exist so designers can tune without code changes.
   - **Don't redesign.** Implement what was asked. If you see a larger improvement, note it in the output but do not apply it.
   - **Preserve existing public API** unless the refactor goal explicitly changes it. Check callers via `docs/codemap.md` before renaming a public member.

5. **Return diffs, not silent rewrites.** Show each changed file as a unified diff (or equivalent clearly-labelled before/after for small changes). Do not present the refactored file as a done fact — the user reviews the diff and applies it.

6. **Emit post-refactor reminders** at the end of your output:
   - Any new file created under `Assets/` needs a committed `.meta` file — Unity Editor generates it on import; commit both together.
   - Run `./scripts/lint.sh` (Linux/macOS) or `pwsh scripts/lint.ps1` (Windows) after applying the diff.
   - Ask the user to run the Unity Test Runner (EditMode + PlayMode) — the editor cannot run headless here, so you cannot run it yourself.

## Output format

1. **Allowlist confirmation** — the files you will touch, stated back explicitly.
2. **Impact summary** — callers or dependents outside the allowlist that the refactor affects (even read-only), so the user knows what to verify manually.
3. **Diffs** — one per changed file, labeled with the file path.
4. **Post-refactor reminders** — `.meta` notes, lint command, Test Runner request.

## Done check

Output is complete when:
- Every change is presented as a diff (no silent full-file rewrites).
- No file outside the allowlist was modified.
- No vendor code was touched.
- Unity serialization conventions are preserved in every changed file.
- Post-refactor reminders (`.meta`, lint, Test Runner) are included.
