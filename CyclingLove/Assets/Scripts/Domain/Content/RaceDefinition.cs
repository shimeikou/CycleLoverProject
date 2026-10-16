using System;
using System.Linq;

namespace CyclingLove.Racing
{
    [Serializable]
    public sealed class RaceCatalog
    {
        public int Version;
        public RaceDefinition[] Races;

        public RaceCatalog Snapshot()
        {
            Validate();
            return new RaceCatalog { Version = Version, Races = Races.Select(race => race.Snapshot()).ToArray() };
        }

        public void Validate()
        {
            if (Version < 1 || Races == null || Races.Length == 0 || Races.Any(race => race == null))
                throw new InvalidOperationException("Invalid race catalog");
            if (Races.Any(race => string.IsNullOrWhiteSpace(race.Id)) ||
                Races.Select(race => race.Id).Distinct().Count() != Races.Length)
                throw new InvalidOperationException("Invalid or duplicate race ID");
            foreach (var race in Races) race.Validate();
        }

        public RaceDefinition Find(string raceId) => Races?.FirstOrDefault(race => race.Id == raceId);
    }

    [Serializable]
    public sealed class RaceDefinition
    {
        public string Id, Name, CourseType, SuccessRouteFlag, FailureRouteFlag;
        public int DistanceKm, TargetScore, SuccessMoney;
        public RaceScoreWeights Weights;

        public RaceDefinition Snapshot() => new()
        {
            Id = Id, Name = Name, CourseType = CourseType,
            SuccessRouteFlag = SuccessRouteFlag, FailureRouteFlag = FailureRouteFlag,
            DistanceKm = DistanceKm, TargetScore = TargetScore, SuccessMoney = SuccessMoney,
            Weights = Weights?.Snapshot()
        };

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(SuccessRouteFlag) ||
                string.IsNullOrWhiteSpace(FailureRouteFlag) || SuccessRouteFlag == FailureRouteFlag ||
                (CourseType != "flat" && CourseType != "climb") ||
                DistanceKm <= 0 || TargetScore <= 0 || SuccessMoney < 0 || Weights == null)
                throw new InvalidOperationException("Invalid race: " + Id);
            Weights.Validate();
        }
    }

    [Serializable]
    public sealed class RaceScoreWeights
    {
        public int Physical, Toughness, Smart, Empathy, Appeal;
        public int Stiffness, Lightness, Comfort, Handling, Aerodynamics, Mastery;

        public RaceScoreWeights Snapshot() => (RaceScoreWeights)MemberwiseClone();

        public void Validate()
        {
            if (new[] { Physical, Toughness, Smart, Empathy, Appeal, Stiffness, Lightness,
                    Comfort, Handling, Aerodynamics, Mastery }.Any(value => value < 0))
                throw new InvalidOperationException("Race score weights cannot be negative");
            if (Physical + Toughness + Smart + Empathy + Appeal + Stiffness + Lightness +
                Comfort + Handling + Aerodynamics + Mastery == 0)
                throw new InvalidOperationException("Race requires at least one score weight");
        }
    }
}
