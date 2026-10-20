using System;
using System.Linq;
using CyclingLove.Equipment;

namespace CyclingLove.Presentation
{
    [Serializable]
    public sealed class BikeVisualCatalog
    {
        public int Version;
        public BikeVisualPartDefinition[] Parts;

        public BikeVisualPartDefinition Find(string partId) =>
            Parts?.FirstOrDefault(part => part.PartId == partId);

        public void Validate(BikeEquipmentCatalog equipment)
        {
            if (Version != 1 || Parts == null || Parts.Length == 0 || Parts.Any(part => part == null))
                throw new InvalidOperationException("Invalid bike visual catalog");
            if (equipment == null)
                throw new InvalidOperationException("Bike equipment catalog is required for visual validation");

            equipment.Validate();
            if (Parts.Any(part => string.IsNullOrWhiteSpace(part.PartId)) ||
                Parts.Select(part => part.PartId).Distinct().Count() != Parts.Length)
                throw new InvalidOperationException("Invalid or duplicate bike visual part ID");

            foreach (var visual in Parts)
            {
                visual.Validate();
                var part = equipment.Find(visual.PartId);
                if (part == null)
                    throw new InvalidOperationException("Bike visual refers to an unknown part: " + visual.PartId);
                if (part.Slot != visual.Slot)
                    throw new InvalidOperationException("Bike visual slot does not match equipment: " + visual.PartId);
            }

            foreach (var part in equipment.Parts)
            {
                if (Find(part.Id) == null)
                    throw new InvalidOperationException("Bike part has no visual mapping: " + part.Id);
            }
        }
    }

    [Serializable]
    public sealed class BikeVisualPartDefinition
    {
        public string PartId;
        public string Slot;
        public string Address;
        public string FallbackAddress;
        public string PackId;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(PartId) || !BikePartSlots.IsValid(Slot) ||
                string.IsNullOrWhiteSpace(Address) || string.IsNullOrWhiteSpace(FallbackAddress) ||
                string.IsNullOrWhiteSpace(PackId))
                throw new InvalidOperationException("Invalid bike visual mapping: " + PartId);
        }
    }
}
