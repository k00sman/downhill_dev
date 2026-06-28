using Downhill.Player;
using NUnit.Framework;

public class PedalInputEvaluatorTests
{
    private static PedalInputEvaluator MakeEvaluator()
    {
        return new()
        {
            cadenceWindowSeconds = 0.5f,
            basePressDrive = 0.2f,
            alternatingDriveBonus = 0.8f,
            sameSideDrive = 0.05f,
            fallbackDrive = 0.4f,
            useSinglePedalFallback = false,
        };
    }

    [Test]
    public void EvaluatePress_AlternatingWithinCadenceWindow_ReturnsDriveWithBonus()
    {
        PedalInputEvaluator evaluator = MakeEvaluator();

        float starter = evaluator.EvaluatePress(PedalSide.Left, 1f);
        float alternating = evaluator.EvaluatePress(PedalSide.Right, 1.25f);

        Assert.AreEqual(evaluator.basePressDrive, starter, 0.001f,
            "The first press should start cadence without granting the alternation bonus.");
        Assert.AreEqual(evaluator.basePressDrive + evaluator.alternatingDriveBonus, alternating, 0.001f,
            "A fresh opposite-side press should grant the cadence drive bonus.");
    }

    [Test]
    public void EvaluatePress_RepeatedSameSideWithinWindow_ReturnsReducedDrive()
    {
        PedalInputEvaluator evaluator = MakeEvaluator();

        evaluator.EvaluatePress(PedalSide.Left, 1f);
        float repeated = evaluator.EvaluatePress(PedalSide.Left, 1.2f);
        evaluator.Reset();
        evaluator.EvaluatePress(PedalSide.Left, 1f);
        float alternating = evaluator.EvaluatePress(PedalSide.Right, 1.2f);

        Assert.AreEqual(evaluator.sameSideDrive, repeated, 0.001f,
            "Same-side spam should use the reduced same-side drive value.");
        Assert.Less(repeated, alternating,
            "Same-side spam must be less effective than alternating pedal input.");
    }

    [Test]
    public void EvaluatePress_StaleInputTimeout_RestartsCadenceWithoutBonus()
    {
        PedalInputEvaluator evaluator = MakeEvaluator();

        evaluator.EvaluatePress(PedalSide.Left, 1f);
        float staleOppositeSide = evaluator.EvaluatePress(PedalSide.Right, 1.75f);

        Assert.AreEqual(evaluator.basePressDrive, staleOppositeSide, 0.001f,
            "A press after the cadence window should restart cadence instead of receiving the alternation bonus.");
    }

    [Test]
    public void EvaluatePress_SinglePedalFallback_ReturnsFallbackDriveForAnySide()
    {
        PedalInputEvaluator evaluator = MakeEvaluator();
        evaluator.useSinglePedalFallback = true;

        float left = evaluator.EvaluatePress(PedalSide.Left, 1f);
        float sameLeft = evaluator.EvaluatePress(PedalSide.Left, 1.1f);
        float right = evaluator.EvaluatePress(PedalSide.Right, 1.2f);

        Assert.AreEqual(evaluator.fallbackDrive, left, 0.001f);
        Assert.AreEqual(evaluator.fallbackDrive, sameLeft, 0.001f);
        Assert.AreEqual(evaluator.fallbackDrive, right, 0.001f);
    }
}
