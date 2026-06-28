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
    public void StepYawDeltaDegrees_StoppedFacingUphill_AllowsRecoveryTurn()
    {
        BikeSteeringModel model = MakeModel();
        Vector3 uphillNormal = new(0f, Mathf.Cos(20f * Mathf.Deg2Rad), -Mathf.Sin(20f * Mathf.Deg2Rad));

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.zero, 1f, uphillNormal.normalized, 0.02f);

        Assert.Greater(yaw, 0f,
            "A stopped bike facing uphill must still be able to turn back downhill.");
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

    [Test]
    public void StepYawDeltaDegrees_SlopeSteer_TurnsAwayFromStiffSlope()
    {
        BikeSteeringModel model = MakeModel();
        model.slopeInfluence = 50f;
        model.turnRateDegreesPerSecond = 0f; // test slope only

        // Facing forward (+Z), flatRight is right (+X).
        // If terrain is higher to the right, ground normal tilts to the left (-X).
        Vector3 groundNormal = new Vector3(-0.3f, 0.95f, 0f).normalized;

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, 0f, groundNormal, 0.02f);

        // We expect it to turn to the left (negative yaw) because the steep terrain is on the right (+X).
        Assert.Less(yaw, 0f, "Should turn left (negative yaw) away from the steep right slope.");
    }
}
