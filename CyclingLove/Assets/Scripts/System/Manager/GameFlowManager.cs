using System;
using CyclingLove.Equipment;
using CyclingLove.Flow;
using CyclingLove.Progression;
using CyclingLove.Racing;
using CyclingLove.Services;
using CyclingLove.Session;
using Cysharp.Threading.Tasks;

namespace CyclingLove.Manager
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
            CampaignContentLoader.LoadAnnual(), EquipmentCatalog, AnnualCalendarContentLoader.LoadRuntime());
        private BikeEquipmentService Equipment => _equipment ??= new BikeEquipmentService(EquipmentCatalog);
        private BikePerformanceService Performance => _performance ??= new BikePerformanceService(EquipmentCatalog);
        private RaceResolutionService RacePreview => _racePreview ??= new RaceResolutionService(RaceCatalog, EquipmentCatalog);
        private RaceProgressService Races => _races ??= new RaceProgressService(
            RaceCatalog, EquipmentCatalog);
        public System.Collections.Generic.IReadOnlyList<GameActionOption> GetActionOptions() => Flow.GetActionOptions(GameSession.PlayerData);
        public GameFlowPhase CurrentPhase => GameSession.PlayerData?.SavedPhase ?? GameFlowPhase.None;
        public bool CanUseBikeMenu => CurrentPhase == GameFlowPhase.ActionSelection;
        public GameSceneRequest CurrentRequest { get; private set; }

        public async UniTask StartNewGameFlow()
        {
            await GameSession.InitConfig();
            GameSession.PlayerData = Flow.NewGame(GameSession.SystemConfig.Profile);
            BuildNextRequest();
        }

        public async UniTask LoadGameFlow()
        {
            // Do not replace a live session until the load has been validated.
            var loaded = await SaveDataService.Load(GameSession.CurrentSaveSlot);
            Flow.Validate(loaded);
            GameSession.PlayerData = loaded;
            BuildNextRequest();
        }

        public async UniTask ResumeCurrentFlow()
        {
            await GameSession.InitConfig();
            if (GameSession.PlayerData == null)
                GameSession.PlayerData = Flow.NewGame(GameSession.SystemConfig.Profile);
            BuildNextRequest();
        }

        public async UniTask PersistProfileProgress()
        {
            if (!ProfileProgressService.RegisterCompletedRun(GameSession.SystemConfig.Profile, GameSession.PlayerData)) return;
            await SaveDataService.SaveSystem(GameSession.SystemConfig);
        }

        public GameSceneRequest PeekCurrentRequest() => CurrentRequest;
        public GameSceneRequest BuildNextRequest()
        {
            return CurrentRequest = Flow.Request(GameSession.PlayerData);
        }

        public GameSceneRequest SubmitSceneResult(GameSceneResult result)
        {
            Flow.Submit(GameSession.PlayerData, result);
            return BuildNextRequest();
        }

        public EquipmentOperationResult PurchasePart(string partId) =>
            Equipment.Purchase(GameSession.PlayerData, partId);

        public EquipmentOperationResult EquipPart(string partId) =>
            Equipment.Equip(GameSession.PlayerData, partId);

        public System.Collections.Generic.IReadOnlyList<BikePartDefinition> GetBikeParts() =>
            Array.ConvertAll(EquipmentCatalog.Parts, part => part.Snapshot());

        public BikePerformanceResult GetBikePerformance() =>
            Performance.Calculate(GameSession.PlayerData);

        public BikePerformanceComparison CompareBikePart(string partId) =>
            Performance.Compare(GameSession.PlayerData, partId);

        public System.Collections.Generic.IReadOnlyList<RaceDefinition> GetRaces() =>
            Array.ConvertAll(RaceCatalog.Races, race => race.Snapshot());

        public RaceResolution PreviewRace(string raceId) =>
            RacePreview.Evaluate(GameSession.PlayerData, raceId);

        public RaceApplicationResult RunRace(string raceId) =>
            Races.ResolveAndApply(GameSession.PlayerData, raceId);

        public bool HasAttemptedRaceToday(string raceId) =>
            RaceProgressService.HasAttemptedToday(GameSession.PlayerData, raceId);

        public GameSkipPreview GetSkipPreview(int maxDays) =>
            Flow.GetSkipPreview(GameSession.PlayerData, maxDays);

        public bool SkipNormalDays(int maxDays)
        {
            var skipped = Flow.SkipNormalDays(GameSession.PlayerData, maxDays);
            if (skipped) BuildNextRequest();
            return skipped;
        }
    }
}
