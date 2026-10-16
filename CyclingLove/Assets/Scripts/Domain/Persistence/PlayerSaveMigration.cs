using System.Collections.Generic;
using System.GameSession;
using CyclingLove.Equipment;

namespace System.Service
{
    public static class PlayerSaveMigration
    {
        public static void UpgradeToCurrent(GamePlayerData data)
        {
            if (data == null) throw new InvalidOperationException("セーブデータを読み込めませんでした。");
            if (data.FlowVersion == PlayerSaveValidator.SchemaVersion)
            {
                EnsureCollections(data);
                return;
            }
            if (data.FlowVersion != 1 && data.FlowVersion != 2)
                throw new InvalidOperationException("この形式のセーブデータには対応していません。");

            EnsureCollections(data);
            if (data.FlowVersion == 1)
            {
                foreach (var heroineId in data.MetHeroineIds)
                {
                    var progress = data.EnsureHeroineProgress(heroineId);
                    progress.IsMet = true;
                    progress.IsUnlocked = true;
                    if (progress.RouteStep == 0) progress.RouteStep = 1;
                }
            }
            data.FlowVersion = PlayerSaveValidator.SchemaVersion;
        }

        private static void EnsureCollections(GamePlayerData data)
        {
            data.ClearedEventKeys ??= new List<string>();
            data.ActiveRouteFlags ??= new List<string>();
            data.MetHeroineIds ??= new List<string>();
            data.HeroineProgresses ??= new List<HeroineProgress>();
            data.OwnedPartIds ??= new List<string>();
            data.EquippedBike ??= new PlayerBikeEquipment();
            data.BikeMasteries ??= new List<BikeMasteryProgress>();
            data.RaceAttempts ??= new List<RaceAttemptRecord>();
            data.SeasonGoalResults ??= new List<SeasonGoalRecord>();
            data.AnnualEndingResult ??= new AnnualEndingRecord();
        }
    }
}
