using System.Service;
using CyclingLove.Content;
using NUnit.Framework;
using UnityEngine;

public sealed class EquipmentRaceLoopTests
{
    [Test]
    public void IncomePurchaseEquipTrainingRaceAndSaveFormOneLoop()
    {
        var equipment = EquipmentContentLoader.LoadRuntime();
        var data = new CampaignGameService(CampaignContentLoader.LoadRuntime(), equipment).NewGame();
        var shop = new BikeEquipmentService(equipment);
        var race = new RaceProgressService(RaceContentLoader.LoadRuntime(), equipment);

        new CampaignEffects { Money = 200000 }.Apply(data, 100);
        Assert.IsTrue(shop.Purchase(data, "frame_zenith_climber_01").Succeeded);
        Assert.IsTrue(shop.Equip(data, "frame_zenith_climber_01").Succeeded);
        new CampaignEffects { Physical = 3, Toughness = 3, BikeMastery = 40 }.Apply(data, 100);

        var performance = new BikePerformanceService(equipment).Calculate(data);
        Assert.AreEqual("frame_zenith_climber_01", data.EquippedBike.FramePartId);
        Assert.AreEqual(2, performance.MasteryLevel);
        Assert.AreEqual(performance.BasePerformance.Comfort + 2, performance.TotalPerformance.Comfort);
        Assert.AreEqual(performance.BasePerformance.Handling + 2, performance.TotalPerformance.Handling);

        var result = race.ResolveAndApply(data, "race_hillclimb_trial");
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(7000, result.RewardMoney);
        Assert.Contains("race_hillclimb_trial_cleared", data.ActiveRouteFlags);

        var restored = JsonUtility.FromJson<System.GameSession.GamePlayerData>(JsonUtility.ToJson(data));
        PlayerSaveMigration.UpgradeToCurrent(restored);
        PlayerSaveValidator.Validate(restored);
        equipment.ValidateLoadout(restored.OwnedPartIds, restored.EquippedBike);
        Assert.AreEqual("frame_zenith_climber_01", restored.EquippedBike.FramePartId);
        Assert.AreEqual(40, restored.FindBikeMastery(restored.EquippedBike.LoadoutId).Experience);
        Assert.AreEqual(result.Score, restored.RaceAttempts[0].Score);
    }
}
