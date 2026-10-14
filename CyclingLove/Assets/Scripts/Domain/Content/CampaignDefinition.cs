using System;
using System.Collections.Generic;
using System.Linq;
using System.GameSession;

namespace CyclingLove.Content
{
    [Serializable]
    public sealed class CampaignDefinition
    {
        public string Id;
        public int Version;
        public int StartYear, StartMonth, StartDay;
        public int OvernightRecovery;
        public string EndingTitle, EndingBody;
        public string ChoiceDescription;
        public ActionDefinition[] Actions;
        public CampaignDay[] Days;

        public CampaignDefinition Snapshot()
        {
            Validate();
            var copy = (CampaignDefinition)MemberwiseClone();
            copy.Actions = Actions.Select(a => a.Snapshot()).ToArray();
            copy.Days = Days.Select(d => d.Snapshot()).ToArray();
            return copy;
        }
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id) || Version < 1 || Days == null || Days.Length == 0 || Actions == null || Actions.Length == 0 || OvernightRecovery < 0)
                throw new InvalidOperationException("Invalid campaign definition");
            _ = new DateTime(StartYear, StartMonth, StartDay).AddDays(Days.Length);
            RequireUnique(Actions.Select(a => a?.Id), "action");
            RequireUnique(Days.Select(d => d?.Id), "day");
            foreach (var a in Actions)
            {
                if (string.IsNullOrWhiteSpace(a.Name) || a.StaminaCost < 0 || a.MoneyCost < 0 || a.Effects == null)
                    throw new InvalidOperationException("Invalid action: " + a.Id);
                RequirePages(a.Pages); a.Effects.Validate();
            }
            foreach (var d in Days)
            {
                if (string.IsNullOrWhiteSpace(d.Title) || d.ActionIds == null || d.ActionIds.Length == 0 || d.MorningEffects == null || d.AfternoonEffects == null)
                    throw new InvalidOperationException("Invalid day: " + d.Id);
                RequireUnique(d.ActionIds, "day action");
                if (d.ActionIds.Any(id => !Actions.Any(a => a.Id == id))) throw new InvalidOperationException("Missing action reference: " + d.Id);
                // Every day must remain playable at zero resources.
                if (!d.ActionIds.Any(id => Actions.Any(a => a.Id == id && a.MoneyCost == 0 && a.StaminaCost == 0)))
                    throw new InvalidOperationException("Day requires a free fallback action: " + d.Id);
                RequirePages(d.MorningPages); RequirePages(d.AfternoonPages);
                d.MorningEffects.Validate(); d.AfternoonEffects.Validate();
            }
            RequirePages(new[] { EndingTitle, EndingBody });
        }
        private static void RequireUnique(IEnumerable<string> values, string kind)
        {
            var ids = values.ToArray();
            if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != ids.Length)
                throw new InvalidOperationException("Invalid or duplicate " + kind + " ID");
        }
        private static void RequirePages(string[] pages)
        {
            if (pages == null || pages.Length == 0 || pages.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("Empty scenario page");
        }
    }
    [Serializable]
    public sealed class CampaignDay
    {
        public string Id, Title, SummaryNote;
        public string[] MorningPages, AfternoonPages, ActionIds;
        public CampaignEffects MorningEffects, AfternoonEffects;
        public CampaignDay Snapshot()
        {
            var copy = (CampaignDay)MemberwiseClone();
            copy.MorningPages = (string[])MorningPages.Clone();
            copy.AfternoonPages = (string[])AfternoonPages.Clone();
            copy.ActionIds = (string[])ActionIds.Clone();
            copy.MorningEffects = MorningEffects.Snapshot();
            copy.AfternoonEffects = AfternoonEffects.Snapshot();
            return copy;
        }
    }
    [Serializable]
    public sealed class ActionDefinition
    {
        public string Id, Name;
        public int StaminaCost, MoneyCost;
        public string[] Pages;
        public CampaignEffects Effects;
        public ActionDefinition Snapshot()
        {
            var copy = (ActionDefinition)MemberwiseClone();
            copy.Pages = (string[])Pages.Clone(); copy.Effects = Effects.Snapshot();
            return copy;
        }
        public string Describe()
        {
            var parts = new List<string>();
            if (StaminaCost > 0) parts.Add("体力 -" + StaminaCost);
            if (MoneyCost > 0) parts.Add("資金 -" + MoneyCost);
            var values = new[] { Effects.Money, Effects.Physical, Effects.Toughness, Effects.Smart, Effects.Empathy, Effects.Appeal, Effects.Stamina };
            var names = new[] { "資金", "フィジカル", "タフネス", "スマート", "エンパシー", "アピール", "体力" };
            for (int i = 0; i < values.Length; i++) if (values[i] != 0) parts.Add($"{names[i]} {values[i]:+0;-0;0}");
            return string.Join(" / ", parts);
        }
    }
    [Serializable]
    public sealed class CampaignEffects
    {
        public int Money, Physical, Toughness, Smart, Empathy, Appeal, Stamina;
        public string HeroineId, RouteFlag;
        public CampaignEffects Snapshot() => (CampaignEffects)MemberwiseClone();
        public void Validate()
        {
            // Resource costs belong to action costs; this first schema only accepts positive rewards.
            if (new[] { Money, Physical, Toughness, Smart, Empathy, Appeal, Stamina }.Any(v => v < 0))
                throw new InvalidOperationException("Negative reward: use action costs instead");
        }
        public void Apply(GamePlayerData d)
        {
            d.Money += Money; d.Physical += Physical; d.Toughness += Toughness;
            d.Smart += Smart; d.Empathy += Empathy; d.Appeal += Appeal;
            d.Stamina = Math.Min(100, d.Stamina + Stamina);
            if (!string.IsNullOrEmpty(HeroineId) && !d.MetHeroineIds.Contains(HeroineId)) d.MetHeroineIds.Add(HeroineId);
            if (!string.IsNullOrEmpty(RouteFlag) && !d.ActiveRouteFlags.Contains(RouteFlag)) d.ActiveRouteFlags.Add(RouteFlag);
        }
    }
}
