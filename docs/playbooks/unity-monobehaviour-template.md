# Playbook: unity-monobehaviour-template

**Purpose:** Given a script name and responsibility description, generate a
convention-compliant MonoBehaviour for the Downhill repo. The output encodes
every established C# and Unity convention found in the codebase — namespace,
field layout, lifecycle order, validation, event wiring — so new scripts are
indistinguishable in style from the canonical examples.

## When to use

Use this skill whenever adding a new MonoBehaviour to `Assets/Scripts/`. Do not
use it to modify an existing script — edit those directly.

## Inputs to gather

Before generating, confirm these with the user if not provided:

1. **Script name** — the class name (PascalCase, no `MonoBehaviour` suffix).
2. **Responsibility** — one sentence: what does this component own at runtime?
3. **Target folder** — which `Assets/Scripts/<Area>/` folder does it live in?
   If the user is unsure, propose one based on the responsibility and list the
   existing areas (`Player`, `Input`, …).
4. **Serialized dependencies** — what component or asset references does it
   need? (e.g. `Rigidbody`, `PlayerInputReader`, a ScriptableObject config)
5. **Events to subscribe** — does it listen to any C# events or Unity messages
   on other components?
6. **Hard component deps** — which components must be on the same GameObject?
   (drives `[RequireComponent]`)

Do not guess. If a piece of information would materially change the output,
ask.

## Conventions to honor

**Namespace**
The namespace equals the `rootNamespace` of the asmdef in the target folder.
Example: scripts under `Assets/Scripts/Player/` use `Downhill.Player` because
`Downhill.Player.asmdef` declares `"rootNamespace": "Downhill.Player"`.
If the target folder has no asmdef yet, propose one and derive the namespace
from `Downhill.<Area>`.

**Braces and indent**
Allman braces (opening `{` on its own line) everywhere. 4-space indent. LF
line endings. Final newline. (Matches `.editorconfig`.)

**Block-scoped namespace declaration** (`namespace Foo { ... }` wrapping the
class, not file-scoped `namespace Foo;`). `.editorconfig` enforces this.

**Serialized fields**
- Always `[SerializeField] private <Type> _camelCase;` — underscore-prefixed
  camelCase. Never `public` for inspector-driven fields unless Unity itself
  requires it (rare).
- Group by concern under `[Header("...")]` blocks. Put component references
  first ("References"), then tunables grouped by sub-system.
- Public serialized fields (discouraged, only when a third-party API demands
  it) use plain camelCase with no underscore — but prefer `[SerializeField]
  private` in all new code.

**Public read access**
- Component and Transform refs: expression-bodied properties —
  `public Rigidbody Body => _body;`
- Computed/mutable state: auto-properties with private setter —
  `public BikeState State { get; private set; }`
- Never expose raw `_camelCase` fields publicly.

**Lifecycle method visibility**
All Unity messages (`Awake`, `OnEnable`, `OnDisable`, `OnValidate`, `Update`,
`FixedUpdate`, `LateUpdate`, `OnDestroy`, `OnTriggerEnter`, etc.) must be
`private`. Unity calls them via reflection; `public`/`protected` adds nothing
and the linter will flag empty public Unity messages.

**Required components**
Declare hard deps with `[RequireComponent(typeof(...))]` above the class.
In `OnValidate()`, auto-wire those refs with `GetComponent<>()` so the
Inspector is never left blank by accident.

**Ref validation in Awake**
For every `[SerializeField]` ref that must be non-null at runtime, call a
guard in `Awake()`:

```csharp
if (reference == null)
{
    Debug.LogError($"{nameof(ClassName)}: '{nameof(_field)}' reference is not wired.", this);
}
```

Use `nameof()` for both class and field — avoids stale strings on rename.

**Event subscription**
Subscribe in `OnEnable`, unsubscribe in `OnDisable`. Always guard with a null
check before subscribing/unsubscribing so `OnDisable` is safe when `Awake`
hasn't completed cleanly:

```csharp
private void OnEnable()
{
    if (_dependency == null) { return; }
    _dependency.SomeEvent += OnSomeEvent;
}

private void OnDisable()
{
    if (_dependency == null) { return; }
    _dependency.SomeEvent -= OnSomeEvent;
}
```

**Update vs FixedUpdate**
- `Update()`: lightweight polling only (read input values, update UI). No
  physics, no allocations.
- `FixedUpdate()`: all Rigidbody writes and physics queries.
- Heavy logic (state machines, pathfinding, coroutine-driven sequences) lives
  outside both — prefer events or `StartCoroutine` triggered from a message.
- If `Update()` would be empty, omit it entirely (the linter flags empty Unity
  messages as errors).

**No constructors**
MonoBehaviours must not define constructors. Use `Awake` for initialization.

**Explicit types over var**
`.editorconfig` enforces `csharp_style_var_*= false:warning` — always write
the full type, never `var`.

**Accessibility modifiers**
Every member needs an explicit access modifier (`private`, `public`, etc.).
`dotnet_style_require_accessibility_modifiers = always:warning` is enforced.

**Braces always**
`csharp_prefer_braces = true:warning` — single-statement `if`/`for`/`foreach`
bodies still require `{ }`.

**asmdef placement**
New gameplay scripts live under `Assets/Scripts/<Area>/` alongside an asmdef.
If an asmdef already exists for the target area, use it. If not, propose
`Downhill.<Area>.asmdef` with `"rootNamespace": "Downhill.<Area>"` and ask
before creating it.

**.meta files**
Unity generates a `.meta` file for every asset in `Assets/`, including `.cs`
scripts. The skill cannot generate the GUID inside `.meta`. After writing the
`.cs` file, **open or save it in the Unity Editor** — Unity creates the
`.meta` automatically. Both the `.cs` and its `.meta` must be committed
together. The skill must include this reminder in its output.

## Template

The skeleton below uses `YourComponent` as a placeholder. Replace it and fill
in actual fields, `[Header]` groups, `[RequireComponent]` lines, and lifecycle
bodies to match the gathered inputs. Omit any lifecycle method whose body would
be empty.

```csharp
using UnityEngine;

namespace Downhill.Area  // replace with target asmdef rootNamespace
{
    /// <One-sentence summary of what this component owns at runtime.>
    [RequireComponent(typeof(Rigidbody))]  // list hard deps; remove if none
    public class YourComponent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody _body;
        // add other component refs here

        [Header("Settings")]
        [SerializeField] private float _speed = 5f;
        // add tuning fields grouped by concern; add more [Header] blocks as needed

        public Rigidbody Body => _body;

        public SomeState State { get; private set; }

        private void OnValidate()
        {
            if (_body == null)
            {
                _body = GetComponent<Rigidbody>();
            }
        }

        private void Awake()
        {
            if (_body == null)
            {
                Debug.LogError($"{nameof(YourComponent)}: '{nameof(_body)}' reference is not wired.", this);
            }
        }

        private void OnEnable()
        {
            // subscribe to events; guard with null check first
        }

        private void OnDisable()
        {
            // mirror unsubscribe
        }

        private void FixedUpdate()
        {
            // physics writes go here
        }
    }
}
```

## Output and reminders

Produce:
1. The complete `.cs` file content, ready to save to
   `Assets/Scripts/<Area>/<ClassName>.cs`.
2. A reminder block after the code:

> **After saving:** open or re-save the file in the Unity Editor so it
> generates `<ClassName>.cs.meta`. Commit **both** files together — the `.cs`
> and the `.meta`.

If a new asmdef is needed, produce its JSON content as a second code block and
include the same `.meta` reminder for it.

## Done check

Before declaring done, confirm:
- [ ] Namespace matches the target folder's asmdef `rootNamespace`.
- [ ] All `[SerializeField]` fields are `private` with `_camelCase` naming.
- [ ] Fields are grouped under `[Header("...")]` by concern.
- [ ] Public read-access is via expression-bodied properties or auto-properties.
- [ ] All Unity lifecycle methods are `private`.
- [ ] `[RequireComponent]` declared for every hard component dep.
- [ ] `OnValidate()` auto-wires refs via `GetComponent<>()`.
- [ ] `Awake()` validates required refs with `Debug.LogError` + `nameof()`.
- [ ] Event subscriptions are in `OnEnable` / `OnDisable` with null guards.
- [ ] No empty Unity message methods.
- [ ] No heavy logic in `Update()`.
- [ ] `.meta` reminder is included in the output.
