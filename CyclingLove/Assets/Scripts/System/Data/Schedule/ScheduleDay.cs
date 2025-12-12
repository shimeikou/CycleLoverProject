using System.Collections.Generic;
using System.Linq;

namespace System.Data.Schedule
{
    public enum Weather
    {
        Sunny,
        Cloudy,
        Rainy,
        Snow,
        Storm
    }

    public enum ActionTiming
    {
        Morning,
        Afternoon,
        Evening,
        Extra,
    }
    
    [Serializable]
    public class ScheduleDay
    {
        public int Year;
        public int Month;
        public int Day;
        
        public Weather Weather;
        public DayOfWeek CurrentDay;

        public List<ActionEventPair> ActionEvents = new ();
        
        public string GetEventKey(ActionTiming timing)
        {
            return (from pair in ActionEvents where pair.Timing == timing select pair.EventKey).FirstOrDefault();
        }
    }
    
    [Serializable]
    public struct ActionEventPair
    {
        public ActionTiming Timing;
        public string EventKey;
        

        public ActionEventPair(string eventKey)
        {
            EventKey = eventKey;
            Timing = ActionTiming.Morning;
        }
    }
}