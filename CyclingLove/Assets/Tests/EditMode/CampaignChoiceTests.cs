using System.Linq;
using System;
using CyclingLove.Content;
using CyclingLove.Flow;
using CyclingLove.Services;
using CyclingLove.State;
using NUnit.Framework;

public sealed class CampaignChoiceTests
{
    [Test]
    public void ConditionalChoiceFallsBackAndAppliesSelectedRewardOnce()
    {
        var definition = CampaignContentLoader.LoadPrototype();
        var action = definition.Actions[0];
        action.ChoicePrompt = "どちらを大切にする？";
        action.Choices = new[]
        {
            new CampaignChoiceDefinition
            {
                Id = "choice_close", Label = "本音を聞く", Description = "好感度1以上",
                Pages = new[] { "彼女は少しだけ本音を話した。" },
                Conditions = new[] { new CampaignCondition { Type = "affection", Id = "hero_bikeshop", Value = 1 } },
                Effects = new CampaignEffects { HeroineId = "hero_bikeshop", Affection = 1 }
            },
            new CampaignChoiceDefinition
            {
                Id = "choice_fallback", Label = "走る話をする", Description = "いつでも選べる",
                Pages = new[] { "二人は次に走る道について話した。" },
                Effects = new CampaignEffects { Empathy = 2 }
            }
        };

        var flow = new CampaignGameService(definition);
        var data = flow.NewGame();
        while (data.SavedPhase == GameFlowPhase.Novel) Submit(flow, data, GameFlowCompletionType.NovelClosed);
        Submit(flow, data, GameFlowCompletionType.ActionSelected, action: action.Id);
        while (data.SavedPhase == GameFlowPhase.Novel) Submit(flow, data, GameFlowCompletionType.NovelClosed);

        Assert.AreEqual(GameFlowPhase.NovelChoice, data.SavedPhase);
        flow.Validate(data);
        var options = flow.GetActionOptions(data);
        Assert.IsFalse(options.Single(x => x.ActionId == "choice_close").CanExecute);
        Assert.IsTrue(options.Single(x => x.ActionId == "choice_fallback").CanExecute);
        Assert.IsFalse(flow.Submit(data, new GameSceneResult
        {
            ExpectedRevision = data.Revision,
            CompletionType = GameFlowCompletionType.NovelChoiceSelected,
            SelectedChoiceId = "choice_close"
        }));

        var empathy = data.Empathy;
        var clearedEvents = data.ClearedEventKeys.Count;
        Submit(flow, data, GameFlowCompletionType.NovelChoiceSelected, choice: "choice_fallback");
        flow.Validate(data);
        Submit(flow, data, GameFlowCompletionType.NovelClosed);
        Assert.AreEqual(GameFlowPhase.DailySummary, data.SavedPhase);
        Assert.AreEqual(empathy + 2, data.Empathy);
        Assert.AreEqual("choice_fallback", data.SelectedChoiceId);
        Assert.AreEqual(clearedEvents + 1, data.ClearedEventKeys.Count);
        Assert.IsFalse(flow.Submit(data, new GameSceneResult
        {
            ExpectedRevision = data.Revision,
            CompletionType = GameFlowCompletionType.NovelClosed
        }));
        Assert.AreEqual(empathy + 2, data.Empathy);
    }

    [Test]
    public void ChoiceWithoutUnconditionalFallbackIsRejected()
    {
        var definition = CampaignContentLoader.LoadPrototype();
        definition.Actions[0].Choices = new[]
        {
            new CampaignChoiceDefinition
            {
                Id = "locked", Label = "選べない", Pages = new[] { "到達不能" },
                Conditions = new[] { new CampaignCondition { Type = "physical", Value = 999 } },
                Effects = new CampaignEffects()
            }
        };
        Assert.Throws<InvalidOperationException>(() => new CampaignGameService(definition));
    }

    [Test]
    public void ConditionsReadStatsHeroineAndFlags()
    {
        var data = new CampaignGameService(CampaignContentLoader.LoadPrototype()).NewGame();
        data.Physical = 7;
        data.ActiveRouteFlags.Add("route_started");
        data.ClearedEventKeys.Add("event_seen");
        var heroine = data.EnsureHeroineProgress("hero_bikeshop");
        heroine.Affection = 3;
        heroine.RouteStep = 2;
        heroine.IsMet = true;

        Assert.IsTrue(new CampaignCondition { Type = "physical", Value = 7 }.IsMet(data));
        Assert.IsTrue(new CampaignCondition { Type = "affection", Id = "hero_bikeshop", Value = 3 }.IsMet(data));
        Assert.IsTrue(new CampaignCondition { Type = "route_step", Id = "hero_bikeshop", Value = 2 }.IsMet(data));
        Assert.IsTrue(new CampaignCondition { Type = "heroine_met", Id = "hero_bikeshop" }.IsMet(data));
        Assert.IsTrue(new CampaignCondition { Type = "route_flag", Id = "route_started" }.IsMet(data));
        Assert.IsTrue(new CampaignCondition { Type = "event_cleared", Id = "event_seen" }.IsMet(data));
    }

    [Test]
    public void NegatedConditionsExpressBelowThresholdAndMissingFlag()
    {
        var data = new CampaignGameService(CampaignContentLoader.LoadPrototype()).NewGame();
        data.Physical = 6;
        data.ActiveRouteFlags.Add("route_started");

        Assert.IsTrue(new CampaignCondition { Type = "physical", Value = 7, Not = true }.IsMet(data));
        Assert.IsFalse(new CampaignCondition { Type = "physical", Value = 6, Not = true }.IsMet(data));
        Assert.IsTrue(new CampaignCondition { Type = "route_flag", Id = "route_missing", Not = true }.IsMet(data));
        Assert.IsFalse(new CampaignCondition { Type = "route_flag", Id = "route_started", Not = true }.IsMet(data));
    }

    private static void Submit(CampaignGameService flow, CyclingLove.State.GamePlayerData data,
        GameFlowCompletionType type, string action = null, string choice = null)
    {
        Assert.IsTrue(flow.Submit(data, new GameSceneResult
        {
            ExpectedRevision = data.Revision,
            CompletionType = type,
            SelectedActionId = action,
            SelectedChoiceId = choice
        }));
    }
}
