using System;
using CyclingLove.Equipment;
using UnityEngine;

namespace CyclingLove.Services
{
    public static class EquipmentContentLoader
    {
        public const string RuntimeResourceKey = "Content/BikeParts";

        public static BikeEquipmentCatalog LoadRuntime()
        {
            var source = UnityEngine.Resources.Load<TextAsset>(RuntimeResourceKey);
            if (source == null || string.IsNullOrWhiteSpace(source.text))
                throw new InvalidOperationException("Bike equipment catalog is missing: " + RuntimeResourceKey);
            try
            {
                var catalog = JsonUtility.FromJson<BikeEquipmentCatalog>(source.text) ??
                    throw new InvalidOperationException("Bike equipment catalog is empty");
                catalog.Validate();
                return catalog;
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("Invalid bike equipment JSON", ex);
            }
        }
    }
}
