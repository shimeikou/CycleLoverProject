using CyclingLove.Calendar;

namespace CyclingLove.Services
{
    public static class AnnualCalendarContentLoader
    {
        public const string RuntimeResourceKey = "Content/AnnualCalendar";

        public static AnnualCalendarDefinition LoadRuntime()
        {
            var definition = JsonContentReader.Load<AnnualCalendarDefinition>(RuntimeResourceKey);
            definition.Validate();
            return definition;
        }
    }
}
