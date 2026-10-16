using System.GameSession;
using CyclingLove.Equipment;

namespace System.Service
{
    public enum EquipmentOperationStatus
    {
        Success,
        MissingPart,
        AlreadyOwned,
        NotOwned,
        InsufficientFunds,
        AlreadyEquipped
    }

    public sealed class EquipmentOperationResult
    {
        public EquipmentOperationStatus Status;
        public string PartId;
        public int MoneySpent;
        public bool Succeeded => Status == EquipmentOperationStatus.Success;
    }

    public sealed class BikeEquipmentService
    {
        private readonly BikeEquipmentCatalog catalog;

        public BikeEquipmentService(BikeEquipmentCatalog definition)
        {
            catalog = (definition ?? throw new ArgumentNullException(nameof(definition))).Snapshot();
        }

        public EquipmentOperationResult Purchase(GamePlayerData data, string partId)
        {
            RequireState(data);
            var part = catalog.Find(partId);
            if (part == null) return Result(EquipmentOperationStatus.MissingPart, partId);
            if (data.OwnedPartIds.Contains(partId)) return Result(EquipmentOperationStatus.AlreadyOwned, partId);
            if (data.Money < part.Price) return Result(EquipmentOperationStatus.InsufficientFunds, partId);

            data.Money -= part.Price;
            data.OwnedPartIds.Add(partId);
            data.Revision++;
            catalog.ValidateLoadout(data.OwnedPartIds, data.EquippedBike);
            return Result(EquipmentOperationStatus.Success, partId, part.Price);
        }

        public EquipmentOperationResult Equip(GamePlayerData data, string partId)
        {
            RequireState(data);
            var part = catalog.Find(partId);
            if (part == null) return Result(EquipmentOperationStatus.MissingPart, partId);
            if (!data.OwnedPartIds.Contains(partId)) return Result(EquipmentOperationStatus.NotOwned, partId);

            var current = part.Slot switch
            {
                BikePartSlots.Frame => data.EquippedBike.FramePartId,
                BikePartSlots.Components => data.EquippedBike.ComponentsPartId,
                BikePartSlots.Wheels => data.EquippedBike.WheelPartId,
                _ => null
            };
            if (current == partId) return Result(EquipmentOperationStatus.AlreadyEquipped, partId);

            switch (part.Slot)
            {
                case BikePartSlots.Frame: data.EquippedBike.FramePartId = partId; break;
                case BikePartSlots.Components: data.EquippedBike.ComponentsPartId = partId; break;
                case BikePartSlots.Wheels: data.EquippedBike.WheelPartId = partId; break;
                default: throw new InvalidOperationException("Unknown bike part slot: " + part.Slot);
            }
            data.Revision++;
            catalog.ValidateLoadout(data.OwnedPartIds, data.EquippedBike);
            return Result(EquipmentOperationStatus.Success, partId);
        }

        private void RequireState(GamePlayerData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            catalog.ValidateLoadout(data.OwnedPartIds, data.EquippedBike);
            if (data.Money < 0) throw new InvalidOperationException("Player money cannot be negative");
        }

        private static EquipmentOperationResult Result(EquipmentOperationStatus status, string partId, int moneySpent = 0) =>
            new() { Status = status, PartId = partId, MoneySpent = moneySpent };
    }
}
