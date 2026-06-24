using System.Collections;
using Downhill.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

public class PlayerInputReaderTests : InputTestFixture
{
    [UnityTest]
    public IEnumerator Turn_ReflectsGamepadLeftStickX()
    {
        Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
        GameObject go = new("reader");
        PlayerInputReader reader = go.AddComponent<PlayerInputReader>();
        yield return null; // Awake + OnEnable run

        Set(gamepad.leftStick, new Vector2(1f, 0f));
        yield return null; // Update reads the value

        Assert.Greater(reader.Turn, 0.5f, "Turn should follow left stick X deflection");
        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator PedalLeftPressed_FiresOnLeftShoulder()
    {
        Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
        GameObject go = new("reader");
        PlayerInputReader reader = go.AddComponent<PlayerInputReader>();
        bool fired = false;
        reader.PedalLeftPressed += () => fired = true;
        yield return null;

        Press(gamepad.leftShoulder);
        yield return null;

        Assert.IsTrue(fired, "PedalLeftPressed should fire when the left bumper is pressed");
        Object.Destroy(go);
    }
}
