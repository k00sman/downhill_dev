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
