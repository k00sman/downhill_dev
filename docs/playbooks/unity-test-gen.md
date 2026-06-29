# Playbook: unity-test-gen

**Purpose:** Scaffold EditMode or PlayMode NUnit tests for a target class or
behavior in the Downhill Unity project, encoding the project's actual assembly
layout, naming conventions, and input-testing patterns.

## When to use

When a developer needs a starter test file for a class or behavior and wants it
to land in the correct assembly with the right patterns already in place — not a
blank file they must research from scratch.

## Inputs to gather

Before generating anything, confirm:

1. **Target class** — the C# class name under test (e.g. `BikeMovementModel`,
   `PlayerInputReader`).
2. **Behavior to cover** — one or more specific scenarios in plain language
   (e.g. "speed stays below maxSpeed after many downhill steps").
3. **Assembly reference** — which game assembly the class lives in
   (`Downhill.Player`, `Downhill.Input`, or other). Check the existing
   `.asmdef` files if unsure.

If any of these are missing, ask before proceeding.

## Choosing EditMode vs PlayMode

| Situation | Assembly |
|-----------|----------|
| Pure C# logic / math (no `MonoBehaviour`, no `GameObject`, no physics, no coroutines, no input) | **EditMode** — `Assets/Tests/EditMode/` |
| Needs `MonoBehaviour`, `GameObject`, physics, coroutines, frame stepping, or Unity Input System | **PlayMode** — `Assets/Tests/PlayMode/` |

**Decision rule:** if the class under test can be instantiated with `new` and
tested with `Assert` in a plain method, it is EditMode. If it needs a
`GameObject` or must run across frames, it is PlayMode.

## Asmdef conventions (match exactly)

**EditMode** (`Assets/Tests/EditMode/Downhill.Tests.EditMode.asmdef`):
- `"includePlatforms": ["Editor"]`
- `"references"`: `UnityEngine.TestRunner`, `UnityEditor.TestRunner`,
  `Unity.InputSystem`, `Downhill.Input`, `Downhill.Player`
- `"overrideReferences": true`, `"precompiledReferences": ["nunit.framework.dll"]`
- `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`

**PlayMode** (`Assets/Tests/PlayMode/Downhill.Tests.PlayMode.asmdef`):
- `"includePlatforms": []` (all platforms)
- Same references as EditMode **plus** `Unity.InputSystem.TestFramework`
- Same `overrideReferences`, `precompiledReferences`, `defineConstraints`

If you add a new game assembly reference, add it to both `.asmdef` files as
appropriate and note this in the output.

## Templates

### EditMode template

Pure-math or pure-logic class; instantiate with `new`; use `[Test]`.

```csharp
using <GameAssembly>;          // e.g. Downhill.Player
using NUnit.Framework;
using UnityEngine;

public class <ClassName>Tests
{
    private static <ClassName> Make<ClassName>()
    {
        return new()
        {
            // set known, simple values so expected outcomes are obvious
        };
    }

    [Test]
    public void <Method>_<Scenario>_<Expected>()
    {
        // Arrange
        <ClassName> subject = Make<ClassName>();

        // Act
        var result = subject.<Method>(<args>);

        // Assert
        Assert.<Assertion>(result, <expected>);
    }
}
```

**Naming:** `Method_Scenario_Expected` (e.g. `Step_Downhill_NoPedal_SpeedsUp`).
**Assertions:** match the style in `BikeMovementModelTests.cs` — prefer
`Assert.Greater`, `Assert.Less`, `Assert.LessOrEqual`, `Assert.AreEqual` with
an optional delta, and a quoted failure message when the intent isn't obvious.

### PlayMode template

Runtime behavior needing `MonoBehaviour`, `GameObject`, or Unity Input System;
derive from `InputTestFixture`; use `[UnityTest]` returning `IEnumerator`.

```csharp
using System.Collections;
using <GameAssembly>;          // e.g. Downhill.Input
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

public class <ClassName>Tests : InputTestFixture
{
    [UnityTest]
    public IEnumerator <Method>_<Scenario>_<Expected>()
    {
        // Arrange — create a virtual device via InputTestFixture
        Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
        GameObject go = new("<descriptive-name>");
        <ClassName> subject = go.AddComponent<<ClassName>>();
        yield return null; // Awake + OnEnable run

        // Act — queue input and pump a frame
        Set(gamepad.<control>, <value>);   // or Press(gamepad.<button>)
        yield return null;                 // or new WaitForFixedUpdate()

        // Assert
        Assert.<Assertion>(subject.<Property>, <expected>, "<failure message>");

        Object.Destroy(go);
    }
}
```

**`InputTestFixture` setup pattern** (from `PlayerInputReaderTests.cs`):
1. Derive from `InputTestFixture` — the base class handles device registration
   and cleanup between tests.
2. Call `InputSystem.AddDevice<Gamepad>()` (or other device type) inside the
   test — not in `[SetUp]`, so each test gets a fresh device.
3. Add the component under test to a new `GameObject` after creating the device.
4. `yield return null` once to let `Awake` and `OnEnable` run.
5. Use `Set(device.control, value)` for axis/stick input or
   `Press(device.button)` / `Release(device.button)` for buttons.
6. `yield return null` again to let the component read the input in `Update`.
7. Assert, then `Object.Destroy(go)` at the end to clean up the scene.

**Frame stepping:** use `yield return null` for one Update tick;
`yield return new WaitForFixedUpdate()` when physics or FixedUpdate is involved.

## Output & reminders

Produce the complete `.cs` file content, ready to save at the correct path under
`Assets/Tests/EditMode/` or `Assets/Tests/PlayMode/`.

Remind the user of the following every time:

1. **Run the Unity Test Runner** (Window → General → Test Runner) and execute
   both EditMode and PlayMode suites. Do not claim tests pass until a human
   reports the results — tests cannot be run here.
2. **Commit the `.meta` file.** After opening the project in the Editor, Unity
   generates a `.meta` for every new `.cs` file. Commit both `<TestFile>.cs`
   and `<TestFile>.cs.meta` — omitting the `.meta` causes GUID conflicts for
   other contributors.
3. If you added a new game assembly reference to an `.asmdef`, commit that
   `.asmdef` and its `.meta` as well.

## Done check

- [ ] Target class and behavior confirmed before generating.
- [ ] Correct assembly chosen (EditMode for pure logic, PlayMode for runtime).
- [ ] EditMode test uses `[Test]` and plain `new`; no `MonoBehaviour`.
- [ ] PlayMode test derives from `InputTestFixture`, uses `[UnityTest]`,
      `IEnumerator`, and the `Set`/`Press` + `yield return null` pattern.
- [ ] Method names follow `Method_Scenario_Expected`.
- [ ] User reminded to run the Unity Test Runner and commit the `.meta`.
