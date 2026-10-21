using CyclingLove.Content;
using CyclingLove.Flow;
using CyclingLove.Services;
using NUnit.Framework;

public sealed class BikePerformanceServiceTests
{
    [Test]
    public void PerformanceSumsPartsAndAppliesMasteryToComfortAndHandling()
    {
        var catalog = EquipmentContentLoader.LoadRuntime();
        var data = new CampaignGameService(CampaignContentLoader.LoadRuntime(), catalog).NewGame();
        data.AddCurrentBikeMastery(40);

        var result = new BikePerformanceService(catalog).Calculate(data);

        Assert.AreEqual(5, result.BasePerformance.Stiffness);
        Assert.AreEqual(3, result.BasePerformance.Lightness);
        Assert.AreEqual(8, result.BasePerformance.Comfort);
        Assert.AreEqual(7, result.BasePerformance.Handling);
        Assert.AreEqual(2, result.BasePerformance.Aerodynamics);
        Assert.AreEqual(2, result.MasteryLevel);
        Assert.AreEqual(10, result.TotalPerformance.Comfort);
        Assert.AreEqual(9, result.TotalPerformance.Handling);
        Assert.AreEqual(RiderStyle.Endurance, result.RiderStyle);
    }

    [Test]
    public void CandidateComparisonUsesCandidateLoadoutMasteryAndReportsFiveDeltas()
    {
        var catalog = EquipmentContentLoader.LoadRuntime();
        var data = new CampaignGameService(CampaignContentLoader.LoadRuntime(), catalog).NewGame();
        var comparison = new BikePerformanceService(catalog).Compare(data, "frame_zenith_climber_01");

        Assert.AreEqual(2, comparison.Delta.Stiffness);
        Assert.AreEqual(4, comparison.Delta.Lightness);
        Assert.AreEqual(-2, comparison.Delta.Comfort);
        Assert.AreEqual(1, comparison.Delta.Handling);
        Assert.AreEqual(1, comparison.Delta.Aerodynamics);
    }

    [Test]
    public void CampaignEffectAwardsMasteryToCurrentLoadoutOnly()
    {
        var catalog = EquipmentContentLoader.LoadRuntime();
        var data = new CampaignGameService(CampaignContentLoader.LoadRuntime(), catalog).NewGame();
        var firstLoadout = data.EquippedBike.LoadoutId;

        new CampaignEffects { BikeMastery = 7 }.Apply(data, 100);

        Assert.AreEqual(7, data.FindBikeMastery(firstLoadout).Experience);
        Assert.AreEqual("愛車熟練度 +7", new CampaignEffects { BikeMastery = 7 }.Describe());
    }
}
