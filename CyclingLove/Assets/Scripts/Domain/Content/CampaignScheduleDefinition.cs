using System;
using System.Collections.Generic;
using System.Linq;
using CyclingLove.Calendar;

namespace CyclingLove.Content
{
    [Serializable]
    public sealed class CampaignScheduleDefinition
    {
        public string DefaultWorkdayTemplateId;
        public string DefaultHolidayTemplateId;
        public CampaignDayTemplate[] Templates;
        public CampaignDayRule[] Rules;

        public CampaignDay[] Materialize(AnnualCalendarDefinition calendarDefinition)
        {
            if (calendarDefinition == null) throw new ArgumentNullException(nameof(calendarDefinition));
            calendarDefinition.Validate();
            Validate(calendarDefinition);
            var calendar = new AnnualCalendarService(calendarDefinition);
            var templates = Templates.ToDictionary(value => value.Id, value => value.Day);
            var rules = (Rules ?? Array.Empty<CampaignDayRule>()).OrderByDescending(value => value.Priority).ToArray();
            var result = new List<CampaignDay>();
            for (var date = calendarDefinition.StartDate; date <= calendarDefinition.EndDate; date = date.AddDays(1))
            {
                var day = calendar.ResolveDay(date);
                var rule = rules.FirstOrDefault(value => value.Matches(day));
                var templateId = rule?.TemplateId ?? (day.IsHoliday ? DefaultHolidayTemplateId : DefaultWorkdayTemplateId);
                var materialized = templates[templateId].Snapshot();
                materialized.Id = "annual_" + date.ToString("yyyy_MM_dd");
                result.Add(materialized);
            }
            return result.ToArray();
        }

        public void Validate(AnnualCalendarDefinition calendar)
        {
            if (Templates == null || Templates.Length == 0 || Templates.Any(value => value == null || value.Day == null))
                throw new InvalidOperationException("Annual campaign schedule requires day templates");
            RequireUnique(Templates.Select(value => value.Id), "day template");
            var templateIds = Templates.Select(value => value.Id).ToArray();
            if (!templateIds.Contains(DefaultWorkdayTemplateId) || !templateIds.Contains(DefaultHolidayTemplateId))
                throw new InvalidOperationException("Annual campaign schedule defaults must reference templates");
            var rules = Rules ?? Array.Empty<CampaignDayRule>();
            if (rules.Any(value => value == null))
                throw new InvalidOperationException("Annual campaign schedule contains an empty rule");
            RequireUnique(rules.Select(value => value.Id), "day rule");
            if (rules.Select(value => value.Priority).Distinct().Count() != rules.Length)
                throw new InvalidOperationException("Annual campaign day rule priorities must be unique");
            foreach (var rule in rules) rule.Validate(calendar, templateIds);
        }

        private static void RequireUnique(IEnumerable<string> values, string kind)
        {
            var ids = values.ToArray();
            if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != ids.Length)
                throw new InvalidOperationException("Invalid or duplicate " + kind + " ID");
        }
    }

    [Serializable]
    public sealed class CampaignDayTemplate
    {
        public string Id;
        public CampaignDay Day;
    }

    [Serializable]
    public sealed class CampaignDayRule
    {
        public string Id, TemplateId, SeasonId, SpecialSlotId, HolidayMode;
        public int Priority;
        public int Year, Month, Day;

        public bool Matches(AnnualDayResolution day)
        {
            if (HasDate && day.Date != new DateTime(Year, Month, Day)) return false;
            if (!string.IsNullOrWhiteSpace(SeasonId) && day.SeasonId != SeasonId) return false;
            if (!string.IsNullOrWhiteSpace(SpecialSlotId) && !day.SpecialSlotIds.Contains(SpecialSlotId)) return false;
            if (HolidayMode == "holiday" && !day.IsHoliday) return false;
            if (HolidayMode == "workday" && day.IsHoliday) return false;
            return true;
        }

        public void Validate(AnnualCalendarDefinition calendar, IReadOnlyCollection<string> templateIds)
        {
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(TemplateId) || !templateIds.Contains(TemplateId))
                throw new InvalidOperationException("Invalid annual campaign day rule: " + Id);
            if (HolidayMode != "any" && HolidayMode != "holiday" && HolidayMode != "workday")
                throw new InvalidOperationException("Invalid annual campaign holiday rule: " + Id);
            if (HasPartialDate)
                throw new InvalidOperationException("Annual campaign day rule date must be complete: " + Id);
            if (HasDate)
            {
                var date = new DateTime(Year, Month, Day);
                if (date < calendar.StartDate || date > calendar.EndDate)
                    throw new InvalidOperationException("Annual campaign day rule is outside the calendar: " + Id);
            }
            if (!string.IsNullOrWhiteSpace(SeasonId) && !calendar.Seasons.Any(value => value.Id == SeasonId))
                throw new InvalidOperationException("Annual campaign day rule references a missing season: " + Id);
            if (!string.IsNullOrWhiteSpace(SpecialSlotId) &&
                !(calendar.DateOverrides ?? Array.Empty<CalendarDateOverride>()).Any(value =>
                    (value.SpecialSlotIds ?? Array.Empty<string>()).Contains(SpecialSlotId)))
                throw new InvalidOperationException("Annual campaign day rule references a missing special slot: " + Id);
            if (!HasDate && string.IsNullOrWhiteSpace(SeasonId) && string.IsNullOrWhiteSpace(SpecialSlotId) && HolidayMode == "any")
                throw new InvalidOperationException("Annual campaign day rule has no condition: " + Id);
        }

        private bool HasDate => Year != 0 && Month != 0 && Day != 0;
        private bool HasPartialDate => (Year != 0 || Month != 0 || Day != 0) && !HasDate;
    }
}
