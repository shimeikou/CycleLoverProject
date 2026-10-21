using System.Linq;
using CyclingLove.Flow;
using CyclingLove.Progression;
using CyclingLove.Services;
using CyclingLove.State;
using NUnit.Framework;

public sealed class ProfileProgressTests
{
    [Test]
    public void CompletedRunIsRecordedOnceAndUnlocksAnnouncer()
    {
        var profile = new ProfileProgress();
        var run = FinishedRun("run-1", "annual_incomplete", false);

        Assert.IsTrue(ProfileProgressService.RegisterCompletedRun(profile, run));
        Assert.IsFalse(ProfileProgressService.RegisterCompletedRun(profile, run));
        Assert.AreEqual(1, profile.CompletedAnnualRuns);
        CollectionAssert.Contains(profile.ClearedEndingIds, "annual_incomplete");
        CollectionAssert.Contains(profile.UnlockedHeroineIds, ProfileProgressService.AnnouncerHeroineId);
        CollectionAssert.DoesNotContain(profile.UnlockedHeroineIds, ProfileProgressService.OnlineRivalHeroineId);
    }

    [Test]
    public void NormalSuccessUnlocksBothHiddenHeroinesForNextRun()
    {
        var profile = new ProfileProgress();
        Assert.IsTrue(ProfileProgressService.RegisterCompletedRun(
            profile, FinishedRun("run-2", "annual_confession_cafe", true)));

        var nextRun = new CampaignGameService(
            CampaignContentLoader.LoadAnnual(), null,
            AnnualCalendarContentLoader.LoadRuntime()).NewGame(profile);

        Assert.IsTrue(nextRun.FindHeroineProgress(ProfileProgressService.AnnouncerHeroineId).IsUnlocked);
        Assert.IsTrue(nextRun.FindHeroineProgress(ProfileProgressService.OnlineRivalHeroineId).IsUnlocked);
        Assert.AreEqual(5, nextRun.Physical);
        Assert.AreEqual(1000, nextRun.Money);
        Assert.AreEqual(0, nextRun.HeroineProgresses[0].Affection);
    }

    [Test]
    public void UnfinishedRunDoesNotChangeProfile()
    {
        var profile = new ProfileProgress();
        var run = FinishedRun("run-3", "annual_confession_bikeshop", true);
        run.SavedPhase = GameFlowPhase.DailySummary;

        Assert.IsFalse(ProfileProgressService.RegisterCompletedRun(profile, run));
        Assert.AreEqual(0, profile.CompletedAnnualRuns);
    }

    [Test]
    public void RouteChoicesFollowPersistentUnlocks()
    {
        var routeAction = CampaignContentLoader.LoadAnnual().Actions.Single(value => value.Id == "evt_annual_route_select");
        var firstRun = new CampaignGameService(
            CampaignContentLoader.LoadAnnual(), null, AnnualCalendarContentLoader.LoadRuntime()).NewGame();
        Assert.IsFalse(routeAction.Choices.Single(value => value.Id == "annual_route_announcer").CanChoose(firstRun));
        Assert.IsFalse(routeAction.Choices.Single(value => value.Id == "annual_route_online_rival").CanChoose(firstRun));

        var profile = new ProfileProgress
        {
            CompletedAnnualRuns = 1,
            ClearedEndingIds = new System.Collections.Generic.List<string> { "annual_confession_cousin" }
        };
        var nextRun = new CampaignGameService(
            CampaignContentLoader.LoadAnnual(), null, AnnualCalendarContentLoader.LoadRuntime()).NewGame(profile);
        Assert.IsTrue(routeAction.Choices.Single(value => value.Id == "annual_route_announcer").CanChoose(nextRun));
        Assert.IsTrue(routeAction.Choices.Single(value => value.Id == "annual_route_online_rival").CanChoose(nextRun));
    }

    private static GamePlayerData FinishedRun(string runId, string endingId, bool succeeded) => new()
    {
        RunId = runId,
        SavedPhase = GameFlowPhase.Finished,
        AnnualEndingResult = new AnnualEndingRecord
        {
            IsResolved = true,
            EndingId = endingId,
            ResultFlag = "result_test",
            Succeeded = succeeded,
            EvaluatedYear = 2027,
            EvaluatedMonth = 3,
            EvaluatedDay = 31
        }
    };
}
