using System;
using System.Collections.Generic;
using System.Linq;

namespace CyclingLove.Equipment
{
    public static class BikePartSlots
    {
        public const string Frame = "frame";
        public const string Components = "components";
        public const string Wheels = "wheels";

        public static bool IsValid(string value) =>
            value == Frame || value == Components || value == Wheels;
    }

    [Serializable]
    public sealed class BikeEquipmentCatalog
    {
        public int Version;
        public BikePartDefinition[] Parts;

        public BikeEquipmentCatalog Snapshot()
        {
            Validate();
            return new BikeEquipmentCatalog
            {
                Version = Version,
                Parts = Parts.Select(part => part.Snapshot()).ToArray()
            };
        }

        public void Validate()
        {
            if (Version < 1 || Parts == null || Parts.Length == 0 || Parts.Any(part => part == null))
                throw new InvalidOperationException("Invalid bike equipment catalog");
            if (Parts.Any(part => string.IsNullOrWhiteSpace(part.Id)) ||
                Parts.Select(part => part.Id).Distinct().Count() != Parts.Length)
                throw new InvalidOperationException("Invalid or duplicate bike part ID");
            foreach (var part in Parts) part.Validate();
        }

        public BikePartDefinition Find(string partId) =>
            Parts?.FirstOrDefault(part => part.Id == partId);

        public void ValidateLoadout(IReadOnlyCollection<string> ownedPartIds, PlayerBikeEquipment equipped)
        {
            if (ownedPartIds == null || equipped == null)
                throw new InvalidOperationException("Bike equipment state is missing");
            if (ownedPartIds.Any(string.IsNullOrWhiteSpace) || ownedPartIds.Distinct().Count() != ownedPartIds.Count)
                throw new InvalidOperationException("Owned bike part IDs are invalid");
            if (ownedPartIds.Any(id => Find(id) == null))
                throw new InvalidOperationException("Owned bike part is missing from the catalog");

            ValidateEquippedSlot(equipped.FramePartId, BikePartSlots.Frame, ownedPartIds);
            ValidateEquippedSlot(equipped.ComponentsPartId, BikePartSlots.Components, ownedPartIds);
            ValidateEquippedSlot(equipped.WheelPartId, BikePartSlots.Wheels, ownedPartIds);
        }

        private void ValidateEquippedSlot(string partId, string expectedSlot, IReadOnlyCollection<string> ownedPartIds)
        {
            if (string.IsNullOrWhiteSpace(partId)) return;
            var part = Find(partId);
            if (part == null || part.Slot != expectedSlot || !ownedPartIds.Contains(partId))
                throw new InvalidOperationException("Equipped bike part is invalid: " + partId);
        }
    }

    [Serializable]
    public sealed class BikePartDefinition
    {
        public string Id, Name, BrandId, Slot;
        public int Price;
        public BikePerformanceStats Performance;
        public string[] Tags;

        public BikePartDefinition Snapshot()
        {
            var copy = (BikePartDefinition)MemberwiseClone();
            copy.Performance = Performance?.Snapshot();
            copy.Tags = Tags == null ? Array.Empty<string>() : (string[])Tags.Clone();
            return copy;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(BrandId) ||
                !BikePartSlots.IsValid(Slot) || Price < 0 || Performance == null)
                throw new InvalidOperationException("Invalid bike part: " + Id);
            Performance.Validate();
            if (Tags == null || Tags.Any(string.IsNullOrWhiteSpace) || Tags.Distinct().Count() != Tags.Length)
                throw new InvalidOperationException("Invalid bike part tags: " + Id);
        }
    }

    [Serializable]
    public sealed class BikePerformanceStats
    {
        public int Stiffness, Lightness, Comfort, Handling, Aerodynamics;

        public BikePerformanceStats Snapshot() => (BikePerformanceStats)MemberwiseClone();

        public void Validate()
        {
            if (new[] { Stiffness, Lightness, Comfort, Handling, Aerodynamics }.Any(value => value < 0))
                throw new InvalidOperationException("Bike performance cannot be negative");
        }

        public static BikePerformanceStats operator +(BikePerformanceStats left, BikePerformanceStats right) => new()
        {
            Stiffness = left.Stiffness + right.Stiffness,
            Lightness = left.Lightness + right.Lightness,
            Comfort = left.Comfort + right.Comfort,
            Handling = left.Handling + right.Handling,
            Aerodynamics = left.Aerodynamics + right.Aerodynamics
        };

        public static BikePerformanceStats operator -(BikePerformanceStats left, BikePerformanceStats right) => new()
        {
            Stiffness = left.Stiffness - right.Stiffness,
            Lightness = left.Lightness - right.Lightness,
            Comfort = left.Comfort - right.Comfort,
            Handling = left.Handling - right.Handling,
            Aerodynamics = left.Aerodynamics - right.Aerodynamics
        };
    }

    [Serializable]
    public sealed class PlayerBikeEquipment
    {
        public string FramePartId;
        public string ComponentsPartId;
        public string WheelPartId;

        public PlayerBikeEquipment Snapshot() => (PlayerBikeEquipment)MemberwiseClone();

        public string LoadoutId =>
            string.IsNullOrWhiteSpace(FramePartId) || string.IsNullOrWhiteSpace(ComponentsPartId) || string.IsNullOrWhiteSpace(WheelPartId)
                ? null
                : $"{FramePartId}|{ComponentsPartId}|{WheelPartId}";
    }
}
