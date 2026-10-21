using CyclingLove.Flow;
using CyclingLove.Services;
using CyclingLove.State;
using NUnit.Framework;
using UnityEngine;

public sealed class BikeEquipmentServiceTests
{
    [Test]
    public void PurchaseAndEquipAreAtomicAndPersistInJson()
    {
        var catalog = EquipmentContentLoader.LoadRuntime();
        var data = new CampaignGameService(CampaignContentLoader.LoadRuntime(), catalog).NewGame();
        var service = new BikeEquipmentService(catalog);
        data.Money = 200000;
        var beforeRevision = data.Revision;

        var purchase = service.Purchase(data, "frame_zenith_climber_01");
        Assert.IsTrue(purchase.Succeeded);
        Assert.AreEqual(180000, purchase.MoneySpent);
        Assert.AreEqual(20000, data.Money);
        Assert.Contains("frame_zenith_climber_01", data.OwnedPartIds);
        Assert.AreEqual(beforeRevision + 1, data.Revision);

        var equip = service.Equip(data, "frame_zenith_climber_01");
        Assert.IsTrue(equip.Succeeded);
        Assert.AreEqual("frame_zenith_climber_01", data.EquippedBike.FramePartId);

        var restored = JsonUtility.FromJson<CyclingLove.State.GamePlayerData>(JsonUtility.ToJson(data));
        catalog.ValidateLoadout(restored.OwnedPartIds, restored.EquippedBike);
        Assert.AreEqual("frame_zenith_climber_01", restored.EquippedBike.FramePartId);
        Assert.AreEqual(20000, restored.Money);
    }

    [Test]
    public void FailedPurchaseAndEquipDoNotChangeState()
    {
        var catalog = EquipmentContentLoader.LoadRuntime();
        var data = new CampaignGameService(CampaignContentLoader.LoadRuntime(), catalog).NewGame();
        var service = new BikeEquipmentService(catalog);
        var money = data.Money;
        var revision = data.Revision;

        Assert.AreEqual(EquipmentOperationStatus.InsufficientFunds,
            service.Purchase(data, "frame_zenith_climber_01").Status);
        Assert.AreEqual(EquipmentOperationStatus.NotOwned,
            service.Equip(data, "frame_zenith_climber_01").Status);
        Assert.AreEqual(money, data.Money);
        Assert.AreEqual(revision, data.Revision);
        Assert.IsFalse(data.OwnedPartIds.Contains("frame_zenith_climber_01"));
    }
}
