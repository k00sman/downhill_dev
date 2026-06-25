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
        using Downhill.Input.DownhillControls controls = new();

        InputAction action = controls.asset.FindActionMap("Bike").FindAction(actionName);
        AssertPolledActionDoesNotWantInitialStateCheck(action, actionName);

        string json = controls.asset.ToJson();
        AssertPolledActionDoesNotUseInitialStateCheck(json, actionName);
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
}
