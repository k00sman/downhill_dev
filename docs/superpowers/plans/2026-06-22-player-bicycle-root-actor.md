# Player Bicycle Root Actor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up the `PFB_Player` actor — a dynamic-Rigidbody root with the required child transforms, a wired `PlayerBikeController` shell, and validation so the scene runs with no null-reference errors. No movement logic.

**Architecture:** Root carries `Rigidbody` + `BoxCollider` + `PlayerInputReader` + `PlayerBikeController`. The mesh moves under a new `BikeBody` lean pivot; `CameraPivot`, `GroundCheck`, `RecoveryAnchor` are empty child anchors. The controller holds direct serialized references (auto-wiring same-GameObject components in `OnValidate`, validating all in `Awake`) and exposes a read-only `BikeState` stub.

**Tech Stack:** Unity 6000.3.17f1, URP 17.3, Input System 1.19.0, new assembly `Downhill.Player` (references `Downhill.Input`), Unity Test Framework (EditMode asset tests + PlayMode `[UnityTest]`).

## Global Constraints

- Do **NOT** run `git commit`, `git add`, or `git push` at any point.
- `.meta` files MUST accompany every new file (Unity generates them on first import; the user confirms they appear).
- Do **NOT** modify third-party content (`Retro Shaders Pro`, `Adrift Team`, `PathCreator`, etc.). The `MSH_BikeDefault.fbx` under `Assets/Art` is first-party and is **not** edited here anyway.
- Active Input Handling stays **"Both"** (handler 2) — do not change.
- Gameplay reads input only through `PlayerInputReader`; never raw key checks.
- Prefab restructuring is done **by the user in the Unity editor** via the walkthroughs below (hand-editing prefab YAML risks GUID/fileID breakage). Claude authors only scripts, asmdefs, and tests.
- Tests are run by the **user** in the Unity Test Runner; Claude never claims a test passed it did not run.

## File Structure

- `Assets/Scripts/Player/Downhill.Player.asmdef` — new gameplay assembly (refs `Downhill.Input`).
- `Assets/Scripts/Player/PlayerBikeController.cs` — the controller shell + `BikeState` enum.
- `Assets/Tests/EditMode/PlayerBikePrefabTests.cs` — prefab structure + wiring assertions.
- `Assets/Tests/PlayMode/PlayerBikeControllerTests.cs` — instantiate-and-validate behaviour.
- `Assets/PFB_Player.prefab` — restructured in-editor (Task 3).
- `Assets/Tests/EditMode/Downhill.Tests.EditMode.asmdef` — add `Downhill.Player` reference.
- `Assets/Tests/PlayMode/Downhill.Tests.PlayMode.asmdef` — add `Downhill.Player` reference.
- `ProjectSettings/TagManager.asset` — gains a `Player` tag (created via editor in Task 3).
- `CHANGELOG.md` — log under `[Unreleased] → Added`.

---

### Task 1: `Downhill.Player` assembly + controller shell

**Files:**
- Create: `Assets/Scripts/Player/Downhill.Player.asmdef`
- Create: `Assets/Scripts/Player/PlayerBikeController.cs`

**Interfaces:**
- Consumes: `Downhill.Input.PlayerInputReader` (Ticket 1.1).
- Produces: `Downhill.Player.PlayerBikeController` with public read-only props `Body` (`Rigidbody`), `Input` (`PlayerInputReader`), `BikeBody`/`CameraPivot`/`GroundCheck`/`RecoveryAnchor` (`Transform`), and `State` (`BikeState`); serialized backing fields named `_body`, `_input`, `_bikeBody`, `_cameraPivot`, `_groundCheck`, `_recoveryAnchor`. `public enum BikeState { Riding, Crashed, Recovering }`.

- [ ] **Step 1: Create the assembly definition**

Create `Assets/Scripts/Player/Downhill.Player.asmdef`:

```json
{
    "name": "Downhill.Player",
    "rootNamespace": "Downhill.Player",
    "references": ["Downhill.Input"],
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

- [ ] **Step 2: Write the controller shell**

Create `Assets/Scripts/Player/PlayerBikeController.cs`:

```csharp
using UnityEngine;
using Downhill.Input;

namespace Downhill.Player
{
    public enum BikeState
    {
        Riding,
        Crashed,
        Recovering,
    }

    /// Scaffolding shell for the player bicycle. Holds the actor's wiring and a
    /// state hook for later crash/chase systems. No movement logic lives here yet
    /// (see Tickets 2.x / 3.x / 4.x).
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerBikeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Rigidbody _body;
        [SerializeField] PlayerInputReader _input;
        [SerializeField] Transform _bikeBody;
        [SerializeField] Transform _cameraPivot;
        [SerializeField] Transform _groundCheck;
        [SerializeField] Transform _recoveryAnchor;

        public Rigidbody Body => _body;
        public PlayerInputReader Input => _input;
        public Transform BikeBody => _bikeBody;
        public Transform CameraPivot => _cameraPivot;
        public Transform GroundCheck => _groundCheck;
        public Transform RecoveryAnchor => _recoveryAnchor;

        public BikeState State { get; private set; } = BikeState.Riding;

        void OnValidate()
        {
            if (_body == null) _body = GetComponent<Rigidbody>();
            if (_input == null) _input = GetComponent<PlayerInputReader>();
        }

        void Awake()
        {
            RequireRef(_body, nameof(_body));
            RequireRef(_input, nameof(_input));
            RequireRef(_bikeBody, nameof(_bikeBody));
            RequireRef(_cameraPivot, nameof(_cameraPivot));
            RequireRef(_groundCheck, nameof(_groundCheck));
            RequireRef(_recoveryAnchor, nameof(_recoveryAnchor));
        }

        void RequireRef(Object reference, string fieldName)
        {
            if (reference == null)
                Debug.LogError($"{nameof(PlayerBikeController)}: '{fieldName}' reference is not wired.", this);
        }
    }
}
```

(`Object` resolves to `UnityEngine.Object`; the file has no `using System;`. The null check uses Unity's overloaded `==`.)

- [ ] **Step 3: Verify it compiles (USER)**

Ask the user to let Unity recompile and report the Console state.
Expected: **no compile errors**; `Downhill.Player.asmdef` shows `Downhill.Input` as an assembly reference in the Inspector. (`.meta` files appear for both new files.)

---

### Task 2: EditMode prefab test (red first)

**Files:**
- Modify: `Assets/Tests/EditMode/Downhill.Tests.EditMode.asmdef`
- Create: `Assets/Tests/EditMode/PlayerBikePrefabTests.cs`

**Interfaces:**
- Consumes: `Downhill.Player.PlayerBikeController`, `Downhill.Input.PlayerInputReader`, the prefab at `Assets/PFB_Player.prefab`.

- [ ] **Step 1: Add the `Downhill.Player` reference to the EditMode test assembly**

Edit `Assets/Tests/EditMode/Downhill.Tests.EditMode.asmdef` — change the `references` array to:

```json
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Unity.InputSystem",
        "Downhill.Input",
        "Downhill.Player"
    ],
```

(Leave every other field unchanged.)

- [ ] **Step 2: Write the failing test**

Create `Assets/Tests/EditMode/PlayerBikePrefabTests.cs`:

```csharp
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Downhill.Input;
using Downhill.Player;

public class PlayerBikePrefabTests
{
    const string PrefabPath = "Assets/PFB_Player.prefab";

    GameObject _prefab;

    [SetUp]
    public void SetUp()
    {
        _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(_prefab, $"Player prefab not found at {PrefabPath}");
    }

    [Test]
    public void Root_HasRequiredComponents()
    {
        Assert.IsNotNull(_prefab.GetComponent<PlayerBikeController>(), "PlayerBikeController missing on root");
        Assert.IsNotNull(_prefab.GetComponent<Rigidbody>(), "Rigidbody missing on root");
        Assert.IsNotNull(_prefab.GetComponent<PlayerInputReader>(), "PlayerInputReader missing on root");
    }

    [Test]
    public void Root_IsTaggedPlayer()
    {
        Assert.AreEqual("Player", _prefab.tag, "Root must be tagged 'Player'");
    }

    [Test]
    public void Collider_IsOnRootOnly()
    {
        Assert.IsNotNull(_prefab.GetComponent<BoxCollider>(), "BoxCollider missing on root");
        var colliders = _prefab.GetComponentsInChildren<Collider>(true);
        Assert.AreEqual(1, colliders.Length, "Expected exactly one collider, on the root");
    }

    [TestCase("BikeBody")]
    [TestCase("CameraPivot")]
    [TestCase("GroundCheck")]
    [TestCase("RecoveryAnchor")]
    public void ChildTransform_Exists(string childName)
    {
        Assert.IsNotNull(_prefab.transform.Find(childName), $"Child transform '{childName}' missing");
    }

    [TestCase("_body")]
    [TestCase("_input")]
    [TestCase("_bikeBody")]
    [TestCase("_cameraPivot")]
    [TestCase("_groundCheck")]
    [TestCase("_recoveryAnchor")]
    public void SerializedReference_IsWired(string fieldName)
    {
        var controller = _prefab.GetComponent<PlayerBikeController>();
        Assert.IsNotNull(controller, "PlayerBikeController missing on root");
        var so = new SerializedObject(controller);
        var prop = so.FindProperty(fieldName);
        Assert.IsNotNull(prop, $"Serialized field '{fieldName}' not found");
        Assert.IsNotNull(prop.objectReferenceValue, $"Serialized field '{fieldName}' is not wired");
    }
}
```

- [ ] **Step 3: Run EditMode tests — expect RED (USER)**

Ask the user to run the EditMode suite.
Expected: `PlayerBikePrefabTests` **fails** (the prefab still has no controller/components/tag/child transforms). `DownhillControlsAssetTests` from Ticket 1.1 still passes. A failure here confirms the test is meaningful before the prefab work.

---

### Task 3: Restructure the prefab in the editor (USER walkthrough) → EditMode green

**Files:**
- Modify: `Assets/PFB_Player.prefab`
- Modify: `ProjectSettings/TagManager.asset` (new `Player` tag)

This is a manual editor task. Walk the user through each step; Claude makes no file edits here.

- [ ] **Step 1: Create the `Player` tag**

Walkthrough:
1. `Edit ▸ Project Settings ▸ Tags and Layers`.
2. Expand **Tags**, click **+**, type `Player`, confirm.
(This writes the tag into `ProjectSettings/TagManager.asset`.)

- [ ] **Step 2: Open the prefab in Prefab Mode**

Walkthrough: In the Project window, double-click `Assets/PFB_Player.prefab` to enter Prefab Mode (isolation view).

- [ ] **Step 3: Tag the root + add the physics + script components**

Walkthrough, with the root `PFB_Player` selected:
1. Top of Inspector, set **Tag → Player**.
2. **Add Component ▸ Rigidbody.** Set: **Mass 80**, **Interpolate = Interpolate**, **Collision Detection = Continuous**, leave **Use Gravity** on, no constraints.
3. **Add Component ▸ Box Collider.** Set **Size = (0.12, 1, 2.16)**, **Center = (0, 0.51, 0)**.
4. **Add Component ▸ Player Input Reader** (`Downhill.Input.PlayerInputReader`).
5. **Add Component ▸ Player Bike Controller** (`Downhill.Player.PlayerBikeController`). It auto-wires its `Body` and `Input` via `OnValidate`; the four transform slots are still empty (expected for now).

- [ ] **Step 4: Create the `BikeBody` lean pivot and reparent the mesh**

Walkthrough:
1. Right-click the root `PFB_Player ▸ Create Empty`; rename it **BikeBody**; set its **Transform to position (0,0,0), rotation (0,0,0), scale (1,1,1)** (Reset).
2. Drag the existing **MSH_BikeDefault** child onto **BikeBody** so it becomes its child. (Use *world-position-preserving* drag — default behaviour; the mesh should not visually move.)
3. Select **MSH_BikeDefault**, find the **Box Collider** that was added on it, and **Remove Component** (right-click the component header ▸ Remove Component). The root now holds the only collider.

- [ ] **Step 5: Create the remaining anchor transforms**

Walkthrough — for each, right-click the root `PFB_Player ▸ Create Empty`, rename, and set a rough local position (tunable later):
1. **CameraPivot** — local position `(0, 1.4, 0)` (≈ chest/head height).
2. **GroundCheck** — local position `(0, 0.05, 0)` (≈ tire contact).
3. **RecoveryAnchor** — local position `(0, 0.8, 0)`.

- [ ] **Step 6: Wire the controller's transform references**

Walkthrough: Select the root, find **Player Bike Controller**, and drag each child into its slot:
- **Bike Body ← BikeBody**
- **Camera Pivot ← CameraPivot**
- **Ground Check ← GroundCheck**
- **Recovery Anchor ← RecoveryAnchor**

Confirm **Body** (Rigidbody) and **Input** (PlayerInputReader) are already populated. If either is empty, drag the root's own components in.

- [ ] **Step 7: Save and exit Prefab Mode**

Walkthrough: `Ctrl+S`, then click the back arrow (`<`) in the breadcrumb to leave Prefab Mode. The scene's existing `PFB_Player` instance inherits all changes.

- [ ] **Step 8: Run EditMode tests — expect GREEN (USER)**

Ask the user to run the EditMode suite.
Expected: all of `PlayerBikePrefabTests` **passes** (components, tag, single collider, four child transforms, all six serialized references wired), and Ticket 1.1 tests still pass.

If any case fails, fix the specific prefab field it names (re-enter Prefab Mode), re-save, re-run. Do not move on until green.

---

### Task 4: PlayMode instantiate-and-validate test

**Files:**
- Modify: `Assets/Tests/PlayMode/Downhill.Tests.PlayMode.asmdef`
- Create: `Assets/Tests/PlayMode/PlayerBikeControllerTests.cs`

**Interfaces:**
- Consumes: `Downhill.Player.PlayerBikeController`, `BikeState`, the prefab at `Assets/PFB_Player.prefab`.

- [ ] **Step 1: Add the `Downhill.Player` reference to the PlayMode test assembly**

Edit `Assets/Tests/PlayMode/Downhill.Tests.PlayMode.asmdef` — change the `references` array to:

```json
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Unity.InputSystem",
        "Unity.InputSystem.TestFramework",
        "Downhill.Input",
        "Downhill.Player"
    ],
```

(Leave every other field unchanged. `includePlatforms` stays `[]` so this remains a PlayMode assembly.)

- [ ] **Step 2: Write the test**

Create `Assets/Tests/PlayMode/PlayerBikeControllerTests.cs`:

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Downhill.Player;

public class PlayerBikeControllerTests
{
    const string PrefabPath = "Assets/PFB_Player.prefab";

    [UnityTest]
    public IEnumerator Prefab_Instantiates_WithoutErrors_AndWiresReferences()
    {
#if UNITY_EDITOR
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab, $"Player prefab not found at {PrefabPath}");

        var instance = Object.Instantiate(prefab);
        yield return null; // Awake + OnEnable + one frame

        LogAssert.NoUnexpectedReceived(); // Awake's validation logged no errors

        var controller = instance.GetComponent<PlayerBikeController>();
        Assert.IsNotNull(controller, "PlayerBikeController missing on instance");
        Assert.AreEqual(BikeState.Riding, controller.State, "State should start as Riding");
        Assert.IsNotNull(controller.Body, "Body not wired");
        Assert.IsNotNull(controller.Input, "Input not wired");
        Assert.IsNotNull(controller.BikeBody, "BikeBody not wired");
        Assert.IsNotNull(controller.CameraPivot, "CameraPivot not wired");
        Assert.IsNotNull(controller.GroundCheck, "GroundCheck not wired");
        Assert.IsNotNull(controller.RecoveryAnchor, "RecoveryAnchor not wired");

        Object.Destroy(instance);
#else
        Assert.Ignore("PlayMode prefab test requires the editor (AssetDatabase).");
        yield break;
#endif
    }
}
```

- [ ] **Step 3: Run PlayMode tests — expect GREEN (USER)**

Ask the user to run the PlayMode suite.
Expected: `PlayerBikeControllerTests` **passes** — the prefab instantiates, `Awake` logs no errors, `State == Riding`, and every reference is non-null. Ticket 1.1 PlayMode tests still pass.

**Fallback (only if Step 3 reports a compile error `The type or namespace 'UnityEditor' could not be found`):** the PlayMode assembly is not resolving `UnityEditor` under `#if UNITY_EDITOR`. Replace the asset-load with a reflection-built instance that still exercises `Awake`/wiring: construct a root `GameObject`, build the same child hierarchy in code, `AddComponent` the Rigidbody/`PlayerInputReader`/`PlayerBikeController`, assign the six private serialized fields via reflection (`typeof(PlayerBikeController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)`), then assert `State == Riding` and the public props are non-null after a frame. Report to the user before switching approaches.

---

### Task 5: Update the changelog

**Files:**
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Add the entry**

Under `## [Unreleased] → ### Added`, above the Ticket 1.1 entry (newest first), insert:

```markdown
- **Player bicycle root actor** (Ticket 1.2) — 2026-06-22
  - Restructured `PFB_Player.prefab` into a dynamic-Rigidbody actor: root carries
    `Rigidbody` + `BoxCollider` + `PlayerInputReader` + `PlayerBikeController` and
    is tagged `Player`. The bike mesh now sits under a `BikeBody` lean pivot, with
    `CameraPivot`, `GroundCheck`, and `RecoveryAnchor` anchor transforms for later
    camera, grounding, and crash-recovery work.
  - Added the `Downhill.Player` assembly and `PlayerBikeController`, a wiring/
    validation shell exposing the actor's references and a read-only `BikeState`
    (Riding/Crashed/Recovering) stub. It auto-wires same-object components and
    logs a named error for any missing link, so the scene runs without
    null-reference errors. No movement logic yet.
  - Added EditMode (prefab structure + serialized-reference wiring) and PlayMode
    (instantiate-and-validate) tests.
```

- [ ] **Step 2: Confirm formatting**

Re-read the changed section; ensure it stays within the existing Keep a Changelog structure and bullet style.

---

## Self-Review

- **Spec coverage:** hierarchy (Task 3), Rigidbody base (Task 3 Step 3), controller shell + validation + `BikeState` (Task 1), EditMode + PlayMode tests (Tasks 2/4), changelog (Task 5), `Player` tag (Task 3 Step 1). All spec sections covered.
- **Type consistency:** field names `_body/_input/_bikeBody/_cameraPivot/_groundCheck/_recoveryAnchor` are identical across the controller (Task 1), the EditMode `SerializedReference_IsWired` cases (Task 2), and the wiring walkthrough (Task 3 Step 6). Public props `Body/Input/BikeBody/CameraPivot/GroundCheck/RecoveryAnchor/State` match between Task 1 and Task 4.
- **No placeholders:** every step has concrete code, exact menu paths, and explicit expected results.
- **Ordering:** script+asmdef (compile) → EditMode test (red) → prefab build (green) → PlayMode test (green) → changelog. Each task ends on an independently checkable result.
