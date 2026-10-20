using CyclingLove.Equipment;
using CyclingLove.Presentation;
using UnityEngine;

namespace System.Service
{
    public static class BikeVisualContentLoader
    {
        public const string RuntimeResourceKey = "Content/BikeVisuals";

        public static BikeVisualCatalog LoadRuntime(BikeEquipmentCatalog equipment = null)
        {
            var source = UnityEngine.Resources.Load<TextAsset>(RuntimeResourceKey);
            if (source == null || string.IsNullOrWhiteSpace(source.text))
                throw new InvalidOperationException("Bike visual catalog is missing: " + RuntimeResourceKey);
            try
            {
                var catalog = JsonUtility.FromJson<BikeVisualCatalog>(source.text) ??
                    throw new InvalidOperationException("Bike visual catalog is empty");
                catalog.Validate(equipment ?? EquipmentContentLoader.LoadRuntime());
                return catalog;
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("Invalid bike visual catalog JSON", ex);
            }
        }
    }
}
