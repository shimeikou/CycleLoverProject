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
        public CampaignRules Rules;
        public string ScenarioId;
        public string EndingTitle, EndingBody;
        public string ChoiceDescription;
        public ActionDefinition[] Actions;
        public CampaignDay[] Days;
        public CampaignConditionalEvent[] ConditionalEvents;

        public CampaignDefinition Snapshot()
        {
            Validate();
            var copy = (CampaignDefinition)MemberwiseClone();
            copy.Rules = Rules.Snapshot();
            copy.Actions = Actions.Select(a => a.Snapshot()).ToArray();
            copy.Days = Days.Select(d => d.Snapshot()).ToArray();
            copy.ConditionalEvents = (ConditionalEvents ?? Array.Empty<CampaignConditionalEvent>())
                .Select(value => value.Snapshot()).ToArray();
            return copy;
        }
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id) || Version < 1 || Days == null || Days.Length == 0 || Actions == null || Actions.Length == 0 || Rules == null)
                throw new InvalidOperationException("Invalid campaign definition");
            Rules.Validate();
            _ = new DateTime(StartYear, StartMonth, StartDay).AddDays(Days.Length);
            RequireUnique(Actions.Select(a => a?.Id), "action");
            RequireUnique(Days.Select(d => d?.Id), "day");
            var conditionalEvents = ConditionalEvents ?? Array.Empty<CampaignConditionalEvent>();
            if (conditionalEvents.Any(value => value == null))
                throw new InvalidOperationException("Campaign contains an empty conditional event");
            RequireUnique(conditionalEvents.Select(value => value.Id), "conditional event");
            foreach (var conditionalEvent in conditionalEvents) conditionalEvent.ValidateResolved();
            foreach (var a in Actions)
            {
                if (string.IsNullOrWhiteSpace(a.Name) || a.StaminaCost < 0 || a.MoneyCost < 0 || a.Effects == null)
                    throw new InvalidOperationException("Invalid action: " + a.Id);
                RequirePages(a.Pages); a.Effects.Validate();
                if (a.Choices != null && a.Choices.Length > 0)
                {
                    RequireUnique(a.Choices.Select(c => c?.Id), "choice");
                    if (!a.Choices.Any(c => c.Conditions == null || c.Conditions.Length == 0))
                        throw new InvalidOperationException("Choice requires an unconditional fallback: " + a.Id);
                    foreach (var choice in a.Choices)
                    {
                        if (string.IsNullOrWhiteSpace(choice.Label) || choice.Effects == null)
                            throw new InvalidOperationException("Invalid choice: " + choice.Id);
                        RequirePages(choice.Pages);
                        choice.Effects.Validate();
                        foreach (var condition in choice.Conditions ?? Array.Empty<CampaignCondition>()) condition.Validate();
                    }
                }
            }
            foreach (var d in Days.Concat(conditionalEvents.Select(value => value.Day)))
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
        public bool IsHoliday;
        public string MorningScenarioId, MorningNodeId;
        public string MorningBackgroundId, MorningCharacterId, MorningOutfitId, MorningExpressionId;
        public string AfternoonScenarioId, AfternoonNodeId;
        public string AfternoonBackgroundId, AfternoonCharacterId, AfternoonOutfitId, AfternoonExpressionId;
        public string[] MorningPages, AfternoonPages, ActionIds;
        public CampaignEffects MorningEffects, AfternoonEffects;
        public CampaignDay Snapshot()
        {
            var copy = (CampaignDay)MemberwiseClone();
            copy.MorningPages = MorningPages == null ? null : (string[])MorningPages.Clone();
            copy.AfternoonPages = AfternoonPages == null ? null : (string[])AfternoonPages.Clone();
            copy.ActionIds = ActionIds == null ? null : (string[])ActionIds.Clone();
            copy.MorningEffects = MorningEffects?.Snapshot();
            copy.AfternoonEffects = AfternoonEffects?.Snapshot();
            return copy;
        }
    }
    [Serializable]
    public sealed class ActionDefinition
    {
        public string Id, Name, ChoicePrompt;
        public bool AutoSelectChoice;
        public string ScenarioId, NodeId;
        public string BackgroundId, CharacterId, OutfitId, ExpressionId;
        public int StaminaCost, MoneyCost;
        public string[] Pages;
        public CampaignEffects Effects;
        public CampaignChoiceDefinition[] Choices;
        public ActionDefinition Snapshot()
        {
            var copy = (ActionDefinition)MemberwiseClone();
            copy.Pages = (string[])Pages.Clone(); copy.Effects = Effects.Snapshot();
            copy.Choices = Choices?.Select(c => c.Snapshot()).ToArray();
            return copy;
        }
        public string Describe()
        {
            var parts = new List<string>();
            if (StaminaCost > 0) parts.Add("体力 -" + StaminaCost);
            if (MoneyCost > 0) parts.Add("資金 -" + MoneyCost);
            var rewards = Effects.Describe();
            if (!string.IsNullOrEmpty(rewards)) parts.Add(rewards);
            return string.Join(" / ", parts);
        }
    }

    [Serializable]
    public sealed class CampaignChoiceDefinition
    {
        public string Id, Label, Description;
        public string ScenarioId, NodeId;
        public string BackgroundId, CharacterId, OutfitId, ExpressionId;
        public string[] Pages;
        public CampaignCondition[] Conditions;
        public CampaignEffects Effects;

        public CampaignChoiceDefinition Snapshot()
        {
            var copy = (CampaignChoiceDefinition)MemberwiseClone();
            copy.Pages = (string[])Pages.Clone();
            copy.Conditions = Conditions?.Select(c => c.Snapshot()).ToArray();
            copy.Effects = Effects.Snapshot();
            return copy;
        }

        public bool CanChoose(GamePlayerData data) =>
            (Conditions ?? Array.Empty<CampaignCondition>()).All(c => c.IsMet(data));
    }

    [Serializable]
    public sealed class CampaignCondition
    {
        public string Type, Id;
        public int Value;
        public bool Not;

        public CampaignCondition Snapshot() => (CampaignCondition)MemberwiseClone();

        public void Validate()
        {
            var numeric = new[] { "money", "physical", "toughness", "smart", "empathy", "appeal", "stamina", "regional_contribution", "public_awareness", "fox_favor", "completed_quest_count" };
            var identified = new[] { "affection", "route_step", "heroine_met", "heroine_unlocked", "route_flag", "event_cleared", "race_attempted", "race_succeeded", "quest_completed" };
            if (!numeric.Contains(Type) && !identified.Contains(Type))
                throw new InvalidOperationException("Unknown condition type: " + Type);
            if (identified.Contains(Type) && string.IsNullOrWhiteSpace(Id))
                throw new InvalidOperationException("Condition requires an ID: " + Type);
            if (Value < 0) throw new InvalidOperationException("Condition value cannot be negative");
        }

        public bool IsMet(GamePlayerData data) => Evaluate(data) != Not;

        private bool Evaluate(GamePlayerData data)
        {
            return Type switch
            {
                "money" => data.Money >= Value,
                "physical" => data.Physical >= Value,
                "toughness" => data.Toughness >= Value,
                "smart" => data.Smart >= Value,
                "empathy" => data.Empathy >= Value,
                "appeal" => data.Appeal >= Value,
                "stamina" => data.Stamina >= Value,
                "regional_contribution" => data.RegionalContribution >= Value,
                "public_awareness" => data.PublicAwareness >= Value,
                "fox_favor" => data.FoxFavor >= Value,
                "completed_quest_count" => (data.CompletedQuestIds?.Count ?? 0) >= Value,
                "affection" => (data.FindHeroineProgress(Id)?.Affection ?? 0) >= Value,
                "route_step" => (data.FindHeroineProgress(Id)?.RouteStep ?? 0) >= Value,
                "heroine_met" => data.FindHeroineProgress(Id)?.IsMet == true,
                "heroine_unlocked" => data.FindHeroineProgress(Id)?.IsUnlocked == true,
                "route_flag" => data.ActiveRouteFlags.Contains(Id),
                "event_cleared" => data.ClearedEventKeys.Contains(Id),
                "race_attempted" => data.RaceAttempts.Any(record => record.RaceId == Id),
                "race_succeeded" => data.RaceAttempts.Any(record => record.RaceId == Id && record.Succeeded),
                "quest_completed" => data.CompletedQuestIds?.Contains(Id) == true,
                _ => false
            };
        }
    }
    [Serializable]
    public sealed class CampaignEffects
    {
        public int Money, Physical, Toughness, Smart, Empathy, Appeal, Stamina, Affection, RouteStep, BikeMastery;
        public int RegionalContribution, PublicAwareness, FoxFavor;
        public string HeroineId, RouteFlag, QuestId;
        public CampaignEffects Snapshot() => (CampaignEffects)MemberwiseClone();
        public void Validate()
        {
            // Resource costs belong to action costs; this first schema only accepts positive rewards.
            if (new[] { Money, Physical, Toughness, Smart, Empathy, Appeal, Stamina, Affection, RouteStep, BikeMastery, RegionalContribution, PublicAwareness, FoxFavor }.Any(v => v < 0))
                throw new InvalidOperationException("Negative reward: use action costs instead");
            if ((Affection != 0 || RouteStep != 0) && string.IsNullOrWhiteSpace(HeroineId))
                throw new InvalidOperationException("Heroine reward requires HeroineId");
            if (!string.IsNullOrEmpty(QuestId) && string.IsNullOrWhiteSpace(QuestId))
                throw new InvalidOperationException("QuestId cannot contain only whitespace");
        }
        public string Describe()
        {
            var values = new[] { Money, Physical, Toughness, Smart, Empathy, Appeal, Stamina };
            var names = new[] { "資金", "フィジカル", "タフネス", "スマート", "エンパシー", "アピール", "体力" };
            var parts = new List<string>();
            for (int i = 0; i < values.Length; i++) if (values[i] != 0) parts.Add($"{names[i]} {values[i]:+0;-0;0}");
            if (Affection != 0) parts.Add($"好感度 +{Affection}");
            if (RouteStep != 0) parts.Add($"ルート段階 {RouteStep}");
            if (BikeMastery != 0) parts.Add($"愛車熟練度 +{BikeMastery}");
            if (RegionalContribution != 0) parts.Add($"地域貢献 +{RegionalContribution}");
            if (PublicAwareness != 0) parts.Add($"知名度 +{PublicAwareness}");
            if (FoxFavor != 0) parts.Add($"狐の加護 +{FoxFavor}");
            return string.Join(" / ", parts);
        }
        public void Apply(GamePlayerData d, int maxStamina)
        {
            d.CompletedQuestIds ??= new List<string>();
            if (!string.IsNullOrWhiteSpace(QuestId) && d.CompletedQuestIds.Contains(QuestId)) return;
            d.Money += Money; d.Physical += Physical; d.Toughness += Toughness;
            d.Smart += Smart; d.Empathy += Empathy; d.Appeal += Appeal;
            d.Stamina = (int)Math.Min(maxStamina, (long)d.Stamina + Stamina);
            d.RegionalContribution += RegionalContribution;
            d.PublicAwareness += PublicAwareness;
            d.FoxFavor += FoxFavor;
            d.AddCurrentBikeMastery(BikeMastery);
            if (!string.IsNullOrEmpty(HeroineId))
            {
                if (!d.MetHeroineIds.Contains(HeroineId)) d.MetHeroineIds.Add(HeroineId);
                var progress = d.EnsureHeroineProgress(HeroineId);
                progress.IsMet = true;
                progress.IsUnlocked = true;
                progress.Affection = Math.Min(100, progress.Affection + Affection);
                progress.RouteStep = Math.Max(progress.RouteStep, Math.Max(1, RouteStep));
            }
            if (!string.IsNullOrEmpty(RouteFlag) && !d.ActiveRouteFlags.Contains(RouteFlag)) d.ActiveRouteFlags.Add(RouteFlag);
            if (!string.IsNullOrWhiteSpace(QuestId)) d.CompletedQuestIds.Add(QuestId);
        }
    }
}
