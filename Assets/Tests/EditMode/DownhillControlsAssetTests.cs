using System.Linq;
using System.Text.RegularExpressions;
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

    [TestCase("PedalLeft", "<Mouse>/leftButton", "Keyboard&Mouse")]
    [TestCase("PedalRight", "<Mouse>/rightButton", "Keyboard&Mouse")]
    [TestCase("PedalLeft", "<Gamepad>/leftTrigger", "Gamepad")]
    [TestCase("PedalRight", "<Gamepad>/rightTrigger", "Gamepad")]
    [TestCase("FrontBrake", "<Keyboard>/w", "Keyboard&Mouse")]
    [TestCase("RearBrake", "<Keyboard>/s", "Keyboard&Mouse")]
    [TestCase("FrontBrake", "<Gamepad>/leftShoulder", "Gamepad")]
    [TestCase("RearBrake", "<Gamepad>/rightShoulder", "Gamepad")]
    public void BikeAction_UsesShippedBinding(string actionName, string expectedPath, string expectedGroup)
    {
        InputActionMap map = _asset.FindActionMap("Bike");
        AssertActionHasBinding(map.FindAction(actionName), actionName, expectedPath, expectedGroup);
    }

    [TestCase("PedalLeft", "<Mouse>/leftButton", "Keyboard&Mouse")]
    [TestCase("PedalRight", "<Mouse>/rightButton", "Keyboard&Mouse")]
    [TestCase("PedalLeft", "<Gamepad>/leftTrigger", "Gamepad")]
    [TestCase("PedalRight", "<Gamepad>/rightTrigger", "Gamepad")]
    [TestCase("FrontBrake", "<Keyboard>/w", "Keyboard&Mouse")]
    [TestCase("RearBrake", "<Keyboard>/s", "Keyboard&Mouse")]
    [TestCase("FrontBrake", "<Gamepad>/leftShoulder", "Gamepad")]
    [TestCase("RearBrake", "<Gamepad>/rightShoulder", "Gamepad")]
    public void GeneratedWrapper_BikeAction_UsesShippedBinding(
        string actionName, string expectedPath, string expectedGroup)
    {
        Downhill.Input.DownhillControls controls = new();
        try
        {
            InputActionMap map = controls.asset.FindActionMap("Bike");
            AssertActionHasBinding(map.FindAction(actionName), actionName, expectedPath, expectedGroup);
        }
        finally
        {
            DestroyGeneratedControls(controls);
        }
    }

    [TestCase("FrontBrake")]
    [TestCase("RearBrake")]
    [TestCase("Turn")]
    [TestCase("Freelook")]
    public void PolledAction_DoesNotWantInitialStateCheck(string actionName)
    {
        InputActionMap map = _asset.FindActionMap("Bike");
        InputAction action = map.FindAction(actionName);
        AssertPolledActionDoesNotWantInitialStateCheck(action, actionName);

        string json = _asset.ToJson();
        AssertPolledActionDoesNotUseInitialStateCheck(json, actionName);
    }

    [TestCase("FrontBrake")]
    [TestCase("RearBrake")]
    [TestCase("Turn")]
    [TestCase("Freelook")]
    public void GeneratedWrapper_PolledAction_DoesNotWantInitialStateCheck(string actionName)
    {
        Downhill.Input.DownhillControls controls = new();
        try
        {
            InputAction action = controls.asset.FindActionMap("Bike").FindAction(actionName);
            AssertPolledActionDoesNotWantInitialStateCheck(action, actionName);

            string json = controls.asset.ToJson();
            AssertPolledActionDoesNotUseInitialStateCheck(json, actionName);
        }
        finally
        {
            DestroyGeneratedControls(controls);
        }
    }

    private static void AssertPolledActionDoesNotWantInitialStateCheck(InputAction action, string actionName)
    {
        Assert.IsNotNull(action, $"Action '{actionName}' missing");
        Assert.AreEqual(InputActionType.PassThrough, action.type,
            $"'{actionName}' is polled by PlayerInputReader.Update; Value actions always force initial-state checks.");
        Assert.IsFalse(action.wantsInitialStateCheck,
            $"'{actionName}' should not schedule InputSystem.onBeforeUpdate initial-state checks.");
    }

    private static void AssertPolledActionDoesNotUseInitialStateCheck(string json, string actionName)
    {
        string pattern = $"\"name\"\\s*:\\s*\"{Regex.Escape(actionName)}\"[\\s\\S]*?\"initialStateCheck\"\\s*:\\s*false";
        Assert.IsTrue(Regex.IsMatch(json, pattern),
            $"'{actionName}' is polled by PlayerInputReader.Update and should not run initial-state callbacks.");
    }

    private static void AssertActionHasBinding(
        InputAction action, string actionName, string expectedPath, string expectedGroup)
    {
        Assert.IsNotNull(action, $"Action '{actionName}' missing");
        Assert.IsTrue(action.bindings.Any(b => b.path == expectedPath
                                              && b.groups != null
                                              && b.groups.Contains(expectedGroup)),
            $"'{actionName}' should bind {expectedPath} for {expectedGroup}.");
    }

    private static void DestroyGeneratedControls(Downhill.Input.DownhillControls controls)
    {
        if (controls.asset != null)
        {
            UnityEngine.Object.DestroyImmediate(controls.asset);
        }
    }
}
