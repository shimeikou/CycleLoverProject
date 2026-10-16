using System;
using System.Flow;
using System.Linq;
using System.Service;
using CyclingLove.Calendar;
using NUnit.Framework;
using UnityEngine;

public sealed class AnnualCalendarTests
{
    [Test]
    public void RuntimeCalendarCoversOneYearWithFourSeasonsAndWeekendRules()
    {
        var definition = AnnualCalendarContentLoader.LoadRuntime();
        var service = new AnnualCalendarService(definition);

        Assert.AreEqual("spring", service.ResolveDay(new DateTime(2026, 4, 1)).SeasonId);
        Assert.IsFalse(service.ResolveDay(new DateTime(2026, 4, 1)).IsHoliday);
        Assert.IsTrue(service.ResolveDay(new DateTime(2026, 4, 4)).IsHoliday);
        Assert.AreEqual("summer", service.ResolveDay(new DateTime(2026, 7, 1)).SeasonId);
        Assert.AreEqual("autumn", service.ResolveDay(new DateTime(2026, 10, 1)).SeasonId);
        Assert.AreEqual("winter", service.ResolveDay(new DateTime(2027, 1, 1)).SeasonId);

        var finalDay = service.ResolveDay(new DateTime(2027, 3, 31));
        Assert.IsTrue(finalDay.IsHoliday);
        Assert.IsTrue(finalDay.IsFinalDay);
        CollectionAssert.Contains(finalDay.SpecialSlotIds, "annual_ending");
    }

    [Test]
    public void SeasonalGoalsUseExternalConditionsAndReturnOutcomeFlags()
    {
        var service = new AnnualCalendarService(AnnualCalendarContentLoader.LoadRuntime());
        var data = new System.GameSession.GamePlayerData();

        var incomplete = service.EvaluateSeasonGoal(data, "spring");
        Assert.IsFalse(incomplete.Succeeded);
        Assert.AreEqual("goal_spring_incomplete", incomplete.ResultFlag);

        data.ActiveRouteFlags.Add("goal_spring_ready");
        var succeeded = service.EvaluateSeasonGoal(data, "spring");
        Assert.IsTrue(succeeded.Succeeded);
        Assert.AreEqual("goal_spring_succeeded", succeeded.ResultFlag);
    }

    [Test]
    public void EndingResolutionUsesPriorityAndAlwaysHasFallback()
    {
        var service = new AnnualCalendarService(AnnualCalendarContentLoader.LoadRuntime());
        var data = new System.GameSession.GamePlayerData();

        Assert.AreEqual("annual_incomplete", service.ResolveEnding(data).Id);
        data.ActiveRouteFlags.Add("annual_confession_ready");
        Assert.AreEqual("annual_confession", service.ResolveEnding(data).Id);
    }

    [Test]
    public void SeasonGoalResultIsAppliedOnceAndSurvivesSaveRoundTrip()
    {
        var service = new AnnualProgressService(AnnualCalendarContentLoader.LoadRuntime());
        var data = AnnualPlayer(2026, 6, 30);
        data.ActiveRouteFlags.Add("goal_spring_ready");

        var first = service.ResolveSeasonGoal(data, "spring");
        var repeated = service.ResolveSeasonGoal(data, "spring");

        Assert.IsTrue(first.Applied);
        Assert.IsTrue(first.Succeeded);
        Assert.IsFalse(repeated.Applied);
        Assert.AreEqual(1, data.SeasonGoalResults.Count);
        CollectionAssert.Contains(data.ActiveRouteFlags, "goal_spring_succeeded");
        var restored = JsonUtility.FromJson<System.GameSession.GamePlayerData>(JsonUtility.ToJson(data));
        PlayerSaveValidator.Validate(restored);
        Assert.AreEqual("goal_spring", restored.SeasonGoalResults[0].GoalId);
    }

    [Test]
    public void EndingCannotResolveEarlyAndFinalResultIsAppliedOnce()
    {
        var service = new AnnualProgressService(AnnualCalendarContentLoader.LoadRuntime());
        var data = AnnualPlayer(2027, 3, 30);
        Assert.Throws<InvalidOperationException>(() => service.ResolveEnding(data));

        data.Day = 31;
        data.ActiveRouteFlags.Add("annual_confession_ready");
        var first = service.ResolveEnding(data);
        var repeated = service.ResolveEnding(data);
        Assert.IsTrue(first.Applied);
        Assert.IsFalse(repeated.Applied);
        Assert.AreEqual("annual_confession", data.AnnualEndingResult.EndingId);
        CollectionAssert.Contains(data.ActiveRouteFlags, "result_annual_confession");
    }

    [Test]
    public void CampaignDayPublishesCalendarMetadataAndAppliesDueGoalBeforeAdvancing()
    {
        var definition = CampaignContentLoader.LoadPrototype();
        definition.StartYear = 2026;
        definition.StartMonth = 6;
        definition.StartDay = 30;
        definition.Days = new[] { definition.Days[0] };
        var flow = new CampaignGameService(definition, null, AnnualCalendarContentLoader.LoadRuntime());
        var data = flow.NewGame();
        data.ActiveRouteFlags.Add("goal_spring_ready");

        var opening = flow.Request(data);
        Assert.AreEqual("spring", opening.SeasonId);
        Assert.AreEqual("春", opening.SeasonDisplayName);
        CollectionAssert.Contains(opening.SpecialSlotIds, "season_spring_review");

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
                    input.SelectedActionId = flow.GetActionOptions(data).First(value => value.CanExecute).ActionId;
                    break;
                case GameFlowPhase.NovelChoice:
                    input.CompletionType = GameFlowCompletionType.NovelChoiceSelected;
                    input.SelectedChoiceId = flow.GetActionOptions(data).First(value => value.CanExecute).ActionId;
                    break;
                default:
                    Assert.Fail("Unexpected phase: " + data.SavedPhase);
                    break;
            }
            Assert.IsTrue(flow.Submit(data, input));
        }

        Assert.AreEqual(GameFlowPhase.DailySummary, data.SavedPhase);
        Assert.IsTrue(flow.Submit(data, new GameSceneResult
        {
            CompletionType = GameFlowCompletionType.SummaryClosed,
            ExpectedRevision = data.Revision
        }));
        Assert.AreEqual(GameFlowPhase.Finished, data.SavedPhase);
        Assert.AreEqual(1, data.SeasonGoalResults.Count);
        Assert.AreEqual("goal_spring", data.SeasonGoalResults[0].GoalId);
        Assert.AreEqual(2026, data.SeasonGoalResults[0].EvaluatedYear);
        Assert.AreEqual(6, data.SeasonGoalResults[0].EvaluatedMonth);
        Assert.AreEqual(30, data.SeasonGoalResults[0].EvaluatedDay);
    }

    [Test]
    public void AnnualCampaignMaterializesThinScheduleIntoAValidatedYear()
    {
        var definition = CampaignContentLoader.LoadAnnual();
        var start = new DateTime(definition.StartYear, definition.StartMonth, definition.StartDay);

        Assert.AreEqual(365, definition.Days.Length);
        Assert.AreEqual("仕事と自由行動", Day(new DateTime(2026, 4, 1)).Title);
        Assert.AreEqual("休日の過ごし方", Day(new DateTime(2026, 4, 4)).Title);
        Assert.AreEqual("季節の振り返り", Day(new DateTime(2026, 6, 30)).Title);
        Assert.AreEqual("一年の最終日", Day(new DateTime(2027, 3, 31)).Title);
        Assert.AreEqual("annual_2027_03_31", definition.Days[^1].Id);
        Assert.DoesNotThrow(() => new CampaignGameService(
            definition, null, AnnualCalendarContentLoader.LoadRuntime()));

        CyclingLove.Content.CampaignDay Day(DateTime date) => definition.Days[(date - start).Days];
    }

    private static System.GameSession.GamePlayerData AnnualPlayer(int year, int month, int day) => new()
    {
        FlowVersion = PlayerSaveValidator.SchemaVersion,
        Year = year,
        Month = month,
        Day = day,
        DayStartStats = new int[7]
    };
}
