using System.GameSession;
using System.Linq;
using CyclingLove.Equipment;
using CyclingLove.Racing;

namespace System.Service
{
    public sealed class RaceApplicationResult
    {
        public string AttemptId, RaceId, ResultRouteFlag;
        public int Score, TargetScore, RewardMoney;
        public bool Succeeded;
        public bool Applied;
    }

    public sealed class RaceProgressService
    {
        private readonly RaceResolutionService resolver;

        public RaceProgressService(RaceCatalog races, BikeEquipmentCatalog equipment)
        {
            resolver = new RaceResolutionService(races, equipment);
        }

        public RaceApplicationResult ResolveAndApply(GamePlayerData data, string raceId, string attemptId)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(attemptId)) throw new ArgumentException("Race attempt ID is required", nameof(attemptId));
            data.RaceAttempts ??= new System.Collections.Generic.List<RaceAttemptRecord>();
            var existing = data.RaceAttempts.FirstOrDefault(record => record.AttemptId == attemptId);
            if (existing != null)
            {
                if (existing.RaceId != raceId)
                    throw new InvalidOperationException("Race attempt ID is already used by another race");
                return FromRecord(existing, false);
            }

            var resolution = resolver.Evaluate(data, raceId);
            var resultFlag = resolution.Succeeded ? resolution.SuccessRouteFlag : resolution.FailureRouteFlag;
            var reward = resolution.Succeeded ? resolution.SuccessMoney : 0;
            if (reward > int.MaxValue - data.Money)
                throw new InvalidOperationException("Race reward exceeds the money limit");

            var record = new RaceAttemptRecord
            {
                AttemptId = attemptId, RaceId = resolution.RaceId,
                Score = resolution.Score, TargetScore = resolution.TargetScore,
                Succeeded = resolution.Succeeded, RewardMoney = reward,
                ResultRouteFlag = resultFlag
            };
            data.RaceAttempts.Add(record);
            data.Money += reward;
            if (!data.ActiveRouteFlags.Contains(resultFlag)) data.ActiveRouteFlags.Add(resultFlag);
            data.Revision++;
            return FromRecord(record, true);
        }

        private static RaceApplicationResult FromRecord(RaceAttemptRecord record, bool applied) => new()
        {
            AttemptId = record.AttemptId, RaceId = record.RaceId,
            Score = record.Score, TargetScore = record.TargetScore,
            Succeeded = record.Succeeded, RewardMoney = record.RewardMoney,
            ResultRouteFlag = record.ResultRouteFlag, Applied = applied
        };
    }
}
