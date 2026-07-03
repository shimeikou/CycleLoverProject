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
        private SoTotalSchedule _schedule;
        private List<ScheduleDay> _debugScheduleDays = new();
        
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

                    if (_schedule?.MonthScheduleDataPairs == null || _schedule.MonthScheduleDataPairs.Count == 0)
                    {
                        GameLogger.LogWarning("[ScheduleMasterDataHolder] Loaded schedule asset is empty. Fallback to debug schedule.");
                        LoadDebugSchedule();
                    }
                }
                else
                {
                    GameLogger.LogError("[ScheduleMasterDataHolder] Schedule load failed");
                    LoadDebugSchedule();
                }
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[ScheduleMasterDataHolder] Exception: {e}");
                LoadDebugSchedule();
            }
        }

        public override void Clear()
        {
            _schedule?.Clear();
            _debugScheduleDays?.Clear();
        }

        public void Release()
        {
            if (!_handle.HasValue) return;
            Addressables.Release(_handle);
            _handle = null;
        }

        public bool TryGetScheduleDay(in GameDate nextDate, out ScheduleDay scheduleDay)
        {
            if (_debugScheduleDays == null || _debugScheduleDays.Count == 0)
            {
                LoadDebugSchedule();
            }

            var year = nextDate.Year;
            var month = nextDate.Month;
            var dayValue = nextDate.Day;

            scheduleDay = _debugScheduleDays?.FirstOrDefault(day =>
                day.Year == year &&
                day.Month == month &&
                day.Day == dayValue);

            if (scheduleDay != null)
            {
                return true;
            }

            scheduleDay = null;
            return false;
        }

        private void LoadDebugSchedule()
        {
            _debugScheduleDays = SampleScheduleFactory.CreateInitialThreeDays();
        }
    }
}
