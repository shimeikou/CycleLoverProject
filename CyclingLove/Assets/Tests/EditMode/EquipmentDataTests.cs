using System;
using System.Collections.Generic;
using System.Service;
using CyclingLove.Equipment;
using NUnit.Framework;
using UnityEngine;

public sealed class EquipmentDataTests
{
    [Test]
    public void RuntimeCatalogAndStarterLoadoutAreValidAndRoundTrip()
    {
        var catalog = EquipmentContentLoader.LoadRuntime();
        var flow = new CampaignGameService(CampaignContentLoader.LoadRuntime(), catalog);
        var data = flow.NewGame();

        Assert.AreEqual(8, catalog.Parts.Length);
        Assert.AreEqual(3, data.OwnedPartIds.Count);
        Assert.AreEqual("frame_musashi_touring_01", data.EquippedBike.FramePartId);
        Assert.AreEqual("components_takumi_reliable_01", data.EquippedBike.ComponentsPartId);
        Assert.AreEqual("wheels_takumi_endurance_01", data.EquippedBike.WheelPartId);

        var restored = JsonUtility.FromJson<System.GameSession.GamePlayerData>(JsonUtility.ToJson(data));
        PlayerSaveMigration.UpgradeToCurrent(restored);
        PlayerSaveValidator.Validate(restored);
        catalog.ValidateLoadout(restored.OwnedPartIds, restored.EquippedBike);
        Assert.AreEqual(data.EquippedBike.FramePartId, restored.EquippedBike.FramePartId);
    }

    [Test]
    public void LoadoutRejectsUnownedAndWrongSlotParts()
    {
        var catalog = EquipmentContentLoader.LoadRuntime();
        var owned = new List<string> { "frame_musashi_touring_01", "wheels_takumi_endurance_01" };

        Assert.Throws<InvalidOperationException>(() => catalog.ValidateLoadout(owned, new PlayerBikeEquipment
        {
            FramePartId = "frame_musashi_touring_01",
            ComponentsPartId = "components_takumi_reliable_01",
            WheelPartId = "wheels_takumi_endurance_01"
        }));

        owned.Add("components_takumi_reliable_01");
        Assert.Throws<InvalidOperationException>(() => catalog.ValidateLoadout(owned, new PlayerBikeEquipment
        {
            FramePartId = "wheels_takumi_endurance_01"
        }));
    }
}
