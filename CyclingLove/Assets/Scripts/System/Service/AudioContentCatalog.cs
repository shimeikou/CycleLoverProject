using System.Collections.Generic;
using System.Linq;
using System;
using CyclingLove.Calendar;
using CyclingLove.Flow;

namespace CyclingLove.Audio
{
    public static class AudioContentKinds
    {
        public const string Bgm = "bgm";
        public const string Ambient = "ambient";
        public const string Se = "se";

        public static bool IsKnown(string value) =>
            value == Bgm || value == Ambient || value == Se;
    }

    [Serializable]
    public sealed class AudioContentEntry
    {
        public string Id;
        public string Kind;
        public string Address;
        public string PackId;
        public string AssetPath;
        public string ResourcePath;
        public bool Loop;
        public float Volume = 1f;
    }

    [Serializable]
    public sealed class AudioPresentationRule
    {
        public string Id;
        public int Priority;
        public string Phase;
        public string Timing;
        public string ScenarioId;
        public string CharacterId;
        public string BackgroundId;
        public string BackgroundPrefix;
        public string NodeContains;
        public string BgmId;
        public string AmbientId;
        public string EnterSeId;

        public bool Matches(GameSceneRequest request)
        {
            if (request == null) return false;
            if (!string.IsNullOrWhiteSpace(Phase) &&
                !string.Equals(Phase, request.Phase.ToString(), StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrWhiteSpace(Timing) &&
                !string.Equals(Timing, request.Timing.ToString(), StringComparison.OrdinalIgnoreCase)) return false;
            if (!MatchesExact(ScenarioId, request.ScenarioId) ||
                !MatchesExact(CharacterId, request.CharacterId) ||
                !MatchesExact(BackgroundId, request.BackgroundId)) return false;
            if (!string.IsNullOrWhiteSpace(BackgroundPrefix) &&
                !(request.BackgroundId?.StartsWith(BackgroundPrefix, StringComparison.Ordinal) ?? false)) return false;
            if (!string.IsNullOrWhiteSpace(NodeContains) &&
                !(request.NodeId?.Contains(NodeContains, StringComparison.Ordinal) ?? false)) return false;
            return true;
        }

        public void Validate(IReadOnlyDictionary<string, AudioContentEntry> entries)
        {
            if (string.IsNullOrWhiteSpace(Id) || Priority < 0)
                throw new InvalidOperationException("Invalid audio presentation rule: " + Id);
            if (!string.IsNullOrWhiteSpace(Phase) &&
                !Enum.TryParse<GameFlowPhase>(Phase, true, out _))
                throw new InvalidOperationException("Unknown audio rule phase: " + Id);
            if (!string.IsNullOrWhiteSpace(Timing) &&
                !Enum.TryParse<ActionTiming>(Timing, true, out _))
                throw new InvalidOperationException("Unknown audio rule timing: " + Id);
            if (new[] { BgmId, AmbientId, EnterSeId }.All(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("Audio rule has no output: " + Id);
            RequireKind(entries, BgmId, AudioContentKinds.Bgm, Id);
            RequireKind(entries, AmbientId, AudioContentKinds.Ambient, Id);
            RequireKind(entries, EnterSeId, AudioContentKinds.Se, Id);
        }

        private static bool MatchesExact(string expected, string actual) =>
            string.IsNullOrWhiteSpace(expected) || string.Equals(expected, actual, StringComparison.Ordinal);

        private static void RequireKind(
            IReadOnlyDictionary<string, AudioContentEntry> entries, string entryId, string kind, string ruleId)
        {
            if (string.IsNullOrWhiteSpace(entryId)) return;
            if (!entries.TryGetValue(entryId, out var entry) || entry.Kind != kind)
                throw new InvalidOperationException($"Audio rule {ruleId} references an invalid {kind}: {entryId}");
        }
    }

    public sealed class AudioPresentationCue
    {
        public string BgmId;
        public string AmbientId;
        public string EnterSeId;
    }

    [Serializable]
    public sealed class AudioContentCatalog
    {
        public int Version;
        public AudioContentEntry[] Entries;
        public AudioPresentationRule[] Rules;

        public void Validate()
        {
            if (Version < 1 || Entries == null || Entries.Length == 0)
                throw new InvalidOperationException("Invalid audio content catalog");
            if (Entries.Any(value => value == null || string.IsNullOrWhiteSpace(value.Id)) ||
                Entries.Select(value => value.Id).Distinct().Count() != Entries.Length)
                throw new InvalidOperationException("Invalid or duplicate audio content ID");
            foreach (var entry in Entries)
            {
                if (!AudioContentKinds.IsKnown(entry.Kind))
                    throw new InvalidOperationException("Unknown audio content kind: " + entry.Id);
                if (string.IsNullOrWhiteSpace(entry.Address) || !entry.Address.StartsWith("audio/", StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(entry.PackId) || string.IsNullOrWhiteSpace(entry.AssetPath) ||
                    !entry.AssetPath.StartsWith("Assets/Content/", StringComparison.Ordinal) ||
                    entry.Volume <= 0f || entry.Volume > 1f)
                    throw new InvalidOperationException("Invalid audio content entry: " + entry.Id);
                if (!entry.AssetPath.EndsWith("/" + entry.Id + ".wav", StringComparison.Ordinal))
                    throw new InvalidOperationException("Audio asset path does not match its ID: " + entry.Id);
                if ((entry.Kind == AudioContentKinds.Se) == entry.Loop)
                    throw new InvalidOperationException("Invalid audio loop setting: " + entry.Id);
            }
            var rules = Rules ?? Array.Empty<AudioPresentationRule>();
            if (rules.Any(value => value == null || string.IsNullOrWhiteSpace(value.Id)) ||
                rules.Select(value => value.Id).Distinct().Count() != rules.Length)
                throw new InvalidOperationException("Invalid or duplicate audio presentation rule");
            var entries = Entries.ToDictionary(value => value.Id, StringComparer.Ordinal);
            foreach (var rule in rules) rule.Validate(entries);
        }

        public AudioContentEntry Find(string id) =>
            string.IsNullOrWhiteSpace(id) ? null : Entries?.FirstOrDefault(value => value.Id == id);

        public AudioPresentationCue Resolve(GameSceneRequest request)
        {
            var cue = new AudioPresentationCue();
            foreach (var rule in (Rules ?? Array.Empty<AudioPresentationRule>())
                         .Where(value => value.Matches(request))
                         .OrderByDescending(value => value.Priority)
                         .ThenBy(value => value.Id, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(cue.BgmId) && !string.IsNullOrWhiteSpace(rule.BgmId))
                    cue.BgmId = rule.BgmId;
                if (string.IsNullOrWhiteSpace(cue.AmbientId) && !string.IsNullOrWhiteSpace(rule.AmbientId))
                    cue.AmbientId = rule.AmbientId;
                if (string.IsNullOrWhiteSpace(cue.EnterSeId) && !string.IsNullOrWhiteSpace(rule.EnterSeId))
                    cue.EnterSeId = rule.EnterSeId;
            }
            return cue;
        }
    }
}
