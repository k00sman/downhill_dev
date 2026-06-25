using System.Collections;
using Downhill.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerBikeControllerTests
{
    private const string PrefabPath = "Assets/PFB_Player.prefab";

    [UnityTest]
    public IEnumerator Prefab_Instantiates_WithoutErrors_AndWiresReferences()
    {
#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab, $"Player prefab not found at {PrefabPath}");

        GameObject instance = Object.Instantiate(prefab);
        yield return null; // Awake + OnEnable + one frame

        LogAssert.NoUnexpectedReceived(); // Awake's validation logged no errors

        PlayerBikeController controller = instance.GetComponent<PlayerBikeController>();
        Assert.IsNotNull(controller, "PlayerBikeController missing on instance");
        Assert.AreEqual(BikeState.Riding, controller.State, "State should start as Riding");
        Assert.IsNotNull(controller.Body, "Body not wired");
        Assert.IsNotNull(controller.Input, "Input not wired");
        Assert.IsNotNull(controller.BikeBody, "BikeBody not wired");
        Assert.IsNotNull(controller.CameraPivot, "CameraPivot not wired");
        Assert.IsNotNull(controller.GroundCheck, "GroundCheck not wired");
        Assert.IsNotNull(controller.RecoveryAnchor, "RecoveryAnchor not wired");

        Object.DestroyImmediate(instance);
#else
        Assert.Ignore("PlayMode prefab test requires the editor (AssetDatabase).");
        yield break;
#endif
    }

    [UnityTest]
    public IEnumerator Prefab_KeepsPhysicsRotationFrozenForCodeOwnedYaw()
    {
#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab, $"Player prefab not found at {PrefabPath}");

        GameObject instance = Object.Instantiate(prefab);
        yield return null; // Awake applies runtime Rigidbody setup.

        PlayerBikeController controller = instance.GetComponent<PlayerBikeController>();
        RigidbodyConstraints constraints = controller.Body.constraints;
        Assert.IsTrue((constraints & RigidbodyConstraints.FreezeRotationX) != 0,
            "Pitch must stay locked until crash/air control is implemented.");
        Assert.IsTrue((constraints & RigidbodyConstraints.FreezeRotationY) != 0,
            "Physics yaw must stay locked so only controller-owned steering rotates the bike.");
        Assert.IsTrue((constraints & RigidbodyConstraints.FreezeRotationZ) != 0,
            "Roll must stay locked until visual lean/physics are implemented.");
        LogAssert.NoUnexpectedReceived();

        Object.DestroyImmediate(instance);
#else
        Assert.Ignore("PlayMode prefab test requires the editor (AssetDatabase).");
        yield break;
#endif
    }

    [UnityTest]
    public IEnumerator GroundedBikeBody_PitchesToTerrainWhileRootStaysFrozen()
    {
#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab, $"Player prefab not found at {PrefabPath}");

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(20f, 0f, 0f));
        ground.transform.localScale = new Vector3(50f, 1f, 50f);

        GameObject instance = Object.Instantiate(prefab);
        instance.transform.SetPositionAndRotation(Vector3.up * 1f, Quaternion.identity);

        for (int i = 0; i < 30; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        PlayerBikeController controller = instance.GetComponent<PlayerBikeController>();
        Assert.IsNotNull(controller);
        Assert.IsTrue((controller.Body.constraints & RigidbodyConstraints.FreezeRotation) == RigidbodyConstraints.FreezeRotation,
            "Root Rigidbody physics rotation should remain fully frozen.");
        Assert.Greater(Mathf.Abs(controller.BikeBody.localEulerAngles.x), 1f,
            "BikeBody should visually pitch on sloped ground.");

        LogAssert.NoUnexpectedReceived();
        Object.DestroyImmediate(instance);
        Object.DestroyImmediate(ground);
#else
        Assert.Ignore("PlayMode prefab test requires the editor (AssetDatabase).");
        yield break;
#endif
    }

}
