using System;
using System.Collections.Generic;
using System.Linq;
using System.Service;
using CyclingLove.Calendar;
using CyclingLove.Content;
using CyclingLove.Racing;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CyclingLove.Editor
{
    public sealed class CampaignBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report) => ValidateContent();

        [MenuItem("Tools/Cycling Love/Validate Campaign Data")]
        public static void ValidateFromMenu()
        {
            ValidateContent();
            Debug.Log("Campaign data validation passed: prototype, regression, annual, equipment, bike visuals, races, calendar, and audio");
        }

        // The optional reader permits invalid-input tests without editing production assets.
        public static void ValidateContent(Func<string, string> readText = null)
        {
            if (readText != null)
            {
                ValidateSource(CampaignContentLoader.PrototypeResourceKey,
                    () => CampaignContentLoader.Load(CampaignContentLoader.PrototypeResourceKey, readText));
                return;
            }

            CampaignDefinition annual = null;
            AnnualCalendarDefinition calendar = null;
            RaceCatalog races = null;
            ValidateSource(CampaignContentLoader.PrototypeResourceKey, CampaignContentLoader.LoadPrototype);
            ValidateSource(CampaignContentLoader.RuntimeResourceKey, CampaignContentLoader.LoadRuntime);
            ValidateSource(CampaignContentLoader.AnnualResourceKey, () => annual = CampaignContentLoader.LoadAnnual());
            ValidateSource(EquipmentContentLoader.RuntimeResourceKey, EquipmentContentLoader.LoadRuntime);
            ValidateSource(BikeVisualContentLoader.RuntimeResourceKey, () => BikeVisualContentLoader.LoadRuntime());
            ValidateSource(RaceContentLoader.RuntimeResourceKey, () => races = RaceContentLoader.LoadRuntime());
            ValidateSource(AnnualCalendarContentLoader.RuntimeResourceKey, () => calendar = AnnualCalendarContentLoader.LoadRuntime());
            ValidateSource("annual reachability", () =>
            {
                ValidateAnnualReachability(annual, calendar, races);
                return true;
            });
            ValidateSource("visual asset references", () =>
            {
                WarnForMissingVisualAssets(new[]
                {
                    CampaignContentLoader.LoadPrototype(),
                    CampaignContentLoader.LoadRuntime(),
                    annual
                });
                return true;
            });
            ValidateSource(AudioContentLoader.RuntimeResourceKey, AudioContentLoader.LoadRuntime);
            ValidateSource("present audio assets", () =>
            {
                AudioAssetPipeline.ValidatePresentAssetsForBuild();
                return true;
            });
        }

        private static void ValidateSource<T>(string key, Func<T> validate)
        {
            try
            {
                validate();
            }
            catch (Exception ex)
            {
                throw new BuildFailedException($"Content validation failed [{key}]: {ex.Message}");
            }
        }

        public static void ValidateAnnualReachability(
            CampaignDefinition campaign,
            AnnualCalendarDefinition calendar,
            RaceCatalog races)
        {
            if (campaign == null || calendar == null || races == null)
                throw new InvalidOperationException("Annual reachability inputs are missing");

            var producedFlags = new HashSet<string>(StringComparer.Ordinal);
            foreach (var action in campaign.Actions)
            {
                AddFlag(producedFlags, action.Effects?.RouteFlag);
                foreach (var choice in action.Choices ?? Array.Empty<CampaignChoiceDefinition>())
                    AddFlag(producedFlags, choice.Effects?.RouteFlag);
            }
            foreach (var goal in calendar.SeasonGoals ?? Array.Empty<SeasonalGoalDefinition>())
            {
                AddFlag(producedFlags, goal.SuccessFlag);
                AddFlag(producedFlags, goal.FailureFlag);
            }
            foreach (var ending in calendar.Endings ?? Array.Empty<AnnualEndingDefinition>())
                AddFlag(producedFlags, ending.ResultFlag);
            foreach (var race in races.Races ?? Array.Empty<RaceDefinition>())
            {
                AddFlag(producedFlags, race.SuccessRouteFlag);
                AddFlag(producedFlags, race.FailureRouteFlag);
            }

            var requiredFlags = Conditions(campaign, calendar)
                .Where(value => value.Type == "route_flag" && !value.Not)
                .Select(value => value.Id)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var missingFlags = requiredFlags.Where(value => !producedFlags.Contains(value)).ToArray();
            if (missingFlags.Length > 0)
                throw new InvalidOperationException("Annual route flags have no producer: " + string.Join(", ", missingFlags));

            var routeSteps = campaign.Actions
                .SelectMany(action => new[] { action.Effects }
                    .Concat((action.Choices ?? Array.Empty<CampaignChoiceDefinition>()).Select(choice => choice.Effects)))
                .Where(effect => effect != null && !string.IsNullOrWhiteSpace(effect.HeroineId))
                .GroupBy(effect => effect.HeroineId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Max(effect => effect.RouteStep), StringComparer.Ordinal);
            var missingSteps = Conditions(campaign, calendar)
                .Where(value => value.Type == "route_step" && !value.Not)
                .Where(value => !routeSteps.TryGetValue(value.Id, out var maximum) || maximum < value.Value)
                .Select(value => $"{value.Id}:{value.Value}")
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (missingSteps.Length > 0)
                throw new InvalidOperationException("Annual route steps cannot be produced: " + string.Join(", ", missingSteps));
        }

        private static IEnumerable<CampaignCondition> Conditions(
            CampaignDefinition campaign,
            AnnualCalendarDefinition calendar)
        {
            var actionConditions = campaign.Actions.SelectMany(action =>
                (action.Choices ?? Array.Empty<CampaignChoiceDefinition>())
                    .SelectMany(choice => choice.Conditions ?? Array.Empty<CampaignCondition>()));
            var eventConditions = (campaign.ConditionalEvents ?? Array.Empty<CampaignConditionalEvent>())
                .SelectMany(value => value.Conditions ?? Array.Empty<CampaignCondition>());
            var goalConditions = (calendar.SeasonGoals ?? Array.Empty<SeasonalGoalDefinition>())
                .SelectMany(value => value.Conditions ?? Array.Empty<CampaignCondition>());
            var endingConditions = (calendar.Endings ?? Array.Empty<AnnualEndingDefinition>())
                .SelectMany(value => value.Conditions ?? Array.Empty<CampaignCondition>());
            return actionConditions.Concat(eventConditions).Concat(goalConditions).Concat(endingConditions);
        }

        private static void AddFlag(ISet<string> flags, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) flags.Add(value);
        }

        public static void WarnForMissingVisualAssets(IEnumerable<CampaignDefinition> campaigns)
        {
            var missing = FindMissingVisualAssets(
                campaigns,
                backgroundId => Resources.Load<Sprite>($"Images/Backgrounds/{backgroundId}") != null,
                HasCharacterSprite);
            if (missing.Length == 0) return;
            Debug.LogWarning(
                $"Visual asset audit found {missing.Length} unresolved references; fallback visuals will be used:\n" +
                string.Join("\n", missing));
        }

        public static string[] FindMissingVisualAssets(
            IEnumerable<CampaignDefinition> campaigns,
            Func<string, bool> hasBackground,
            Func<string, string, string, bool> hasCharacter)
        {
            if (campaigns == null) throw new ArgumentNullException(nameof(campaigns));
            if (hasBackground == null) throw new ArgumentNullException(nameof(hasBackground));
            if (hasCharacter == null) throw new ArgumentNullException(nameof(hasCharacter));

            var visuals = campaigns
                .Where(campaign => campaign != null)
                .SelectMany(Visuals)
                .ToArray();
            var missingBackgrounds = visuals
                .Select(value => value.BackgroundId)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .Where(value => !hasBackground(value))
                .Select(value => "background:" + value);
            var missingCharacters = visuals
                .Where(value => !string.IsNullOrWhiteSpace(value.CharacterId))
                .Select(value => new
                {
                    value.CharacterId,
                    OutfitId = value.OutfitId ?? string.Empty,
                    ExpressionId = string.IsNullOrWhiteSpace(value.ExpressionId) ? "neutral" : value.ExpressionId
                })
                .Distinct()
                .Where(value => !hasCharacter(value.CharacterId, value.OutfitId, value.ExpressionId))
                .Select(value => $"character:{value.CharacterId}/{value.OutfitId}/{value.ExpressionId}");
            return missingBackgrounds.Concat(missingCharacters)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static IEnumerable<VisualReference> Visuals(CampaignDefinition campaign)
        {
            foreach (var day in (campaign.Days ?? Array.Empty<CampaignDay>())
                         .Concat((campaign.ConditionalEvents ?? Array.Empty<CampaignConditionalEvent>())
                             .Select(value => value.Day).Where(value => value != null)))
            {
                yield return new VisualReference(day.MorningBackgroundId, day.MorningCharacterId,
                    day.MorningOutfitId, day.MorningExpressionId);
                yield return new VisualReference(day.AfternoonBackgroundId, day.AfternoonCharacterId,
                    day.AfternoonOutfitId, day.AfternoonExpressionId);
            }
            foreach (var action in campaign.Actions ?? Array.Empty<ActionDefinition>())
            {
                yield return new VisualReference(action.BackgroundId, action.CharacterId,
                    action.OutfitId, action.ExpressionId);
                foreach (var choice in action.Choices ?? Array.Empty<CampaignChoiceDefinition>())
                    yield return new VisualReference(choice.BackgroundId, choice.CharacterId,
                        choice.OutfitId, choice.ExpressionId);
            }
        }

        private static bool HasCharacterSprite(string characterId, string outfitId, string expressionId)
        {
            if (!string.IsNullOrWhiteSpace(outfitId))
            {
                if (Resources.Load<Sprite>($"Images/Characters/{characterId}/{outfitId}/{expressionId}") != null ||
                    Resources.Load<Sprite>($"Images/Characters/{characterId}_{outfitId}_{expressionId}") != null)
                    return true;
                if (expressionId != "neutral" &&
                    (Resources.Load<Sprite>($"Images/Characters/{characterId}/{outfitId}/neutral") != null ||
                     Resources.Load<Sprite>($"Images/Characters/{characterId}_{outfitId}_neutral") != null))
                    return true;
            }
            if (Resources.Load<Sprite>($"Images/Characters/{characterId}_{expressionId}") != null) return true;
            return Resources.Load<Sprite>($"Images/Characters/{characterId}_neutral") != null;
        }

        private sealed class VisualReference
        {
            public readonly string BackgroundId, CharacterId, OutfitId, ExpressionId;

            public VisualReference(string backgroundId, string characterId, string outfitId, string expressionId)
            {
                BackgroundId = backgroundId;
                CharacterId = characterId;
                OutfitId = outfitId;
                ExpressionId = expressionId;
            }
        }
    }
}
