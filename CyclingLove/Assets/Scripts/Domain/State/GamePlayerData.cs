using System.Data.Schedule;
using System.Collections.Generic;
using CyclingLove.Equipment;

namespace System.GameSession
{
    [Serializable]
    public class GamePlayerData
    {
        // ========= 基本プロフィール =========
        public string PlayerName = "主人公";
        public int Age = 28;

        // ========= ゲーム進行 =========
        public int Day = 1;
        public int Month = 4;
        public int Year = 2026;
        public ActionTiming Timing = ActionTiming.Morning;
        
        public int Money = 1000;
        public string LastVisitedPlace = "Home";

        // ========= ステータス =========
        public int Physical = 5;    // 身体能力・フィジカル
        public int Toughness = 5;   // 精神力・タフネス
        public int Smart = 5;       // 知識・スマート
        public int Empathy = 5;     // 共感力・エンパシー
        public int Appeal = 5;      // 表現力・アピール

        // The three-day prototype stores its exact interaction checkpoint.
        public int FlowVersion;
        public string CampaignId;
        public int ContentVersion;
        public System.Flow.GameFlowPhase SavedPhase = System.Flow.GameFlowPhase.Novel;
        public int NarrativePage;
        public string SelectedActionId;
        public string SelectedChoiceId;
        public int Revision;
        public int Stamina = 100;
        public int[] DayStartStats;

        // ========= 進行フラグ =========
        public List<string> ClearedEventKeys = new();
        public List<string> ActiveRouteFlags = new();
        public List<string> MetHeroineIds = new();
        public List<HeroineProgress> HeroineProgresses = new();

        // ========= 所持機材・現在装備 =========
        public List<string> OwnedPartIds = new();
        public PlayerBikeEquipment EquippedBike = new();
        public List<BikeMasteryProgress> BikeMasteries = new();
        public List<RaceAttemptRecord> RaceAttempts = new();
        public List<SeasonGoalRecord> SeasonGoalResults = new();
        public AnnualEndingRecord AnnualEndingResult = new();

        public BikeMasteryProgress FindBikeMastery(string loadoutId) =>
            BikeMasteries?.Find(x => x != null && x.LoadoutId == loadoutId);

        public void AddCurrentBikeMastery(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0 || string.IsNullOrWhiteSpace(EquippedBike?.LoadoutId)) return;
            BikeMasteries ??= new List<BikeMasteryProgress>();
            var progress = FindBikeMastery(EquippedBike.LoadoutId);
            if (progress == null)
            {
                progress = new BikeMasteryProgress { LoadoutId = EquippedBike.LoadoutId };
                BikeMasteries.Add(progress);
            }
            progress.Experience = Math.Min(100, progress.Experience + amount);
        }

        public HeroineProgress FindHeroineProgress(string heroineId) =>
            HeroineProgresses?.Find(x => x != null && x.HeroineId == heroineId);

        public HeroineProgress EnsureHeroineProgress(string heroineId)
        {
            if (string.IsNullOrWhiteSpace(heroineId)) throw new ArgumentException("Heroine ID is required", nameof(heroineId));
            HeroineProgresses ??= new List<HeroineProgress>();
            var progress = FindHeroineProgress(heroineId);
            if (progress != null) return progress;
            progress = new HeroineProgress { HeroineId = heroineId };
            HeroineProgresses.Add(progress);
            return progress;
        }
    }

    [Serializable]
    public sealed class HeroineProgress
    {
        public string HeroineId;
        public int Affection;
        public int RouteStep;
        public bool IsMet;
        public bool IsUnlocked;
    }

    [Serializable]
    public sealed class BikeMasteryProgress
    {
        public string LoadoutId;
        public int Experience;
    }

    [Serializable]
    public sealed class RaceAttemptRecord
    {
        public string AttemptId;
        public string RaceId;
        public int Score;
        public int TargetScore;
        public bool Succeeded;
        public int RewardMoney;
        public string ResultRouteFlag;
    }

    [Serializable]
    public sealed class SeasonGoalRecord
    {
        public string GoalId, SeasonId, ResultFlag;
        public int EvaluatedYear, EvaluatedMonth, EvaluatedDay;
        public bool Succeeded;
    }

    [Serializable]
    public sealed class AnnualEndingRecord
    {
        public bool IsResolved;
        public string EndingId, HeroineId, ResultFlag;
        public int EvaluatedYear, EvaluatedMonth, EvaluatedDay;
        public bool Succeeded;
    }
}
