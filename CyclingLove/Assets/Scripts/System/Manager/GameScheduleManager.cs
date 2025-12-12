using System.Data.Schedule;
using System.Event;
using System.MasterDataHolder;
using Cysharp.Threading.Tasks;

namespace System.Manager
{
    public class GameScheduleManager : MonoSingleton<GameScheduleManager>
    {
        private readonly ScheduleMasterDataHolder _scheduleMasterData = new ();
        
        public UniTask LoadSchedule()
        {
            return _scheduleMasterData.LoadSo();
        }

        public GameEvent GetNextGameEvent(GameDate nextDate)
        {
            _scheduleMasterData.GetByDate(in nextDate, out var nextGameEvent);
            return nextGameEvent;
        }
        
       
    }
}