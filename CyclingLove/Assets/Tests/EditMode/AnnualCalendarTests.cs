using System.Linq;
using System;
using CyclingLove.Calendar;
using CyclingLove.Content;
using CyclingLove.Flow;
using CyclingLove.Persistence;
using CyclingLove.Services;
using CyclingLove.State;
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
        var data = new CyclingLove.State.GamePlayerData();

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
        var data = new CyclingLove.State.GamePlayerData();

        Assert.AreEqual("annual_incomplete", service.ResolveEnding(data).Id);
        MakeConfessionReady(data);
        var ending = service.ResolveEnding(data);
        Assert.AreEqual("annual_confession_bikeshop", ending.Id);
        Assert.AreEqual("hero_bikeshop", ending.HeroineId);
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
        var restored = JsonUtility.FromJson<CyclingLove.State.GamePlayerData>(JsonUtility.ToJson(data));
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
        MakeConfessionReady(data);
        var first = service.ResolveEnding(data);
        var repeated = service.ResolveEnding(data);
        Assert.IsTrue(first.Applied);
        Assert.IsFalse(repeated.Applied);
        Assert.AreEqual("annual_confession_bikeshop", data.AnnualEndingResult.EndingId);
        CollectionAssert.Contains(data.ActiveRouteFlags, "result_annual_confession_bikeshop");
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
        Assert.AreEqual("6月30日（春・平日・特別日）", opening.DisplayDateText);

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
        Assert.AreEqual(365, definition.Days.Select(value => value.MorningNodeId).Distinct().Count());
        Assert.AreEqual(365, definition.Days.Select(value => value.AfternoonNodeId).Distinct().Count());
        Assert.AreEqual(52, definition.Actions.Length);
        Assert.AreEqual(49, definition.ConditionalEvents.Length);
        Assert.DoesNotThrow(() => new CampaignGameService(
            definition, null, AnnualCalendarContentLoader.LoadRuntime()));

        CyclingLove.Content.CampaignDay Day(DateTime date) => definition.Days[(date - start).Days];
    }

    [Test]
    public void ConditionalEventUsesPriorityAndRemainsFixedForTheDay()
    {
        var definition = CampaignContentLoader.LoadAnnual();
        definition.ConditionalEvents = new[]
        {
            ConditionalEvent(definition, "event_low", 10, "低優先イベント"),
            ConditionalEvent(definition, "event_high", 20, "高優先イベント")
        };
        var flow = new CampaignGameService(definition, null, AnnualCalendarContentLoader.LoadRuntime());
        var data = flow.NewGame();
        data.ActiveRouteFlags.Add("event_ready");
        data.ResolvedDayKey = null;
        data.ResolvedDayEventId = null;
        data.ScenarioCheckpoint.Clear();

        var first = flow.Request(data);
        Assert.AreEqual("高優先イベント", first.DisplayName);
        Assert.AreEqual("event_high", data.ResolvedDayEventId);

        data.ActiveRouteFlags.Remove("event_ready");
        var repeated = flow.Request(data);
        Assert.AreEqual("高優先イベント", repeated.DisplayName);

        var restored = JsonUtility.FromJson<CyclingLove.State.GamePlayerData>(JsonUtility.ToJson(data));
        var afterLoad = flow.Request(restored);
        Assert.AreEqual("event_high", restored.ResolvedDayEventId);
        Assert.AreEqual("高優先イベント", afterLoad.DisplayName);
    }

    [Test]
    public void SkipStopsBeforeUnreadConditionalEventAndDoesNotGrantDailyRewards()
    {
        var definition = CampaignContentLoader.LoadAnnual();
        var conditionalEvent = ConditionalEvent(definition, "event_april_3", 100, "三日のイベント");
        conditionalEvent.StartDay = 3;
        conditionalEvent.EndDay = 3;
        definition.ConditionalEvents = new[] { conditionalEvent };
        var flow = new CampaignGameService(definition, null, AnnualCalendarContentLoader.LoadRuntime());
        var data = flow.NewGame();
        data.ActiveRouteFlags.Add("event_ready");
        var initialMoney = data.Money;

        var preview = flow.GetSkipPreview(data, 7);
        Assert.AreEqual(2, preview.SkippableDays);
        Assert.AreEqual("条件イベント", preview.StopReason);
        Assert.AreEqual("4月3日", preview.StopDateText);
        Assert.IsTrue(flow.SkipNormalDays(data, 7));
        Assert.AreEqual(3, data.Day);
        Assert.AreEqual(initialMoney, data.Money);
        Assert.AreEqual("三日のイベント", flow.Request(data).DisplayName);
    }

    [Test]
    public void SkipStopsAtHolidayEvenWhenNoEventMatches()
    {
        var definition = CampaignContentLoader.LoadAnnual();
        definition.ConditionalEvents = Array.Empty<CampaignConditionalEvent>();
        var flow = new CampaignGameService(definition, null, AnnualCalendarContentLoader.LoadRuntime());
        var data = flow.NewGame();

        var preview = flow.GetSkipPreview(data, 7);
        Assert.AreEqual(3, preview.SkippableDays);
        Assert.AreEqual("休日", preview.StopReason);
        Assert.AreEqual("4月4日", preview.StopDateText);
    }

    [Test]
    public void ShortCampaignCannotUseAnnualSkip()
    {
        var flow = new CampaignGameService(
            CampaignContentLoader.LoadRuntime(), null, AnnualCalendarContentLoader.LoadRuntime());
        var data = flow.NewGame();

        var preview = flow.GetSkipPreview(data, 7);
        Assert.AreEqual(0, preview.SkippableDays);
        Assert.AreEqual("年間キャンペーン外", preview.StopReason);
        Assert.IsFalse(flow.SkipNormalDays(data, 7));
    }

    [Test]
    public void ConditionalEventCanBeLimitedToDaysOfWeek()
    {
        var definition = CampaignContentLoader.LoadAnnual();
        var conditionalEvent = ConditionalEvent(definition, "event_friday", 100, "金曜のイベント");
        conditionalEvent.Conditions = Array.Empty<CampaignCondition>();
        conditionalEvent.DaysOfWeek = new[] { "friday" };
        definition.ConditionalEvents = new[] { conditionalEvent };
        var flow = new CampaignGameService(definition, null, AnnualCalendarContentLoader.LoadRuntime());
        var data = flow.NewGame();

        Assert.AreNotEqual("金曜のイベント", flow.Request(data).DisplayName);
        var preview = flow.GetSkipPreview(data, 7);
        Assert.AreEqual("条件イベント", preview.StopReason);
        Assert.AreEqual("4月3日", preview.StopDateText);
        Assert.Throws<InvalidOperationException>(() =>
        {
            conditionalEvent.DaysOfWeek = new[] { "fri" };
            conditionalEvent.Validate();
        });
    }

    [Test]
    public void DebugStartMovesNewGameToRequestedMorningWithCustomStats()
    {
        var flow = new CampaignGameService(CampaignContentLoader.LoadAnnual(), null, AnnualCalendarContentLoader.LoadRuntime());
        var data = flow.NewGame();
        data.Physical = 9;
        data.ActiveRouteFlags.Add("goal_spring_ready");

        flow.MoveToMorning(data, new DateTime(2026, 6, 30));

        var request = flow.Request(data);
        Assert.AreEqual("6月30日", request.CurrentDateText);
        Assert.AreEqual("季節の振り返り", request.DisplayName);
        Assert.AreEqual(GameFlowPhase.Novel, data.SavedPhase);
        Assert.AreEqual(9, data.DayStartStats[1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => flow.MoveToMorning(data, new DateTime(2027, 4, 1)));
    }

    [Test]
    public void BuildingRequestsDoesNotChangeTheSavedState()
    {
        var flow = new CampaignGameService(CampaignContentLoader.LoadAnnual(), null, AnnualCalendarContentLoader.LoadRuntime());
        var data = flow.NewGame();
        Assert.AreEqual("2026-04-01", data.ResolvedDayKey);
        var before = JsonUtility.ToJson(data);

        flow.Request(data);
        flow.GetActionOptions(data);
        flow.GetSkipPreview(data, 7);

        Assert.AreEqual(before, JsonUtility.ToJson(data));
    }

    [Test]
    public void SkipStopsOnFinalDayEvenWhenItIsANormalWorkday()
    {
        var definition = CampaignContentLoader.LoadAnnual();
        definition.ConditionalEvents = Array.Empty<CampaignConditionalEvent>();
        var calendar = AnnualCalendarContentLoader.LoadRuntime();
        calendar.WeekendsAreHolidays = false;
        calendar.DateOverrides = Array.Empty<CalendarDateOverride>();
        var flow = new CampaignGameService(definition, null, calendar);
        var data = flow.NewGame();
        data.Year = 2027; data.Month = 3; data.Day = 27;

        var preview = flow.GetSkipPreview(data, 7);
        Assert.AreEqual(4, preview.SkippableDays);
        Assert.AreEqual("最終日", preview.StopReason);
        Assert.IsTrue(flow.SkipNormalDays(data, 7));
        Assert.AreEqual(31, data.Day);
        Assert.AreEqual(GameFlowPhase.Novel, data.SavedPhase);
        Assert.IsNotNull(flow.Request(data));
    }

    [Test]
    public void SkipResolvesSeasonGoalWhoseDeadlineIsASkippedDay()
    {
        var definition = CampaignContentLoader.LoadAnnual();
        definition.ConditionalEvents = Array.Empty<CampaignConditionalEvent>();
        var calendar = AnnualCalendarContentLoader.LoadRuntime();
        calendar.DateOverrides = Array.Empty<CalendarDateOverride>();
        var spring = calendar.SeasonGoals.Single(goal => goal.SeasonId == "spring");
        spring.DeadlineMonth = 4; spring.DeadlineDay = 2;
        var flow = new CampaignGameService(definition, null, calendar);
        var data = flow.NewGame();

        Assert.IsTrue(flow.SkipNormalDays(data, 7));
        Assert.AreEqual(4, data.Day);
        var record = data.SeasonGoalResults.Single(value => value.GoalId == spring.Id);
        Assert.AreEqual(2, record.EvaluatedDay);
        CollectionAssert.Contains(data.ActiveRouteFlags, "goal_spring_incomplete");
    }

    [Test]
    public void AnnualManifestStartDateMustMatchCalendar()
    {
        string Read(string key)
        {
            var source = Resources.Load<TextAsset>(key);
            if (source == null) return null;
            return key == CampaignContentLoader.AnnualResourceKey
                ? source.text.Replace("\"StartDay\": 1", "\"StartDay\": 2")
                : source.text;
        }

        var error = Assert.Throws<InvalidOperationException>(() =>
            CampaignContentLoader.Load(CampaignContentLoader.AnnualResourceKey, Read));
        StringAssert.Contains("start date", error.Message);
    }

    [Test]
    public void AnnualScheduleRejectsEventWhoseDateAndHolidayRulesCannotOverlap()
    {
        var calendar = AnnualCalendarContentLoader.LoadRuntime();
        var schedule = JsonUtility.FromJson<CampaignScheduleDefinition>(
            Resources.Load<TextAsset>("Content/AnnualSchedule").text);
        var target = schedule.ConditionalEvents[0];
        target.StartYear = target.EndYear = 2026;
        target.StartMonth = target.EndMonth = 4;
        target.StartDay = target.EndDay = 4; // Saturday
        target.SeasonId = null;
        target.SpecialSlotId = null;
        target.HolidayMode = "workday";

        var error = Assert.Throws<InvalidOperationException>(() => schedule.Validate(calendar));
        StringAssert.Contains("no matching calendar day", error.Message);
        StringAssert.Contains(target.Id, error.Message);
    }

    [Test]
    public void AnnualBikeshopRouteConnectsSeasonGoalsToConfessionEnding()
    {
        var calendar = AnnualCalendarContentLoader.LoadRuntime();
        var flow = new CampaignGameService(CampaignContentLoader.LoadAnnual(), null, calendar);
        var progress = new AnnualProgressService(calendar);
        var data = flow.NewGame();
        data.Physical = 7;
        data.Toughness = 7;
        data.Smart = 7;
        data.Empathy = 7;

        PlayEvent(flow, data, new DateTime(2026, 4, 1), "annual_route_select", "annual_route_bikeshop");
        PlayEvent(flow, data, new DateTime(2026, 4, 2), "annual_bikeshop_spring_intro", "annual_reunion_memory");
        PlayEvent(flow, data, new DateTime(2026, 6, 1), "annual_bikeshop_spring_goal", "annual_spring_together");
        SetDate(data, new DateTime(2026, 6, 30));
        Assert.IsTrue(progress.ResolveSeasonGoal(data, "spring").Succeeded);

        PlayEvent(flow, data, new DateTime(2026, 7, 10), "annual_bikeshop_summer_offer", "annual_offer_listen");
        PlayEvent(flow, data, new DateTime(2026, 9, 1), "annual_bikeshop_summer_goal", "annual_summer_race_push");
        SetDate(data, new DateTime(2026, 9, 30));
        Assert.IsTrue(progress.ResolveSeasonGoal(data, "summer").Succeeded);

        PlayEvent(flow, data, new DateTime(2026, 10, 10), "annual_bikeshop_autumn_memory", "annual_memory_future");
        PlayEvent(flow, data, new DateTime(2026, 12, 1), "annual_bikeshop_autumn_goal", "annual_autumn_both");
        SetDate(data, new DateTime(2026, 12, 31));
        Assert.IsTrue(progress.ResolveSeasonGoal(data, "autumn").Succeeded);

        PlayEvent(flow, data, new DateTime(2027, 1, 15), "annual_bikeshop_winter_climb", "annual_winter_side_by_side");
        PlayEvent(flow, data, new DateTime(2027, 3, 1), "annual_bikeshop_winter_final", "annual_final_confess");
        SetDate(data, new DateTime(2027, 3, 31));
        var results = progress.ResolveDueMilestones(data);

        Assert.AreEqual(8, data.FindHeroineProgress("hero_bikeshop").RouteStep);
        Assert.GreaterOrEqual(data.FindHeroineProgress("hero_bikeshop").Affection, 7);
        Assert.IsTrue(results.Single(value => value.DefinitionId == "goal_winter").Succeeded);
        Assert.AreEqual("annual_confession_bikeshop", data.AnnualEndingResult.EndingId);
        Assert.AreEqual("hero_bikeshop", data.AnnualEndingResult.HeroineId);
        CollectionAssert.Contains(data.ActiveRouteFlags, "result_annual_confession_bikeshop");
    }

    [Test]
    public void AnnualCafeRouteConnectsSeasonGoalsToConfessionEnding()
    {
        var calendar = AnnualCalendarContentLoader.LoadRuntime();
        var flow = new CampaignGameService(CampaignContentLoader.LoadAnnual(), null, calendar);
        var progress = new AnnualProgressService(calendar);
        var data = flow.NewGame();
        data.Smart = 7;
        data.Empathy = 7;

        PlayEvent(flow, data, new DateTime(2026, 4, 1), "annual_route_select", "annual_route_cafe");
        PlayEvent(flow, data, new DateTime(2026, 4, 2), "annual_cafe_spring_intro", "cafe_meeting_listen");
        PlayEvent(flow, data, new DateTime(2026, 6, 1), "annual_cafe_spring_goal", "cafe_spring_pace");
        SetDate(data, new DateTime(2026, 6, 30));
        Assert.IsTrue(progress.ResolveSeasonGoal(data, "spring").Succeeded);

        PlayEvent(flow, data, new DateTime(2026, 7, 10), "annual_cafe_summer_past", "cafe_past_accept");
        PlayEvent(flow, data, new DateTime(2026, 9, 1), "annual_cafe_summer_goal", "cafe_longride_break");
        SetDate(data, new DateTime(2026, 9, 30));
        Assert.IsTrue(progress.ResolveSeasonGoal(data, "summer").Succeeded);

        PlayEvent(flow, data, new DateTime(2026, 10, 10), "annual_cafe_autumn_mother", "cafe_mother_respect");
        PlayEvent(flow, data, new DateTime(2026, 12, 1), "annual_cafe_autumn_goal", "cafe_newmenu_plan");
        SetDate(data, new DateTime(2026, 12, 31));
        Assert.IsTrue(progress.ResolveSeasonGoal(data, "autumn").Succeeded);

        PlayEvent(flow, data, new DateTime(2027, 1, 15), "annual_cafe_winter_memory", "cafe_memory_present");
        PlayEvent(flow, data, new DateTime(2027, 3, 1), "annual_cafe_winter_final", "cafe_final_confess");
        SetDate(data, new DateTime(2027, 3, 31));
        var results = progress.ResolveDueMilestones(data);

        Assert.AreEqual(8, data.FindHeroineProgress("hero_cafe_master").RouteStep);
        Assert.GreaterOrEqual(data.FindHeroineProgress("hero_cafe_master").Affection, 7);
        Assert.IsTrue(results.Single(value => value.DefinitionId == "goal_winter").Succeeded);
        Assert.AreEqual("annual_confession_cafe", data.AnnualEndingResult.EndingId);
        Assert.AreEqual("hero_cafe_master", data.AnnualEndingResult.HeroineId);
        CollectionAssert.Contains(data.ActiveRouteFlags, "result_annual_confession_cafe");
    }

    [Test]
    public void AnnualCousinRouteConnectsSeasonGoalsToConfessionEnding()
    {
        var calendar = AnnualCalendarContentLoader.LoadRuntime();
        var flow = new CampaignGameService(CampaignContentLoader.LoadAnnual(), null, calendar);
        var progress = new AnnualProgressService(calendar);
        var data = flow.NewGame();
        data.Empathy = 7;
        data.Appeal = 7;

        PlayEvent(flow, data, new DateTime(2026, 4, 1), "annual_route_select", "annual_route_cousin");
        PlayEvent(flow, data, new DateTime(2026, 4, 2), "annual_cousin_spring_intro", "cousin_meeting_scenery");
        PlayEvent(flow, data, new DateTime(2026, 6, 1), "annual_cousin_spring_goal", "cousin_spring_natural");
        SetDate(data, new DateTime(2026, 6, 30));
        Assert.IsTrue(progress.ResolveSeasonGoal(data, "spring").Succeeded);

        PlayEvent(flow, data, new DateTime(2026, 7, 10), "annual_cousin_summer_viral", "cousin_viral_feeling");
        PlayEvent(flow, data, new DateTime(2026, 9, 1), "annual_cousin_summer_goal", "cousin_foodride_detour");
        SetDate(data, new DateTime(2026, 9, 30));
        Assert.IsTrue(progress.ResolveSeasonGoal(data, "summer").Succeeded);

        PlayEvent(flow, data, new DateTime(2026, 10, 10), "annual_cousin_autumn_burnout", "cousin_burnout_rest");
        PlayEvent(flow, data, new DateTime(2026, 12, 1), "annual_cousin_autumn_goal", "cousin_town_together");
        SetDate(data, new DateTime(2026, 12, 31));
        Assert.IsTrue(progress.ResolveSeasonGoal(data, "autumn").Succeeded);

        PlayEvent(flow, data, new DateTime(2027, 1, 15), "annual_cousin_winter_offline", "cousin_offline_memory");
        PlayEvent(flow, data, new DateTime(2027, 3, 1), "annual_cousin_winter_final", "cousin_final_confess");
        SetDate(data, new DateTime(2027, 3, 31));
        var results = progress.ResolveDueMilestones(data);

        Assert.AreEqual(8, data.FindHeroineProgress("hero_cousin").RouteStep);
        Assert.GreaterOrEqual(data.FindHeroineProgress("hero_cousin").Affection, 7);
        Assert.IsTrue(results.Single(value => value.DefinitionId == "goal_winter").Succeeded);
        Assert.AreEqual("annual_confession_cousin", data.AnnualEndingResult.EndingId);
        Assert.AreEqual("hero_cousin", data.AnnualEndingResult.HeroineId);
        CollectionAssert.Contains(data.ActiveRouteFlags, "result_annual_confession_cousin");
    }

    [Test]
    public void AnnualAnnouncerRouteConnectsSecondRunUnlockToConfessionEnding()
    {
        var calendar = AnnualCalendarContentLoader.LoadRuntime();
        var flow = new CampaignGameService(CampaignContentLoader.LoadAnnual(), null, calendar);
        var progress = new AnnualProgressService(calendar);
        var profile = new CyclingLove.State.ProfileProgress
        {
            CompletedAnnualRuns = 1,
            RecordedRunIds = new System.Collections.Generic.List<string> { "first-run" },
            ClearedEndingIds = new System.Collections.Generic.List<string> { "annual_incomplete" }
        };
        var data = flow.NewGame(profile);
        data.Smart = 7;
        data.Empathy = 7;
        data.Appeal = 7;

        PlayEvent(flow, data, new DateTime(2026, 4, 1), "annual_route_select", "annual_route_announcer");
        PlayEvent(flow, data, new DateTime(2026, 4, 2), "annual_announcer_spring_intro", "announcer_meeting_ride");
        PlayEvent(flow, data, new DateTime(2026, 6, 1), "annual_announcer_spring_goal", "announcer_spring_people");
        SetDate(data, new DateTime(2026, 6, 30));
        Assert.IsTrue(progress.ResolveSeasonGoal(data, "spring").Succeeded);

        PlayEvent(flow, data, new DateTime(2026, 7, 10), "annual_announcer_summer_regret", "announcer_regret_listen");
        PlayEvent(flow, data, new DateTime(2026, 9, 1), "annual_announcer_summer_goal", "announcer_summer_story");
        SetDate(data, new DateTime(2026, 9, 30));
        Assert.IsTrue(progress.ResolveSeasonGoal(data, "summer").Succeeded);

        PlayEvent(flow, data, new DateTime(2026, 10, 10), "annual_announcer_autumn_camera", "announcer_camera_voice");
        PlayEvent(flow, data, new DateTime(2026, 12, 1), "annual_announcer_autumn_goal", "announcer_autumn_live");
        SetDate(data, new DateTime(2026, 12, 31));
        Assert.IsTrue(progress.ResolveSeasonGoal(data, "autumn").Succeeded);

        PlayEvent(flow, data, new DateTime(2027, 1, 15), "annual_announcer_winter_choice", "announcer_choice_self");
        PlayEvent(flow, data, new DateTime(2027, 3, 1), "annual_announcer_winter_final", "announcer_final_confess");
        SetDate(data, new DateTime(2027, 3, 31));
        var results = progress.ResolveDueMilestones(data);

        Assert.AreEqual(8, data.FindHeroineProgress("hero_announcer").RouteStep);
        Assert.GreaterOrEqual(data.FindHeroineProgress("hero_announcer").Affection, 7);
        Assert.IsTrue(results.Single(value => value.DefinitionId == "goal_winter").Succeeded);
        Assert.AreEqual("annual_confession_announcer", data.AnnualEndingResult.EndingId);
        Assert.AreEqual("hero_announcer", data.AnnualEndingResult.HeroineId);
        CollectionAssert.Contains(data.ActiveRouteFlags, "result_annual_confession_announcer");
    }

    [Test]
    public void AnnualOnlineRivalRouteConnectsNormalSuccessUnlockToConfessionEnding()
    {
        var calendar = AnnualCalendarContentLoader.LoadRuntime();
        var flow = new CampaignGameService(CampaignContentLoader.LoadAnnual(), null, calendar);
        var progress = new AnnualProgressService(calendar);
        var profile = new CyclingLove.State.ProfileProgress
        {
            CompletedAnnualRuns = 1,
            RecordedRunIds = new System.Collections.Generic.List<string> { "normal-success-run" },
            ClearedEndingIds = new System.Collections.Generic.List<string> { "annual_confession_bikeshop" }
        };
        var data = flow.NewGame(profile);
        data.Physical = 7;
        data.Toughness = 7;
        data.Empathy = 7;

        PlayEvent(flow, data, new DateTime(2026, 4, 1), "annual_route_select", "annual_route_online_rival");
        PlayEvent(flow, data, new DateTime(2026, 4, 2), "annual_rival_spring_intro", "rival_challenge_accept");
        PlayEvent(flow, data, new DateTime(2026, 6, 1), "annual_rival_spring_goal", "rival_spring_respect");
        SetDate(data, new DateTime(2026, 6, 30));
        Assert.IsTrue(progress.ResolveSeasonGoal(data, "spring").Succeeded);

        PlayEvent(flow, data, new DateTime(2026, 7, 10), "annual_rival_summer_truth", "rival_truth_now");
        PlayEvent(flow, data, new DateTime(2026, 9, 1), "annual_rival_summer_goal", "rival_summer_pace");
        SetDate(data, new DateTime(2026, 9, 30));
        Assert.IsTrue(progress.ResolveSeasonGoal(data, "summer").Succeeded);

        PlayEvent(flow, data, new DateTime(2026, 10, 10), "annual_rival_autumn_fear", "rival_fear_walk");
        PlayEvent(flow, data, new DateTime(2026, 12, 1), "annual_rival_autumn_goal", "rival_autumn_side");
        SetDate(data, new DateTime(2026, 12, 31));
        Assert.IsTrue(progress.ResolveSeasonGoal(data, "autumn").Succeeded);

        PlayEvent(flow, data, new DateTime(2027, 1, 15), "annual_rival_winter_race", "rival_race_finish");
        PlayEvent(flow, data, new DateTime(2027, 3, 1), "annual_rival_winter_final", "rival_final_confess");
        SetDate(data, new DateTime(2027, 3, 31));
        var results = progress.ResolveDueMilestones(data);

        Assert.AreEqual(8, data.FindHeroineProgress("hero_online_rival").RouteStep);
        Assert.GreaterOrEqual(data.FindHeroineProgress("hero_online_rival").Affection, 7);
        Assert.IsTrue(results.Single(value => value.DefinitionId == "goal_winter").Succeeded);
        Assert.AreEqual("annual_confession_online_rival", data.AnnualEndingResult.EndingId);
        Assert.AreEqual("hero_online_rival", data.AnnualEndingResult.HeroineId);
        CollectionAssert.Contains(data.ActiveRouteFlags, "result_annual_confession_online_rival");
    }

    [Test]
    public void AnnualRoutesRequireRelationshipChoicesForConfession()
    {
        var definition = CampaignContentLoader.LoadAnnual();

        AssertRouteBalance(definition, "hero_bikeshop", "evt_annual_bikeshop_", "annual_final_confess");
        AssertRouteBalance(definition, "hero_cafe_master", "evt_annual_cafe_", "cafe_final_confess");
        AssertRouteBalance(definition, "hero_cousin", "evt_annual_cousin_", "cousin_final_confess");
        AssertRouteBalance(definition, "hero_announcer", "evt_annual_announcer_", "announcer_final_confess");
        AssertRouteBalance(definition, "hero_online_rival", "evt_annual_rival_", "rival_final_confess");
    }

    private static void AssertRouteBalance(
        CampaignDefinition definition,
        string heroineId,
        string actionPrefix,
        string confessionChoiceId)
    {
        var actions = definition.Actions
            .Where(value => value.Id.StartsWith(actionPrefix, StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(8, actions.Length, heroineId + " should have eight annual events");
        Assert.AreEqual(1, actions[0].Effects.Affection, heroineId + " meeting should establish initial affection");
        Assert.IsTrue(actions.Skip(1).All(value => value.Effects.Affection == 0),
            heroineId + " should gain later affection from choices, not automatic event entry");

        var finalAction = actions[^1];
        var confession = finalAction.Choices.Single(value => value.Id == confessionChoiceId);

        var relationshipPath = BalancePlayer(heroineId);
        foreach (var action in actions.Take(actions.Length - 1))
        {
            action.Effects.Apply(relationshipPath, 100);
            action.Choices
                .Where(value => value.CanChoose(relationshipPath))
                .OrderByDescending(value => value.Effects.Affection)
                .First()
                .Effects.Apply(relationshipPath, 100);
        }
        finalAction.Effects.Apply(relationshipPath, 100);
        Assert.IsTrue(confession.CanChoose(relationshipPath), heroineId + " relationship path should unlock confession");

        var fallbackPath = BalancePlayer(heroineId);
        foreach (var action in actions.Take(actions.Length - 1))
        {
            action.Effects.Apply(fallbackPath, 100);
            action.Choices
                .Where(value => value.Conditions == null || value.Conditions.Length == 0)
                .OrderBy(value => value.Effects.Affection)
                .First()
                .Effects.Apply(fallbackPath, 100);
        }
        finalAction.Effects.Apply(fallbackPath, 100);
        Assert.IsFalse(confession.CanChoose(fallbackPath), heroineId + " fallback path should keep confession locked");
        Assert.Less(fallbackPath.FindHeroineProgress(heroineId).Affection, 7);
    }

    private static CyclingLove.State.GamePlayerData BalancePlayer(string heroineId)
    {
        var data = new CyclingLove.State.GamePlayerData
        {
            Money = 99999,
            Stamina = 100,
            Physical = 99,
            Toughness = 99,
            Smart = 99,
            Empathy = 99,
            Appeal = 99
        };
        data.EnsureHeroineProgress(heroineId);
        return data;
    }

    private static CampaignConditionalEvent ConditionalEvent(
        CampaignDefinition definition, string id, int priority, string title)
    {
        var day = definition.Days[0].Snapshot();
        day.Id = id;
        day.Title = title;
        return new CampaignConditionalEvent
        {
            Id = id,
            TemplateId = "test",
            Priority = priority,
            HolidayMode = "any",
            StartYear = 2026,
            StartMonth = 4,
            StartDay = 1,
            EndYear = 2026,
            EndMonth = 4,
            EndDay = 30,
            Conditions = new[] { new CampaignCondition { Type = "route_flag", Id = "event_ready" } },
            Day = day
        };
    }

    private static void PlayEvent(
        CampaignGameService flow,
        CyclingLove.State.GamePlayerData data,
        DateTime date,
        string eventId,
        string choiceId)
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
                    var choice = flow.GetActionOptions(data).Single(value => value.ActionId == choiceId);
                    Assert.IsTrue(choice.CanExecute, choiceId + " should be selectable");
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

    private static void SetDate(CyclingLove.State.GamePlayerData data, DateTime date)
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

    private static void MakeConfessionReady(CyclingLove.State.GamePlayerData data)
    {
        data.ActiveRouteFlags.AddRange(new[]
        {
            "annual_confession_bikeshop_ready",
            "goal_spring_succeeded",
            "goal_summer_succeeded",
            "goal_autumn_succeeded",
            "goal_winter_succeeded"
        });
        var heroine = data.EnsureHeroineProgress("hero_bikeshop");
        heroine.IsMet = true;
        heroine.IsUnlocked = true;
        heroine.RouteStep = 8;
    }

    private static CyclingLove.State.GamePlayerData AnnualPlayer(int year, int month, int day) => new()
    {
        FlowVersion = PlayerSaveValidator.SchemaVersion,
        RunId = "annual-test-run",
        Year = year,
        Month = month,
        Day = day,
        DayStartStats = new int[7]
    };
}
