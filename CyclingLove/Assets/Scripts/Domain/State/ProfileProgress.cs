using System.Collections.Generic;

namespace System.GameSession
{
    [Serializable]
    public sealed class ProfileProgress
    {
        public const int CurrentVersion = 1;
        public int Version = CurrentVersion;
        public int CompletedAnnualRuns;
        public List<string> RecordedRunIds = new();
        public List<string> ClearedEndingIds = new();
        public List<string> UnlockedHeroineIds = new();
    }
}
