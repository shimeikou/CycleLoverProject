using System.Collections.Generic;
using System.Data.Schedule;
using System.Flow;
using System.GameSession;
using System.Manager;
using Cysharp.Threading.Tasks;

namespace Scene.MainGrand
{
    public sealed class MainSceneModel
    {
        private const string MainGameSceneName = "MainGameScene";

        public async UniTask Initialize()
        {
            await GameFlowManager.Instance.ResumeCurrentFlow();
        }

        public GameFlowPhase GetCurrentPhase()
        {
            return GameFlowManager.Instance.CurrentPhase;
        }

        public GameSceneRequest GetCurrentRequest()
        {
            return GameFlowManager.Instance.PeekCurrentRequest() ?? GameFlowManager.Instance.BuildNextRequest();
        }

        public bool IsCurrentScenePlayable()
        {
            var request = GetCurrentRequest();
            return request != null &&
                   request.Phase != GameFlowPhase.Finished &&
                   request.UnitySceneName == MainGameSceneName;
        }

        public IReadOnlyList<MainSceneActionOption> GetActionOptions()
        {
            return new List<MainSceneActionOption>
            {
                new() { ActionId = "act_training_roadwork_001", Label = "基礎練習", Description = "ロード練習をしてフィジカルを鍛える仮行動。" },
                new() { ActionId = "act_town_walk_001", Label = "町へ行く", Description = "町を見て回って交流のきっかけを作る仮行動。" },
                new() { ActionId = "act_rest_home_001", Label = "家で休む", Description = "体力管理のために休息する仮行動。" },
            };
        }

        public MainSceneStepResult CompleteCurrentPhase(GameSceneResult sceneResult)
        {
            var request = GetCurrentRequest();
            var beforeDateText = GameSession.CurrentDay.ToString();
            var currentTiming = GameSession.CurrentDay.ActionTiming;

            if (request == null)
            {
                return CreateEmptyResult(beforeDateText, currentTiming, "No scene request");
            }

            var nextRequest = GameFlowManager.Instance.SubmitSceneResult(sceneResult);
            var rewardResult = request.Phase == GameFlowPhase.Novel
                ? GameFlowManager.Instance.LastEventApplicationResult
                : null;

            return new MainSceneStepResult
            {
                Phase = request.Phase,
                NextPhase = nextRequest?.Phase ?? GameFlowPhase.Finished,
                BeforeDateText = beforeDateText,
                AfterDateText = GameSession.CurrentDay.ToString(),
                Timing = currentTiming,
                EventKey = request.EventKey,
                DisplayName = request.DisplayName,
                RewardDebugText = rewardResult?.ToDebugText() ?? sceneResult?.Notes ?? "no rewards",
                WasEventResolved = !string.IsNullOrWhiteSpace(request.EventKey),
                HasRewardChange = rewardResult?.HasAnyChange ?? false,
                RequestedSceneName = request.UnitySceneName,
                NextSceneName = nextRequest?.UnitySceneName,
                CompletionType = sceneResult?.CompletionType ?? GameFlowCompletionType.None,
                SelectedActionId = sceneResult?.SelectedActionId,
                ScenarioId = request.ScenarioId,
            };
        }

        private static MainSceneStepResult CreateEmptyResult(string beforeDateText, ActionTiming timing, string displayName)
        {
            return new MainSceneStepResult
            {
                Phase = GameFlowPhase.None,
                NextPhase = GameFlowPhase.None,
                BeforeDateText = beforeDateText,
                AfterDateText = beforeDateText,
                Timing = timing,
                DisplayName = displayName,
                RewardDebugText = displayName,
                RequestedSceneName = MainGameSceneName,
                NextSceneName = MainGameSceneName,
                CompletionType = GameFlowCompletionType.None,
            };
        }
    }

    public sealed class MainSceneActionOption
    {
        public string ActionId;
        public string Label;
        public string Description;
    }

    public sealed class MainSceneStepResult
    {
        public GameFlowPhase Phase;
        public GameFlowPhase NextPhase;
        public string BeforeDateText;
        public string AfterDateText;
        public ActionTiming Timing;
        public string EventKey;
        public string DisplayName;
        public bool WasEventResolved;
        public string RewardDebugText;
        public bool HasRewardChange;
        public string RequestedSceneName;
        public string NextSceneName;
        public GameFlowCompletionType CompletionType;
        public string SelectedActionId;
        public string ScenarioId;
    }
}
