using System.Collections;
using System.IO;
using System;
using CyclingLove.Flow;
using CyclingLove.Services;
using CyclingLove.State;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;

public sealed class AnnualSaveResumeTests
{
    private string slot;

    [SetUp]
    public void SetUp() => slot = "annual_restart_" + Guid.NewGuid().ToString("N");

    [TearDown]
    public void TearDown()
    {
        var path = SaveDataService.SavePath(slot);
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
    }

    [UnityTest]
    public IEnumerator AnnualChoiceResumesFromFileAndAppliesRewardOnce()
    {
        return VerifyAnnualRestart().ToCoroutine();
    }

    private async UniTask VerifyAnnualRestart()
    {
        var originalFlow = CreateFlow();
        var original = originalFlow.NewGame();

        Submit(originalFlow, original, GameFlowCompletionType.NovelClosed);
        Submit(originalFlow, original, GameFlowCompletionType.ActionSelected, action: "evt_annual_route_select");
        Submit(originalFlow, original, GameFlowCompletionType.NovelClosed);
        Submit(originalFlow, original, GameFlowCompletionType.NovelClosed);
        Assert.AreEqual(GameFlowPhase.NovelChoice, original.SavedPhase);
        Submit(originalFlow, original, GameFlowCompletionType.NovelChoiceSelected, choice: "annual_route_bikeshop");
        Assert.AreEqual(GameFlowPhase.Novel, original.SavedPhase);

        var expected = originalFlow.Request(original);
        await SaveDataService.Save(original, slot);

        // Recreate both the data and flow service, matching an application restart.
        var restored = await SaveDataService.Load(slot);
        var restoredFlow = CreateFlow();
        restoredFlow.Validate(restored);
        var actual = restoredFlow.Request(restored);

        Assert.AreEqual(expected.ScenarioId, actual.ScenarioId);
        Assert.AreEqual(expected.NodeId, actual.NodeId);
        Assert.AreEqual(expected.PageId, actual.PageId);
        Assert.AreEqual(expected.BodyText, actual.BodyText);
        Assert.AreEqual(expected.Revision, actual.Revision);
        Assert.AreEqual(JsonUtility.ToJson(original), JsonUtility.ToJson(restored));

        Submit(originalFlow, original, GameFlowCompletionType.NovelClosed);
        Submit(restoredFlow, restored, GameFlowCompletionType.NovelClosed);

        Assert.AreEqual(GameFlowPhase.DailySummary, restored.SavedPhase);
        Assert.AreEqual(JsonUtility.ToJson(original), JsonUtility.ToJson(restored));
        Assert.AreEqual(1, restored.ActiveRouteFlags.FindAll(id => id == "route_bikeshop_annual").Count);
    }

    private static CampaignGameService CreateFlow() => new(
        CampaignContentLoader.LoadAnnual(),
        EquipmentContentLoader.LoadRuntime(),
        AnnualCalendarContentLoader.LoadRuntime());

    private static void Submit(
        CampaignGameService flow,
        GamePlayerData data,
        GameFlowCompletionType type,
        string action = null,
        string choice = null)
    {
        Assert.IsTrue(flow.Submit(data, new GameSceneResult
        {
            CompletionType = type,
            SelectedActionId = action,
            SelectedChoiceId = choice,
            ExpectedRevision = data.Revision
        }));
    }
}
