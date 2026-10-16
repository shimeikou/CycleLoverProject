using System.Flow;
using System.GameSession;
using System.Service;
using CyclingLove.Equipment;
using CyclingLove.Racing;
using Cysharp.Threading.Tasks;
using GameSessionState = global::System.GameSession.GameSession;

namespace System.Manager
{
    public sealed class GameFlowManager : MonoSingleton<GameFlowManager>
    {
        private CampaignGameService _flow;
        private BikeEquipmentService _equipment;
        private BikePerformanceService _performance;
        private RaceResolutionService _racePreview;
        private RaceProgressService _races;
        private BikeEquipmentCatalog _equipmentCatalog;
        private RaceCatalog _raceCatalog;
        private BikeEquipmentCatalog EquipmentCatalog => _equipmentCatalog ??= EquipmentContentLoader.LoadRuntime();
        private RaceCatalog RaceCatalog => _raceCatalog ??= RaceContentLoader.LoadRuntime();
        private CampaignGameService Flow => _flow ??= new CampaignGameService(
            CampaignContentLoader.LoadRuntime(), EquipmentCatalog, AnnualCalendarContentLoader.LoadRuntime());
        private BikeEquipmentService Equipment => _equipment ??= new BikeEquipmentService(EquipmentCatalog);
        private BikePerformanceService Performance => _performance ??= new BikePerformanceService(EquipmentCatalog);
        private RaceResolutionService RacePreview => _racePreview ??= new RaceResolutionService(RaceCatalog, EquipmentCatalog);
        private RaceProgressService Races => _races ??= new RaceProgressService(
            RaceCatalog, EquipmentCatalog);
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

        public EquipmentOperationResult PurchasePart(string partId) =>
            Equipment.Purchase(GameSessionState.PlayerData, partId);

        public EquipmentOperationResult EquipPart(string partId) =>
            Equipment.Equip(GameSessionState.PlayerData, partId);

        public System.Collections.Generic.IReadOnlyList<BikePartDefinition> GetBikeParts() =>
            Array.ConvertAll(EquipmentCatalog.Parts, part => part.Snapshot());

        public BikePerformanceResult GetBikePerformance() =>
            Performance.Calculate(GameSessionState.PlayerData);

        public BikePerformanceComparison CompareBikePart(string partId) =>
            Performance.Compare(GameSessionState.PlayerData, partId);

        public System.Collections.Generic.IReadOnlyList<RaceDefinition> GetRaces() =>
            Array.ConvertAll(RaceCatalog.Races, race => race.Snapshot());

        public RaceResolution PreviewRace(string raceId) =>
            RacePreview.Evaluate(GameSessionState.PlayerData, raceId);

        public RaceApplicationResult RunRace(string raceId) =>
            Races.ResolveAndApply(GameSessionState.PlayerData, raceId);

        public bool HasAttemptedRaceToday(string raceId) =>
            RaceProgressService.HasAttemptedToday(GameSessionState.PlayerData, raceId);
    }
}
