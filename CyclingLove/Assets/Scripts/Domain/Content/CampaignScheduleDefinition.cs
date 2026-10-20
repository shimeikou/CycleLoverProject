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
        public CampaignConditionalEvent[] ConditionalEvents;

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
                materialized.MorningNodeId = DateNodeId(materialized.MorningNodeId, materialized.Id, "morning");
                materialized.AfternoonNodeId = DateNodeId(materialized.AfternoonNodeId, materialized.Id, "afternoon");
                result.Add(materialized);
            }
            return result.ToArray();
        }

        public CampaignConditionalEvent[] MaterializeConditionalEvents(AnnualCalendarDefinition calendarDefinition)
        {
            if (calendarDefinition == null) throw new ArgumentNullException(nameof(calendarDefinition));
            calendarDefinition.Validate();
            Validate(calendarDefinition);
            var templates = Templates.ToDictionary(value => value.Id, value => value.Day);
            return (ConditionalEvents ?? Array.Empty<CampaignConditionalEvent>()).Select(value =>
            {
                var copy = value.Snapshot();
                copy.Day = templates[copy.TemplateId].Snapshot();
                copy.Day.Id = copy.Id;
                copy.Day.MorningNodeId = EventNodeId(copy.Day.MorningNodeId, copy.Id, "morning");
                copy.Day.AfternoonNodeId = EventNodeId(copy.Day.AfternoonNodeId, copy.Id, "afternoon");
                return copy;
            }).ToArray();
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
            var calendarService = new AnnualCalendarService(calendar);
            foreach (var rule in rules)
            {
                rule.Validate(calendar, templateIds);
                if (!HasMatchingDay(calendar, calendarService, rule.Matches))
                    throw new InvalidOperationException("Annual campaign day rule has no matching calendar day: " + rule.Id);
            }
            var conditionalEvents = ConditionalEvents ?? Array.Empty<CampaignConditionalEvent>();
            if (conditionalEvents.Any(value => value == null))
                throw new InvalidOperationException("Annual campaign schedule contains an empty conditional event");
            RequireUnique(conditionalEvents.Select(value => value.Id), "conditional event");
            foreach (var conditionalEvent in conditionalEvents)
            {
                conditionalEvent.Validate(calendar, templateIds);
                if (!HasMatchingDay(calendar, calendarService, conditionalEvent.MatchesDay))
                    throw new InvalidOperationException("Conditional event has no matching calendar day: " + conditionalEvent.Id);
            }
        }

        private static bool HasMatchingDay(
            AnnualCalendarDefinition calendar,
            AnnualCalendarService calendarService,
            Func<AnnualDayResolution, bool> predicate)
        {
            for (var date = calendar.StartDate; date <= calendar.EndDate; date = date.AddDays(1))
                if (predicate(calendarService.ResolveDay(date))) return true;
            return false;
        }

        private static string DateNodeId(string nodeId, string dayId, string timing) =>
            string.IsNullOrWhiteSpace(nodeId) ? $"{dayId}_{timing}" : $"{nodeId}_{dayId}";

        private static string EventNodeId(string nodeId, string eventId, string timing) =>
            string.IsNullOrWhiteSpace(nodeId) ? $"{eventId}_{timing}" : $"{nodeId}_{eventId}";

        private static void RequireUnique(IEnumerable<string> values, string kind)
        {
            var ids = values.ToArray();
            if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != ids.Length)
                throw new InvalidOperationException("Invalid or duplicate " + kind + " ID");
        }
    }

    [Serializable]
    public sealed class CampaignConditionalEvent
    {
        public string Id, TemplateId, SeasonId, SpecialSlotId, HolidayMode;
        public int Priority;
        public int StartYear, StartMonth, StartDay;
        public int EndYear, EndMonth, EndDay;
        public bool Repeatable;
        public CampaignCondition[] Conditions;
        public CampaignDay Day;

        public CampaignConditionalEvent Snapshot()
        {
            var copy = (CampaignConditionalEvent)MemberwiseClone();
            copy.Conditions = (Conditions ?? Array.Empty<CampaignCondition>())
                .Select(value => value.Snapshot()).ToArray();
            copy.Day = Day?.Snapshot();
            return copy;
        }

        public bool Matches(AnnualDayResolution day, System.GameSession.GamePlayerData data)
        {
            if (!MatchesDay(day)) return false;
            if (!Repeatable && data.ClearedEventKeys.Contains($"{Id}_{System.Data.Schedule.ActionTiming.Morning}")) return false;
            return (Conditions ?? Array.Empty<CampaignCondition>()).All(value => value.IsMet(data));
        }

        public bool MatchesDay(AnnualDayResolution day)
        {
            if (HasPeriod && (day.Date < StartDate || day.Date > EndDate)) return false;
            if (!string.IsNullOrWhiteSpace(SeasonId) && day.SeasonId != SeasonId) return false;
            if (!string.IsNullOrWhiteSpace(SpecialSlotId) && !day.SpecialSlotIds.Contains(SpecialSlotId)) return false;
            if (HolidayMode == "holiday" && !day.IsHoliday) return false;
            if (HolidayMode == "workday" && day.IsHoliday) return false;
            return true;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(TemplateId) || Priority < 0)
                throw new InvalidOperationException("Invalid conditional event: " + Id);
            if (HolidayMode != "any" && HolidayMode != "holiday" && HolidayMode != "workday")
                throw new InvalidOperationException("Invalid conditional event holiday mode: " + Id);
            if (HasPartialPeriod || HasPeriod && StartDate > EndDate)
                throw new InvalidOperationException("Invalid conditional event period: " + Id);
            foreach (var condition in Conditions ?? Array.Empty<CampaignCondition>()) condition.Validate();
        }

        public void ValidateResolved()
        {
            Validate();
            if (Day == null) throw new InvalidOperationException("Conditional event has no resolved day: " + Id);
        }

        public void Validate(AnnualCalendarDefinition calendar, IReadOnlyCollection<string> templateIds)
        {
            Validate();
            if (!templateIds.Contains(TemplateId))
                throw new InvalidOperationException("Conditional event references a missing template: " + Id);
            if (HasPeriod && (StartDate < calendar.StartDate || EndDate > calendar.EndDate))
                throw new InvalidOperationException("Conditional event is outside the calendar: " + Id);
            if (!string.IsNullOrWhiteSpace(SeasonId) && !calendar.Seasons.Any(value => value.Id == SeasonId))
                throw new InvalidOperationException("Conditional event references a missing season: " + Id);
            if (!string.IsNullOrWhiteSpace(SpecialSlotId) &&
                !(calendar.DateOverrides ?? Array.Empty<CalendarDateOverride>()).Any(value =>
                    (value.SpecialSlotIds ?? Array.Empty<string>()).Contains(SpecialSlotId)))
                throw new InvalidOperationException("Conditional event references a missing special slot: " + Id);
        }

        private DateTime StartDate => new(StartYear, StartMonth, StartDay);
        private DateTime EndDate => new(EndYear, EndMonth, EndDay);
        private bool HasPeriod => StartYear != 0 && StartMonth != 0 && StartDay != 0 && EndYear != 0 && EndMonth != 0 && EndDay != 0;
        private bool HasPartialPeriod =>
            (StartYear != 0 || StartMonth != 0 || StartDay != 0 || EndYear != 0 || EndMonth != 0 || EndDay != 0) && !HasPeriod;
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
