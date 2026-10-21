using System;
using CyclingLove.Calendar;
using UnityEngine;

namespace CyclingLove.Services
{
    public static class AnnualCalendarContentLoader
    {
        public const string RuntimeResourceKey = "Content/AnnualCalendar";

        public static AnnualCalendarDefinition LoadRuntime()
        {
            var source = UnityEngine.Resources.Load<TextAsset>(RuntimeResourceKey);
            if (source == null || string.IsNullOrWhiteSpace(source.text))
                throw new InvalidOperationException("Annual calendar is missing: " + RuntimeResourceKey);
            try
            {
                var definition = JsonUtility.FromJson<AnnualCalendarDefinition>(source.text) ??
                                 throw new InvalidOperationException("Annual calendar is empty");
                definition.Validate();
                return definition;
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("Invalid annual calendar JSON", ex);
            }
        }
    }
}
