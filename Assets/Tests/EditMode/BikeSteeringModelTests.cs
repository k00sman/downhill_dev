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
            velocityAlignmentYawRateDegreesPerSecond = 45f,
            lowSpeedDownhillAlignmentYawRateDegreesPerSecond = 70f,
            downhillAlignmentMinSlopeDegrees = 5f,
            slopeSteerResponse = 6f,
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
    public void StepYawDeltaDegrees_StoppedFacingUphill_AlignsTowardDownhill()
    {
        BikeSteeringModel model = MakeModel();
        Vector3 uphillNormal = new(0f, Mathf.Cos(20f * Mathf.Deg2Rad), -Mathf.Sin(20f * Mathf.Deg2Rad));

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.zero, 0f, uphillNormal.normalized, 0.02f);

        Assert.Greater(Mathf.Abs(yaw), 0.0001f,
            "A stopped bike facing uphill should align toward the downhill fall line instead of waiting for speed.");
    }

    [Test]
    public void StepYawDeltaDegrees_LateralVelocity_AlignsTowardTravelDirection()
    {
        BikeSteeringModel model = MakeModel();
        model.turnRateDegreesPerSecond = 0f;
        model.slopeInfluence = 0f;

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.right * 2f, 0f, Vector3.up, 0.02f);

        Assert.Greater(yaw, 0f,
            "When the bike is sliding right, heading alignment should yaw toward that travel direction.");
    }

    [Test]
    public void StepYawDeltaDegrees_AlreadyAlignedWithVelocity_DoesNotAddAlignmentYaw()
    {
        BikeSteeringModel model = MakeModel();
        model.turnRateDegreesPerSecond = 0f;
        model.slopeInfluence = 0f;

        float yaw = model.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 2f, 0f, Vector3.up, 0.02f);

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

    [Test]
    public void StepYawDeltaDegrees_SlopeSteer_NormalFlip_SmoothsOppositeYaw()
    {
        BikeSteeringModel smoothed = MakeModel();
        smoothed.turnRateDegreesPerSecond = 0f;
        smoothed.slopeInfluence = 100f;
        smoothed.slopeSteerResponse = 4f;

        Vector3 higherRight = new Vector3(-0.3f, 0.95f, 0f).normalized;
        Vector3 higherLeft = new Vector3(0.3f, 0.95f, 0f).normalized;

        _ = smoothed.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, 0f, higherRight, 0.02f);
        float smoothedFlipYaw = smoothed.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, 0f, higherLeft, 0.02f);

        BikeSteeringModel instant = MakeModel();
        instant.turnRateDegreesPerSecond = 0f;
        instant.slopeInfluence = 100f;
        instant.slopeSteerResponse = 100000f;
        float instantFlipYaw = instant.StepYawDeltaDegrees(
            Vector3.forward, Vector3.forward * 5f, 0f, higherLeft, 0.02f);

        Assert.Less(Mathf.Abs(smoothedFlipYaw), Mathf.Abs(instantFlipYaw) * 0.25f,
            "Slope steering should smooth abrupt terrain-normal flips instead of snapping to the full opposite yaw.");
    }
}
