using System;
using System.Collections.Generic;
using System.Linq;
using CyclingLove.Content;

namespace CyclingLove.Calendar
{
    [Serializable]
    public sealed class AnnualCalendarDefinition
    {
        public string Id;
        public int Version;
        public int StartYear, StartMonth, StartDay;
        public int EndYear, EndMonth, EndDay;
        public bool WeekendsAreHolidays;
        public AnnualSeasonDefinition[] Seasons;
        public CalendarDateOverride[] DateOverrides;
        public SeasonalGoalDefinition[] SeasonGoals;
        public AnnualEndingDefinition[] Endings;

        public DateTime StartDate => new(StartYear, StartMonth, StartDay);
        public DateTime EndDate => new(EndYear, EndMonth, EndDay);

        public AnnualCalendarDefinition Snapshot()
        {
            Validate();
            var copy = (AnnualCalendarDefinition)MemberwiseClone();
            copy.Seasons = Seasons.Select(value => value.Snapshot()).ToArray();
            copy.DateOverrides = (DateOverrides ?? Array.Empty<CalendarDateOverride>()).Select(value => value.Snapshot()).ToArray();
            copy.SeasonGoals = (SeasonGoals ?? Array.Empty<SeasonalGoalDefinition>()).Select(value => value.Snapshot()).ToArray();
            copy.Endings = Endings.Select(value => value.Snapshot()).ToArray();
            return copy;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id) || Version < 1)
                throw new InvalidOperationException("Invalid annual calendar identity");
            var start = StartDate;
            var end = EndDate;
            if (end != start.AddYears(1).AddDays(-1))
                throw new InvalidOperationException("Annual calendar must cover exactly one year");
            if (Seasons == null || Seasons.Length != 4 || Seasons.Any(value => value == null))
                throw new InvalidOperationException("Annual calendar requires four seasons");
            RequireUnique(Seasons.Select(value => value.Id), "season");
            foreach (var season in Seasons) season.Validate(start, end);
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                if (Seasons.Count(season => season.Contains(date)) != 1)
                    throw new InvalidOperationException("Every annual date must belong to exactly one season: " + date.ToString("yyyy-MM-dd"));
            }

            var overrides = DateOverrides ?? Array.Empty<CalendarDateOverride>();
            if (overrides.Any(value => value == null))
                throw new InvalidOperationException("Annual calendar contains an empty date override");
            RequireUnique(overrides.Select(value => value.DateKey), "calendar override date");
            foreach (var value in overrides) value.Validate(start, end);

            var goals = SeasonGoals ?? Array.Empty<SeasonalGoalDefinition>();
            if (goals.Any(value => value == null))
                throw new InvalidOperationException("Annual calendar contains an empty season goal");
            RequireUnique(goals.Select(value => value.Id), "season goal");
            RequireUnique(goals.Select(value => value.SeasonId), "season goal season");
            foreach (var goal in goals)
            {
                var season = Seasons.FirstOrDefault(value => value.Id == goal.SeasonId);
                if (season == null)
                    throw new InvalidOperationException("Season goal references a missing season: " + goal.Id);
                goal.Validate(start, end);
                if (!season.Contains(goal.Deadline))
                    throw new InvalidOperationException("Season goal deadline is outside its season: " + goal.Id);
            }

            if (Endings == null || Endings.Length == 0 || Endings.Any(value => value == null))
                throw new InvalidOperationException("Annual calendar requires ending rules");
            RequireUnique(Endings.Select(value => value.Id), "annual ending");
            if (Endings.Select(value => value.Priority).Distinct().Count() != Endings.Length)
                throw new InvalidOperationException("Annual ending priorities must be unique");
            foreach (var ending in Endings) ending.Validate();
            if (!Endings.Any(value => value.Conditions == null || value.Conditions.Length == 0))
                throw new InvalidOperationException("Annual endings require an unconditional fallback");
        }

        private static void RequireUnique(IEnumerable<string> values, string kind)
        {
            var ids = values.ToArray();
            if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != ids.Length)
                throw new InvalidOperationException("Invalid or duplicate " + kind + " ID");
        }
    }

    [Serializable]
    public sealed class AnnualSeasonDefinition
    {
        public string Id, DisplayName;
        public int StartYear, StartMonth, StartDay;
        public int EndYear, EndMonth, EndDay;
        public DateTime StartDate => new(StartYear, StartMonth, StartDay);
        public DateTime EndDate => new(EndYear, EndMonth, EndDay);
        public bool Contains(DateTime date) => date.Date >= StartDate && date.Date <= EndDate;
        public AnnualSeasonDefinition Snapshot() => (AnnualSeasonDefinition)MemberwiseClone();
        public void Validate(DateTime calendarStart, DateTime calendarEnd)
        {
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(DisplayName) ||
                StartDate > EndDate || StartDate < calendarStart || EndDate > calendarEnd)
                throw new InvalidOperationException("Invalid annual season: " + Id);
        }
    }

    [Serializable]
    public sealed class CalendarDateOverride
    {
        public int Year, Month, Day;
        public string HolidayMode;
        public string[] SpecialSlotIds;
        public DateTime Date => new(Year, Month, Day);
        public string DateKey => Date.ToString("yyyy-MM-dd");
        public CalendarDateOverride Snapshot()
        {
            var copy = (CalendarDateOverride)MemberwiseClone();
            copy.SpecialSlotIds = SpecialSlotIds == null ? Array.Empty<string>() : (string[])SpecialSlotIds.Clone();
            return copy;
        }
        public void Validate(DateTime start, DateTime end)
        {
            if (Date < start || Date > end || (HolidayMode != "inherit" && HolidayMode != "holiday" && HolidayMode != "workday"))
                throw new InvalidOperationException("Invalid calendar date override: " + DateKey);
            var ids = SpecialSlotIds ?? Array.Empty<string>();
            if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != ids.Length)
                throw new InvalidOperationException("Invalid special slot IDs: " + DateKey);
        }
    }

    [Serializable]
    public sealed class SeasonalGoalDefinition
    {
        public string Id, SeasonId, SuccessFlag, FailureFlag;
        public int DeadlineYear, DeadlineMonth, DeadlineDay;
        public CampaignCondition[] Conditions;
        public DateTime Deadline => new(DeadlineYear, DeadlineMonth, DeadlineDay);
        public SeasonalGoalDefinition Snapshot()
        {
            var copy = (SeasonalGoalDefinition)MemberwiseClone();
            copy.Conditions = (Conditions ?? Array.Empty<CampaignCondition>()).Select(value => value.Snapshot()).ToArray();
            return copy;
        }
        public void Validate(DateTime start, DateTime end)
        {
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(SeasonId) ||
                string.IsNullOrWhiteSpace(SuccessFlag) || string.IsNullOrWhiteSpace(FailureFlag) ||
                SuccessFlag == FailureFlag || Deadline < start || Deadline > end)
                throw new InvalidOperationException("Invalid seasonal goal: " + Id);
            foreach (var condition in Conditions ?? Array.Empty<CampaignCondition>()) condition.Validate();
        }
    }

    [Serializable]
    public sealed class AnnualEndingDefinition
    {
        public string Id, HeroineId, ResultFlag;
        public int Priority;
        public bool IsSuccess;
        public CampaignCondition[] Conditions;
        public AnnualEndingDefinition Snapshot()
        {
            var copy = (AnnualEndingDefinition)MemberwiseClone();
            copy.Conditions = (Conditions ?? Array.Empty<CampaignCondition>()).Select(value => value.Snapshot()).ToArray();
            return copy;
        }
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(ResultFlag))
                throw new InvalidOperationException("Invalid annual ending: " + Id);
            foreach (var condition in Conditions ?? Array.Empty<CampaignCondition>()) condition.Validate();
        }
    }
}
