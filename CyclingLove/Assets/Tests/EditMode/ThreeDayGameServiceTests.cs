using System;
using System.Flow;
using System.GameSession;
using System.Service;
using NUnit.Framework;
using UnityEngine;

public sealed class ThreeDayGameServiceTests
{
    private readonly ThreeDayGameService flow = new();
    private void Submit(GamePlayerData d, GameFlowCompletionType type, string action = null)
    {
        Assert.IsTrue(flow.Submit(d, new GameSceneResult { CompletionType = type, SelectedActionId = action, ExpectedRevision = d.Revision }));
    }
    private void Read(GamePlayerData d) { Submit(d, GameFlowCompletionType.NovelClosed); Submit(d, GameFlowCompletionType.NovelClosed); }

    [Test]
    public void ThreeDifferentDaysReachEndWithExpectedRewards()
    {
        var d = flow.NewGame();
        foreach (var action in ThreeDayGameService.Actions)
        {
            Read(d);
            Submit(d, GameFlowCompletionType.ActionSelected, action);
            Read(d);
            Assert.AreEqual(GameFlowPhase.DailySummary, d.SavedPhase);
            Submit(d, GameFlowCompletionType.SummaryClosed);
        }
        Assert.AreEqual(GameFlowPhase.Finished, d.SavedPhase);
        Assert.AreEqual(4, d.Day);
        Assert.AreEqual(2400, d.Money);
        Assert.AreEqual(7, d.Physical);
        Assert.AreEqual(7, d.Toughness);
        Assert.AreEqual(8, d.Smart);
        Assert.AreEqual(7, d.Empathy);
        Assert.AreEqual(6, d.Appeal);
        Assert.AreEqual(100, d.Stamina);
        Assert.AreEqual(6, d.ClearedEventKeys.Count);
        CollectionAssert.Contains(d.MetHeroineIds, "hero_bikeshop");
    }

    [Test]
    public void SaveSnapshotAtEveryCheckpointContinuesExactlyLikeOriginal()
    {
        var original = flow.NewGame();
        for (int step = 0; step < 18; step++)
        {
            var restored = JsonUtility.FromJson<GamePlayerData>(JsonUtility.ToJson(original));
            flow.Validate(restored);
            Assert.AreEqual(flow.Request(original).BodyText, flow.Request(restored).BodyText);
            var type = original.SavedPhase == GameFlowPhase.ActionSelection ? GameFlowCompletionType.ActionSelected :
                original.SavedPhase == GameFlowPhase.DailySummary ? GameFlowCompletionType.SummaryClosed : GameFlowCompletionType.NovelClosed;
            var input = new GameSceneResult { CompletionType = type, SelectedActionId = ThreeDayGameService.Actions[(original.Day - 1) % 3], ExpectedRevision = original.Revision };
            flow.Submit(original, input); flow.Submit(restored, input);
            Assert.AreEqual(JsonUtility.ToJson(original), JsonUtility.ToJson(restored));
        }
        Assert.AreEqual(GameFlowPhase.Finished, original.SavedPhase);
    }

    [Test]
    public void RepeatedInputCannotDoubleAwardWork()
    {
        var d = flow.NewGame();
        Submit(d, GameFlowCompletionType.NovelClosed);
        var input = new GameSceneResult { CompletionType = GameFlowCompletionType.NovelClosed, ExpectedRevision = d.Revision };
        Assert.IsTrue(flow.Submit(d, input));
        Assert.IsFalse(flow.Submit(d, input));
        Assert.AreEqual(1500, d.Money);
        Assert.AreEqual(6, d.Smart);
    }

    [Test]
    public void InsufficientResourcesRejectActionButRestRemainsAvailable()
    {
        var d = flow.NewGame(); Read(d); d.Stamina = 0; d.Money = 0;
        foreach (var action in new[] { ThreeDayGameService.Training, ThreeDayGameService.Town, "unknown" })
            Assert.IsFalse(flow.Submit(d, new GameSceneResult { CompletionType = GameFlowCompletionType.ActionSelected, ExpectedRevision = d.Revision, SelectedActionId = action }));
        Submit(d, GameFlowCompletionType.ActionSelected, ThreeDayGameService.Rest);
        Read(d);
        Assert.AreEqual(30, d.Stamina);
    }

    [Test]
    public void ReadingAndReopeningSummaryNeverAppliesRewards()
    {
        var d = flow.NewGame(); Read(d); Submit(d, GameFlowCompletionType.ActionSelected, ThreeDayGameService.Training); Read(d);
        string snapshot = JsonUtility.ToJson(d);
        for (int i = 0; i < 5; i++) StringAssert.Contains("フィジカル　+2", flow.Request(d).BodyText);
        Assert.AreEqual(snapshot, JsonUtility.ToJson(d));
    }

    [Test]
    public void NewGameDoesNotReuseOldSession()
    {
        var d = flow.NewGame(); Read(d);
        var fresh = flow.NewGame();
        Assert.AreEqual(1000, fresh.Money);
        Assert.IsEmpty(fresh.ClearedEventKeys);
        Assert.AreEqual(0, fresh.Revision);
        Assert.AreEqual(GameFlowPhase.Novel, fresh.SavedPhase);
    }

    [Test]
    public void InvalidAndOldSaveDataAreRejected()
    {
        Assert.Throws<InvalidOperationException>(() => flow.Validate(new GamePlayerData()));
        var d = flow.NewGame(); d.Day = 99;
        Assert.Throws<InvalidOperationException>(() => flow.Validate(d));
        d = flow.NewGame(); d.DayStartStats = null;
        Assert.Throws<InvalidOperationException>(() => flow.Validate(d));
        d = flow.NewGame(); d.SavedPhase = GameFlowPhase.ActionSelection;
        Assert.Throws<InvalidOperationException>(() => flow.Validate(d));
    }
}
