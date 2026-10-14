using System.GameSession;

namespace System.Service
{
    // Structural validation only. The active campaign validates content references and progress.
    public static class PlayerSaveValidator
    {
        public const int SchemaVersion = 1;
        public static void Validate(GamePlayerData d)
        {
            if (d == null || d.FlowVersion != SchemaVersion)
                throw new InvalidOperationException("この形式のセーブデータには対応していません。");
            if (d.Year < 1 || d.Year > 9999 || d.Month < 1 || d.Month > 12 || d.Day < 1 || d.Day > DateTime.DaysInMonth(d.Year, d.Month) ||
                d.NarrativePage < 0 || d.Revision < 0 || d.Stamina < 0 || d.Stamina > 100 || d.Money < 0 ||
                d.DayStartStats == null || d.DayStartStats.Length != 7 || d.ClearedEventKeys == null || d.ActiveRouteFlags == null || d.MetHeroineIds == null ||
                !Enum.IsDefined(typeof(System.Flow.GameFlowPhase), d.SavedPhase) || !Enum.IsDefined(typeof(System.Data.Schedule.ActionTiming), d.Timing))
                throw new InvalidOperationException("セーブデータの内容を確認できませんでした。");
        }
    }
}
