using System.Data.Schedule;
using System.Event;
using System.Flow;
using System.GameSession;
using System.Service;
using Cysharp.Threading.Tasks;
using GameSessionState = global::System.GameSession.GameSession;

namespace System.Manager
{
    public sealed class GameFlowManager : MonoSingleton<GameFlowManager>
    {
        private readonly GameDayProgressionService _progressionService = new();

        public GameFlowPhase CurrentPhase { get; private set; } = GameFlowPhase.None;
        public GameEvent CurrentEvent { get; private set; }
        public GameSceneRequest CurrentRequest { get; private set; }
        public EventApplicationResult LastEventApplicationResult { get; private set; }

        public async UniTask StartNewGameFlow()
        {
            GameSessionState.IsNewGame = true;
            await EnsureSessionReady();

            CurrentPhase = GameFlowPhase.Boot;
            CurrentEvent = null;
            LastEventApplicationResult = null;
            CurrentRequest = BuildNextRequestInternal();
        }

        public async UniTask ResumeCurrentFlow()
        {
            await EnsureSessionReady();

            if (CurrentPhase == GameFlowPhase.None || CurrentPhase == GameFlowPhase.Finished)
            {
                CurrentPhase = GameFlowPhase.Boot;
            }

            LastEventApplicationResult = null;
            CurrentRequest ??= BuildNextRequestInternal();
        }

        public GameSceneRequest PeekCurrentRequest()
        {
            return CurrentRequest;
        }

        public GameSceneRequest BuildNextRequest()
        {
            CurrentRequest = BuildNextRequestInternal();
            return CurrentRequest;
        }

        public GameSceneRequest SubmitSceneResult(GameSceneResult result)
        {
            ApplySceneResult(result);
            CurrentRequest = BuildNextRequestInternal();
            return CurrentRequest;
        }

        private async UniTask EnsureSessionReady()
        {
            await GameSessionState.InitConfig();

            if (GameSessionState.PlayerData == null || GameSessionState.CurrentDay.Year == 0)
            {
                await GameSessionState.SetUpPlayerData();
            }

            await GameScheduleManager.Instance.LoadSchedule();
        }

        private GameSceneRequest BuildNextRequestInternal()
        {
            while (true)
            {
                switch (CurrentPhase)
                {
                    case GameFlowPhase.None:
                    case GameFlowPhase.Boot:
                        CurrentPhase = GameFlowPhase.ResolveSchedule;
                        continue;

                    case GameFlowPhase.ResolveSchedule:
                        CurrentEvent = GameScheduleManager.Instance.ResolveCurrentEvent(GameSessionState.CurrentDay, GameSessionState.PlayerData);
                        if (CurrentEvent == null)
                        {
                            CurrentPhase = GameFlowPhase.AdvanceTime;
                            continue;
                        }

                        CurrentPhase = MapEventToPhase(CurrentEvent);
                        return CreateRequest(CurrentPhase, CurrentEvent);

                    case GameFlowPhase.ActionSelection:
                    case GameFlowPhase.Novel:
                    case GameFlowPhase.MiniGameAction:
                    case GameFlowPhase.DailySummary:
                    case GameFlowPhase.Finished:
                        return CreateRequest(CurrentPhase, CurrentEvent);

                    case GameFlowPhase.AdvanceTime:
                        _progressionService.Advance(GameSessionState.PlayerData, GameSessionState.CurrentDay);
                        CurrentEvent = null;
                        LastEventApplicationResult = null;

                        if (!GameScheduleManager.Instance.TryGetScheduleDay(GameSessionState.CurrentDay, out _))
                        {
                            CurrentPhase = GameFlowPhase.Finished;
                            return CreateRequest(CurrentPhase, null);
                        }

                        CurrentPhase = GameFlowPhase.ResolveSchedule;
                        continue;

                    default:
                        CurrentPhase = GameFlowPhase.Finished;
                        return CreateRequest(CurrentPhase, null);
                }
            }
        }

        private void ApplySceneResult(GameSceneResult result)
        {
            switch (CurrentPhase)
            {
                case GameFlowPhase.ActionSelection:
                    if (CurrentEvent != null && !string.IsNullOrWhiteSpace(result?.SelectedActionId))
                    {
                        CurrentEvent.ActionId = result.SelectedActionId;
                    }

                    CurrentPhase = GameFlowPhase.Novel;
                    break;

                case GameFlowPhase.Novel:
                    ExecuteCurrentEvent();
                    CurrentPhase = ShouldEnterMiniGame(CurrentEvent)
                        ? GameFlowPhase.MiniGameAction
                        : CurrentEvent != null && CurrentEvent.Type == GameEventType.Summary
                            ? GameFlowPhase.DailySummary
                            : GameFlowPhase.AdvanceTime;
                    break;

                case GameFlowPhase.MiniGameAction:
                    CurrentPhase = GameFlowPhase.AdvanceTime;
                    break;

                case GameFlowPhase.DailySummary:
                    CurrentPhase = GameFlowPhase.AdvanceTime;
                    break;
            }
        }

        private static GameFlowPhase MapEventToPhase(GameEvent gameEvent)
        {
            if (gameEvent == null)
            {
                return GameFlowPhase.AdvanceTime;
            }

            return gameEvent.Type switch
            {
                GameEventType.FreeTime => GameFlowPhase.ActionSelection,
                _ => GameFlowPhase.Novel,
            };
        }

        private static bool ShouldEnterMiniGame(GameEvent gameEvent)
        {
            if (gameEvent == null)
            {
                return false;
            }

            return gameEvent.Type == GameEventType.Race ||
                   gameEvent.Type == GameEventType.Shop ||
                   !string.IsNullOrWhiteSpace(gameEvent.ActionId);
        }

        private void ExecuteCurrentEvent()
        {
            if (CurrentEvent == null)
            {
                return;
            }

            CurrentEvent.Start();
            CurrentEvent.Running();
            CurrentEvent.Finish();
            LastEventApplicationResult = _progressionService.CompleteCurrentEvent(CurrentEvent, GameSessionState.PlayerData, GameSessionState.CurrentDay);
        }

        private static GameSceneRequest CreateRequest(GameFlowPhase phase, GameEvent gameEvent)
        {
            var sceneType = MapPhaseToSceneType(phase);
            return new GameSceneRequest
            {
                Phase = phase,
                SceneType = sceneType,
                UnitySceneName = GameSceneRouteTable.ResolveUnitySceneName(sceneType),
                CurrentDateText = GameSessionState.CurrentDay.ToString(),
                Timing = GameSessionState.CurrentDay.ActionTiming,
                EventKey = gameEvent?.EventKey,
                ScenarioId = gameEvent?.ScenarioId,
                ActionId = gameEvent?.ActionId,
                DisplayName = gameEvent?.DisplayName ?? phase.ToString(),
            };
        }

        private static GameFlowSceneType MapPhaseToSceneType(GameFlowPhase phase)
        {
            return phase switch
            {
                GameFlowPhase.ActionSelection => GameFlowSceneType.ActionSelection,
                GameFlowPhase.Novel => GameFlowSceneType.Novel,
                GameFlowPhase.MiniGameAction => GameFlowSceneType.MiniGameAction,
                GameFlowPhase.DailySummary => GameFlowSceneType.Summary,
                GameFlowPhase.Finished => GameFlowSceneType.Result,
                _ => GameFlowSceneType.None,
            };
        }
    }
}
