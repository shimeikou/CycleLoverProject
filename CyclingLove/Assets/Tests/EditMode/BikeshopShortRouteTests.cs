using System.Collections.Generic;
using CyclingLove.Flow;
using CyclingLove.Services;
using CyclingLove.State;
using NUnit.Framework;
using UnityEngine;

public sealed class BikeshopShortRouteTests
{
    [Test]
    public void BikeshopPresentationIdsResolveAndLegacySpriteRemainsUsable()
    {
        var definition = CampaignContentLoader.LoadRuntime();
        var visit = System.Array.Find(definition.Actions, x => x.Id == "act_bikeshop_visit");
        Assert.AreEqual("bikeshop_short", definition.ScenarioId);
        Assert.AreEqual("action_bikeshop_visit", visit.NodeId);
        Assert.AreEqual("bikeshop_interior", visit.BackgroundId);
        Assert.AreEqual("hero_bikeshop", visit.CharacterId);
        Assert.AreEqual("shop", visit.OutfitId);
        Assert.AreEqual("neutral", visit.ExpressionId);
        Assert.IsNotNull(Resources.Load<Sprite>($"Images/Characters/{visit.CharacterId}_{visit.ExpressionId}"));

        var opening = new CampaignGameService(definition).Request(new CampaignGameService(definition).NewGame());
        Assert.AreEqual("bikeshop_short", opening.ScenarioId);
        Assert.AreEqual("day01_morning", opening.NodeId);
        Assert.AreEqual("grandmother_room_morning", opening.BackgroundId);
    }

    [Test]
    public void ShortRouteMarksItsTwoSpecialHolidays()
    {
        var definition = CampaignContentLoader.LoadRuntime();
        Assert.IsTrue(definition.Days[4].IsHoliday);
        Assert.IsTrue(definition.Days[9].IsHoliday);
        Assert.IsFalse(definition.Days[0].IsHoliday);

        var flow = new CampaignGameService(definition);
        var data = flow.NewGame();
        data.Day = 5;
        var request = flow.Request(data);
        Assert.IsTrue(request.IsHoliday);
        Assert.AreEqual("4月5日（休日）", request.DisplayDateText);
    }

    [Test]
    public void PreparedRouteReachesSuccessEndingAndSurvivesEveryCheckpoint()
    {
        var actions = new Dictionary<int, string>
        {
            [1] = "act_training_roadwork_001", [2] = "act_rest_home_001", [3] = "act_rest_home_001",
            [4] = "act_bikeshop_visit", [5] = "evt_bikeshop_first_lesson", [6] = "act_bikeshop_visit",
            [7] = "evt_bikeshop_conflict", [8] = "act_hill_training", [9] = "evt_bikeshop_eve",
            [10] = "evt_bikeshop_hillclimb"
        };
        var choices = new Dictionary<int, string>
        {
            [5] = "lesson_safe", [7] = "conflict_ask", [9] = "eve_fun"
        };

        var result = Play(actions, choices);

        Assert.AreEqual(GameFlowPhase.Finished, result.SavedPhase);
        Assert.Contains("result_bikeshop_short_success", result.ActiveRouteFlags);
        Assert.GreaterOrEqual(result.Physical, 7);
        Assert.GreaterOrEqual(result.Toughness, 7);
        Assert.GreaterOrEqual(result.FindHeroineProgress("hero_bikeshop").Affection, 3);
        Assert.AreEqual(5, result.FindHeroineProgress("hero_bikeshop").RouteStep);
    }

    [Test]
    public void EvePromiseCoversOneMissingThresholdAndEndingIsSelectedAutomatically()
    {
        var actions = new Dictionary<int, string>
        {
            [1] = "act_training_roadwork_001", [2] = "act_town_walk_001", [3] = "act_town_walk_001",
            [4] = "act_bikeshop_visit", [5] = "evt_bikeshop_first_lesson", [6] = "act_bikeshop_visit",
            [7] = "evt_bikeshop_conflict", [8] = "act_maintenance_study", [9] = "evt_bikeshop_eve",
            [10] = "evt_bikeshop_hillclimb"
        };
        var choices = new Dictionary<int, string>
        {
            [5] = "lesson_speed", [7] = "conflict_ask", [9] = "eve_fun"
        };

        var result = Play(actions, choices);

        Assert.AreEqual(6, result.Toughness, "The promise path should cover exactly one missing threshold.");
        Assert.Contains("goal_bikeshop_hill_ready", result.ActiveRouteFlags);
        Assert.Contains("result_bikeshop_short_success", result.ActiveRouteFlags);
        Assert.IsFalse(result.ActiveRouteFlags.Contains("result_bikeshop_short_retry"));
    }

    [Test]
    public void UnpreparedRouteUsesFallbackAndStillFinishes()
    {
        var actions = new Dictionary<int, string>
        {
            [1] = "act_rest_home_001", [2] = "act_rest_home_001", [3] = "act_rest_home_001",
            [4] = "act_rest_home_001", [5] = "evt_bikeshop_first_lesson", [6] = "act_rest_home_001",
            [7] = "evt_bikeshop_conflict", [8] = "act_rest_home_001", [9] = "evt_bikeshop_eve",
            [10] = "evt_bikeshop_hillclimb"
        };
        var choices = new Dictionary<int, string>
        {
            [5] = "lesson_speed", [7] = "conflict_fallback", [9] = "eve_win"
        };

        var result = Play(actions, choices);

        Assert.AreEqual(GameFlowPhase.Finished, result.SavedPhase);
        Assert.Contains("result_bikeshop_short_retry", result.ActiveRouteFlags);
        Assert.IsFalse(result.ActiveRouteFlags.Contains("result_bikeshop_short_success"));
    }

    private static GamePlayerData Play(IReadOnlyDictionary<int, string> actions, IReadOnlyDictionary<int, string> choices)
    {
        var flow = new CampaignGameService(CampaignContentLoader.LoadRuntime());
        var data = flow.NewGame();
        for (var step = 0; step < 160 && data.SavedPhase != GameFlowPhase.Finished; step++)
        {
            var day = (new System.DateTime(data.Year, data.Month, data.Day) - new System.DateTime(2026, 4, 1)).Days + 1;
            var input = new GameSceneResult { ExpectedRevision = data.Revision };
            switch (data.SavedPhase)
            {
                case GameFlowPhase.Novel:
                    input.CompletionType = GameFlowCompletionType.NovelClosed;
                    break;
                case GameFlowPhase.ActionSelection:
                    input.CompletionType = GameFlowCompletionType.ActionSelected;
                    input.SelectedActionId = actions[day];
                    break;
                case GameFlowPhase.NovelChoice:
                    input.CompletionType = GameFlowCompletionType.NovelChoiceSelected;
                    input.SelectedChoiceId = choices[day];
                    break;
                case GameFlowPhase.DailySummary:
                    input.CompletionType = GameFlowCompletionType.SummaryClosed;
                    break;
                default:
                    Assert.Fail("Unexpected phase: " + data.SavedPhase);
                    break;
            }
            Assert.IsTrue(flow.Submit(data, input), $"Rejected at day {day}, phase {data.SavedPhase}");

            var restored = JsonUtility.FromJson<GamePlayerData>(JsonUtility.ToJson(data));
            flow.Validate(restored);
            Assert.AreEqual(flow.Request(data).BodyText, flow.Request(restored).BodyText);
            data = restored;
        }
        return data;
    }
}
