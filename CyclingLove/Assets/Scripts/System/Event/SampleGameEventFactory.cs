using System.Collections.Generic;

namespace System.Event
{
    public static class SampleGameEventFactory
    {
        public static List<GameEvent> CreateInitialThreeDays()
        {
            return new List<GameEvent>
            {
                CreateScenario(
                    "evt_2026_04_01_morning_001",
                    "scn_intro_work_001",
                    "導入: 仕事",
                    "都会を離れた主人公の現在地を説明する導入。"),
                CreateScenario(
                    "evt_2026_04_01_afternoon_001",
                    "scn_intro_grandma_001",
                    "導入: 祖母の家",
                    "祖母との再会と、地方での暮らしの空気を見せる。"),
                CreateSummary(
                    "evt_2026_04_01_evening_001",
                    "scn_intro_summary_001",
                    "1日目まとめ"),
                CreateScenario(
                    "evt_2026_04_02_morning_001",
                    "scn_intro_work_002",
                    "導入: 2日目の仕事",
                    "主人公が今の生活基盤をどう支えているかを示す。"),
                CreateScenario(
                    "evt_2026_04_02_afternoon_001",
                    "scn_intro_bike_001",
                    "祖父のロードバイク発見",
                    "物置で古いロードバイクを見つける最初の転機。"),
                CreateSummary(
                    "evt_2026_04_02_evening_001",
                    "scn_intro_summary_002",
                    "2日目まとめ"),
                CreateScenario(
                    "evt_2026_04_03_morning_001",
                    "scn_intro_work_003",
                    "導入: 3日目の仕事",
                    "地方での暮らしに少し慣れ始めた様子を出す。"),
                CreateScenario(
                    "evt_2026_04_03_afternoon_001",
                    "scn_heroine1_meet_001",
                    "ヒロイン1との出会い",
                    "自転車屋を訪れ、ヒロイン1と初対面する。",
                    "hero_bikeshop",
                    new List<EventReward>
                    {
                        new() { Type = RewardType.MarkHeroineMet, StringValue = "hero_bikeshop" },
                        new() { Type = RewardType.AddRouteFlag, StringValue = "route_bikeshop_intro_started" },
                        new() { Type = RewardType.ClearEvent }
                    }),
                CreateSummary(
                    "evt_2026_04_03_evening_001",
                    "scn_intro_summary_003",
                    "3日目まとめ"),
            };
        }

        private static GameEvent CreateScenario(
            string eventKey,
            string scenarioId,
            string displayName,
            string description,
            string heroineId = null,
            List<EventReward> rewards = null)
        {
            return new GameEvent
            {
                EventKey = eventKey,
                Type = GameEventType.Scenario,
                ScenarioId = scenarioId,
                DisplayName = displayName,
                Description = description,
                TargetHeroineId = heroineId,
                Rewards = rewards ?? new List<EventReward>
                {
                    new() { Type = RewardType.ClearEvent }
                }
            };
        }

        private static GameEvent CreateSummary(
            string eventKey,
            string scenarioId,
            string displayName)
        {
            return new GameEvent
            {
                EventKey = eventKey,
                Type = GameEventType.Summary,
                ScenarioId = scenarioId,
                DisplayName = displayName,
                Rewards = new List<EventReward>
                {
                    new() { Type = RewardType.ClearEvent }
                }
            };
        }
    }
}
