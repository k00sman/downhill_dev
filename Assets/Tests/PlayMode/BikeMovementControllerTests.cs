using System.Collections;
using Downhill.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

public class BikeMovementControllerTests : InputTestFixture
{
    private const string PrefabPath = "Assets/PFB_Player.prefab";

    private GameObject _ground;
    private GameObject _player;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
    }

    [TearDown]
    public override void TearDown()
    {
        if (_player != null)
        {
            Object.DestroyImmediate(_player);
        }

        if (_ground != null)
        {
            Object.DestroyImmediate(_ground);
        }

        base.TearDown();
    }

    // Big flat or tilted ground slab the player can rest on.
    private GameObject MakeGround(Quaternion rotation)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.transform.SetPositionAndRotation(Vector3.zero, rotation);
        g.transform.localScale = new Vector3(50f, 1f, 50f);
        return g;
    }

    private IEnumerator SpawnPlayerAbove(GameObject ground, float height)
    {
#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab, $"Player prefab not found at {PrefabPath}");
        _player = Object.Instantiate(prefab);
        _player.transform.SetPositionAndRotation(ground.transform.position + (Vector3.up * height), Quaternion.identity);
        // Settle onto the ground for a few physics frames.
        for (int i = 0; i < 25; i++)
        {
            yield return new WaitForFixedUpdate();
        }
#else
        Assert.Ignore("PlayMode movement tests require the editor (AssetDatabase).");
        yield break;
#endif
    }

    private static float ForwardSpeed(PlayerBikeController c)
    {
        return Vector3.Dot(c.Body.linearVelocity, c.transform.forward);
    }

    [UnityTest]
    public IEnumerator FlatGround_BleedsInitialSpeed()
    {
        _ground = MakeGround(Quaternion.identity);
        yield return SpawnPlayerAbove(_ground, 1.0f);
        PlayerBikeController c = _player.GetComponent<PlayerBikeController>();

        c.Body.linearVelocity = c.transform.forward * 5f;
        for (int i = 0; i < 40; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        Assert.Less(ForwardSpeed(c), 5f, "Drag should bleed speed on flat ground");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator DownhillGround_GainsSpeed()
    {
        // Tilt so the surface descends toward +Z (the player's forward):
        // a +X rotation drops the slab's +Z edge downhill.
        _ground = MakeGround(Quaternion.Euler(20f, 0f, 0f));
        yield return SpawnPlayerAbove(_ground, 1.0f);
        PlayerBikeController c = _player.GetComponent<PlayerBikeController>();

        float before = ForwardSpeed(c);
        for (int i = 0; i < 60; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        Assert.Greater(ForwardSpeed(c), before + 0.5f, "Should accelerate downhill");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator Pedalling_IncreasesSpeed_VsControl()
    {
        // Control run: flat, no pedal.
        _ground = MakeGround(Quaternion.identity);
        yield return SpawnPlayerAbove(_ground, 1.0f);
        PlayerBikeController c = _player.GetComponent<PlayerBikeController>();
        for (int i = 0; i < 40; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        float control = ForwardSpeed(c);

        // Pedalled run: keep topping up pedal power.
        for (int i = 0; i < 40; i++)
        {
            c.AddPedalPower(1f);
            yield return new WaitForFixedUpdate();
        }
        Assert.Greater(ForwardSpeed(c), control + 0.5f, "Pedalling should add speed");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator SteepDownhill_KeepsMostlyGrounded_WithoutUpwardVelocitySpikes()
    {
#if UNITY_EDITOR
        _ground = MakeGround(Quaternion.Euler(35f, 0f, 0f));
        yield return SpawnPlayerAbove(_ground, 1.0f);
        PlayerBikeController c = _player.GetComponent<PlayerBikeController>();

        c.Body.linearVelocity = c.transform.forward * 6f;

        int groundedFrames = 0;
        float maxUpwardVelocity = 0f;
        for (int i = 0; i < 90; i++)
        {
            yield return new WaitForFixedUpdate();
            if (c.IsGrounded)
            {
                groundedFrames++;
            }

            maxUpwardVelocity = Mathf.Max(maxUpwardVelocity, c.Body.linearVelocity.y);
        }

        Assert.GreaterOrEqual(groundedFrames, 75,
            $"Steep downhill movement should not flicker airborne on a continuous slope. groundedFrames={groundedFrames}");
        Assert.LessOrEqual(maxUpwardVelocity, 1.0f,
            $"Grounded contact should not produce large upward velocity spikes. maxUpwardVelocity={maxUpwardVelocity}");
        LogAssert.NoUnexpectedReceived();
#else
        Assert.Ignore("PlayMode movement tests require the editor (AssetDatabase).");
        yield break;
#endif
    }

    [UnityTest]
    public IEnumerator TurnInput_WhileMoving_RotatesHeadingAndVelocity()
    {
#if UNITY_EDITOR
        _ground = MakeGround(Quaternion.identity);
        yield return SpawnPlayerAbove(_ground, 1.0f);
        PlayerBikeController c = _player.GetComponent<PlayerBikeController>();

        Quaternion initialRotation = c.transform.rotation;
        c.Body.linearVelocity = c.transform.forward * 5f;

        Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
        Set(gamepad.leftStick, new Vector2(1f, 0f));
        yield return null; // PlayerInputReader.Update reads Turn.

        for (int i = 0; i < 20; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        float yawDelta = Quaternion.Angle(initialRotation, c.transform.rotation);
        Assert.Greater(yawDelta, 1f, "Turn input should rotate the bike root heading.");

        Vector3 flatVelocity = Vector3.ProjectOnPlane(c.Body.linearVelocity, Vector3.up);
        Assert.Greater(flatVelocity.magnitude, 0.1f, "Bike should retain forward velocity while steering.");
        Assert.Greater(Vector3.Dot(flatVelocity.normalized, c.transform.forward), 0.95f,
            "Grounded movement velocity should follow the steered heading.");

        LogAssert.NoUnexpectedReceived();
#else
        Assert.Ignore("PlayMode steering tests require the editor (AssetDatabase).");
        yield break;
#endif
    }

    //     [UnityTest]
    //     public IEnumerator BankedGroundWithoutTurnInput_DoesNotRotateHeading()
    //     {
    // #if UNITY_EDITOR
    //         _ground = MakeGround(Quaternion.Euler(0f, 0f, 12f));
    //         yield return SpawnPlayerAbove(_ground, 1.0f);
    //         PlayerBikeController c = _player.GetComponent<PlayerBikeController>();

    //         Vector3 initialForward = c.transform.forward;
    //         c.Body.linearVelocity = initialForward * 5f;

    //         for (int i = 0; i < 30; i++)
    //         {
    //             yield return new WaitForFixedUpdate();
    //         }

    //         float signedYaw = Vector3.SignedAngle(initialForward, c.transform.forward, Vector3.up);
    //         Assert.AreEqual(0f, signedYaw, 0.25f,
    //             "Terrain should not create hidden self-steering yaw in Ticket 3.1.");

    //         LogAssert.NoUnexpectedReceived();
    // #else
    //         Assert.Ignore("PlayMode steering tests require the editor (AssetDatabase).");
    //         yield break;
    // #endif
    //     }
}
