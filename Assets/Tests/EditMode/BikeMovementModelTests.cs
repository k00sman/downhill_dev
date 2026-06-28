using Downhill.Player;
using NUnit.Framework;
using UnityEngine;

public class BikeMovementModelTests
{
    // Known, simple tuning so expected directions are obvious.
    private static BikeMovementModel MakeModel()
    {
        return new()
        {
            maxSpeed = 13.6f,
            slopeDriveGain = 0.34f,
            pedalAccel = 6.4f,
            drag = 0.02f,
            gravity = 9.81f,
            lateralGrip = 6f,
        };
    }

    // A ground normal for a plane that descends along +Z (downhill ahead).
    // Such a plane's normal tilts forward (toward +Z) by `angleDeg`.
    private static Vector3 DownhillNormal(float angleDeg)
    {
        float r = angleDeg * Mathf.Deg2Rad;
        return new Vector3(0f, Mathf.Cos(r), Mathf.Sin(r)).normalized;
    }

    private static Vector3 UphillNormal(float angleDeg)
    {
        float r = angleDeg * Mathf.Deg2Rad;
        return new Vector3(0f, Mathf.Cos(r), -Mathf.Sin(r)).normalized;
    }

    [Test]
    public void Flat_NoPedal_WithSpeed_Slows()
    {
        BikeMovementModel m = MakeModel();
        Vector3 v = Vector3.forward * 5f;
        Vector3 result = m.Step(v, Vector3.forward, Vector3.up, 0f, 0.02f);
        Assert.Less(Vector3.Dot(result, Vector3.forward), 5f);
        Assert.Greater(Vector3.Dot(result, Vector3.forward), 0f);
    }

    [Test]
    public void Downhill_NoPedal_FromRest_SpeedsUp()
    {
        BikeMovementModel m = MakeModel();
        Vector3 n = DownhillNormal(20f);
        Vector3 result = m.Step(Vector3.zero, Vector3.forward, n, 0f, 0.02f);
        Assert.Greater(Vector3.Dot(result, Vector3.forward), 0f);
    }

    [Test]
    public void Flat_Pedalling_FromRest_SpeedsUp()
    {
        BikeMovementModel m = MakeModel();
        Vector3 result = m.Step(Vector3.zero, Vector3.forward, Vector3.up, 1f, 0.02f);
        Assert.Greater(Vector3.Dot(result, Vector3.forward), 0f);
    }

    [Test]
    public void SpeedCap_Respected_OverManySteps()
    {
        BikeMovementModel m = MakeModel();
        Vector3 n = DownhillNormal(45f);
        Vector3 v = Vector3.zero;
        for (int i = 0; i < 2000; i++)
        {
            v = m.Step(v, Vector3.forward, n, 1f, 0.02f);
        }

        Assert.LessOrEqual(Vector3.Dot(v, Vector3.forward), m.maxSpeed + 0.01f);
    }

    [Test]
    public void Uphill_FromRest_BuildsDownhillVelocity()
    {
        BikeMovementModel m = MakeModel();
        // Uphill ahead: surface rises toward +Z, so its normal tilts back (-Z)
        // and facing +Z climbs.
        Vector3 n = UphillNormal(20f);
        Vector3 result = m.Step(Vector3.zero, Vector3.forward, n, 0f, 0.02f);
        Assert.Less(Vector3.Dot(result, Vector3.forward), 0f,
            "A stopped bike facing uphill should begin slipping back down the fall line.");
    }

    [Test]
    public void GentleUphill_WithModerateSpeed_CoastsAcrossShortRise()
    {
        BikeMovementModel m = MakeModel();
        // 0.4m up across 10m is a shallow trail undulation, not a wall.
        float angleDeg = Mathf.Atan2(0.4f, 10f) * Mathf.Rad2Deg;
        Vector3 groundNormal = UphillNormal(angleDeg);
        Vector3 velocity = Vector3.forward * 3f;
        float distance = 0f;
        float dt = 0.02f;

        for (int i = 0; i < 500 && distance < 10f; i++)
        {
            velocity = m.Step(velocity, Vector3.forward, groundNormal, 0f, dt);
            distance += Vector3.Dot(velocity, Vector3.forward) * dt;
        }

        Assert.GreaterOrEqual(distance, 10f,
            "A moderate entry speed should carry across a short 0.4m rise.");
        Assert.Greater(Vector3.Dot(velocity, Vector3.forward), 0f,
            "The rise should not kill all forward speed.");
    }

    [Test]
    public void SmoothBumpNormals_WithoutForces_DoNotPumpOrKillSpeed()
    {
        BikeMovementModel m = MakeModel();
        m.drag = 0f;
        m.gravity = 0f;

        Vector3 velocity = Vector3.forward * 5f;
        Vector3 uphill = UphillNormal(5f);
        Vector3 downhill = DownhillNormal(5f);

        for (int i = 0; i < 200; i++)
        {
            Vector3 normal = i % 2 == 0 ? uphill : downhill;
            velocity = m.Step(velocity, Vector3.forward, normal, 0f, 0.02f);
        }

        Assert.AreEqual(5f, Vector3.Dot(velocity, Vector3.forward), 0.01f,
            "Changing smooth ground normals should not alter speed when no forces act.");
    }

    [Test]
    public void SteepDownhill_AllowsDownwardTangentFollow()
    {
        BikeMovementModel m = MakeModel();
        m.drag = 0f;
        m.gravity = 0f;

        Vector3 velocity = Vector3.forward * 5f;
        Vector3 result = m.Step(velocity, Vector3.forward, DownhillNormal(40f), 0f, 0.02f);

        Assert.Less(result.y, -0.1f,
            "Grounded downhill movement should include downward tangent velocity so the bike follows steep terrain.");
        Assert.AreEqual(5f, Vector3.Dot(result, Vector3.forward), 0.001f,
            "Horizontal forward speed accounting should remain stable.");
    }

    [Test]
    public void SideBankedDownhill_DoesNotInjectSidewaysVelocity()
    {
        BikeMovementModel m = MakeModel();
        m.drag = 0f;
        m.gravity = 0f;

        Vector3 velocity = Vector3.forward * 5f;
        Vector3 groundNormal = new Vector3(0.25f, 0.9f, 0.25f).normalized;

        Vector3 result = m.Step(velocity, Vector3.forward, groundNormal, 0f, 0.02f);

        Assert.AreEqual(0f, result.x, 0.001f,
            "Banked terrain should not drag the bike sideways when the player is not steering.");
        Assert.AreEqual(5f, Vector3.Dot(result, Vector3.forward), 0.001f);
    }

    [Test]
    public void SideSlope_WithGravity_BuildsLateralDownhillVelocity()
    {
        BikeMovementModel m = MakeModel();
        m.drag = 0f;
        m.lateralGrip = 0f;

        float angle = 20f * Mathf.Deg2Rad;
        Vector3 groundNormal = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0f).normalized;

        Vector3 result = m.Step(Vector3.zero, Vector3.forward, groundNormal, 0f, 0.02f);

        Assert.Less(result.x, -0.001f,
            "A side slope should create lateral downhill drift instead of constraining motion to bike forward.");
        Assert.AreEqual(0f, Vector3.Dot(result, Vector3.forward), 0.001f,
            "A pure side slope should not invent forward speed.");
    }

    [Test]
    public void Flat_LateralVelocity_DampsTowardForwardTrack()
    {
        BikeMovementModel m = MakeModel();
        m.drag = 0f;
        m.gravity = 0f;
        m.lateralGrip = 8f;

        Vector3 velocity = (Vector3.forward * 3f) + (Vector3.right * 4f);

        Vector3 result = m.Step(velocity, Vector3.forward, Vector3.up, 0f, 0.02f);

        Assert.Less(Mathf.Abs(result.x), 4f,
            "Lateral grip should bleed sideways slip without deleting forward motion.");
        Assert.AreEqual(3f, Vector3.Dot(result, Vector3.forward), 0.001f);
    }

    [Test]
    public void SteepUphill_CapsUpwardLaunchVelocity()
    {
        BikeMovementModel m = MakeModel();
        m.drag = 0f;
        m.gravity = 0f;
        m.maxGroundedUpSpeed = 0.75f;

        Vector3 velocity = Vector3.forward * 5f;
        Vector3 result = m.Step(velocity, Vector3.forward, UphillNormal(40f), 0f, 0.02f);

        Assert.LessOrEqual(result.y, m.maxGroundedUpSpeed + 0.001f,
            "Grounded uphill movement should not launch the bike upward on bumps or convex terrain.");
        Assert.AreEqual(5f, Vector3.Dot(result, Vector3.forward), 0.001f);
    }

    [Test]
    public void DegenerateHeading_ReturnsInputVelocity()
    {
        BikeMovementModel m = MakeModel();
        Vector3 v = Vector3.forward * 3f;
        // facing parallel to the normal -> projected heading is ~zero.
        Vector3 result = m.Step(v, Vector3.up, Vector3.up, 1f, 0.02f);
        Assert.AreEqual(v, result);
    }

    [Test]
    public void StepDetailed_ReturnsForwardAndLateralTelemetry()
    {
        BikeMovementModel m = MakeModel();
        m.drag = 0f;
        m.gravity = 0f;
        m.lateralGrip = 0f;

        Vector3 velocity = (Vector3.forward * 3f) + (Vector3.right * 4f);

        BikeMovementResult result = m.StepDetailed(
            velocity, Vector3.forward, Vector3.up, 0f, 0f, 0.02f);

        Assert.AreEqual(3f, result.ForwardSpeed, 0.001f);
        Assert.AreEqual(4f, result.LateralSpeed, 0.001f);
        Assert.AreEqual(result.Velocity, m.Step(velocity, Vector3.forward, Vector3.up, 0f, 0f, 0.02f));
    }
}
