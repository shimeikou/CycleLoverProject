using System.Collections.Generic;
using System.Data.Schedule;
using System.Event;
using System.Linq;
using System.ScriptableObjectClass;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using Util;

namespace System.MasterDataHolder
{
    public sealed class ScheduleMasterDataHolder : MasterDataHolderBase
    {
        private List<List<ScheduleDay>> _schedule = new();
        
        private const string LoadKey = "Assets/ScriptableObject/SO_MainSchedule.asset";

        public override async UniTask LoadSo()
        {
            var handle = Addressables.LoadAssetAsync<SoTotalSchedule>(LoadKey);
            await handle.Task;
            // if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            // {
            //     _schedule = handle.Result;
            // }
            // else
            // {
            //     GameLogger.LogInfo("[ScheduleMasterDataHolder]Schedule load failed");
            //     Clear();
            // }
        }

        public override void Clear()
        {
            _schedule?.Clear();
        }


        public void GetByDate(in GameDate nextDate, out GameEvent gameEvent)
        {
            gameEvent = null;
            
            var nextMouth = nextDate.Month;
            var nextDay = nextDate.Day;
            var mouthData = _schedule.SelectMany(x => x).Where(x => x.Day == nextMouth && x.Month == nextDay);
            if (mouthData == null)
            {
                GameLogger.LogError("[ScheduleMasterDataHolder]Schedule load failed");
                return;
            }
            var dayData = mouthData.FirstOrDefault(x => x.Day == nextDay);
            if (dayData == null)
            {
                GameLogger.LogError("[ScheduleMasterDataHolder]Schedule load failed");
                return;
            }

            var timing = nextDate.ActionTiming;
            var eventKey = dayData.GetEventKey(timing);


        }
    }
}