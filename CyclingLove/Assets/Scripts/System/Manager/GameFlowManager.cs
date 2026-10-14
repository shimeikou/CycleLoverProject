using System.Flow;
using System.GameSession;
using System.Service;
using Cysharp.Threading.Tasks;
using GameSessionState = global::System.GameSession.GameSession;

namespace System.Manager
{
    public sealed class GameFlowManager : MonoSingleton<GameFlowManager>
    {
        private readonly ThreeDayGameService _flow = new();
        public GameFlowPhase CurrentPhase => GameSessionState.PlayerData?.SavedPhase ?? GameFlowPhase.None;
        public GameSceneRequest CurrentRequest { get; private set; }
        public EventApplicationResult LastEventApplicationResult => null;

        public async UniTask StartNewGameFlow()
        {
            await GameSessionState.InitConfig();
            GameSessionState.IsNewGame = true;
            GameSessionState.PlayerData = _flow.NewGame();
            BuildNextRequest();
        }

        public async UniTask LoadGameFlow()
        {
            // Do not replace a live session until the load has been validated.
            var loaded = await SaveDataService.Load(GameSessionState.CurrentSaveSlot);
            _flow.Validate(loaded);
            GameSessionState.PlayerData = loaded;
            GameSessionState.IsNewGame = false;
            BuildNextRequest();
        }

        public async UniTask ResumeCurrentFlow()
        {
            await GameSessionState.InitConfig();
            if (GameSessionState.PlayerData == null || GameSessionState.PlayerData.FlowVersion != ThreeDayGameService.Version)
                GameSessionState.PlayerData = _flow.NewGame();
            BuildNextRequest();
        }

        public GameSceneRequest PeekCurrentRequest() => CurrentRequest;
        public GameSceneRequest BuildNextRequest()
        {
            var d = GameSessionState.PlayerData;
            GameSessionState.CurrentDay.Year = d.Year;
            GameSessionState.CurrentDay.Month = d.Month;
            GameSessionState.CurrentDay.Day = d.Day;
            GameSessionState.CurrentDay.ActionTiming = d.Timing;
            return CurrentRequest = _flow.Request(d);
        }

        public GameSceneRequest SubmitSceneResult(GameSceneResult result)
        {
            _flow.Submit(GameSessionState.PlayerData, result);
            return BuildNextRequest();
        }
    }
}
