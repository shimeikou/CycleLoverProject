using System.Collections.Generic;
using System.Data.Schedule;
using System.Event;
using System.Linq;
using System.ScriptableObjectClass;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Util;

namespace System.MasterDataHolder
{
    public sealed class ScheduleMasterDataHolder : MasterDataHolderBase
    {
        private SoTotalSchedule _schedule = new();
        
        private const string LoadKey = "Assets/ScriptableObject/SO_TotalSchedule.asset";

        private AsyncOperationHandle<SoTotalSchedule>? _handle;
        public override async UniTask LoadSo()
        {
            _handle = Addressables.LoadAssetAsync<SoTotalSchedule>(LoadKey);

            try
            {
                await _handle.Value.Task;

                if (_handle.Value.Status == AsyncOperationStatus.Succeeded)
                {
                    _schedule = _handle.Value.Result;
                    
                }
                else
                {
                    GameLogger.LogError("[ScheduleMasterDataHolder] Schedule load failed");
                    Clear();
                }
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[ScheduleMasterDataHolder] Exception: {e}");
                Clear();
            }
        }

        public override void Clear()
        {
            _schedule?.Clear();
        }

        public void Release()
        {
            if (!_handle.HasValue) return;
            Addressables.Release(_handle);
            _handle = null;
        }

        public void GetByDate(in GameDate nextDate, out GameEvent gameEvent)
        {
            gameEvent = null;
            
            var nextMouth = nextDate.Month;
            var nextDay = nextDate.Day;
            var mouthData = _schedule.MonthScheduleDataPairs?.Where(x => x.Day == nextMouth && x.Month == nextDay);
            var dayData = mouthData.FirstOrDefault(x => x.Day == nextDay);
            if (dayData == null)
            {
                GameLogger.LogError("[ScheduleMasterDataHolder]Schedule load failed");
                return;
            }
        }
    }
}