# Gameplay Control Map Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Define the Downhill prototype's gameplay bindings on Unity's Input System and expose them through a thin wrapper so gameplay scripts query named actions without hardcoded key checks.

**Architecture:** A dedicated `DownhillControls.inputactions` asset holds one `Bike` action map. Unity codegen produces a strongly-typed `DownhillControls` class from it. A `PlayerInputReader` MonoBehaviour owns that class, enables/disables the map, and exposes a small queryable surface (polled axes + edge events). The Input scripts live in their own `Downhill.Input` assembly so they can be unit-tested.

**Tech Stack:** Unity 6000.3.17f1 (Unity 6.3), URP 17.3, `com.unity.inputsystem`, `com.unity.test-framework` 1.4.6 (EditMode + PlayMode/`InputTestFixture`).

## Global Constraints

- Unity editor version: **6000.3.17f1**. URP 17.3.
- `ProjectSettings.asset` → `activeInputHandler: 2` (**Both** — keeps legacy `Input.GetAxis` working for the third-party Retro Shaders demo scripts).
- Namespace for all new runtime code: **`Downhill.Input`**.
- Action map name: **`Bike`**. Asset name: **`DownhillControls`**.
- Stable action names, exactly: **`PedalLeft`, `PedalRight`, `FrontBrake`, `RearBrake`, `Turn`, `Jump`, `Freelook`**.
- Control scheme names, exactly: **`Keyboard&Mouse`** and **`Gamepad`**.
- **Do NOT run `git commit`, `git add`, or `git push`.** The user commits manually and gates all history. Every task ends at its verification step — no commit step.
- Do not touch the stock `Assets/InputSystem_Actions.inputactions` or `PFB_Player.prefab` (the prefab is Ticket 1.2).
- The Unity editor cannot run headless in the planning environment; the executor runs tests via the Unity Test Runner window or the batchmode CLI shown in each task.

---

### Task 1: Install Input System package and set the active input handler

**Files:**
- Modify: `Packages/manifest.json`
- Modify: `ProjectSettings/ProjectSettings.asset` (`activeInputHandler`)

**Interfaces:**
- Consumes: nothing.
- Produces: the `Unity.InputSystem` assembly and `UnityEngine.InputSystem` namespace, available to all later tasks.

- [ ] **Step 1: Install the package via Package Manager**

In the Unity editor: `Window > Package Manager > Unity Registry`, find **Input System**, click **Install**. When Unity prompts "This project is using the old Input Manager… enable the new one?", choose **Both** (or **Yes** then fix the handler in Step 2). Unity restarts the editor.

- [ ] **Step 2: Verify the package landed in the manifest**

Confirm `Packages/manifest.json` now contains a line like:

```json
"com.unity.inputsystem": "1.14.2",
```

(The exact patch version is whatever Package Manager resolved as verified for Unity 6.3 — accept it.)

- [ ] **Step 3: Verify / set the active input handler to Both**

Open `Edit > Project Settings > Player > Other Settings > Active Input Handling` and set it to **Both**. Confirm `ProjectSettings/ProjectSettings.asset` contains:

```
  activeInputHandler: 2
```

- [ ] **Step 4: Verify the project compiles**

In the editor, confirm the Console shows no compile errors after the restart. The Retro Shaders demo scripts (which use legacy `Input`) must still compile — that is the point of handler `2`.

Expected: clean Console, Input System menu items present under `Assets > Create > Input Actions`.

- [ ] **Step 5: Stop for review (no commit).**

---

### Task 2: EditMode test asmdef + failing asset-structure test

This task writes the test that pins down the asset's shape. It fails now because the asset does not exist yet; Task 3 makes it pass.

**Files:**
- Create: `Assets/Tests/EditMode/Downhill.Tests.EditMode.asmdef`
- Create: `Assets/Tests/EditMode/DownhillControlsAssetTests.cs`

**Interfaces:**
- Consumes: `Unity.InputSystem` (Task 1). Loads the asset by path `Assets/Scripts/Input/DownhillControls.inputactions` (created in Task 3).
- Produces: nothing other tasks consume.

- [ ] **Step 1: Create the EditMode test assembly definition**

Create `Assets/Tests/EditMode/Downhill.Tests.EditMode.asmdef`:

```json
{
    "name": "Downhill.Tests.EditMode",
    "rootNamespace": "",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Unity.InputSystem"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Write the failing test**

Create `Assets/Tests/EditMode/DownhillControlsAssetTests.cs`:

```csharp
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

public class DownhillControlsAssetTests
{
    const string AssetPath = "Assets/Scripts/Input/DownhillControls.inputactions";

    InputActionAsset _asset;

    [SetUp]
    public void SetUp()
    {
        _asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
        Assert.IsNotNull(_asset, $"Input actions asset not found at {AssetPath}");
    }

    [Test]
    public void BikeMap_Exists()
    {
        Assert.IsNotNull(_asset.FindActionMap("Bike"), "Action map 'Bike' missing");
    }

    [TestCase("PedalLeft")]
    [TestCase("PedalRight")]
    [TestCase("FrontBrake")]
    [TestCase("RearBrake")]
    [TestCase("Turn")]
    [TestCase("Jump")]
    [TestCase("Freelook")]
    public void Action_Exists(string actionName)
    {
        var map = _asset.FindActionMap("Bike");
        Assert.IsNotNull(map.FindAction(actionName), $"Action '{actionName}' missing");
    }

    [TestCase("Keyboard&Mouse")]
    [TestCase("Gamepad")]
    public void ControlScheme_Exists(string scheme)
    {
        Assert.IsTrue(_asset.controlSchemes.Any(s => s.name == scheme),
            $"Control scheme '{scheme}' missing");
    }

    [Test]
    public void EveryAction_HasKeyboardAndGamepadBindings()
    {
        var map = _asset.FindActionMap("Bike");
        foreach (var action in map.actions)
        {
            bool kbm = action.bindings.Any(b => b.groups != null && b.groups.Contains("Keyboard&Mouse"));
            bool pad = action.bindings.Any(b => b.groups != null && b.groups.Contains("Gamepad"));
            Assert.IsTrue(kbm, $"'{action.name}' has no Keyboard&Mouse binding");
            Assert.IsTrue(pad, $"'{action.name}' has no Gamepad binding");
        }
    }
}
```

- [ ] **Step 3: Run the test and verify it FAILS**

Test Runner window (`Window > General > Test Runner > EditMode > Run All`), or batchmode:

```bash
"<UnityEditorPath>/Unity" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results-editmode.xml
```

Expected: FAIL — every test errors in `SetUp` with "Input actions asset not found at Assets/Scripts/Input/DownhillControls.inputactions".

- [ ] **Step 4: Stop for review (no commit).**

---

### Task 3: Create the Input assembly, the actions asset, and the generated class

**Files:**
- Create: `Assets/Scripts/Input/Downhill.Input.asmdef`
- Create: `Assets/Scripts/Input/DownhillControls.inputactions`
- Generated by Unity: `Assets/Scripts/Input/DownhillControls.cs`

**Interfaces:**
- Consumes: `Unity.InputSystem` (Task 1).
- Produces: the `Downhill.Input` assembly; the `InputActionAsset` at the path Task 2 loads; the generated `Downhill.Input.DownhillControls` class with a `Bike` accessor exposing `PedalLeft`, `PedalRight`, `FrontBrake`, `RearBrake`, `Turn`, `Jump`, `Freelook` `InputAction`s, and `Enable()/Disable()/Dispose()`. Task 4 consumes this class.

- [ ] **Step 1: Create the runtime assembly definition**

Create `Assets/Scripts/Input/Downhill.Input.asmdef`:

```json
{
    "name": "Downhill.Input",
    "rootNamespace": "Downhill.Input",
    "references": ["Unity.InputSystem"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

(`autoReferenced: true` lets future gameplay scripts in `Assembly-CSharp` use `PlayerInputReader` without extra wiring.)

- [ ] **Step 2: Create the actions asset file**

Create `Assets/Scripts/Input/DownhillControls.inputactions` with exactly this content:

```json
{
    "name": "DownhillControls",
    "maps": [
        {
            "name": "Bike",
            "id": "a1111111-1111-4111-8111-111111111111",
            "actions": [
                { "name": "PedalLeft",  "type": "Button", "id": "a2000001-0000-4000-8000-000000000001", "expectedControlType": "Button",  "processors": "", "interactions": "", "initialStateCheck": false },
                { "name": "PedalRight", "type": "Button", "id": "a2000002-0000-4000-8000-000000000002", "expectedControlType": "Button",  "processors": "", "interactions": "", "initialStateCheck": false },
                { "name": "FrontBrake", "type": "Value",  "id": "a2000003-0000-4000-8000-000000000003", "expectedControlType": "Axis",    "processors": "", "interactions": "", "initialStateCheck": true },
                { "name": "RearBrake",  "type": "Value",  "id": "a2000004-0000-4000-8000-000000000004", "expectedControlType": "Axis",    "processors": "", "interactions": "", "initialStateCheck": true },
                { "name": "Turn",       "type": "Value",  "id": "a2000005-0000-4000-8000-000000000005", "expectedControlType": "Axis",    "processors": "", "interactions": "", "initialStateCheck": true },
                { "name": "Jump",       "type": "Button", "id": "a2000006-0000-4000-8000-000000000006", "expectedControlType": "Button",  "processors": "", "interactions": "", "initialStateCheck": false },
                { "name": "Freelook",   "type": "Value",  "id": "a2000007-0000-4000-8000-000000000007", "expectedControlType": "Vector2", "processors": "", "interactions": "", "initialStateCheck": true }
            ],
            "bindings": [
                { "name": "", "id": "b0000001-0000-4000-8000-000000000001", "path": "<Keyboard>/leftArrow",  "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "PedalLeft",  "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "b0000002-0000-4000-8000-000000000002", "path": "<Gamepad>/leftShoulder", "interactions": "", "processors": "", "groups": "Gamepad",        "action": "PedalLeft",  "isComposite": false, "isPartOfComposite": false },

                { "name": "", "id": "b0000003-0000-4000-8000-000000000003", "path": "<Keyboard>/rightArrow", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "PedalRight", "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "b0000004-0000-4000-8000-000000000004", "path": "<Gamepad>/rightShoulder","interactions": "", "processors": "", "groups": "Gamepad",        "action": "PedalRight", "isComposite": false, "isPartOfComposite": false },

                { "name": "", "id": "b0000005-0000-4000-8000-000000000005", "path": "<Mouse>/rightButton",   "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "FrontBrake", "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "b0000006-0000-4000-8000-000000000006", "path": "<Gamepad>/rightTrigger","interactions": "", "processors": "", "groups": "Gamepad",        "action": "FrontBrake", "isComposite": false, "isPartOfComposite": false },

                { "name": "", "id": "b0000007-0000-4000-8000-000000000007", "path": "<Keyboard>/leftShift",  "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "RearBrake",  "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "b0000008-0000-4000-8000-000000000008", "path": "<Gamepad>/leftTrigger", "interactions": "", "processors": "", "groups": "Gamepad",        "action": "RearBrake",  "isComposite": false, "isPartOfComposite": false },

                { "name": "1D Axis", "id": "b0000009-0000-4000-8000-000000000009", "path": "1DAxis", "interactions": "", "processors": "", "groups": "", "action": "Turn", "isComposite": true, "isPartOfComposite": false },
                { "name": "negative", "id": "b000000a-0000-4000-8000-00000000000a", "path": "<Keyboard>/a", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Turn", "isComposite": false, "isPartOfComposite": true },
                { "name": "positive", "id": "b000000b-0000-4000-8000-00000000000b", "path": "<Keyboard>/d", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Turn", "isComposite": false, "isPartOfComposite": true },
                { "name": "", "id": "b000000c-0000-4000-8000-00000000000c", "path": "<Gamepad>/leftStick/x", "interactions": "", "processors": "", "groups": "Gamepad", "action": "Turn", "isComposite": false, "isPartOfComposite": false },

                { "name": "", "id": "b000000d-0000-4000-8000-00000000000d", "path": "<Keyboard>/space",     "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Jump", "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "b000000e-0000-4000-8000-00000000000e", "path": "<Gamepad>/buttonSouth","interactions": "", "processors": "", "groups": "Gamepad",        "action": "Jump", "isComposite": false, "isPartOfComposite": false },

                { "name": "", "id": "b000000f-0000-4000-8000-00000000000f", "path": "<Mouse>/delta",        "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Freelook", "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "b0000010-0000-4000-8000-000000000010", "path": "<Gamepad>/rightStick", "interactions": "", "processors": "", "groups": "Gamepad",        "action": "Freelook", "isComposite": false, "isPartOfComposite": false }
            ]
        }
    ],
    "controlSchemes": [
        {
            "name": "Keyboard&Mouse",
            "bindingGroup": "Keyboard&Mouse",
            "devices": [
                { "devicePath": "<Keyboard>", "isOptional": false, "isOR": false },
                { "devicePath": "<Mouse>",    "isOptional": false, "isOR": false }
            ]
        },
        {
            "name": "Gamepad",
            "bindingGroup": "Gamepad",
            "devices": [
                { "devicePath": "<Gamepad>", "isOptional": false, "isOR": false }
            ]
        }
    ]
}
```

- [ ] **Step 3: Enable C# class generation on the asset**

In Unity, select `DownhillControls.inputactions` in the Project window. In the Inspector, tick **Generate C# Class**, then set:
- **C# Class File:** `Assets/Scripts/Input/DownhillControls.cs`
- **C# Class Name:** `DownhillControls`
- **C# Class Namespace:** `Downhill.Input`

Click **Apply**. Unity writes `DownhillControls.cs` and its `.meta`. (Letting Unity write the asset's `.meta` this way avoids hand-editing importer GUIDs.)

- [ ] **Step 4: Confirm the generated class compiles**

Console shows no errors. Confirm `Assets/Scripts/Input/DownhillControls.cs` exists and declares `namespace Downhill.Input` with a `public partial class DownhillControls` and a `Bike` accessor.

- [ ] **Step 5: Run the Task 2 EditMode tests and verify they PASS**

```bash
"<UnityEditorPath>/Unity" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results-editmode.xml
```

Expected: PASS — `BikeMap_Exists`, all seven `Action_Exists` cases, both `ControlScheme_Exists` cases, and `EveryAction_HasKeyboardAndGamepadBindings` all green.

- [ ] **Step 6: Stop for review (no commit).**

---

### Task 4: PlayerInputReader wrapper + PlayMode test

**Files:**
- Create: `Assets/Tests/PlayMode/Downhill.Tests.PlayMode.asmdef`
- Create: `Assets/Tests/PlayMode/PlayerInputReaderTests.cs`
- Create: `Assets/Scripts/Input/PlayerInputReader.cs`

**Interfaces:**
- Consumes: `Downhill.Input.DownhillControls` (Task 3) — its `Bike` accessor and the seven `InputAction`s.
- Produces: `Downhill.Input.PlayerInputReader : MonoBehaviour` with:
  - polled props `float Turn`, `float FrontBrake`, `float RearBrake`, `Vector2 Freelook`, `bool JumpedThisFrame`;
  - events `event System.Action PedalLeftPressed`, `PedalRightPressed`, `Jumped`.
  This is the surface every later gameplay ticket reads input through.

- [ ] **Step 1: Create the PlayMode test assembly definition**

Create `Assets/Tests/PlayMode/Downhill.Tests.PlayMode.asmdef`:

```json
{
    "name": "Downhill.Tests.PlayMode",
    "rootNamespace": "",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Unity.InputSystem",
        "Unity.InputSystem.TestFramework",
        "Downhill.Input"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Write the failing test**

Create `Assets/Tests/PlayMode/PlayerInputReaderTests.cs`:

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using Downhill.Input;

public class PlayerInputReaderTests : InputTestFixture
{
    [UnityTest]
    public IEnumerator Turn_ReflectsGamepadLeftStickX()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var go = new GameObject("reader");
        var reader = go.AddComponent<PlayerInputReader>();
        yield return null; // Awake + OnEnable run

        Set(gamepad.leftStick, new Vector2(1f, 0f));
        yield return null; // Update reads the value

        Assert.Greater(reader.Turn, 0.5f, "Turn should follow left stick X deflection");
        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator PedalLeftPressed_FiresOnLeftShoulder()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var go = new GameObject("reader");
        var reader = go.AddComponent<PlayerInputReader>();
        bool fired = false;
        reader.PedalLeftPressed += () => fired = true;
        yield return null;

        Press(gamepad.leftShoulder);
        yield return null;

        Assert.IsTrue(fired, "PedalLeftPressed should fire when the left bumper is pressed");
        Object.Destroy(go);
    }
}
```

- [ ] **Step 3: Run the test and verify it FAILS**

Expected: compile error / test failure — `PlayerInputReader` does not exist yet (`The type or namespace name 'PlayerInputReader' could not be found`).

- [ ] **Step 4: Implement PlayerInputReader**

Create `Assets/Scripts/Input/PlayerInputReader.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Downhill.Input
{
    /// Thin wrapper over the generated DownhillControls asset. Gameplay reads input
    /// through this component and never touches raw keys or InputActions directly.
    public class PlayerInputReader : MonoBehaviour
    {
        DownhillControls _controls;

        public float Turn { get; private set; }
        public float FrontBrake { get; private set; }
        public float RearBrake { get; private set; }
        public Vector2 Freelook { get; private set; }
        public bool JumpedThisFrame { get; private set; }

        public event Action PedalLeftPressed;
        public event Action PedalRightPressed;
        public event Action Jumped;

        void Awake() => _controls = new DownhillControls();

        void OnEnable()
        {
            var bike = _controls.Bike;
            bike.Enable();
            bike.PedalLeft.performed += OnPedalLeft;
            bike.PedalRight.performed += OnPedalRight;
            bike.Jump.performed += OnJump;
        }

        void OnDisable()
        {
            var bike = _controls.Bike;
            bike.PedalLeft.performed -= OnPedalLeft;
            bike.PedalRight.performed -= OnPedalRight;
            bike.Jump.performed -= OnJump;
            bike.Disable();
        }

        void OnDestroy() => _controls?.Dispose();

        void Update()
        {
            var bike = _controls.Bike;
            Turn = bike.Turn.ReadValue<float>();
            FrontBrake = bike.FrontBrake.ReadValue<float>();
            RearBrake = bike.RearBrake.ReadValue<float>();
            Freelook = bike.Freelook.ReadValue<Vector2>();
            JumpedThisFrame = bike.Jump.WasPerformedThisFrame();
        }

        void OnPedalLeft(InputAction.CallbackContext _) => PedalLeftPressed?.Invoke();
        void OnPedalRight(InputAction.CallbackContext _) => PedalRightPressed?.Invoke();
        void OnJump(InputAction.CallbackContext _) => Jumped?.Invoke();
    }
}
```

- [ ] **Step 5: Run the PlayMode tests and verify they PASS**

Test Runner window (`PlayMode > Run All`), or batchmode:

```bash
"<UnityEditorPath>/Unity" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults results-playmode.xml
```

Expected: PASS — `Turn_ReflectsGamepadLeftStickX` and `PedalLeftPressed_FiresOnLeftShoulder` both green.

- [ ] **Step 6: Re-run the full suite (EditMode + PlayMode) and confirm green**

Expected: all tests from Task 2 and Task 4 pass; Console has no errors.

- [ ] **Step 7: Stop for review (no commit).**

---

## Acceptance criteria (Ticket 1.1) → coverage

- Inputs for pedal left, pedal right, front brake, rear brake, turn, jump, freelook defined → Task 3 asset, verified by Task 2 tests.
- Gameplay scripts can query named actions without hardcoded key checks → `PlayerInputReader`, Task 4.
- Keyboard and controller mappings both exist → Task 3 bindings, verified by `EveryAction_HasKeyboardAndGamepadBindings` (Task 2).

## Notes for the executor

- If batchmode CLI is unavailable, use the in-editor Test Runner window — the pass/fail expectations are identical.
- `InputTestFixture` saves and restores Input System state around each test, so the synthetic devices added in PlayMode tests do not leak into the editor session.
- Default keybinds are intentionally rough (ticket: "prefer stable action names over perfect keybinds"). Action names and scheme names above are the stable contract; bindings can be retuned later without touching `PlayerInputReader`.
