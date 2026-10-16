using System;
using System.Flow;
using System.GameSession;
using System.Service;
using CyclingLove.Content;
using NUnit.Framework;
using UnityEngine;

public sealed class HeroineProgressTests
{
    [Test]
    public void MeetingHeroineCreatesSingleProgressEntry()
    {
        var flow = new ThreeDayGameService();
        var data = flow.NewGame();
        for (int day = 0; day < 3; day++)
        {
            Read(flow, data);
            Submit(flow, data, GameFlowCompletionType.ActionSelected, ThreeDayGameService.Rest);
            Read(flow, data);
            Submit(flow, data, GameFlowCompletionType.SummaryClosed);
        }
        var progress = data.FindHeroineProgress("hero_bikeshop");
        Assert.IsNotNull(progress);
        Assert.IsTrue(progress.IsMet);
        Assert.IsTrue(progress.IsUnlocked);
        Assert.AreEqual(1, progress.RouteStep);
        Assert.AreEqual(0, progress.Affection);
        Assert.AreEqual(1, data.HeroineProgresses.Count);
    }

    [Test]
    public void VersionOneSaveMigratesMissingHeroineProgressAndRoundTrips()
    {
        var data = new ThreeDayGameService().NewGame();
        data.FlowVersion = 1;
        data.MetHeroineIds.Add("hero_bikeshop");
        data.HeroineProgresses = null;
        var legacyJson = JsonUtility.ToJson(data);
        var restored = JsonUtility.FromJson<GamePlayerData>(legacyJson);
        PlayerSaveMigration.UpgradeToCurrent(restored);
        PlayerSaveValidator.Validate(restored);
        Assert.AreEqual(PlayerSaveValidator.SchemaVersion, restored.FlowVersion);
        Assert.IsTrue(restored.FindHeroineProgress("hero_bikeshop").IsMet);
        Assert.AreEqual(1, restored.FindHeroineProgress("hero_bikeshop").RouteStep);

        restored.FindHeroineProgress("hero_bikeshop").Affection = 12;
        restored.FindHeroineProgress("hero_bikeshop").RouteStep = 3;
        var roundTrip = JsonUtility.FromJson<GamePlayerData>(JsonUtility.ToJson(restored));
        PlayerSaveValidator.Validate(roundTrip);
        Assert.AreEqual(12, roundTrip.FindHeroineProgress("hero_bikeshop").Affection);
        Assert.AreEqual(3, roundTrip.FindHeroineProgress("hero_bikeshop").RouteStep);
    }

    [Test]
    public void InvalidOrDuplicateHeroineProgressIsRejected()
    {
        var data = new ThreeDayGameService().NewGame();
        data.HeroineProgresses.Add(new HeroineProgress { HeroineId = "hero_bikeshop", Affection = 101 });
        Assert.Throws<InvalidOperationException>(() => PlayerSaveValidator.Validate(data));
        data.HeroineProgresses[0].Affection = 1;
        data.HeroineProgresses.Add(new HeroineProgress { HeroineId = "hero_bikeshop" });
        Assert.Throws<InvalidOperationException>(() => PlayerSaveValidator.Validate(data));
    }

    [Test]
    public void CampaignEffectsApplyAffectionAndAdvanceRouteWithoutGoingBackwards()
    {
        var data = new ThreeDayGameService().NewGame();
        var progress = data.EnsureHeroineProgress("hero_bikeshop");
        progress.Affection = 99;
        progress.RouteStep = 3;

        new CampaignEffects { HeroineId = "hero_bikeshop", Affection = 5, RouteStep = 2 }
            .Apply(data, 100);

        Assert.AreEqual(100, progress.Affection);
        Assert.AreEqual(3, progress.RouteStep);
        Assert.IsTrue(progress.IsMet);
        Assert.IsTrue(progress.IsUnlocked);
        StringAssert.Contains("好感度 +5", new CampaignEffects { HeroineId = "hero_bikeshop", Affection = 5 }.Describe());
    }

    [Test]
    public void HeroineRewardWithoutHeroineIdIsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => new CampaignEffects { Affection = 1 }.Validate());
        Assert.Throws<InvalidOperationException>(() => new CampaignEffects { RouteStep = 2 }.Validate());
    }

    private static void Read(CampaignGameService flow, GamePlayerData data)
    {
        Submit(flow, data, GameFlowCompletionType.NovelClosed);
        Submit(flow, data, GameFlowCompletionType.NovelClosed);
    }

    private static void Submit(CampaignGameService flow, GamePlayerData data, GameFlowCompletionType type, string action = null)
    {
        Assert.IsTrue(flow.Submit(data, new GameSceneResult { ExpectedRevision = data.Revision, CompletionType = type, SelectedActionId = action }));
    }
}
