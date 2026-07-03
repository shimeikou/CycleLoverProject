using System.Data.Schedule;
using System.Event;
using System.MasterDataHolder;
using System.GameSession;
using System.Service;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace System.Manager
{
    public class GameScheduleManager : MonoSingleton<GameScheduleManager>
    {
        private readonly ScheduleMasterDataHolder _scheduleMasterData = new ();
        private readonly GameEventResolverService _eventResolverService = new();
        private readonly IReadOnlyList<GameEvent> _sampleEventCatalog = SampleGameEventFactory.CreateInitialThreeDays();
        
        public UniTask LoadSchedule()
        {
            return _scheduleMasterData.LoadSo();
        }

        public GameEvent GetNextGameEvent(GameDate nextDate)
        {
            return ResolveCurrentEvent(nextDate, global::System.GameSession.GameSession.PlayerData);
        }

        public GameEvent ResolveCurrentEvent(GameDate nextDate, GamePlayerData playerData)
        {
            if (!_scheduleMasterData.TryGetScheduleDay(in nextDate, out var scheduleDay))
            {
                return null;
            }

            return _eventResolverService.ResolveEvent(
                scheduleDay,
                nextDate.ActionTiming,
                _sampleEventCatalog,
                playerData,
                nextDate);
        }

        public bool TryGetScheduleDay(in GameDate nextDate, out ScheduleDay scheduleDay)
        {
            return _scheduleMasterData.TryGetScheduleDay(in nextDate, out scheduleDay);
        }

        public IReadOnlyList<GameEvent> GetEventCatalog()
        {
            return _sampleEventCatalog;
        }
        
        public ScheduleMasterDataHolder GetData()
        {
            return _scheduleMasterData;
        }

       
    }
}
