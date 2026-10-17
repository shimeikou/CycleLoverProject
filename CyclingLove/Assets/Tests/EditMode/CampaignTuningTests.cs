using System;
using System.GameSession;
using CyclingLove.Content;
using CyclingLove.Editor;
using NUnit.Framework;
using System.Service;

namespace CyclingLove.Tests
{
    public sealed class CampaignTuningTests
    {
        [Test]
        public void Evaluate_UsesConditionBoundaryAndPriority()
        {
            var calendar = AnnualCalendarContentLoader.LoadRuntime();
            var campaign = new CampaignDefinition
            {
                ConditionalEvents = new[]
                {
                    Event("fallback", 10),
                    Event("money_boundary", 20, new CampaignCondition { Type = "money", Value = 10 })
                }
            };
            var data = new GamePlayerData { Money = 9 };

            var below = CampaignTuningEvaluator.Evaluate(campaign, calendar, data, calendar.StartDate);
            Assert.That(below.SelectedEvent.Id, Is.EqualTo("fallback"));

            data.Money = 10;
            var atBoundary = CampaignTuningEvaluator.Evaluate(campaign, calendar, data, calendar.StartDate);
            Assert.That(atBoundary.SelectedEvent.Id, Is.EqualTo("money_boundary"));
            Assert.That(atBoundary.MatchingEvents.Length, Is.EqualTo(2));
        }

        [Test]
        public void Evaluate_LoadedAnnualData_ReturnsGoalsAndEnding()
        {
            var calendar = AnnualCalendarContentLoader.LoadRuntime();
            var report = CampaignTuningEvaluator.Evaluate(
                CampaignContentLoader.LoadAnnual(),
                calendar,
                new GamePlayerData(),
                calendar.StartDate);

            Assert.That(report.Day.Date, Is.EqualTo(calendar.StartDate));
            Assert.That(report.SeasonGoals.Length, Is.EqualTo(4));
            Assert.That(report.Ending, Is.Not.Null);
        }

        private static CampaignConditionalEvent Event(string id, int priority, params CampaignCondition[] conditions) =>
            new()
            {
                Id = id,
                Priority = priority,
                HolidayMode = "any",
                Repeatable = true,
                Conditions = conditions,
                Day = new CampaignDay { Title = id }
            };
    }
}
