# Steering Input Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Ticket 3.1 steering so the bike turns from `PlayerInputReader.Turn`, keeps yaw code-owned, and lets banked terrain gently turn the bike away from raised elevation even when the player is not steering.

**Architecture:** Add a pure `[System.Serializable]` `BikeSteeringModel` next to `BikeMovementModel`, then have `PlayerBikeController` apply a bounded yaw delta before calling the existing movement model. Terrain steering uses explicit left/right terrain probes to measure side-to-side bank; raw averaged ground normals are not used for yaw because they can convert bump/contact noise into random self-turns. Tests lead the change: EditMode covers the pure steering math, PlayMode covers prefab constraints, steering integration through the existing input pipeline, banked-ground self-turn, and pitch-only ground not self-turning.

**Tech Stack:** Unity 6000.3, C#, NUnit EditMode/PlayMode tests, Unity Input System Test Framework, `Downhill.Player`, `Downhill.Input`.

---

## File Map

- Create `Assets/Scripts/Player/BikeSteeringModel.cs`: pure steering/yaw math and tuning fields.
- Create `Assets/Tests/EditMode/BikeSteeringModelTests.cs`: deterministic unit tests for yaw direction, clamps, deadzone, speed gate, and terrain camber steering.
- Modify `Assets/Scripts/Player/PlayerBikeController.cs`: add `_steering`, keep Rigidbody physics rotation frozen, read `_input.Turn`, apply code-owned yaw before movement.
- Modify `Assets/Tests/PlayMode/PlayerBikeControllerTests.cs`: update the old yaw-frozen assertion.
- Modify `Assets/Tests/PlayMode/BikeMovementControllerTests.cs`: add integration coverage that turn input changes heading and velocity direction, that banked ground can steer the bike without turn input, and that downhill pitch without side-to-side bank does not create yaw drift.
- Modify `Assets/Tests/PlayMode/PlayerInputReaderTests.cs`: register keyboard, mouse, and gamepad devices under `InputTestFixture` before enabling the shipped action map.
- Modify `CHANGELOG.md`: add Ticket 3.1 entry under `[Unreleased]`.
- Modify `docs/codemap.md`: regenerate after implementation.

No Git staging, commits, or pushes are part of this plan by user instruction.

## Debugging Update

After the first implementation, two issues changed the plan:

- PlayMode input tests still hit `InputActionState.OnBeforeInitialUpdate`.
  The test fixture was only registering a gamepad while the action map also
  contains keyboard and mouse bindings. Tests now create the full default input
  device set before adding `PlayerInputReader`.
- The terrain steering shortcut used a bank angle inferred from the averaged
  ground normal. That can react to noisy terrain normals and feel like random
  left/right turning. The controller now measures actual side-to-side height
  with left/right downward probes and passes that signed bank angle into
  `BikeSteeringModel`.

## Task 1: Steering Model Tests

**Files:**
- Create: `Assets/Tests/EditMode/BikeSteeringModelTests.cs`
- Later implementation: `Assets/Scripts/Player/BikeSteeringModel.cs`

- [ ] **Step 1: Write failing EditMode tests**

Create `Assets/Tests/EditMode/BikeSteeringModelTests.cs` with:

```csharp
using Downhill.Player;
using NUnit.Framework;
using UnityEngine;

public class BikeSteeringModelTests
{
    private static BikeSteeringModel MakeModel()
    {
        return new()
        {
            turnRateDegreesPerSecond = 100f,
            maxYawDeltaDegrees = 8f,
            minSpeedForSteering = 0.5f,
            turnDeadzone = 0.05f,
            terrainBankDeadzoneDegrees = 3f,
            terrainTurnStrength = 0.35f,
            terrainTurnMaxDegreesPerSecond = 35f,
        };
    }

    private static Vector3 BankedNormal(float rollDegrees)
    {
        return Quaternion.AngleAxis(rollDegrees, Vector3.forward) * Vector3.up;
    }

    [Test]
    public void StepYawDeltaDegrees_RightInput_ReturnsPositiveYaw()
    {
        BikeSteeringModel model = MakeModel();

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, Vector3.up, 1f, 0.02f);

        Assert.Greater(yaw, 0f, "Right turn input should produce positive yaw.");
    }

    [Test]
    public void StepYawDeltaDegrees_LeftInput_ReturnsNegativeYaw()
    {
        BikeSteeringModel model = MakeModel();

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, Vector3.up, -1f, 0.02f);

        Assert.Less(yaw, 0f, "Left turn input should produce negative yaw.");
    }

    [Test]
    public void StepYawDeltaDegrees_AtRest_DoesNotSpinInPlace()
    {
        BikeSteeringModel model = MakeModel();

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.zero, Vector3.up, 1f, 0.02f);

        Assert.AreEqual(0f, yaw, 0.0001f);
    }

    [Test]
    public void StepYawDeltaDegrees_InsideDeadzone_ReturnsZero()
    {
        BikeSteeringModel model = MakeModel();

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, Vector3.up, 0.01f, 0.02f);

        Assert.AreEqual(0f, yaw, 0.0001f);
    }

    [Test]
    public void StepYawDeltaDegrees_HighTurnRate_ClampsYawDelta()
    {
        BikeSteeringModel model = MakeModel();
        model.turnRateDegreesPerSecond = 1000f;
        model.maxYawDeltaDegrees = 3f;

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, Vector3.up, 1f, 0.02f);

        Assert.LessOrEqual(yaw, model.maxYawDeltaDegrees + 0.0001f);
    }

    [Test]
    public void StepYawDeltaDegrees_BankedGroundWithoutInput_TurnsAwayFromElevation()
    {
        BikeSteeringModel model = MakeModel();

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, BankedNormal(12f), 0f, 0.02f);

        Assert.Less(yaw, 0f,
            "If the terrain rises to the bike's right, camber should turn the bike away from that elevation.");
    }

    [Test]
    public void StepYawDeltaDegrees_SmallBankWithoutInput_ReturnsZero()
    {
        BikeSteeringModel model = MakeModel();

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, BankedNormal(1f), 0f, 0.02f);

        Assert.AreEqual(0f, yaw, 0.0001f,
            "Tiny normal changes should not create terrain-steering drift.");
    }

    [Test]
    public void StepYawDeltaDegrees_SameDirectionBank_IncreasesTurnMagnitude()
    {
        BikeSteeringModel model = MakeModel();

        float flatYaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, Vector3.up, 1f, 0.02f);
        float assistedYaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, BankedNormal(-12f), 1f, 0.02f);

        Assert.Greater(assistedYaw, flatYaw,
            "A bank that agrees with player input should gently increase turn magnitude.");
        Assert.LessOrEqual(assistedYaw - flatYaw,
            model.terrainTurnMaxDegreesPerSecond * 0.02f + 0.0001f);
    }

    [Test]
    public void StepYawDeltaDegrees_OppositeDirectionBank_ResistsButDoesNotReverseInput()
    {
        BikeSteeringModel model = MakeModel();

        float flatYaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, Vector3.up, 1f, 0.02f);
        float bankedYaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, BankedNormal(12f), 1f, 0.02f);

        Assert.Less(bankedYaw, flatYaw,
            "Terrain that rises into the player's turn should resist that turn.");
        Assert.Greater(bankedYaw, 0f,
            "Default terrain steering should not overpower full player input at low-to-medium speed.");
    }
}
```

- [ ] **Step 2: Verify red**

Run:

```bash
./scripts/lint.sh --check
```

Expected before implementation: compile fails because `BikeSteeringModel` does not exist. If Unity-generated project files are unavailable, continue after noting that Unity Test Runner must verify the red/green cycle.

- [ ] **Step 3: Implement `BikeSteeringModel`**

Create `Assets/Scripts/Player/BikeSteeringModel.cs` with the model described in the tests.

- [ ] **Step 4: Verify green for model**

Run:

```bash
./scripts/lint.sh --check
```

Expected: no compile errors from `BikeSteeringModelTests.cs`.

## Task 2: Controller Steering Integration Tests

**Files:**
- Modify: `Assets/Tests/PlayMode/PlayerBikeControllerTests.cs`
- Modify: `Assets/Tests/PlayMode/BikeMovementControllerTests.cs`
- Later implementation: `Assets/Scripts/Player/PlayerBikeController.cs`

- [ ] **Step 1: Update prefab constraint test to expect code-owned yaw**

In `PlayerBikeControllerTests.cs`, replace `Prefab_FreezesYawUntilSteeringExists` with `Prefab_KeepsPhysicsRotationFrozenForCodeOwnedYaw`. Assert `FreezeRotationX`, `FreezeRotationY`, and `FreezeRotationZ` are all set so physics cannot rotate the bike.

- [ ] **Step 2: Add PlayMode steering integration test**

In `BikeMovementControllerTests.cs`, add `InputSystem` using statements and derive from `InputTestFixture`. Add a test that spawns the prefab on flat ground, gives it forward velocity, sets gamepad left stick X, waits fixed updates, and asserts yaw changed and velocity points along the new heading. Add a second test that spawns the prefab on banked ground, gives it forward velocity without turn input, and asserts heading turns away from the raised side.

- [ ] **Step 3: Verify red**

Run:

```bash
./scripts/lint.sh --check
```

Expected before controller integration: the updated yaw constraint test fails once run in Unity, and the integration behavior is not satisfied yet. Compile should still succeed if Task 1 is complete.

- [ ] **Step 4: Integrate steering in `PlayerBikeController`**

Add `_steering`, keep physics rotation constraints fully frozen, compute yaw delta when grounded, and assign yaw directly before movement.

- [ ] **Step 5: Verify green for integration**

Run:

```bash
./scripts/lint.sh --check
```

Expected: compile/lint check completes or reports only existing analyzer warnings. Human must still run Unity Test Runner for PlayMode assertions.

## Task 3: Docs and Codemap

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `docs/codemap.md`

- [ ] **Step 1: Update changelog**

Add a new `### Added` entry under `[Unreleased]` for Ticket 3.1 steering.

- [ ] **Step 2: Regenerate codemap**

Run:

```bash
bash scripts/generate-codemap.sh
```

Expected: `docs/codemap.md` includes `BikeSteeringModel` and the new `PlayerBikeController --> BikeSteeringModel` relationship.

- [ ] **Step 3: Final verification**

Run:

```bash
./scripts/lint.sh --check
```

Expected: no error-severity findings. Report any warnings explicitly.

## Manual Verification Required

Ask the user to run Unity Test Runner:

- EditMode suite
- PlayMode suite

Manual playtest checks:

- On a flat area with forward speed, left/right input turns the bike predictably.
- At rest, turn input does not spin the bike in place.
- On banked or curved downhill terrain, terrain can gently support a player-chosen turn but does not auto-steer when input is neutral.
