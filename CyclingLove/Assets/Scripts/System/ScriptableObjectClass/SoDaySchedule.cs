using System.Data.Schedule;
using UnityEngine;

namespace System.ScriptableObjectClass
{
    
    [CreateAssetMenu(fileName = "SO_DaySchedule", menuName = "ScriptableObjects/DaySchedule", order = 0)]
    public class SoDaySchedule : ScriptableObject
    {
        public ScheduleDay ScheduleDayData;
    }
}