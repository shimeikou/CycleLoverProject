using System.Collections.Generic;
using System.Linq;
using System;
using CyclingLove.Equipment;
using CyclingLove.State;

namespace CyclingLove.Content
{
    [Serializable]
    public sealed class CampaignRules
    {
        public int MaxStamina;
        public int OvernightRecovery;
        public InitialPlayerDefinition InitialPlayer;
        public void Validate()
        {
            if (MaxStamina <= 0 || OvernightRecovery < 0 || InitialPlayer == null)
                throw new InvalidOperationException("Invalid campaign rules");
            InitialPlayer.Validate(MaxStamina);
        }
        public CampaignRules Snapshot() => new CampaignRules
        {
            MaxStamina = MaxStamina, OvernightRecovery = OvernightRecovery,
            InitialPlayer = InitialPlayer.Snapshot()
        };
    }

    [Serializable]
    public sealed class InitialPlayerDefinition
    {
        public string PlayerName, LastVisitedPlace;
        public int Age, Money, Stamina, Physical, Toughness, Smart, Empathy, Appeal;
        public string[] OwnedPartIds;
        public PlayerBikeEquipment EquippedBike;
        public void Validate(int maxStamina)
        {
            if (string.IsNullOrWhiteSpace(PlayerName) || string.IsNullOrWhiteSpace(LastVisitedPlace) || Age <= 0 ||
                Stamina > maxStamina || new[] { Money, Stamina, Physical, Toughness, Smart, Empathy, Appeal }.Any(v => v < 0))
                throw new InvalidOperationException("Invalid initial player parameters");
            if (OwnedPartIds == null || OwnedPartIds.Any(string.IsNullOrWhiteSpace) ||
                OwnedPartIds.Distinct().Count() != OwnedPartIds.Length || EquippedBike == null)
                throw new InvalidOperationException("Invalid initial bike equipment");
            foreach (var equippedId in new[] { EquippedBike.FramePartId, EquippedBike.ComponentsPartId, EquippedBike.WheelPartId })
                if (!string.IsNullOrWhiteSpace(equippedId) && !OwnedPartIds.Contains(equippedId))
                    throw new InvalidOperationException("Initial equipped part must be owned");
        }
        public InitialPlayerDefinition Snapshot()
        {
            var copy = (InitialPlayerDefinition)MemberwiseClone();
            copy.OwnedPartIds = (string[])OwnedPartIds.Clone();
            copy.EquippedBike = EquippedBike.Snapshot();
            return copy;
        }
        public void Apply(GamePlayerData d)
        {
            d.PlayerName = PlayerName; d.Age = Age; d.LastVisitedPlace = LastVisitedPlace;
            d.Money = Money; d.Stamina = Stamina; d.Physical = Physical; d.Toughness = Toughness;
            d.Smart = Smart; d.Empathy = Empathy; d.Appeal = Appeal;
            d.OwnedPartIds = new List<string>(OwnedPartIds);
            d.EquippedBike = EquippedBike.Snapshot();
        }
    }
}
