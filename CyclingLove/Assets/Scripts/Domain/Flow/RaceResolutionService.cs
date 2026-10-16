using System.GameSession;
using CyclingLove.Equipment;
using CyclingLove.Racing;

namespace System.Service
{
    public sealed class RaceResolution
    {
        public string RaceId, RaceName, CourseType, SuccessRouteFlag, FailureRouteFlag;
        public int Score, TargetScore, SuccessMoney;
        public bool Succeeded;
    }

    public sealed class RaceResolutionService
    {
        private readonly RaceCatalog races;
        private readonly BikePerformanceService performance;

        public RaceResolutionService(RaceCatalog raceCatalog, BikeEquipmentCatalog equipmentCatalog)
        {
            races = (raceCatalog ?? throw new ArgumentNullException(nameof(raceCatalog))).Snapshot();
            performance = new BikePerformanceService(equipmentCatalog);
        }

        public RaceResolution Evaluate(GamePlayerData data, string raceId)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var race = races.Find(raceId) ?? throw new InvalidOperationException("Race is missing: " + raceId);
            var bike = performance.Calculate(data);
            var w = race.Weights;
            var b = bike.TotalPerformance;
            var score = data.Physical * w.Physical + data.Toughness * w.Toughness + data.Smart * w.Smart +
                data.Empathy * w.Empathy + data.Appeal * w.Appeal + b.Stiffness * w.Stiffness +
                b.Lightness * w.Lightness + b.Comfort * w.Comfort + b.Handling * w.Handling +
                b.Aerodynamics * w.Aerodynamics + bike.MasteryLevel * w.Mastery;
            return new RaceResolution
            {
                RaceId = race.Id, RaceName = race.Name, CourseType = race.CourseType,
                Score = score, TargetScore = race.TargetScore, Succeeded = score >= race.TargetScore,
                SuccessMoney = race.SuccessMoney, SuccessRouteFlag = race.SuccessRouteFlag,
                FailureRouteFlag = race.FailureRouteFlag
            };
        }
    }
}
