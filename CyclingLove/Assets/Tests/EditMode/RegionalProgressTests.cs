using System.Collections.Generic;
using System.Linq;
using System;
using CyclingLove.Calendar;
using CyclingLove.Content;
using CyclingLove.Editor;
using CyclingLove.Flow;
using CyclingLove.Persistence;
using CyclingLove.Services;
using CyclingLove.State;
using NUnit.Framework;
using UnityEngine;

public sealed class RegionalProgressTests
{
    [Test]
    public void QuestRewardAppliesOnceAndPersists()
    {
        var data = ValidPlayer();
        var reward = new CampaignEffects
        {
            QuestId = "quest_test",
            RegionalContribution = 2,
            PublicAwareness = 3,
            FoxFavor = 1,
            Money = 500
        };

        reward.Apply(data, 100);
        reward.Apply(data, 100);

        Assert.AreEqual(2, data.RegionalContribution);
        Assert.AreEqual(3, data.PublicAwareness);
        Assert.AreEqual(1, data.FoxFavor);
        Assert.AreEqual(1500, data.Money);
        CollectionAssert.AreEqual(new[] { "quest_test" }, data.CompletedQuestIds);

        var restored = JsonUtility.FromJson<GamePlayerData>(JsonUtility.ToJson(data));
        PlayerSaveMigration.UpgradeToCurrent(restored);
        PlayerSaveValidator.Validate(restored);
        CollectionAssert.AreEqual(data.CompletedQuestIds, restored.CompletedQuestIds);
        Assert.AreEqual(data.RegionalContribution, restored.RegionalContribution);
    }

    [Test]
    public void RegionalConditionsReadSavedProgress()
    {
        var data = ValidPlayer();
        data.RegionalContribution = 4;
        data.PublicAwareness = 3;
        data.FoxFavor = 2;
        data.CompletedQuestIds.AddRange(new[] { "quest_a", "quest_b" });

        Assert.IsTrue(new CampaignCondition { Type = "regional_contribution", Value = 4 }.IsMet(data));
        Assert.IsTrue(new CampaignCondition { Type = "public_awareness", Value = 3 }.IsMet(data));
        Assert.IsTrue(new CampaignCondition { Type = "fox_favor", Value = 2 }.IsMet(data));
        Assert.IsTrue(new CampaignCondition { Type = "completed_quest_count", Value = 2 }.IsMet(data));
        Assert.IsTrue(new CampaignCondition { Type = "quest_completed", Id = "quest_b" }.IsMet(data));
        Assert.IsFalse(new CampaignCondition { Type = "quest_completed", Id = "quest_missing" }.IsMet(data));
    }

    [Test]
    public void VersionFourSaveInitializesRegionalQuestCollection()
    {
        var data = ValidPlayer();
        data.FlowVersion = 4;
        data.CompletedQuestIds = null;

        PlayerSaveMigration.UpgradeToCurrent(data);

        Assert.AreEqual(PlayerSaveValidator.SchemaVersion, data.FlowVersion);
        Assert.IsNotNull(data.CompletedQuestIds);
        PlayerSaveValidator.Validate(data);
    }

    [Test]
    public void EveryRouteHasOneRegionalSupportQuest()
    {
        var actions = CampaignContentLoader.LoadAnnual().Actions.ToDictionary(value => value.Id);
        var expected = new Dictionary<string, string>
        {
            ["evt_regional_bikeshop_workshop"] = "hero_bikeshop",
            ["evt_regional_cafe_menu"] = "hero_cafe_master",
            ["evt_regional_cousin_campaign"] = "hero_cousin",
            ["evt_regional_announcer_feature"] = "hero_announcer",
            ["evt_regional_rival_stream"] = "hero_online_rival"
        };

        foreach (var pair in expected)
        {
            var effects = actions[pair.Key].Effects;
            Assert.AreEqual(pair.Value, effects.HeroineId, pair.Key);
            Assert.AreEqual(1, effects.Affection, pair.Key);
            Assert.GreaterOrEqual(effects.RegionalContribution, 1, pair.Key);
            Assert.IsFalse(string.IsNullOrWhiteSpace(effects.QuestId), pair.Key);
        }
    }

    [Test]
    public void RegionalEventsConnectRouteSupportToFoxQuestCompletion()
    {
        var calendar = AnnualCalendarContentLoader.LoadRuntime();
        var flow = new CampaignGameService(CampaignContentLoader.LoadAnnual(), null, calendar);
        var data = flow.NewGame();

        PlayEvent(flow, data, new DateTime(2026, 4, 1), "annual_route_select", "annual_route_bikeshop");
        PlayEvent(flow, data, new DateTime(2026, 4, 2), "annual_bikeshop_spring_intro", "annual_reunion_memory");
        PlayEvent(flow, data, new DateTime(2026, 4, 10), "regional_grandmother_advice");
        var affectionBeforeSupport = data.FindHeroineProgress("hero_bikeshop").Affection;
        PlayEvent(flow, data, new DateTime(2026, 5, 1), "regional_bikeshop_workshop");
        PlayEvent(flow, data, new DateTime(2026, 10, 1), "regional_fox_request");
        PlayEvent(flow, data, new DateTime(2027, 2, 1), "regional_fox_completion");

        CollectionAssert.IsSubsetOf(new[]
        {
            "quest_grandmother_map",
            "quest_bikeshop_workshop",
            "quest_fox_shrine_festival"
        }, data.CompletedQuestIds);
        CollectionAssert.Contains(data.ActiveRouteFlags, "quest_fox_shrine_completed");
        Assert.AreEqual(6, data.RegionalContribution);
        Assert.AreEqual(3, data.FoxFavor);
        Assert.AreEqual(affectionBeforeSupport + 1, data.FindHeroineProgress("hero_bikeshop").Affection);
    }

    [Test]
    public void FoxQuestUsesExactQuestCountAndContributionBoundaries()
    {
        var campaign = CampaignContentLoader.LoadAnnual();
        var calendar = AnnualCalendarContentLoader.LoadRuntime();
        var data = ValidPlayer();
        data.CompletedQuestIds.Add("quest_first");

        var beforeRequest = CampaignTuningEvaluator.Evaluate(
            campaign, calendar, data, new DateTime(2026, 10, 1));
        Assert.IsFalse(beforeRequest.MatchingEvents.Any(value => value.Id == "regional_fox_request"));

        data.CompletedQuestIds.Add("quest_second");
        var atRequestBoundary = CampaignTuningEvaluator.Evaluate(
            campaign, calendar, data, new DateTime(2026, 10, 1));
        Assert.IsTrue(atRequestBoundary.MatchingEvents.Any(value => value.Id == "regional_fox_request"));

        data.ActiveRouteFlags.Add("quest_fox_shrine_started");
        data.RegionalContribution = 2;
        var beforeCompletion = CampaignTuningEvaluator.Evaluate(
            campaign, calendar, data, new DateTime(2027, 2, 1));
        Assert.IsFalse(beforeCompletion.MatchingEvents.Any(value => value.Id == "regional_fox_completion"));

        data.RegionalContribution = 3;
        var atCompletionBoundary = CampaignTuningEvaluator.Evaluate(
            campaign, calendar, data, new DateTime(2027, 2, 1));
        Assert.IsTrue(atCompletionBoundary.MatchingEvents.Any(value => value.Id == "regional_fox_completion"));
    }

    private static void PlayEvent(
        CampaignGameService flow,
        GamePlayerData data,
        DateTime date,
        string eventId,
        string choiceId = null)
    {
        SetDate(data, date);
        flow.Request(data);
        Assert.AreEqual(eventId, data.ResolvedDayEventId);

        for (var step = 0; step < 20 && data.SavedPhase != GameFlowPhase.DailySummary; step++)
        {
            var input = new GameSceneResult { ExpectedRevision = data.Revision };
            switch (data.SavedPhase)
            {
                case GameFlowPhase.Novel:
                    input.CompletionType = GameFlowCompletionType.NovelClosed;
                    break;
                case GameFlowPhase.ActionSelection:
                    input.CompletionType = GameFlowCompletionType.ActionSelected;
                    input.SelectedActionId = flow.GetActionOptions(data).Single(value => value.CanExecute).ActionId;
                    break;
                case GameFlowPhase.NovelChoice:
                    input.CompletionType = GameFlowCompletionType.NovelChoiceSelected;
                    input.SelectedChoiceId = choiceId;
                    break;
                default:
                    Assert.Fail("Unexpected phase: " + data.SavedPhase);
                    break;
            }
            Assert.IsTrue(flow.Submit(data, input));
        }
        Assert.AreEqual(GameFlowPhase.DailySummary, data.SavedPhase);
    }

    private static void SetDate(GamePlayerData data, DateTime date)
    {
        data.Year = date.Year;
        data.Month = date.Month;
        data.Day = date.Day;
        data.SavedPhase = GameFlowPhase.Novel;
        data.Timing = CyclingLove.Calendar.ActionTiming.Morning;
        data.NarrativePage = 0;
        data.SelectedActionId = null;
        data.SelectedChoiceId = null;
        data.ResolvedDayKey = null;
        data.ResolvedDayEventId = null;
        data.DayStartStats = new[]
        {
            data.Money, data.Physical, data.Toughness, data.Smart,
            data.Empathy, data.Appeal, data.Stamina
        };
    }

    private static GamePlayerData ValidPlayer() => new()
    {
        FlowVersion = PlayerSaveValidator.SchemaVersion,
        RunId = "regional-test-run",
        DayStartStats = new int[7]
    };
}
