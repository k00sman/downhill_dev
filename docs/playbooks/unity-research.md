# Playbook: unity-research

**Purpose:** Explore a Unity codebase feature, folder, or script and return a structured summary — files involved, their responsibilities, key patterns and abstractions, data flow, dependencies, edge cases, and a lightweight acceptance-test idea list. Does not produce raw file dumps.

## When to use

Before writing or refactoring any Unity code. Use whenever you need to understand a subsystem, confirm which files own what, map how types talk to each other, or produce the research artifact for a ticket's implementation spec.

## Inputs to gather

Ask for these before starting if they were not provided:

- **Target** — a feature name, script file, folder path (e.g. `Assets/Scripts/Input/`), or ticket reference.
- **Depth** — broad survey (whole subsystem) or focused deep-dive (one or two files)?

## Procedure

1. **Read `docs/codemap.md` first.** It is a Mermaid class diagram of all gameplay types, their fields, public methods, and cross-type dependencies. Start here — it is faster than grepping the full `Assets/Scripts/` tree. Only grep or open individual files after you know from the codemap which ones are relevant.

2. **Identify relevant files.** Using the codemap, note the types that the target touches. List the file paths before opening them.

3. **Read the relevant files.** Read each identified file in full. Do not skip fields, attributes, or `[SerializeField]` annotations — these are often load-bearing for both behavior and Unity serialization.

4. **Produce a structured summary.** Return exactly the following sections, in order. Omit a section only if it is genuinely empty (e.g. no external dependencies). Do not dump raw file content.

   ### Files and responsibilities
   One paragraph per file: path, the single responsibility it owns, what it does *not* own (so the boundary is clear).

   ### Key patterns and abstractions
   Patterns visible across the files: component composition, event/delegate wiring, ScriptableObject use, state machines, coroutines, physics queries, input pipeline shape, etc. Name the pattern and say where it appears.

   ### Data flow
   How data moves through the target. Start at the entry point (e.g. `Update`, an event callback, an external call) and trace through to outputs (Rigidbody changes, UI updates, event fires). A numbered list is fine.

   ### Dependencies and cross-type links
   External types, assemblies, or Unity systems the target depends on. Include assembly definition boundaries if they matter. Note which dependencies are optional (null-checked) vs. required.

   ### Edge cases and gotchas
   Prototype-relevant risks: serialization traps (fields that must stay `public` for Unity to serialize them, `[SerializeField] private` conventions), physics ordering issues, `OnEnable`/`OnDisable` lifecycle assumptions, vendor code that must not be touched (`Assets/PathCreator/`, `Assets/Retro Shaders Pro/`, `Assets/Adrift/`, and other third-party folders), Unity version quirks, Input System "Both" mode implications.

   ### Acceptance-test ideas
   A short bulleted list of lightweight test ideas — EditMode unit tests for pure logic, PlayMode smoke tests for grounded behavior, or manual in-Editor checks. Keep it prototype-oriented: prefer tests that catch regressions quickly, not exhaustive coverage.

## Output format

Plain markdown, the six sections above. Target length: enough to brief a coding agent without it re-reading the source files. Avoid padding. No raw code blocks unless a specific snippet is genuinely load-bearing to understand a pattern.

## Done check

Output is complete when:
- Every file touched by the target has an entry in "Files and responsibilities."
- Data flow is traceable end-to-end without opening a source file.
- Edge cases include at least one Unity-specific risk (serialization, lifecycle, or vendor boundary).
- Acceptance-test ideas include at least one EditMode and one PlayMode (or manual check) entry.
