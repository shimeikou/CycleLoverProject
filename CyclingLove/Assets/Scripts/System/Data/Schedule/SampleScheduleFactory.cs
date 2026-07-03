using System.Collections.Generic;

namespace System.Data.Schedule
{
    public static class SampleScheduleFactory
    {
        public static List<ScheduleDay> CreateInitialThreeDays()
        {
            return new List<ScheduleDay>
            {
                new()
                {
                    Year = 2026,
                    Month = 4,
                    Day = 1,
                    Weather = Weather.Sunny,
                    CurrentDay = DayOfWeek.Wednesday,
                    Season = Season.Spring,
                    Slots = new List<ScheduleSlot>
                    {
                        new(ActionTiming.Morning, ScheduleSlotType.FixedEvent, "evt_2026_04_01_morning_001"),
                        new(ActionTiming.Afternoon, ScheduleSlotType.FixedEvent, "evt_2026_04_01_afternoon_001"),
                        new(ActionTiming.Evening, ScheduleSlotType.Summary, "evt_2026_04_01_evening_001"),
                    }
                },
                new()
                {
                    Year = 2026,
                    Month = 4,
                    Day = 2,
                    Weather = Weather.Cloudy,
                    CurrentDay = DayOfWeek.Thursday,
                    Season = Season.Spring,
                    Slots = new List<ScheduleSlot>
                    {
                        new(ActionTiming.Morning, ScheduleSlotType.FixedEvent, "evt_2026_04_02_morning_001"),
                        new(ActionTiming.Afternoon, ScheduleSlotType.FixedEvent, "evt_2026_04_02_afternoon_001"),
                        new(ActionTiming.Evening, ScheduleSlotType.Summary, "evt_2026_04_02_evening_001"),
                    }
                },
                new()
                {
                    Year = 2026,
                    Month = 4,
                    Day = 3,
                    Weather = Weather.Sunny,
                    CurrentDay = DayOfWeek.Friday,
                    Season = Season.Spring,
                    Slots = new List<ScheduleSlot>
                    {
                        new(ActionTiming.Morning, ScheduleSlotType.FixedEvent, "evt_2026_04_03_morning_001"),
                        new(ActionTiming.Afternoon, ScheduleSlotType.FixedEvent, "evt_2026_04_03_afternoon_001"),
                        new(ActionTiming.Evening, ScheduleSlotType.Summary, "evt_2026_04_03_evening_001"),
                    }
                }
            };
        }
    }
}
