using System;
using System.Flow;
using System.Service;
using NUnit.Framework;
using UnityEngine;

public sealed class CampaignParameterTests
{
    private static void Step(CampaignGameService flow, System.GameSession.GamePlayerData d, GameFlowCompletionType type, string action = null)
    {
        Assert.IsTrue(flow.Submit(d, new GameSceneResult { ExpectedRevision = d.Revision, CompletionType = type, SelectedActionId = action }));
    }
    [Test]
    public void ConfiguredInitialValuesAndStaminaLimitDriveProgressAndRestore()
    {
        var content = CampaignContentLoader.LoadPrototype();
        content.Rules.MaxStamina = 160;
        content.Rules.OvernightRecovery = 7;
        content.Rules.InitialPlayer.Stamina = 120;
        content.Rules.InitialPlayer.Money = 2000;
        content.Rules.InitialPlayer.Physical = 12;
        content.Rules.InitialPlayer.PlayerName = "テスト";
        var flow = new CampaignGameService(content);
        // Mutating the authoring data after construction must not change the running game.
        content.Rules.InitialPlayer.Money = 0;
        var d = flow.NewGame();
        Assert.AreEqual(2000, d.Money); Assert.AreEqual(12, d.Physical); Assert.AreEqual("テスト", d.PlayerName);
        Assert.AreEqual(160, flow.Request(d).MaxStamina);
        PlayerSaveValidator.Validate(d); // Storage validation has no campaign-specific cap.
        flow.Validate(JsonUtility.FromJson<System.GameSession.GamePlayerData>(JsonUtility.ToJson(d)));
        Step(flow, d, GameFlowCompletionType.NovelClosed); Step(flow, d, GameFlowCompletionType.NovelClosed);
        Step(flow, d, GameFlowCompletionType.ActionSelected, ThreeDayGameService.Rest);
        Step(flow, d, GameFlowCompletionType.NovelClosed); Step(flow, d, GameFlowCompletionType.NovelClosed);
        Assert.AreEqual(150, d.Stamina);
        StringAssert.Contains("7回復", flow.Request(d).BodyText);
        StringAssert.Contains("上限160", flow.Request(d).BodyText);
        Step(flow, d, GameFlowCompletionType.SummaryClosed);
        Assert.AreEqual(157, d.Stamina);
        d.Stamina = 161;
        Assert.Throws<InvalidOperationException>(() => flow.Validate(d));
    }
    [Test]
    public void RewardAndCostTextFollowDefinitionsRatherThanLiteralStoryNumbers()
    {
        var content = CampaignContentLoader.LoadPrototype();
        content.Days[0].MorningEffects.Money = 777;
        content.Actions[0].StaminaCost = 17;
        content.Actions[0].Effects.Physical = 9;
        var flow = new CampaignGameService(content); var d = flow.NewGame();
        Step(flow, d, GameFlowCompletionType.NovelClosed);
        StringAssert.Contains("資金 +777", flow.Request(d).BodyText);
        StringAssert.DoesNotContain("資金 +500", flow.Request(d).BodyText);
        Step(flow, d, GameFlowCompletionType.NovelClosed);
        StringAssert.Contains("体力 -17", flow.GetActionOptions(d)[0].Description);
        Step(flow, d, GameFlowCompletionType.ActionSelected, ThreeDayGameService.Training);
        Step(flow, d, GameFlowCompletionType.NovelClosed);
        StringAssert.Contains("フィジカル +9", flow.Request(d).BodyText);
        StringAssert.Contains("体力 -17", flow.Request(d).BodyText);
        Step(flow, d, GameFlowCompletionType.NovelClosed);
        Assert.AreEqual(1777, d.Money); Assert.AreEqual(14, d.Physical); Assert.AreEqual(83, d.Stamina);
    }
    [Test]
    public void InvalidRulesAndMissingContentFailBeforeStarting()
    {
        var c = CampaignContentLoader.LoadPrototype(); c.Rules.MaxStamina = 0;
        Assert.Throws<InvalidOperationException>(() => new CampaignGameService(c));
        c = CampaignContentLoader.LoadPrototype(); c.Rules.InitialPlayer.Stamina = 101;
        Assert.Throws<InvalidOperationException>(() => new CampaignGameService(c));
        c = CampaignContentLoader.LoadPrototype(); c.Rules = null;
        Assert.Throws<InvalidOperationException>(() => new CampaignGameService(c));
        Assert.Throws<InvalidOperationException>(() => CampaignContentLoader.Load("Content/does-not-exist"));
    }
}
