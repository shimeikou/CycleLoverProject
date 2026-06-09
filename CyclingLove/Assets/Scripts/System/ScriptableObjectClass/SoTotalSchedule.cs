using System.Collections.Generic;
using System.Data.Schedule;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace System.ScriptableObjectClass
{
    [CreateAssetMenu(fileName = "SO_TotalSchedule", menuName = "ScriptableObjects/TotalSchedule", order = 0)]
    public class SoTotalSchedule : ScriptableObject
    {
        public List<MonthScheduleReference> MonthScheduleDataPairs;

        public void Clear()
        {
            MonthScheduleDataPairs?.Clear();
        }
    }
    
    [Serializable]
    public class MonthScheduleReference
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public AssetReference Value = null;
        
    }
}