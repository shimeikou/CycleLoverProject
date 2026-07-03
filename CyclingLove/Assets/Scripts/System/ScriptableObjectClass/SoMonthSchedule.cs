using System.Collections.Generic;
using System.Data.Schedule;
using UnityEngine;

namespace System.ScriptableObjectClass
{
    [CreateAssetMenu(fileName = "SO_MonthSchedule", menuName = "ScriptableObjects/MonthSchedule", order = 0)]
    public class SoMonthSchedule : ScriptableObject
    {
        public int Year;
        public int Month;
        public List<ScheduleDay> Days;
    }
}
