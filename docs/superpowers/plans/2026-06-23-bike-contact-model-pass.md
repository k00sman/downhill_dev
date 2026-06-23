# Bike Contact Model Pass Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current single-ray, horizontal-box bike contact behavior with a more stable prototype contact model that follows downhill terrain without popping, preserves the improved speed behavior, and visually pitches the bike to the surface.

**Architecture:** Keep the root Rigidbody upright and rotation-frozen for prototype stability. Split responsibilities: `BikeMovementModel` owns scalar forward-speed math and converts it to a controlled grounded velocity, while `PlayerBikeController` owns ground probing, grounded adhesion, and visual `BikeBody` pitch. The visible bike pitches independently from the physics root so terrain readability improves without letting the Rigidbody topple.

**Tech Stack:** Unity 6000.3.17f1, URP 17.3.0, C#, Unity Test Framework (NUnit EditMode + `[UnityTest]` PlayMode), assemblies `Downhill.Player`, `Downhill.Tests.EditMode`, and `Downhill.Tests.PlayMode`.

---

## Current State And Constraints

- Current runtime code is in `Assets/Scripts/Player/PlayerBikeController.cs` and `Assets/Scripts/Player/BikeMovementModel.cs`.
- Current prefab is `Assets/PFB_Player.prefab`.
- Current tests are in `Assets/Tests/EditMode/BikeMovementModelTests.cs`, `Assets/Tests/EditMode/PlayerBikePrefabTests.cs`, `Assets/Tests/PlayMode/BikeMovementControllerTests.cs`, and `Assets/Tests/PlayMode/PlayerBikeControllerTests.cs`.
- `AGENTS.md` says Unity tests generally cannot be run headless here. The next session should ask the user to run Unity Test Runner tests and do playtests.
- Do not commit, stage, or push. The user commits manually.
- Do not modify third-party content under `Assets/`.
- Preserve user scene changes. At the time this plan was written, `Assets/Scenes/SCN_Tutorial.unity` had user-side player-position edits.
- Keep input reads through `PlayerInputReader`; this pass should not add gameplay input.

## Problem Summary

Several quick fixes improved speed behavior but did not solve jumpiness:

- The bike still pops on steeper slopes.
- The bike visual stays flat because root rotation is frozen and `BikeBody` is not aligned to terrain.
- The root collider is a long horizontal box, so it does not naturally match steep terrain.
- Ground detection uses one short downward ray from `GroundCheck`.
- Movement overwrites Rigidbody velocity every `FixedUpdate`.

The next implementation should treat this as a contact-model pass, not a parameter-tuning pass.

## File Structure

- Modify `Assets/Scripts/Player/BikeMovementModel.cs`
  - Keep speed math pure.
  - Return controlled grounded velocity that can follow downhill tangent while limiting upward launch velocity.
- Modify `Assets/Scripts/Player/PlayerBikeController.cs`
  - Add multi-point ground probing.
  - Add small grounded adhesion.
  - Add visual pitch alignment on `_bikeBody`.
  - Keep root Rigidbody rotation frozen.
- Modify `Assets/Tests/EditMode/BikeMovementModelTests.cs`
  - Replace the current “no vertical velocity on steep ground” expectation.
  - Add tangent-follow and launch-cap tests.
- Modify `Assets/Tests/PlayMode/BikeMovementControllerTests.cs`
  - Add steep-slope grounded stability coverage.
- Modify `Assets/Tests/PlayMode/PlayerBikeControllerTests.cs`
  - Add visual pitch coverage while keeping root constraints coverage.
- Modify `CHANGELOG.md`
  - Add one `Fixed` entry under `[Unreleased]`.

---

### Task 1: Update Movement Model Contract

**Files:**
- Modify: `Assets/Scripts/Player/BikeMovementModel.cs`
- Test: `Assets/Tests/EditMode/BikeMovementModelTests.cs`

**Goal:** Preserve the fixed speed behavior over smooth bumps while allowing downhill tangent-following and preventing upward launch velocity.

- [ ] **Step 1: Replace the steep-ground vertical test**

In `Assets/Tests/EditMode/BikeMovementModelTests.cs`, remove or replace:

```csharp
[Test]
public void SteepGround_DoesNotInjectVerticalVelocity()
```

Add these tests:

```csharp
[Test]
public void SteepDownhill_AllowsDownwardTangentFollow()
{
    var m = MakeModel();
    m.drag = 0f;
    m.gravity = 0f;

    Vector3 velocity = Vector3.forward * 5f;
    Vector3 result = m.Step(velocity, Vector3.forward, DownhillNormal(40f), 0f, 0.02f);

    Assert.Less(result.y, -0.1f,
        "Grounded downhill movement should include downward tangent velocity so the bike follows steep terrain.");
    Assert.AreEqual(5f, Vector3.Dot(result, Vector3.forward), 0.001f,
        "Horizontal forward speed accounting should remain stable.");
}

[Test]
public void SteepUphill_CapsUpwardLaunchVelocity()
{
    var m = MakeModel();
    m.drag = 0f;
    m.gravity = 0f;
    m.maxGroundedUpSpeed = 0.75f;

    Vector3 velocity = Vector3.forward * 5f;
    Vector3 result = m.Step(velocity, Vector3.forward, UphillNormal(40f), 0f, 0.02f);

    Assert.LessOrEqual(result.y, m.maxGroundedUpSpeed + 0.001f,
        "Grounded uphill movement should not launch the bike upward on bumps or convex terrain.");
    Assert.AreEqual(5f, Vector3.Dot(result, Vector3.forward), 0.001f);
}
```

Keep these existing tests because they protect recent improvements:

```csharp
GentleUphill_WithModerateSpeed_CoastsAcrossShortRise
SmoothBumpNormals_WithoutForces_DoNotPumpOrKillSpeed
```

- [ ] **Step 2: Ask the user to run the EditMode test and confirm RED**

Ask the user to run `BikeMovementModelTests` in Unity Test Runner EditMode.

Expected:

```text
SteepDownhill_AllowsDownwardTangentFollow FAILS
SteepUphill_CapsUpwardLaunchVelocity FAILS or does not compile because maxGroundedUpSpeed does not exist
```

- [ ] **Step 3: Implement controlled tangent velocity**

In `Assets/Scripts/Player/BikeMovementModel.cs`, add this tunable field:

```csharp
[Tooltip("Maximum upward grounded velocity allowed when following terrain (m/s).")]
public float maxGroundedUpSpeed = 0.75f;
```

Update `Step` so the scalar speed is still computed from `forwardFlat`, but the output is along `forwardOnSlope` with horizontal speed preserved:

```csharp
float speed = Vector3.Dot(velocity, forwardFlat);
speed += (slopeAccel + pedalAccelTerm) * dt;
speed -= speed * drag * dt;
speed = Mathf.Clamp(speed, 0f, maxSpeed);

float horizontalComponent = Vector3.Dot(forwardOnSlope, forwardFlat);
if (horizontalComponent < 0.1f)
    return forwardFlat * speed;

Vector3 result = forwardOnSlope * (speed / horizontalComponent);
if (result.y > maxGroundedUpSpeed)
    result.y = maxGroundedUpSpeed;

return result;
```

- [ ] **Step 4: Ask the user to run EditMode tests and confirm GREEN**

Ask the user to run `BikeMovementModelTests` in Unity Test Runner EditMode.

Expected:

```text
All BikeMovementModelTests pass.
```

If `SmoothBumpNormals_WithoutForces_DoNotPumpOrKillSpeed` fails because downward/upward tangent output changes vertical velocity while preserving horizontal speed, keep the assertion on horizontal forward speed and do not assert `result.y == 0`.

---

### Task 2: Add Visual Terrain Pitch To BikeBody

**Files:**
- Modify: `Assets/Scripts/Player/PlayerBikeController.cs`
- Test: `Assets/Tests/PlayMode/PlayerBikeControllerTests.cs`

**Goal:** Keep root physics upright, but pitch the visible bike body to match the sampled terrain.

- [ ] **Step 1: Add a failing PlayMode test**

Append this test to `Assets/Tests/PlayMode/PlayerBikeControllerTests.cs`:

```csharp
[UnityTest]
public IEnumerator GroundedBikeBody_PitchesToTerrainWhileRootStaysFrozen()
{
#if UNITY_EDITOR
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    Assert.IsNotNull(prefab, $"Player prefab not found at {PrefabPath}");

    var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
    ground.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
    ground.transform.position = Vector3.zero;
    ground.transform.localScale = new Vector3(50f, 1f, 50f);

    var instance = Object.Instantiate(prefab);
    instance.transform.position = Vector3.up * 1f;
    instance.transform.rotation = Quaternion.identity;

    for (int i = 0; i < 30; i++)
        yield return new WaitForFixedUpdate();

    var controller = instance.GetComponent<PlayerBikeController>();
    Assert.IsNotNull(controller);
    Assert.IsTrue((controller.Body.constraints & RigidbodyConstraints.FreezeRotation) == RigidbodyConstraints.FreezeRotation,
        "Root Rigidbody should remain fully rotation-frozen.");
    Assert.Greater(Mathf.Abs(controller.BikeBody.localEulerAngles.x), 1f,
        "BikeBody should visually pitch on sloped ground.");

    LogAssert.NoUnexpectedReceived();
    Object.Destroy(instance);
    Object.Destroy(ground);
#else
    Assert.Ignore("PlayMode prefab test requires the editor (AssetDatabase).");
    yield break;
#endif
}
```

- [ ] **Step 2: Ask the user to run the PlayMode test and confirm RED**

Ask the user to run `PlayerBikeControllerTests` in Unity Test Runner PlayMode.

Expected:

```text
GroundedBikeBody_PitchesToTerrainWhileRootStaysFrozen FAILS because BikeBody remains identity rotation.
```

- [ ] **Step 3: Add visual pitch fields and helper**

In `Assets/Scripts/Player/PlayerBikeController.cs`, add fields:

```csharp
[Header("Visual terrain alignment")]
[SerializeField] float _terrainPitchLerp = 12f;
[SerializeField] float _maxVisualPitchDegrees = 55f;
```

Add this method to `PlayerBikeController`:

```csharp
void UpdateVisualTerrainPitch(Vector3 groundNormal, float dt)
{
    if (_bikeBody == null)
        return;

    Vector3 slopeForward = Vector3.ProjectOnPlane(transform.forward, groundNormal);
    if (slopeForward.sqrMagnitude < 1e-6f)
        return;

    slopeForward.Normalize();
    float signedPitch = Vector3.SignedAngle(transform.forward, slopeForward, transform.right);
    signedPitch = Mathf.Clamp(signedPitch, -_maxVisualPitchDegrees, _maxVisualPitchDegrees);

    Quaternion target = Quaternion.Euler(signedPitch, 0f, 0f);
    float t = 1f - Mathf.Exp(-Mathf.Max(0f, _terrainPitchLerp) * dt);
    _bikeBody.localRotation = Quaternion.Slerp(_bikeBody.localRotation, target, t);
}
```

In `FixedUpdate`, after confirming `_grounded` and before/after movement assignment, call:

```csharp
UpdateVisualTerrainPitch(hit.normal, dt);
```

When not grounded, optionally ease back toward identity:

```csharp
if (!_grounded)
{
    UpdateVisualTerrainPitch(Vector3.up, dt);
    return;
}
```

- [ ] **Step 4: Ask the user to run PlayMode tests and confirm GREEN**

Ask the user to run `PlayerBikeControllerTests` in Unity Test Runner PlayMode.

Expected:

```text
PlayerBikeControllerTests pass.
Root constraints remain frozen.
BikeBody pitches on sloped test ground.
```

---

### Task 3: Improve Ground Probing Without Reintroducing Bad Snap

**Files:**
- Modify: `Assets/Scripts/Player/PlayerBikeController.cs`
- Test: `Assets/Tests/PlayMode/BikeMovementControllerTests.cs`

**Goal:** Replace the single center ray with a small multi-ray terrain sample so grounding and normals are less brittle on steep slopes and smooth bumps.

- [ ] **Step 1: Add a steep-slope stability PlayMode test**

Append this test to `Assets/Tests/PlayMode/BikeMovementControllerTests.cs`:

```csharp
[UnityTest]
public IEnumerator SteepDownhill_KeepsMostlyGrounded_WithoutUpwardVelocitySpikes()
{
#if UNITY_EDITOR
    _ground = MakeGround(Quaternion.Euler(35f, 0f, 0f));
    yield return SpawnPlayerAbove(_ground, 1.0f);
    var c = _player.GetComponent<PlayerBikeController>();

    c.Body.linearVelocity = c.transform.forward * 6f;

    int groundedFrames = 0;
    float maxUpwardVelocity = 0f;
    for (int i = 0; i < 90; i++)
    {
        yield return new WaitForFixedUpdate();
        if (c.IsGrounded)
            groundedFrames++;
        maxUpwardVelocity = Mathf.Max(maxUpwardVelocity, c.Body.linearVelocity.y);
    }

    Assert.GreaterOrEqual(groundedFrames, 75,
        "Steep downhill movement should not flicker airborne on a continuous slope.");
    Assert.LessOrEqual(maxUpwardVelocity, 1.0f,
        "Grounded contact should not produce large upward velocity spikes.");
    LogAssert.NoUnexpectedReceived();
#else
    Assert.Ignore("PlayMode movement tests require the editor (AssetDatabase).");
    yield break;
#endif
}
```

- [ ] **Step 2: Ask the user to run the test and confirm RED**

Ask the user to run `BikeMovementControllerTests` in Unity Test Runner PlayMode.

Expected:

```text
SteepDownhill_KeepsMostlyGrounded_WithoutUpwardVelocitySpikes may fail due grounded-frame count or upward velocity spike.
```

- [ ] **Step 3: Add multi-point probing**

In `PlayerBikeController`, add:

```csharp
[SerializeField] float _groundProbeForwardOffset = 0.75f;
[SerializeField] float _groundProbeRearOffset = 0.75f;
```

Add a private method:

```csharp
bool TryProbeGround(out RaycastHit bestHit, out Vector3 averagedNormal)
{
    bestHit = default;
    averagedNormal = Vector3.zero;

    Vector3 origin = _groundCheck.position;
    Vector3[] origins =
    {
        origin,
        origin + transform.forward * _groundProbeForwardOffset,
        origin - transform.forward * _groundProbeRearOffset,
    };

    int hitCount = 0;
    float bestDistance = float.PositiveInfinity;

    for (int i = 0; i < origins.Length; i++)
    {
        if (!Physics.Raycast(origins[i], Vector3.down, out RaycastHit hit,
                _groundProbeDistance, _groundMask, QueryTriggerInteraction.Ignore))
            continue;

        hitCount++;
        averagedNormal += hit.normal;

        if (hit.distance < bestDistance)
        {
            bestDistance = hit.distance;
            bestHit = hit;
        }
    }

    if (hitCount == 0)
        return false;

    averagedNormal.Normalize();
    return true;
}
```

Replace the existing single `Physics.Raycast` in `FixedUpdate` with:

```csharp
_grounded = TryProbeGround(out RaycastHit hit, out Vector3 groundNormal);

if (!_grounded)
{
    UpdateVisualTerrainPitch(Vector3.up, dt);
    return;
}
```

Pass `groundNormal` to both movement and visual pitch:

```csharp
UpdateVisualTerrainPitch(groundNormal, dt);

float pedal01 = _pedalPowerMax > 0f ? _pedalPower / _pedalPowerMax : 0f;
_body.linearVelocity = _movement.Step(
    _body.linearVelocity, transform.forward, groundNormal, pedal01, dt);
```

- [ ] **Step 4: Ask the user to run PlayMode tests and confirm GREEN or report metrics**

Ask the user to run `BikeMovementControllerTests` in Unity Test Runner PlayMode.

Expected:

```text
SteepDownhill_KeepsMostlyGrounded_WithoutUpwardVelocitySpikes passes, or reports whether groundedFrames or maxUpwardVelocity still fails.
```

If it still fails, do not tune blindly. Record the exact `groundedFrames` and `maxUpwardVelocity` values by temporarily adding assertion messages or `Debug.Log` output only for the test run.

---

### Task 4: Reduce Collider Slope Mismatch If Pops Remain

**Files:**
- Modify: `Assets/PFB_Player.prefab`
- Test: `Assets/Tests/EditMode/PlayerBikePrefabTests.cs`

**Goal:** If the long horizontal root collider still causes pops, make the physics proxy less likely to bridge into terrain on slopes.

- [ ] **Step 1: Add prefab-shape test**

In `Assets/Tests/EditMode/PlayerBikePrefabTests.cs`, add:

```csharp
[Test]
public void RootCollider_IsCompactContactProxy()
{
    var box = _prefab.GetComponent<BoxCollider>();
    Assert.IsNotNull(box, "BoxCollider missing on root");
    Assert.LessOrEqual(box.size.z, 1.25f,
        "Root contact proxy should be compact enough to avoid bridging steep slope changes.");
    Assert.LessOrEqual(box.size.y, 0.8f,
        "Root contact proxy should not be a tall visible-bike-sized box.");
}
```

- [ ] **Step 2: Ask the user to run the EditMode prefab test and confirm RED**

Ask the user to run `PlayerBikePrefabTests` in Unity Test Runner EditMode.

Expected:

```text
RootCollider_IsCompactContactProxy FAILS because current collider is size z = 2.16 and y = 1.
```

- [ ] **Step 3: Change prefab collider dimensions in Unity Editor**

In the Unity Editor, open `Assets/PFB_Player.prefab`.

Set root `BoxCollider`:

```text
Size:   X 0.35, Y 0.65, Z 1.10
Center: X 0.00, Y 0.35, Z 0.00
```

This is intentionally a contact proxy, not the final crash volume. Later crash/hit detection can use separate triggers.

- [ ] **Step 4: Ask the user to run prefab and movement tests**

Ask the user to run:

```text
EditMode: PlayerBikePrefabTests
PlayMode: BikeMovementControllerTests
PlayMode: PlayerBikeControllerTests
```

Expected:

```text
All pass.
Playtest shows fewer terrain pops on steep continuous slopes.
```

If collision with hazards becomes too forgiving, do not enlarge this collider immediately. Plan a separate crash/hazard trigger volume in the crash ticket.

---

### Task 5: Changelog And Playtest Notes

**Files:**
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Update changelog**

Under `## [Unreleased] → ### Fixed`, add:

```markdown
- **Bike terrain contact stability** — 2026-06-23
  - Added controlled downhill tangent-following with capped upward grounded velocity to reduce steep-slope pops.
  - Added multi-point ground probing so continuous slopes and smooth bumps produce steadier ground normals.
  - Added visual terrain pitch on `BikeBody` while keeping the root Rigidbody rotation-frozen.
  - Tightened the root collider into a compact contact proxy if steep-slope popping persisted after movement/probe changes.
```

If Task 4 was not needed, omit the final bullet about the compact contact proxy.

- [ ] **Step 2: User playtest checklist**

Ask the user to test these exact cases:

```text
1. Same steep slope where jumps were obvious.
2. Smooth rolling terrain where speed deceleration was previously fixed.
3. Small 0.4m / 10m rise at low speed.
4. A descent fast enough to verify there is no runaway acceleration.
5. Visual check: bike body pitches with the slope while the root remains stable.
```

Expected user report should include:

```text
- Does the bike still jump on steep continuous slope?
- Does speed still feel fixed from the previous iteration?
- Does visual pitch feel too strong, too slow, or correct?
- Did the smaller collider make obstacle contact feel obviously wrong?
```

---

## Recommended Execution Order

1. Task 1: Movement model tangent-following and upward cap.
2. Task 2: Visual `BikeBody` pitch.
3. Task 3: Multi-point ground probe.
4. Playtest.
5. Task 4 only if pops remain after Tasks 1-3.
6. Task 5 changelog and playtest notes.

## Stop Conditions

Stop and reassess instead of continuing to tune if any of these happen:

- Speed regression returns on smooth terrain.
- Bike launches harder after tangent-following.
- Multi-point probe causes false ground detection on obstacles or props.
- Smaller collider breaks basic terrain contact or falls through terrain.
- Three consecutive parameter tweaks do not materially improve the same playtest slope.

At that point, the next architecture step is a real wheel/contact model rather than a root Rigidbody box proxy.
