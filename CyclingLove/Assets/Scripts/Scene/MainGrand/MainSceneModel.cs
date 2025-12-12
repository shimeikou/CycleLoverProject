using System;
using System.Event;
using System.GameSession;
using System.Manager;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Scene.MainGrand
{
    public sealed class MainSceneModel
    {
        public static UniTask Initialize()
        {
            return GameScheduleManager.Instance.LoadSchedule();
        }
        

        public static UniTaskVoid LoadMasterData()
        {
            return new UniTaskVoid();
        }

        public static GameEvent GetCurrentEvent()
        {
            var nextDate = GameSession.CurrentDay;
            return GameScheduleManager.Instance.GetNextGameEvent(nextDate);
        }
    }
}