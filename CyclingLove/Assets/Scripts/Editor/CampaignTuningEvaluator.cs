using System.Collections.Generic;
using System.Linq;
using System;
using CyclingLove.Calendar;
using CyclingLove.Content;
using CyclingLove.State;

namespace CyclingLove.Editor
{
    public sealed class CampaignTuningReport
    {
        public AnnualDayResolution Day;
        public CampaignConditionalEvent[] MatchingEvents;
        public CampaignConditionalEvent SelectedEvent;
        public CampaignTuningChoiceReport[] Choices;
        public SeasonalGoalResult[] SeasonGoals;
        public AnnualEndingDefinition Ending;
    }

    public sealed class CampaignTuningChoiceReport
    {
        public string ActionId;
        public string ChoiceId;
        public string Label;
        public bool CanChoose;
        public CampaignTuningConditionReport[] Conditions;
    }

    public sealed class CampaignTuningConditionReport
    {
        public string Type;
        public string Id;
        public int RequiredValue;
        public int CurrentValue;
        public bool IsMet;
        public string DisplayText;
    }

    public static class CampaignTuningEvaluator
    {
        public static CampaignTuningReport Evaluate(
            CampaignDefinition campaign,
            AnnualCalendarDefinition calendar,
            GamePlayerData data,
            DateTime date)
        {
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            if (calendar == null) throw new ArgumentNullException(nameof(calendar));
            if (data == null) throw new ArgumentNullException(nameof(data));

            var calendarService = new AnnualCalendarService(calendar);
            var day = calendarService.ResolveDay(date);
            var matchingEvents = (campaign.ConditionalEvents ?? Array.Empty<CampaignConditionalEvent>())
                .Where(value => value.Matches(day, data))
                .OrderByDescending(value => value.Priority)
                .ThenBy(value => value.Id, StringComparer.Ordinal)
                .ToArray();
            var selectedEvent = matchingEvents.FirstOrDefault();

            return new CampaignTuningReport
            {
                Day = day,
                MatchingEvents = matchingEvents,
                SelectedEvent = selectedEvent,
                Choices = ResolveChoices(campaign, selectedEvent, data),
                SeasonGoals = calendar.Seasons
                    .Select(value => calendarService.EvaluateSeasonGoal(data, value.Id))
                    .ToArray(),
                Ending = calendarService.ResolveEnding(data)
            };
        }

        private static CampaignTuningChoiceReport[] ResolveChoices(
            CampaignDefinition campaign,
            CampaignConditionalEvent selectedEvent,
            GamePlayerData data)
        {
            if (selectedEvent?.Day?.ActionIds == null) return Array.Empty<CampaignTuningChoiceReport>();

            return selectedEvent.Day.ActionIds
                .Select(id => (id, action: campaign.Actions?.FirstOrDefault(value => value.Id == id)))
                .Where(value => value.action?.Choices != null)
                .SelectMany(value => value.action.Choices.Select(choice => new CampaignTuningChoiceReport
                {
                    ActionId = value.id,
                    ChoiceId = choice.Id,
                    Label = choice.Label,
                    CanChoose = choice.CanChoose(data),
                    Conditions = (choice.Conditions ?? Array.Empty<CampaignCondition>())
                        .Select(condition => ResolveCondition(condition, data))
                        .ToArray()
                }))
                .ToArray();
        }

        private static CampaignTuningConditionReport ResolveCondition(
            CampaignCondition condition,
            GamePlayerData data)
        {
            var current = CurrentValue(condition, data);
            var isMet = condition.IsMet(data);
            return new CampaignTuningConditionReport
            {
                Type = condition.Type,
                Id = condition.Id,
                RequiredValue = condition.Value,
                CurrentValue = current,
                IsMet = isMet,
                DisplayText = DescribeCondition(condition, current, isMet)
            };
        }

        private static int CurrentValue(CampaignCondition condition, GamePlayerData data) => condition.Type switch
        {
            "money" => data.Money,
            "physical" => data.Physical,
            "toughness" => data.Toughness,
            "smart" => data.Smart,
            "empathy" => data.Empathy,
            "appeal" => data.Appeal,
            "stamina" => data.Stamina,
            "regional_contribution" => data.RegionalContribution,
            "public_awareness" => data.PublicAwareness,
            "fox_favor" => data.FoxFavor,
            "completed_quest_count" => data.CompletedQuestIds?.Count ?? 0,
            "affection" => data.FindHeroineProgress(condition.Id)?.Affection ?? 0,
            "route_step" => data.FindHeroineProgress(condition.Id)?.RouteStep ?? 0,
            _ => condition.IsMet(data) ? 1 : 0
        };

        private static string DescribeCondition(CampaignCondition condition, int current, bool isMet)
        {
            var label = condition.Type switch
            {
                "money" => "資金",
                "physical" => "フィジカル",
                "toughness" => "タフネス",
                "smart" => "スマート",
                "empathy" => "エンパシー",
                "appeal" => "アピール",
                "stamina" => "体力",
                "affection" => "好感度",
                "route_step" => "ルート段階",
                "regional_contribution" => "地域貢献",
                "public_awareness" => "知名度",
                "fox_favor" => "狐の加護",
                "completed_quest_count" => "完了依頼数",
                "heroine_met" => "出会い済み",
                "heroine_unlocked" => "解放済み",
                "route_flag" => "ルートフラグ",
                "event_cleared" => "既読イベント",
                "race_attempted" => "レース挑戦済み",
                "race_succeeded" => "レース成功済み",
                "quest_completed" => "依頼完了",
                _ => condition.Type
            };
            var target = string.IsNullOrWhiteSpace(condition.Id) ? string.Empty : $" [{condition.Id}]";
            var numeric = condition.Type is "money" or "physical" or "toughness" or "smart" or
                "empathy" or "appeal" or "stamina" or "affection" or "route_step" or
                "regional_contribution" or "public_awareness" or "fox_favor" or "completed_quest_count";
            var value = numeric ? $" {current}{(condition.Not ? "<" : "/")}{condition.Value}" : string.Empty;
            var negation = condition.Not && !numeric ? "なし" : string.Empty;
            return $"{(isMet ? "達成" : "未達")}  {label}{negation}{target}{value}";
        }
    }
}
