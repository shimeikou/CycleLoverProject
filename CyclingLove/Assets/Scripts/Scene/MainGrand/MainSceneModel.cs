using System;
using System.Data.Schedule;
using System.Event;
using System.GameSession;
using System.Manager;
using System.Service;
using Cysharp.Threading.Tasks;

namespace Scene.MainGrand
{
    public sealed class MainSceneModel
    {
        private readonly GameDayProgressionService _progressionService = new();
        
        public async UniTask Initialize()
        {
            await GameSession.InitConfig();

            if (GameSession.PlayerData == null || GameSession.CurrentDay.Year == 0)
            {
                await GameSession.SetUpPlayerData();
            }

            await GameScheduleManager.Instance.LoadSchedule();
        }
        

        public UniTaskVoid LoadMasterData()
        {
            return new UniTaskVoid();
        }

        public GameEvent GetCurrentEvent()
        {
            var nextDate = GameSession.CurrentDay;
            return GameScheduleManager.Instance.ResolveCurrentEvent(nextDate, GameSession.PlayerData);
        }

        public bool HasScheduleForCurrentDay()
        {
            return GameScheduleManager.Instance.TryGetScheduleDay(GameSession.CurrentDay, out _);
        }

        public MainSceneStepResult ProcessCurrentTiming()
        {
            var beforeDateText = GameSession.CurrentDay.ToString();
            var currentTiming = GameSession.CurrentDay.ActionTiming;
            var currentEvent = GetCurrentEvent();
            EventApplicationResult applicationResult = null;

            if (currentEvent != null)
            {
                currentEvent.Start();
                currentEvent.Running();
                currentEvent.Finish();
                applicationResult = _progressionService.CompleteCurrentEvent(currentEvent, GameSession.PlayerData, GameSession.CurrentDay);
            }

            var advancedWithinDay = _progressionService.Advance(GameSession.PlayerData, GameSession.CurrentDay);

            return new MainSceneStepResult
            {
                BeforeDateText = beforeDateText,
                AfterDateText = GameSession.CurrentDay.ToString(),
                Timing = currentTiming,
                EventKey = currentEvent?.EventKey,
                EventType = currentEvent?.Type ?? GameEventType.None,
                DisplayName = currentEvent?.DisplayName,
                WasEventResolved = currentEvent != null,
                AdvancedWithinDay = advancedWithinDay,
                RewardDebugText = applicationResult?.ToDebugText(),
                HasRewardChange = applicationResult?.HasAnyChange ?? false,
            };
        }
    }

    public sealed class MainSceneStepResult
    {
        public string BeforeDateText;
        public string AfterDateText;
        public ActionTiming Timing;
        public string EventKey;
        public GameEventType EventType;
        public string DisplayName;
        public bool WasEventResolved;
        public bool AdvancedWithinDay;
        public string RewardDebugText;
        public bool HasRewardChange;
    }
}
