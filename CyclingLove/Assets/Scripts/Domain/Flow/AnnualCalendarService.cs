using System.Linq;
using System;
using CyclingLove.Content;
using CyclingLove.State;

namespace CyclingLove.Calendar
{
    public sealed class AnnualDayResolution
    {
        public DateTime Date;
        public DayOfWeek DayOfWeek;
        public string SeasonId, SeasonDisplayName;
        public bool IsHoliday, IsFinalDay;
        public string[] SpecialSlotIds;
    }

    public sealed class SeasonalGoalResult
    {
        public string GoalId, SeasonId, ResultFlag;
        public DateTime Deadline;
        public bool Succeeded;
    }

    public sealed class AnnualCalendarService
    {
        private readonly AnnualCalendarDefinition definition;

        public AnnualCalendarService(AnnualCalendarDefinition source)
        {
            definition = (source ?? throw new ArgumentNullException(nameof(source))).Snapshot();
        }

        public AnnualDayResolution ResolveDay(DateTime source)
        {
            var date = source.Date;
            if (date < definition.StartDate || date > definition.EndDate)
                throw new ArgumentOutOfRangeException(nameof(source), "Date is outside the annual calendar");
            var season = definition.Seasons.Single(value => value.Contains(date));
            var dateOverride = definition.DateOverrides.FirstOrDefault(value => value.Date == date);
            var holiday = definition.WeekendsAreHolidays &&
                          (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday);
            if (dateOverride?.HolidayMode == "holiday") holiday = true;
            if (dateOverride?.HolidayMode == "workday") holiday = false;
            return new AnnualDayResolution
            {
                Date = date,
                DayOfWeek = date.DayOfWeek,
                SeasonId = season.Id,
                SeasonDisplayName = season.DisplayName,
                IsHoliday = holiday,
                IsFinalDay = date == definition.EndDate,
                SpecialSlotIds = dateOverride?.SpecialSlotIds == null
                    ? Array.Empty<string>()
                    : (string[])dateOverride.SpecialSlotIds.Clone()
            };
        }

        public SeasonalGoalResult EvaluateSeasonGoal(GamePlayerData data, string seasonId)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var goal = definition.SeasonGoals.SingleOrDefault(value => value.SeasonId == seasonId) ??
                       throw new InvalidOperationException("Season goal is missing: " + seasonId);
            var succeeded = (goal.Conditions ?? Array.Empty<CampaignCondition>()).All(value => value.IsMet(data));
            return new SeasonalGoalResult
            {
                GoalId = goal.Id,
                SeasonId = goal.SeasonId,
                Deadline = goal.Deadline,
                Succeeded = succeeded,
                ResultFlag = succeeded ? goal.SuccessFlag : goal.FailureFlag
            };
        }

        public AnnualEndingDefinition ResolveEnding(GamePlayerData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return definition.Endings
                .OrderByDescending(value => value.Priority)
                .First(value => (value.Conditions ?? Array.Empty<CampaignCondition>()).All(condition => condition.IsMet(data)))
                .Snapshot();
        }
    }
}
