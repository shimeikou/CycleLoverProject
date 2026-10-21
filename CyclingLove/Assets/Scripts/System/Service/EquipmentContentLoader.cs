using CyclingLove.Equipment;

namespace CyclingLove.Services
{
    public static class EquipmentContentLoader
    {
        public const string RuntimeResourceKey = "Content/BikeParts";

        public static BikeEquipmentCatalog LoadRuntime()
        {
            var catalog = JsonContentReader.Load<BikeEquipmentCatalog>(RuntimeResourceKey);
            catalog.Validate();
            return catalog;
        }
    }
}
