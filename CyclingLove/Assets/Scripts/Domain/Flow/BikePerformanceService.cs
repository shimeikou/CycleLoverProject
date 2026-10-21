using System.Collections.Generic;
using System.Linq;
using System;
using CyclingLove.Equipment;
using CyclingLove.State;

namespace CyclingLove.Flow
{
    public enum RiderStyle
    {
        AllRound,
        Sprinter,
        Climber,
        Endurance,
        Technician
    }

    public sealed class BikePerformanceResult
    {
        public string LoadoutId;
        public BikePerformanceStats BasePerformance;
        public BikePerformanceStats TotalPerformance;
        public int MasteryExperience;
        public int MasteryLevel;
        public RiderStyle RiderStyle;
    }

    public sealed class BikePerformanceComparison
    {
        public string CandidatePartId;
        public BikePerformanceResult Current;
        public BikePerformanceResult Candidate;
        public BikePerformanceStats Delta;
    }

    public sealed class BikePerformanceService
    {
        private readonly BikeEquipmentCatalog catalog;

        public BikePerformanceService(BikeEquipmentCatalog definition)
        {
            catalog = (definition ?? throw new ArgumentNullException(nameof(definition))).Snapshot();
        }

        public BikePerformanceResult Calculate(GamePlayerData data) =>
            Calculate(data, data?.EquippedBike);

        public BikePerformanceComparison Compare(GamePlayerData data, string candidatePartId)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var candidatePart = catalog.Find(candidatePartId) ??
                throw new InvalidOperationException("Bike part is missing: " + candidatePartId);
            var candidate = data.EquippedBike.Snapshot();
            switch (candidatePart.Slot)
            {
                case BikePartSlots.Frame: candidate.FramePartId = candidatePartId; break;
                case BikePartSlots.Components: candidate.ComponentsPartId = candidatePartId; break;
                case BikePartSlots.Wheels: candidate.WheelPartId = candidatePartId; break;
            }
            var currentResult = Calculate(data, data.EquippedBike);
            var candidateResult = Calculate(data, candidate);
            return new BikePerformanceComparison
            {
                CandidatePartId = candidatePartId,
                Current = currentResult,
                Candidate = candidateResult,
                Delta = candidateResult.TotalPerformance - currentResult.TotalPerformance
            };
        }

        private BikePerformanceResult Calculate(GamePlayerData data, PlayerBikeEquipment bike)
        {
            if (data == null || bike == null) throw new ArgumentNullException(nameof(data));
            var parts = new[] { catalog.Find(bike.FramePartId), catalog.Find(bike.ComponentsPartId), catalog.Find(bike.WheelPartId) };
            if (parts.Any(part => part == null))
                throw new InvalidOperationException("A complete bike loadout is required for performance calculation");

            var baseStats = new BikePerformanceStats();
            foreach (var part in parts) baseStats += part.Performance;
            var experience = data.FindBikeMastery(bike.LoadoutId)?.Experience ?? 0;
            var masteryLevel = Math.Min(5, experience / 20);
            var total = baseStats.Snapshot();
            total.Comfort += masteryLevel;
            total.Handling += masteryLevel;

            return new BikePerformanceResult
            {
                LoadoutId = bike.LoadoutId,
                BasePerformance = baseStats,
                TotalPerformance = total,
                MasteryExperience = experience,
                MasteryLevel = masteryLevel,
                RiderStyle = ResolveStyle(data, total, masteryLevel)
            };
        }

        private static RiderStyle ResolveStyle(GamePlayerData data, BikePerformanceStats bike, int masteryLevel)
        {
            var scores = new Dictionary<RiderStyle, int>
            {
                [RiderStyle.Sprinter] = data.Physical * 2 + bike.Stiffness + bike.Aerodynamics,
                [RiderStyle.Climber] = data.Toughness * 2 + bike.Lightness + bike.Handling,
                [RiderStyle.Endurance] = data.Toughness + data.Empathy + bike.Comfort * 2 + bike.Handling,
                [RiderStyle.Technician] = data.Smart * 2 + bike.Handling * 2 + masteryLevel
            };
            var ordered = scores.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).ToArray();
            return ordered[0].Value - ordered[1].Value <= 1 ? RiderStyle.AllRound : ordered[0].Key;
        }
    }
}
