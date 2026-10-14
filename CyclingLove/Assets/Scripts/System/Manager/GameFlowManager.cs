using System.Flow;
using System.GameSession;
using System.Service;
using Cysharp.Threading.Tasks;
using GameSessionState = global::System.GameSession.GameSession;

namespace System.Manager
{
    public sealed class GameFlowManager : MonoSingleton<GameFlowManager>
    {
        private CampaignGameService _flow;
        private CampaignGameService Flow => _flow ??= new CampaignGameService(CampaignContentLoader.LoadPrototype());
        public System.Collections.Generic.IReadOnlyList<GameActionOption> GetActionOptions() => Flow.GetActionOptions(GameSessionState.PlayerData);
        public GameFlowPhase CurrentPhase => GameSessionState.PlayerData?.SavedPhase ?? GameFlowPhase.None;
        public GameSceneRequest CurrentRequest { get; private set; }

        public async UniTask StartNewGameFlow()
        {
            await GameSessionState.InitConfig();
            GameSessionState.PlayerData = Flow.NewGame();
            BuildNextRequest();
        }

        public async UniTask LoadGameFlow()
        {
            // Do not replace a live session until the load has been validated.
            var loaded = await SaveDataService.Load(GameSessionState.CurrentSaveSlot);
            Flow.Validate(loaded);
            GameSessionState.PlayerData = loaded;
            BuildNextRequest();
        }

        public async UniTask ResumeCurrentFlow()
        {
            await GameSessionState.InitConfig();
            if (GameSessionState.PlayerData == null)
                GameSessionState.PlayerData = Flow.NewGame();
            BuildNextRequest();
        }

        public GameSceneRequest PeekCurrentRequest() => CurrentRequest;
        public GameSceneRequest BuildNextRequest()
        {
            return CurrentRequest = Flow.Request(GameSessionState.PlayerData);
        }

        public GameSceneRequest SubmitSceneResult(GameSceneResult result)
        {
            Flow.Submit(GameSessionState.PlayerData, result);
            return BuildNextRequest();
        }
    }
}
