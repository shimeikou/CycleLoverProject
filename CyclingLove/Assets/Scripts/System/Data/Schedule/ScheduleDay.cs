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
        Special,
    }

    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter,
    }

    public enum ScheduleSlotType
    {
        FixedEvent,
        FreeChoice,
        ConditionalEvent,
        Summary,
        SpecialEvent,
    }
    
    [Serializable]
    public class ScheduleDay
    {
        public int Year;
        public int Month;
        public int Day;

        public Weather Weather;
        public DayOfWeek CurrentDay;
        public Season Season;
        public bool IsHoliday;
        public bool IsSpecialDay;

        public List<ScheduleSlot> Slots = new();

        public string GetEventKey(ActionTiming timing)
        {
            var slot = GetSlot(timing);
            if (slot == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(slot.FixedEventKey))
            {
                return slot.FixedEventKey;
            }

            return slot.CandidateEventKeys?.FirstOrDefault();
        }

        public ScheduleSlot GetSlot(ActionTiming timing)
        {
            return Slots?.FirstOrDefault(slot => slot.Timing == timing);
        }
    }

    [Serializable]
    public class ScheduleSlot
    {
        public ActionTiming Timing;
        public ScheduleSlotType SlotType;
        public string FixedEventKey;
        public List<string> CandidateEventKeys = new();
        public string FallbackEventKey;

        public ScheduleSlot(ActionTiming timing, ScheduleSlotType slotType, string fixedEventKey = null)
        {
            Timing = timing;
            SlotType = slotType;
            FixedEventKey = fixedEventKey;
        }
    }
}
