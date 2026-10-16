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

        // "ui_" prefix matches attempts saved by earlier builds, so they still count for their day.
        public static string DailyAttemptId(GamePlayerData data, string raceId) =>
            $"ui_{raceId}_{data.Year:0000}{data.Month:00}{data.Day:00}";

        public static bool HasAttemptedToday(GamePlayerData data, string raceId)
        {
            var attemptId = DailyAttemptId(data, raceId);
            return data.RaceAttempts?.Any(record => record != null && record.AttemptId == attemptId) == true;
        }

        public RaceApplicationResult ResolveAndApply(GamePlayerData data, string raceId)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var attemptId = DailyAttemptId(data, raceId);
            data.RaceAttempts ??= new System.Collections.Generic.List<RaceAttemptRecord>();
            var existing = data.RaceAttempts.FirstOrDefault(record => record.AttemptId == attemptId);
            if (existing != null) return FromRecord(existing, false);

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
