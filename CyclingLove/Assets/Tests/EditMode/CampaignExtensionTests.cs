using System;
using System.Linq;
using System.Flow;
using System.GameSession;
using System.Service;
using CyclingLove.Content;
using NUnit.Framework;
using UnityEngine;

public sealed class CampaignExtensionTests
{
    [Test]
    public void FourthDayFourthActionAndLongerTextCrossMonthUsingOnlyData()
    {
        var definition = CampaignContentLoader.LoadPrototype();
        definition.StartDay = 29;
        var extra = new ActionDefinition { Id = "extra", Name = "追加", Pages = new[] { "一", "二", "三" }, Effects = new CampaignEffects { Appeal = 4 } };
        definition.Actions = definition.Actions.Concat(new[] { extra }).ToArray();
        var fourth = JsonUtility.FromJson<CampaignDay>(JsonUtility.ToJson(definition.Days[0]));
        fourth.Id = "fourth";
        definition.Days = definition.Days.Concat(new[] { fourth }).ToArray();
        foreach (var day in definition.Days) day.ActionIds = day.ActionIds.Concat(new[] { "extra" }).ToArray();
        var flow = new CampaignGameService(definition);
        var d = flow.NewGame();
        Assert.AreEqual(4, flow.GetActionOptions(d).Count);
        int pages = 0;
        for (int i = 0; i < 60 && d.SavedPhase != GameFlowPhase.Finished; i++)
        {
            if (d.SavedPhase == GameFlowPhase.Novel) pages++;
            var type = d.SavedPhase == GameFlowPhase.ActionSelection ? GameFlowCompletionType.ActionSelected :
                d.SavedPhase == GameFlowPhase.DailySummary ? GameFlowCompletionType.SummaryClosed : GameFlowCompletionType.NovelClosed;
            Assert.IsTrue(flow.Submit(d, new GameSceneResult { ExpectedRevision = d.Revision, CompletionType = type, SelectedActionId = "extra" }));
        }
        Assert.AreEqual(GameFlowPhase.Finished, d.SavedPhase);
        Assert.AreEqual(5, d.Month); Assert.AreEqual(3, d.Day);
        Assert.AreEqual(24, pages); Assert.AreEqual(21, d.Appeal);
        flow.Validate(d);
    }
    [Test]
    public void RuntimeDoesNotMutateOrObserveLaterChangesToAuthoringData()
    {
        var definition = CampaignContentLoader.LoadPrototype();
        var before = JsonUtility.ToJson(definition);
        var flow = new CampaignGameService(definition);
        var state = flow.NewGame();
        flow.Submit(state, new GameSceneResult { CompletionType = GameFlowCompletionType.NovelClosed, ExpectedRevision = 0 });
        Assert.AreEqual(before, JsonUtility.ToJson(definition));
        var title = flow.Request(state).DisplayName;
        definition.Days[0].Title = "changed";
        Assert.AreEqual(title, flow.Request(state).DisplayName);
    }
    [Test]
    public void BrokenContentIsRejectedBeforePlay()
    {
        var d = CampaignContentLoader.LoadPrototype(); d.Actions[1].Id = d.Actions[0].Id;
        Assert.Throws<InvalidOperationException>(() => new CampaignGameService(d));
        d = CampaignContentLoader.LoadPrototype(); d.Days[0].ActionIds[0] = "missing";
        Assert.Throws<InvalidOperationException>(() => new CampaignGameService(d));
        d = CampaignContentLoader.LoadPrototype(); d.Days[0].MorningPages = new[] { "" };
        Assert.Throws<InvalidOperationException>(() => new CampaignGameService(d));
    }
    [Test]
    public void LegacyPrototypeSaveIsAcceptedButDifferentCampaignIsRejected()
    {
        var flow = new CampaignGameService(CampaignContentLoader.LoadPrototype());
        var state = flow.NewGame(); state.CampaignId = null; state.ContentVersion = 0;
        flow.Validate(state);
        state.CampaignId = "another"; state.ContentVersion = 1;
        Assert.Throws<InvalidOperationException>(() => flow.Validate(state));
    }
}
