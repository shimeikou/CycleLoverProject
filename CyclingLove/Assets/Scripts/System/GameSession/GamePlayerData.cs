using System.Data.Schedule;
using System.Collections.Generic;

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
        public System.Flow.GameFlowPhase SavedPhase = System.Flow.GameFlowPhase.Novel;
        public int NarrativePage;
        public string SelectedActionId;
        public int Revision;
        public int Stamina = 100;
        public int[] DayStartStats;

        // ========= 進行フラグ =========
        public List<string> ClearedEventKeys = new();
        public List<string> ActiveRouteFlags = new();
        public List<string> MetHeroineIds = new();
    }
}
