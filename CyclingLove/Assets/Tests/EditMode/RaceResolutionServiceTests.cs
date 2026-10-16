using System.Service;
using CyclingLove.Content;
using NUnit.Framework;
using UnityEngine;

public sealed class RaceResolutionServiceTests
{
    [Test]
    public void RaceEvaluationIsDeterministicAndTrainingCanReachTarget()
    {
        var equipment = EquipmentContentLoader.LoadRuntime();
        var data = new CampaignGameService(CampaignContentLoader.LoadRuntime(), equipment).NewGame();
        var service = new RaceResolutionService(RaceContentLoader.LoadRuntime(), equipment);

        var initial = service.Evaluate(data, "race_hillclimb_trial");
        Assert.IsFalse(initial.Succeeded);
        Assert.AreEqual(initial.Score, service.Evaluate(data, "race_hillclimb_trial").Score);

        data.Physical = 8;
        data.Toughness = 8;
        var trained = service.Evaluate(data, "race_hillclimb_trial");
        Assert.IsTrue(trained.Succeeded);
        Assert.GreaterOrEqual(trained.Score, trained.TargetScore);
    }

    [Test]
    public void AeroWheelsBenefitFlatCourseMoreThanClimbCourse()
    {
        var equipment = EquipmentContentLoader.LoadRuntime();
        var data = new CampaignGameService(CampaignContentLoader.LoadRuntime(), equipment).NewGame();
        var service = new RaceResolutionService(RaceContentLoader.LoadRuntime(), equipment);
        var flatBefore = service.Evaluate(data, "race_town_flat_trial").Score;
        var climbBefore = service.Evaluate(data, "race_hillclimb_trial").Score;

        data.OwnedPartIds.Add("wheels_heliocentric_aero_01");
        data.EquippedBike.WheelPartId = "wheels_heliocentric_aero_01";
        var flatGain = service.Evaluate(data, "race_town_flat_trial").Score - flatBefore;
        var climbGain = service.Evaluate(data, "race_hillclimb_trial").Score - climbBefore;

        Assert.Greater(flatGain, climbGain);
        Assert.AreEqual(15, flatGain);
        Assert.AreEqual(2, climbGain);
    }

    [Test]
    public void SuccessfulAttemptAppliesRewardAndResultOnlyOnceAndPersists()
    {
        var equipment = EquipmentContentLoader.LoadRuntime();
        var data = new CampaignGameService(CampaignContentLoader.LoadRuntime(), equipment).NewGame();
        var service = new RaceProgressService(RaceContentLoader.LoadRuntime(), equipment);
        data.Physical = 8;
        data.Toughness = 8;
        var moneyBefore = data.Money;

        var first = service.ResolveAndApply(data, "race_hillclimb_trial");
        var repeated = service.ResolveAndApply(data, "race_hillclimb_trial");

        Assert.IsTrue(first.Applied);
        Assert.IsTrue(first.Succeeded);
        Assert.AreEqual(7000, first.RewardMoney);
        Assert.IsFalse(repeated.Applied);
        Assert.AreEqual(moneyBefore + 7000, data.Money);
        Assert.AreEqual(1, data.RaceAttempts.Count);
        Assert.Contains("race_hillclimb_trial_cleared", data.ActiveRouteFlags);
        Assert.IsTrue(new CampaignCondition { Type = "race_attempted", Id = "race_hillclimb_trial" }.IsMet(data));
        Assert.IsTrue(new CampaignCondition { Type = "race_succeeded", Id = "race_hillclimb_trial" }.IsMet(data));

        var restored = JsonUtility.FromJson<System.GameSession.GamePlayerData>(JsonUtility.ToJson(data));
        PlayerSaveValidator.Validate(restored);
        Assert.AreEqual(first.Score, restored.RaceAttempts[0].Score);
        Assert.AreEqual(first.ResultRouteFlag, restored.RaceAttempts[0].ResultRouteFlag);
    }

    [Test]
    public void FailedAttemptRecordsRetryFlagWithoutReward()
    {
        var equipment = EquipmentContentLoader.LoadRuntime();
        var data = new CampaignGameService(CampaignContentLoader.LoadRuntime(), equipment).NewGame();
        var service = new RaceProgressService(RaceContentLoader.LoadRuntime(), equipment);
        var moneyBefore = data.Money;

        var result = service.ResolveAndApply(data, "race_hillclimb_trial");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(0, result.RewardMoney);
        Assert.AreEqual(moneyBefore, data.Money);
        Assert.Contains("race_hillclimb_trial_retry", data.ActiveRouteFlags);
        Assert.IsFalse(new CampaignCondition { Type = "race_succeeded", Id = "race_hillclimb_trial" }.IsMet(data));
    }

    [Test]
    public void SameRaceIsAppliedOncePerDayWithoutUiAttemptIds()
    {
        var equipment = EquipmentContentLoader.LoadRuntime();
        var data = new CampaignGameService(CampaignContentLoader.LoadRuntime(), equipment).NewGame();
        var service = new RaceProgressService(RaceContentLoader.LoadRuntime(), equipment);
        data.Physical = 8;
        data.Toughness = 8;
        var moneyBefore = data.Money;

        Assert.IsTrue(service.ResolveAndApply(data, "race_hillclimb_trial").Applied);
        Assert.IsFalse(service.ResolveAndApply(data, "race_hillclimb_trial").Applied);
        Assert.IsTrue(RaceProgressService.HasAttemptedToday(data, "race_hillclimb_trial"));
        Assert.IsFalse(RaceProgressService.HasAttemptedToday(data, "race_town_flat_trial"));
        Assert.AreEqual(moneyBefore + 7000, data.Money);

        data.Day++;
        Assert.IsFalse(RaceProgressService.HasAttemptedToday(data, "race_hillclimb_trial"));
        Assert.IsTrue(service.ResolveAndApply(data, "race_hillclimb_trial").Applied);
        Assert.AreEqual(moneyBefore + 14000, data.Money);
        Assert.AreEqual(2, data.RaceAttempts.Count);
    }
}
