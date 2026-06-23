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
