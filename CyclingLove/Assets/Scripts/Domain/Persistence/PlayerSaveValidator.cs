using System.GameSession;
using System.Linq;

namespace System.Service
{
    // Structural validation only. The active campaign validates content references and progress.
    public static class PlayerSaveValidator
    {
        public const int SchemaVersion = 5;
        public static void Validate(GamePlayerData d)
        {
            if (d == null || d.FlowVersion != SchemaVersion)
                throw new InvalidOperationException("この形式のセーブデータには対応していません。");
            if (string.IsNullOrWhiteSpace(d.RunId) || d.Year < 1 || d.Year > 9999 || d.Month < 1 || d.Month > 12 || d.Day < 1 || d.Day > DateTime.DaysInMonth(d.Year, d.Month) ||
                d.NarrativePage < 0 || d.Revision < 0 || d.Stamina < 0 || d.Money < 0 ||
                d.RegionalContribution < 0 || d.PublicAwareness < 0 || d.FoxFavor < 0 ||
                d.DayStartStats == null || d.DayStartStats.Length != 7 || d.ClearedEventKeys == null || d.ActiveRouteFlags == null || d.MetHeroineIds == null || d.HeroineProgresses == null ||
                d.CompletedQuestIds == null ||
                d.OwnedPartIds == null || d.EquippedBike == null || d.BikeMasteries == null || d.RaceAttempts == null || d.SeasonGoalResults == null || d.AnnualEndingResult == null ||
                !Enum.IsDefined(typeof(System.Flow.GameFlowPhase), d.SavedPhase) || !Enum.IsDefined(typeof(System.Data.Schedule.ActionTiming), d.Timing))
                throw new InvalidOperationException("セーブデータの内容を確認できませんでした。");
            RequireUnique(d.ClearedEventKeys);
            RequireUnique(d.ActiveRouteFlags);
            RequireUnique(d.MetHeroineIds);
            RequireUnique(d.CompletedQuestIds);
            RequireUnique(d.OwnedPartIds);
            foreach (var equippedId in new[] { d.EquippedBike.FramePartId, d.EquippedBike.ComponentsPartId, d.EquippedBike.WheelPartId })
                if (!string.IsNullOrWhiteSpace(equippedId) && !d.OwnedPartIds.Contains(equippedId))
                    throw new InvalidOperationException("装備中の機材が所持品にありません。");
            if (d.BikeMasteries.Any(x => x == null || string.IsNullOrWhiteSpace(x.LoadoutId) || x.Experience < 0 || x.Experience > 100) ||
                d.BikeMasteries.Select(x => x.LoadoutId).Distinct().Count() != d.BikeMasteries.Count)
                throw new InvalidOperationException("愛車熟練度データを確認できませんでした。");
            if (d.RaceAttempts.Any(x => x == null || string.IsNullOrWhiteSpace(x.AttemptId) || string.IsNullOrWhiteSpace(x.RaceId) ||
                    x.Score < 0 || x.TargetScore <= 0 || x.RewardMoney < 0 || string.IsNullOrWhiteSpace(x.ResultRouteFlag)) ||
                d.RaceAttempts.Select(x => x.AttemptId).Distinct().Count() != d.RaceAttempts.Count)
                throw new InvalidOperationException("レース挑戦記録を確認できませんでした。");
            if (d.SeasonGoalResults.Any(x => x == null || string.IsNullOrWhiteSpace(x.GoalId) ||
                    string.IsNullOrWhiteSpace(x.SeasonId) || string.IsNullOrWhiteSpace(x.ResultFlag) ||
                    !IsDate(x.EvaluatedYear, x.EvaluatedMonth, x.EvaluatedDay)) ||
                d.SeasonGoalResults.Select(x => x.GoalId).Distinct().Count() != d.SeasonGoalResults.Count)
                throw new InvalidOperationException("季節目標の記録を確認できませんでした。");
            if (d.AnnualEndingResult.IsResolved &&
                (string.IsNullOrWhiteSpace(d.AnnualEndingResult.EndingId) || string.IsNullOrWhiteSpace(d.AnnualEndingResult.ResultFlag) ||
                 !IsDate(d.AnnualEndingResult.EvaluatedYear, d.AnnualEndingResult.EvaluatedMonth, d.AnnualEndingResult.EvaluatedDay)))
                throw new InvalidOperationException("年間結末の記録を確認できませんでした。");
            if (!d.AnnualEndingResult.IsResolved &&
                (!string.IsNullOrWhiteSpace(d.AnnualEndingResult.EndingId) || !string.IsNullOrWhiteSpace(d.AnnualEndingResult.HeroineId) ||
                 !string.IsNullOrWhiteSpace(d.AnnualEndingResult.ResultFlag) || d.AnnualEndingResult.EvaluatedYear != 0 ||
                 d.AnnualEndingResult.EvaluatedMonth != 0 || d.AnnualEndingResult.EvaluatedDay != 0 || d.AnnualEndingResult.Succeeded))
                throw new InvalidOperationException("年間結末の未確定記録に値が含まれています。");
            if (d.HeroineProgresses.Any(x => x == null || string.IsNullOrWhiteSpace(x.HeroineId) || x.Affection < 0 || x.Affection > 100 || x.RouteStep < 0) ||
                d.HeroineProgresses.Select(x => x.HeroineId).Distinct().Count() != d.HeroineProgresses.Count)
                throw new InvalidOperationException("ヒロイン進行データを確認できませんでした。");
            if (d.MetHeroineIds.Any(id => d.FindHeroineProgress(id)?.IsMet != true))
                throw new InvalidOperationException("ヒロインの出会い状態が一致しません。");
        }

        private static void RequireUnique(System.Collections.Generic.List<string> values)
        {
            if (values.Any(string.IsNullOrWhiteSpace) || values.Distinct().Count() != values.Count)
                throw new InvalidOperationException("セーブデータのID一覧を確認できませんでした。");
        }

        private static bool IsDate(int year, int month, int day) =>
            year >= 1 && year <= 9999 && month >= 1 && month <= 12 && day >= 1 && day <= DateTime.DaysInMonth(year, month);
    }
}
