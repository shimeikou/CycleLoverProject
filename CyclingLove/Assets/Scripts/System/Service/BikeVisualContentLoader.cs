using CyclingLove.Equipment;
using CyclingLove.Presentation;

namespace CyclingLove.Services
{
    public static class BikeVisualContentLoader
    {
        public const string RuntimeResourceKey = "Content/BikeVisuals";

        public static BikeVisualCatalog LoadRuntime(BikeEquipmentCatalog equipment = null)
        {
            var catalog = JsonContentReader.Load<BikeVisualCatalog>(RuntimeResourceKey);
            catalog.Validate(equipment ?? EquipmentContentLoader.LoadRuntime());
            return catalog;
        }
    }
}
