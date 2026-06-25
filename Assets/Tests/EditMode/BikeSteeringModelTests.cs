using Downhill.Player;
using NUnit.Framework;
using UnityEngine;

public class BikeSteeringModelTests
{
    private static BikeSteeringModel MakeModel()
    {
        return new()
        {
            turnRateDegreesPerSecond = 100f,
            maxYawDeltaDegrees = 8f,
            minSpeedForSteering = 0.5f,
            turnDeadzone = 0.05f,
        };
    }

    [Test]
    public void StepYawDeltaDegrees_RightInput_ReturnsPositiveYaw()
    {
        BikeSteeringModel model = MakeModel();

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, 1f, 0.02f);

        Assert.Greater(yaw, 0f, "Right turn input should produce positive yaw.");
    }

    [Test]
    public void StepYawDeltaDegrees_LeftInput_ReturnsNegativeYaw()
    {
        BikeSteeringModel model = MakeModel();

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, -1f, 0.02f);

        Assert.Less(yaw, 0f, "Left turn input should produce negative yaw.");
    }

    [Test]
    public void StepYawDeltaDegrees_AtRest_DoesNotSpinInPlace()
    {
        BikeSteeringModel model = MakeModel();

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.zero, 1f, 0.02f);

        Assert.AreEqual(0f, yaw, 0.0001f);
    }

    [Test]
    public void StepYawDeltaDegrees_InsideDeadzone_ReturnsZero()
    {
        BikeSteeringModel model = MakeModel();

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, 0.01f, 0.02f);

        Assert.AreEqual(0f, yaw, 0.0001f);
    }

    [Test]
    public void StepYawDeltaDegrees_DefaultModel_IgnoresSmallStickDrift()
    {
        BikeSteeringModel model = new();

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, 0.1f, 0.02f);

        Assert.AreEqual(0f, yaw, 0.0001f,
            "Default steering should ignore common small analog-stick drift.");
    }

    [Test]
    public void StepYawDeltaDegrees_HighTurnRate_ClampsYawDelta()
    {
        BikeSteeringModel model = MakeModel();
        model.turnRateDegreesPerSecond = 1000f;
        model.maxYawDeltaDegrees = 3f;

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, 1f, 0.02f);

        Assert.LessOrEqual(yaw, model.maxYawDeltaDegrees + 0.0001f);
    }

}
