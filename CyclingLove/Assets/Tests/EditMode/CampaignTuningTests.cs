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

        [Test]
        public void Evaluate_ChoiceReportShowsEveryUnmetConditionAndUnlocksAtBoundary()
        {
            var calendar = AnnualCalendarContentLoader.LoadRuntime();
            var gatedChoice = new CampaignChoiceDefinition
            {
                Id = "gated",
                Label = "条件付き",
                Pages = new[] { "本文" },
                Effects = new CampaignEffects(),
                Conditions = new[]
                {
                    new CampaignCondition { Type = "physical", Value = 7 },
                    new CampaignCondition { Type = "affection", Id = "hero_bikeshop", Value = 5 }
                }
            };
            var campaign = new CampaignDefinition
            {
                Actions = new[]
                {
                    new ActionDefinition
                    {
                        Id = "event_action",
                        Choices = new[]
                        {
                            gatedChoice,
                            new CampaignChoiceDefinition
                            {
                                Id = "fallback",
                                Label = "代替",
                                Pages = new[] { "本文" },
                                Effects = new CampaignEffects()
                            }
                        }
                    }
                },
                ConditionalEvents = new[] { EventWithAction("event", 10, "event_action") }
            };
            var data = new GamePlayerData { Physical = 6 };
            data.EnsureHeroineProgress("hero_bikeshop").Affection = 4;

            var below = CampaignTuningEvaluator.Evaluate(campaign, calendar, data, calendar.StartDate);
            var belowGated = Array.Find(below.Choices, value => value.ChoiceId == "gated");
            Assert.That(belowGated.CanChoose, Is.False);
            Assert.That(Array.FindAll(belowGated.Conditions, value => !value.IsMet).Length, Is.EqualTo(2));
            Assert.That(Array.Find(below.Choices, value => value.ChoiceId == "fallback").CanChoose, Is.True);

            data.Physical = 7;
            data.FindHeroineProgress("hero_bikeshop").Affection = 5;
            var atBoundary = CampaignTuningEvaluator.Evaluate(campaign, calendar, data, calendar.StartDate);
            var boundaryGated = Array.Find(atBoundary.Choices, value => value.ChoiceId == "gated");
            Assert.That(boundaryGated.CanChoose, Is.True);
            Assert.That(Array.TrueForAll(boundaryGated.Conditions, value => value.IsMet), Is.True);
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

        private static CampaignConditionalEvent EventWithAction(string id, int priority, string actionId)
        {
            var value = Event(id, priority);
            value.Day.ActionIds = new[] { actionId };
            return value;
        }
    }
}
