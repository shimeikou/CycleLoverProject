using System.Flow;
using System.GameSession;
using System.Linq;
using System.Service;
using NUnit.Framework;
using UnityEngine;

public sealed class ScenarioCheckpointTests
{
    [Test]
    public void SamePageIsRestoredAfterAnEarlierPageIsInserted()
    {
        var originalDefinition = CampaignContentLoader.LoadPrototype();
        var originalFlow = new CampaignGameService(originalDefinition);
        var data = originalFlow.NewGame();
        Assert.IsTrue(originalFlow.Submit(data, new GameSceneResult
        {
            CompletionType = GameFlowCompletionType.NovelClosed,
            ExpectedRevision = data.Revision
        }));
        var expected = originalFlow.Request(data);
        var saved = JsonUtility.FromJson<GamePlayerData>(JsonUtility.ToJson(data));

        var changedDefinition = CampaignContentLoader.LoadPrototype();
        changedDefinition.Days[0].MorningPages = new[] { "後から挿入された導入文。" }
            .Concat(changedDefinition.Days[0].MorningPages).ToArray();
        var changedFlow = new CampaignGameService(changedDefinition);

        changedFlow.Validate(saved);
        var restored = changedFlow.Request(saved);

        Assert.AreEqual(2, saved.NarrativePage);
        Assert.AreEqual(expected.BodyText, restored.BodyText);
        Assert.AreEqual(expected.ScenarioId, restored.ScenarioId);
        Assert.AreEqual(expected.NodeId, restored.NodeId);
        Assert.AreEqual(expected.PageId, restored.PageId);
        Assert.IsEmpty(saved.ClearedEventKeys, "Restoring text must not apply event rewards");
    }

    [Test]
    public void MissingPageKeyReturnsToNodeStartWithoutApplyingRewards()
    {
        var originalFlow = new CampaignGameService(CampaignContentLoader.LoadPrototype());
        var data = originalFlow.NewGame();
        Assert.IsTrue(originalFlow.Submit(data, new GameSceneResult
        {
            CompletionType = GameFlowCompletionType.NovelClosed,
            ExpectedRevision = data.Revision
        }));
        var saved = JsonUtility.FromJson<GamePlayerData>(JsonUtility.ToJson(data));
        var moneyBeforeRestore = saved.Money;

        var changedDefinition = CampaignContentLoader.LoadPrototype();
        changedDefinition.Days[0].MorningPages[1] = "保存時の文章を置き換えた。";
        var changedFlow = new CampaignGameService(changedDefinition);

        changedFlow.Validate(saved);

        Assert.AreEqual(0, saved.NarrativePage);
        Assert.AreEqual(changedDefinition.Days[0].MorningPages[0], changedFlow.Request(saved).BodyText);
        Assert.AreEqual(moneyBeforeRestore, saved.Money);
        Assert.IsEmpty(saved.ClearedEventKeys);
    }

    [Test]
    public void VersionFiveSaveKeepsItsNumericPageOnceAndGainsStableCheckpoint()
    {
        var flow = new CampaignGameService(CampaignContentLoader.LoadPrototype());
        var data = flow.NewGame();
        data.FlowVersion = 5;
        data.NarrativePage = 1;
        data.ScenarioCheckpoint = null;

        flow.Validate(data);

        Assert.AreEqual(PlayerSaveValidator.SchemaVersion, data.FlowVersion);
        Assert.AreEqual(1, data.NarrativePage);
        Assert.IsFalse(data.ScenarioCheckpoint.IsEmpty);
        Assert.AreEqual(flow.Request(data).PageId, data.ScenarioCheckpoint.PageId);
    }
}
