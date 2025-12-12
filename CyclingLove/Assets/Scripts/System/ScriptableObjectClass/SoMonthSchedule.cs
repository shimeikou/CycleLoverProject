using System.Collections.Generic;
using System.Data.Schedule;
using UnityEngine;

namespace System.ScriptableObjectClass
{
    [CreateAssetMenu(fileName = "SO_MonthSchedule", menuName = "ScriptableObjects/MonthSchedule", order = 0)]
    public class SoMonthSchedule : ScriptableObject
    {
        public int Month { get; private set; }
        public List<ScheduleDay> Days;
    }
}