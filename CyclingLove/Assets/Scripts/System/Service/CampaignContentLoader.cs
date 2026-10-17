using CyclingLove.Content;
using CyclingLove.Calendar;
using System.Linq;
using UnityEngine;

namespace System.Service
{
    // Only this adapter knows where content is stored. Domain receives an assembled snapshot.
    public static class CampaignContentLoader
    {
        public const string PrototypeResourceKey = "Content/PrototypeCampaign";
        public const string RuntimeResourceKey = "Content/BikeshopShortCampaign";
        public const string AnnualResourceKey = "Content/AnnualCampaign";
        public static CampaignDefinition LoadPrototype() => Load(PrototypeResourceKey);
        public static CampaignDefinition LoadRuntime() => Load(RuntimeResourceKey);
        public static CampaignDefinition LoadAnnual() => Load(AnnualResourceKey);
        public static CampaignDefinition Load(string resourceKey) => Load(resourceKey, ReadResourceText);

        // Share decoding/validation with build checks without changing the live Resources data.
        public static CampaignDefinition Load(string resourceKey, Func<string, string> readText)
        {
            if (readText == null) throw new ArgumentNullException(nameof(readText));
            var manifest = Read<CampaignManifest>(resourceKey, readText);
            var hasDays = !string.IsNullOrWhiteSpace(manifest.DaysResource);
            var hasSchedule = !string.IsNullOrWhiteSpace(manifest.ScheduleResource) && !string.IsNullOrWhiteSpace(manifest.CalendarResource);
            if (hasDays == hasSchedule)
                throw new InvalidOperationException("Campaign manifest requires either DaysResource or ScheduleResource with CalendarResource");
            CampaignScheduleDefinition schedule = null;
            AnnualCalendarDefinition calendar = null;
            CampaignDay[] days;
            if (hasDays)
            {
                days = Read<DayCatalog>(manifest.DaysResource, readText).Days;
            }
            else
            {
                schedule = Read<CampaignScheduleDefinition>(manifest.ScheduleResource, readText);
                calendar = Read<AnnualCalendarDefinition>(manifest.CalendarResource, readText);
                var manifestStart = new DateTime(manifest.StartYear, manifest.StartMonth, manifest.StartDay);
                if (manifestStart != calendar.StartDate)
                    throw new InvalidOperationException("Campaign start date must match its annual calendar");
                days = schedule.Materialize(calendar);
            }
            var actionResources = new[] { manifest.ActionsResource }
                .Concat(manifest.AdditionalActionsResources ?? Array.Empty<string>());
            var definition = new CampaignDefinition
            {
                Id = manifest.Id, Version = manifest.Version,
                StartYear = manifest.StartYear, StartMonth = manifest.StartMonth, StartDay = manifest.StartDay,
                ScenarioId = string.IsNullOrWhiteSpace(manifest.ScenarioId) ? manifest.Id : manifest.ScenarioId,
                EndingTitle = manifest.EndingTitle, EndingBody = manifest.EndingBody, ChoiceDescription = manifest.ChoiceDescription,
                Rules = Read<CampaignRules>(manifest.RulesResource, readText),
                Actions = actionResources
                    .SelectMany(key => Read<ActionCatalog>(key, readText).Actions ?? Array.Empty<ActionDefinition>())
                    .ToArray(),
                Days = days,
                ConditionalEvents = schedule?.MaterializeConditionalEvents(calendar) ?? Array.Empty<CampaignConditionalEvent>()
            };
            definition.Validate();
            return definition;
        }
        private static string ReadResourceText(string key)
        {
            var source = UnityEngine.Resources.Load<TextAsset>(key);
            return source == null ? null : source.text;
        }
        private static T Read<T>(string key, Func<string, string> readText) where T : class
        {
            if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Missing content resource key");
            var source = readText(key);
            if (source == null) throw new InvalidOperationException("Content resource missing: " + key);
            if (string.IsNullOrWhiteSpace(source)) throw new InvalidOperationException("Empty content: " + key);
            try { return JsonUtility.FromJson<T>(source) ?? throw new InvalidOperationException("Empty content: " + key); }
            catch (ArgumentException ex) { throw new InvalidOperationException("Invalid JSON content: " + key, ex); }
        }
        [Serializable] private sealed class CampaignManifest
        {
            public string Id, ScenarioId, EndingTitle, EndingBody, ChoiceDescription;
            public int Version, StartYear, StartMonth, StartDay;
            public string RulesResource, ActionsResource, DaysResource, ScheduleResource, CalendarResource;
            public string[] AdditionalActionsResources;
        }
        [Serializable] private sealed class ActionCatalog { public ActionDefinition[] Actions; }
        [Serializable] private sealed class DayCatalog { public CampaignDay[] Days; }
    }
}
