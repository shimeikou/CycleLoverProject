using System;
using System.Collections.Generic;
using System.GameSession;
using System.Linq;
using CyclingLove.Calendar;
using CyclingLove.Content;

namespace CyclingLove.Editor
{
    public sealed class CampaignTuningReport
    {
        public AnnualDayResolution Day;
        public CampaignConditionalEvent[] MatchingEvents;
        public CampaignConditionalEvent SelectedEvent;
        public SeasonalGoalResult[] SeasonGoals;
        public AnnualEndingDefinition Ending;
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

            return new CampaignTuningReport
            {
                Day = day,
                MatchingEvents = matchingEvents,
                SelectedEvent = matchingEvents.FirstOrDefault(),
                SeasonGoals = calendar.Seasons
                    .Select(value => calendarService.EvaluateSeasonGoal(data, value.Id))
                    .ToArray(),
                Ending = calendarService.ResolveEnding(data)
            };
        }
    }
}
