using System.Data.Schedule;

namespace System.Flow
{
    public enum GameFlowPhase
    {
        None,
        Boot,
        ResolveSchedule,
        ActionSelection,
        Novel,
        MiniGameAction,
        DailySummary,
        AdvanceTime,
        Finished,
        NovelChoice,
    }

    public enum GameFlowSceneType
    {
        None,
        Init,
        ActionSelection,
        Novel,
        MiniGameAction,
        Summary,
        Result,
    }

    public enum GameFlowCompletionType
    {
        None,
        EnterScene,
        ActionSelected,
        NovelClosed,
        MiniGameFinished,
        SummaryClosed,
        ForcedAdvance,
        NovelChoiceSelected,
    }

    public sealed class GameActionOption
    {
        public string ActionId, Label, Description;
        public bool CanExecute;
    }

    public sealed class GameSkipPreview
    {
        public int SkippableDays;
        public string StopDateText;
        public string StopReason;
    }

    public sealed class GameSceneRequest
    {
        public GameFlowPhase Phase;
        public GameFlowSceneType SceneType;
        public string UnitySceneName;
        public string CurrentDateText;
        public int DayNumber, TotalDays, MaxStamina;
        public string SeasonId, SeasonDisplayName;
        public bool IsHoliday, IsAnnualFinalDay;
        public string[] SpecialSlotIds;
        public ActionTiming Timing;
        public string EventKey;
        public string ScenarioId;
        public string NodeId;
        public string PageId;
        public string BackgroundId;
        public string CharacterId;
        public string OutfitId;
        public string ExpressionId;
        public string ActionId;
        public string DisplayName;
        public string BodyText;
        public string ContinueLabel;
        public int Revision;

        public string DisplayDateText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CurrentDateText))
                    return CurrentDateText;
                if (string.IsNullOrWhiteSpace(SeasonDisplayName))
                    return IsHoliday ? CurrentDateText + "（休日）" : CurrentDateText;
                var details = new System.Collections.Generic.List<string>
                {
                    SeasonDisplayName,
                    IsHoliday ? "休日" : "平日"
                };
                if (SpecialSlotIds?.Length > 0) details.Add("特別日");
                if (IsAnnualFinalDay) details.Add("最終日");
                return CurrentDateText + "（" + string.Join("・", details) + "）";
            }
        }
    }

    public sealed class GameSceneResult
    {
        public GameFlowCompletionType CompletionType;
        public string SelectedActionId;
        public string SelectedChoiceId;
        public int MiniGameScore;
        public int ExpectedRevision = -1;
        public string Notes;
    }

    public static class GameSceneRouteTable
    {
        public static string ResolveUnitySceneName(GameFlowSceneType sceneType)
        {
            return sceneType switch
            {
                GameFlowSceneType.Init => "InitScene",
                GameFlowSceneType.ActionSelection => "MainGameScene",
                GameFlowSceneType.Novel => "MainGameScene",
                GameFlowSceneType.MiniGameAction => "MainGameScene",
                GameFlowSceneType.Summary => "MainGameScene",
                GameFlowSceneType.Result => "MainGameScene",
                _ => "MainGameScene",
            };
        }
    }
}
