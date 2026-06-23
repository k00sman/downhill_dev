using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Downhill.Player;

public class BikeMovementControllerTests
{
    const string PrefabPath = "Assets/PFB_Player.prefab";

    GameObject _ground;
    GameObject _player;

    [TearDown]
    public void TearDown()
    {
        if (_player != null) Object.Destroy(_player);
        if (_ground != null) Object.Destroy(_ground);
    }

    // Big flat or tilted ground slab the player can rest on.
    GameObject MakeGround(Quaternion rotation)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.transform.rotation = rotation;
        g.transform.position = Vector3.zero;
        g.transform.localScale = new Vector3(50f, 1f, 50f);
        return g;
    }

    IEnumerator SpawnPlayerAbove(GameObject ground, float height)
    {
#if UNITY_EDITOR
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab, $"Player prefab not found at {PrefabPath}");
        _player = Object.Instantiate(prefab);
        _player.transform.position = ground.transform.position + Vector3.up * height;
        _player.transform.rotation = Quaternion.identity;
        // Settle onto the ground for a few physics frames.
        for (int i = 0; i < 25; i++) yield return new WaitForFixedUpdate();
#else
        Assert.Ignore("PlayMode movement tests require the editor (AssetDatabase).");
        yield break;
#endif
    }

    static float ForwardSpeed(PlayerBikeController c) =>
        Vector3.Dot(c.Body.linearVelocity, c.transform.forward);

    [UnityTest]
    public IEnumerator FlatGround_BleedsInitialSpeed()
    {
        _ground = MakeGround(Quaternion.identity);
        yield return SpawnPlayerAbove(_ground, 1.0f);
        var c = _player.GetComponent<PlayerBikeController>();

        c.Body.linearVelocity = c.transform.forward * 5f;
        for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();

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
        var c = _player.GetComponent<PlayerBikeController>();

        float before = ForwardSpeed(c);
        for (int i = 0; i < 60; i++) yield return new WaitForFixedUpdate();

        Assert.Greater(ForwardSpeed(c), before + 0.5f, "Should accelerate downhill");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator Pedalling_IncreasesSpeed_VsControl()
    {
        // Control run: flat, no pedal.
        _ground = MakeGround(Quaternion.identity);
        yield return SpawnPlayerAbove(_ground, 1.0f);
        var c = _player.GetComponent<PlayerBikeController>();
        for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
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
            $"Steep downhill movement should not flicker airborne on a continuous slope. groundedFrames={groundedFrames}");
        Assert.LessOrEqual(maxUpwardVelocity, 1.0f,
            $"Grounded contact should not produce large upward velocity spikes. maxUpwardVelocity={maxUpwardVelocity}");
        LogAssert.NoUnexpectedReceived();
#else
        Assert.Ignore("PlayMode movement tests require the editor (AssetDatabase).");
        yield break;
#endif
    }
}
