using System.Collections.Generic;
using System.Linq;
using System;
using CyclingLove.Flow;
using CyclingLove.State;

namespace CyclingLove.Progression
{
    public static class ProfileProgressService
    {
        public const string AnnouncerHeroineId = "hero_announcer";
        public const string OnlineRivalHeroineId = "hero_online_rival";

        private static readonly string[] NormalSuccessEndings =
        {
            "annual_confession_bikeshop",
            "annual_confession_cafe",
            "annual_confession_cousin"
        };

        public static void Normalize(ProfileProgress profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (profile.Version != 0 && profile.Version != ProfileProgress.CurrentVersion)
                throw new InvalidOperationException("Unsupported profile version");
            profile.Version = ProfileProgress.CurrentVersion;
            profile.RecordedRunIds ??= new List<string>();
            profile.ClearedEndingIds ??= new List<string>();
            profile.UnlockedHeroineIds ??= new List<string>();
            if (profile.CompletedAnnualRuns < 0 || HasInvalidIds(profile.RecordedRunIds) ||
                HasInvalidIds(profile.ClearedEndingIds) || HasInvalidIds(profile.UnlockedHeroineIds))
                throw new InvalidOperationException("Invalid profile progress");
            RefreshUnlocks(profile);
        }

        public static bool RegisterCompletedRun(ProfileProgress profile, GamePlayerData run)
        {
            Normalize(profile);
            if (run == null || run.SavedPhase != GameFlowPhase.Finished ||
                run.AnnualEndingResult?.IsResolved != true || string.IsNullOrWhiteSpace(run.RunId))
                return false;
            if (profile.RecordedRunIds.Contains(run.RunId)) return false;

            profile.RecordedRunIds.Add(run.RunId);
            profile.CompletedAnnualRuns++;
            AddUnique(profile.ClearedEndingIds, run.AnnualEndingResult.EndingId);
            RefreshUnlocks(profile);
            return true;
        }

        public static void ApplyUnlocks(ProfileProgress profile, GamePlayerData run)
        {
            if (profile == null || run == null) return;
            Normalize(profile);
            foreach (var heroineId in profile.UnlockedHeroineIds)
                run.EnsureHeroineProgress(heroineId).IsUnlocked = true;
        }

        private static void RefreshUnlocks(ProfileProgress profile)
        {
            if (profile.CompletedAnnualRuns >= 1)
                AddUnique(profile.UnlockedHeroineIds, AnnouncerHeroineId);
            if (profile.CompletedAnnualRuns >= 1 && profile.ClearedEndingIds.Any(NormalSuccessEndings.Contains))
                AddUnique(profile.UnlockedHeroineIds, OnlineRivalHeroineId);
        }

        private static bool HasInvalidIds(IReadOnlyCollection<string> values) =>
            values.Any(string.IsNullOrWhiteSpace) || values.Distinct().Count() != values.Count;

        private static void AddUnique(ICollection<string> values, string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value)) values.Add(value);
        }
    }
}
