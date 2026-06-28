using System.Collections;
using Downhill.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

public class PlayerInputReaderTests : InputTestFixture
{
    private GameObject _readerObject;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
    }

    [TearDown]
    public override void TearDown()
    {
        if (_readerObject != null)
        {
            Object.DestroyImmediate(_readerObject);
        }

        base.TearDown();
    }

    [UnityTest]
    public IEnumerator Turn_ReflectsGamepadLeftStickX()
    {
        Gamepad gamepad = AddDefaultDevices();
        PlayerInputReader reader = CreateReader();
        yield return null; // Awake runs, then Update can poll controls.

        Set(gamepad.leftStick, new Vector2(1f, 0f));
        yield return null; // Update reads the value

        Assert.Greater(reader.Turn, 0.5f, "Turn should follow left stick X deflection");
    }

    [UnityTest]
    public IEnumerator PedalLeftPressed_FiresOnMouseLeftButton()
    {
        AddDefaultDevices();
        Mouse mouse = InputSystem.GetDevice<Mouse>();
        PlayerInputReader reader = CreateReader();
        bool fired = false;
        reader.PedalLeftPressed += () => fired = true;
        yield return null;

        Press(mouse.leftButton);
        yield return null;

        Assert.IsTrue(fired, "PedalLeftPressed should fire when the left mouse button is pressed");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator PedalRightPressed_FiresOnMouseRightButton()
    {
        AddDefaultDevices();
        Mouse mouse = InputSystem.GetDevice<Mouse>();
        PlayerInputReader reader = CreateReader();
        bool fired = false;
        reader.PedalRightPressed += () => fired = true;
        yield return null;

        Press(mouse.rightButton);
        yield return null;

        Assert.IsTrue(fired, "PedalRightPressed should fire when the right mouse button is pressed");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator PedalLeftPressed_FiresOnLeftTrigger()
    {
        Gamepad gamepad = AddDefaultDevices();
        PlayerInputReader reader = CreateReader();
        bool fired = false;
        reader.PedalLeftPressed += () => fired = true;
        yield return null;

        Set(gamepad.leftTrigger, 1f);
        yield return null;

        Assert.IsTrue(fired, "PedalLeftPressed should fire when the left trigger is pressed");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator PedalRightPressed_FiresOnRightTrigger()
    {
        Gamepad gamepad = AddDefaultDevices();
        PlayerInputReader reader = CreateReader();
        bool fired = false;
        reader.PedalRightPressed += () => fired = true;
        yield return null;

        Set(gamepad.rightTrigger, 1f);
        yield return null;

        Assert.IsTrue(fired, "PedalRightPressed should fire when the right trigger is pressed");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator FrontBrake_ReflectsLeftShoulder()
    {
        Gamepad gamepad = AddDefaultDevices();
        PlayerInputReader reader = CreateReader();
        yield return null;

        Press(gamepad.leftShoulder);
        yield return null;

        Assert.Greater(reader.FrontBrake, 0.5f, "FrontBrake should read the left shoulder button");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator RearBrake_ReflectsRightShoulder()
    {
        Gamepad gamepad = AddDefaultDevices();
        PlayerInputReader reader = CreateReader();
        yield return null;

        Press(gamepad.rightShoulder);
        yield return null;

        Assert.Greater(reader.RearBrake, 0.5f, "RearBrake should read the right shoulder button");
        LogAssert.NoUnexpectedReceived();
    }

    private static Gamepad AddDefaultDevices()
    {
        InputSystem.AddDevice<Keyboard>();
        InputSystem.AddDevice<Mouse>();
        return InputSystem.AddDevice<Gamepad>();
    }

    private PlayerInputReader CreateReader()
    {
        _readerObject = new GameObject("reader");
        return _readerObject.AddComponent<PlayerInputReader>();
    }
}
