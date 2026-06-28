using Downhill.Player;
using NUnit.Framework;
using UnityEngine;

public class CameraPedalBobModelTests
{
    [Test]
    public void StepLensShift_NoPedalPower_ReturnsZero()
    {
        CameraPedalBobModel model = new()
        {
            verticalLensShiftAmplitude = 0.05f,
            frequencyHz = 2.5f,
            activityResponse = 20f,
        };

        Vector2 offset = model.StepLensShift(0f, 0.05f);

        Assert.AreEqual(Vector2.zero, offset);
    }

    [Test]
    public void StepLensShift_WithPedalPower_ReturnsSmallVerticalLensShift()
    {
        CameraPedalBobModel model = new()
        {
            verticalLensShiftAmplitude = 0.05f,
            frequencyHz = 2.5f,
            activityResponse = 1000f,
        };

        Vector2 offset = model.StepLensShift(1f, 0.05f);

        Assert.AreEqual(0f, offset.x, 0.0001f);
        Assert.Greater(Mathf.Abs(offset.y), 0.001f);
        Assert.LessOrEqual(Mathf.Abs(offset.y), model.verticalLensShiftAmplitude + 0.0001f);
    }

    [Test]
    public void StepLensShift_AfterPedalPowerStops_DecaysTowardZero()
    {
        CameraPedalBobModel model = new()
        {
            verticalLensShiftAmplitude = 0.05f,
            frequencyHz = 2.5f,
            activityResponse = 20f,
        };

        Vector2 activeOffset = model.StepLensShift(1f, 0.05f);

        Vector2 settledOffset = activeOffset;
        for (int i = 0; i < 20; i++)
        {
            settledOffset = model.StepLensShift(0f, 0.02f);
        }

        Assert.Less(Mathf.Abs(settledOffset.y), Mathf.Abs(activeOffset.y) * 0.1f,
            "Pedal camera bob should ease out when the player stops pedaling.");
    }
}
