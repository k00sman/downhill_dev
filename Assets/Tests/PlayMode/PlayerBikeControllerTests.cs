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

    [UnityTest]
    public IEnumerator Prefab_FreezesYawUntilSteeringExists()
    {
#if UNITY_EDITOR
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab, $"Player prefab not found at {PrefabPath}");

        var instance = Object.Instantiate(prefab);
        yield return null; // Awake applies runtime Rigidbody setup.

        var controller = instance.GetComponent<PlayerBikeController>();
        var constraints = controller.Body.constraints;
        Assert.IsTrue((constraints & RigidbodyConstraints.FreezeRotationX) != 0,
            "Pitch must stay locked until crash/air control is implemented.");
        Assert.IsTrue((constraints & RigidbodyConstraints.FreezeRotationY) != 0,
            "Yaw must stay locked until steering is implemented.");
        Assert.IsTrue((constraints & RigidbodyConstraints.FreezeRotationZ) != 0,
            "Roll must stay locked until visual lean/physics are implemented.");
        LogAssert.NoUnexpectedReceived();

        Object.Destroy(instance);
#else
        Assert.Ignore("PlayMode prefab test requires the editor (AssetDatabase).");
        yield break;
#endif
    }

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

}
