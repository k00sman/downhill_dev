using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

public class DownhillControlsAssetTests
{
    private const string AssetPath = "Assets/Scripts/Input/DownhillControls.inputactions";

    private InputActionAsset _asset;

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
        InputActionMap map = _asset.FindActionMap("Bike");
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
        InputActionMap map = _asset.FindActionMap("Bike");
        foreach (InputAction action in map.actions)
        {
            bool kbm = action.bindings.Any(b => b.groups != null && b.groups.Contains("Keyboard&Mouse"));
            bool pad = action.bindings.Any(b => b.groups != null && b.groups.Contains("Gamepad"));
            Assert.IsTrue(kbm, $"'{action.name}' has no Keyboard&Mouse binding");
            Assert.IsTrue(pad, $"'{action.name}' has no Gamepad binding");
        }
    }
}
