using System.Collections.Generic;
using System;
using CyclingLove.Calendar;
using CyclingLove.Content;
using CyclingLove.Editor;
using CyclingLove.Racing;
using CyclingLove.Services;
using NUnit.Framework;
using UnityEditor.Build;
using UnityEngine;

public sealed class CampaignBuildValidatorTests
{
    [Serializable] private sealed class ActionsFile { public ActionDefinition[] Actions; }
    [Serializable] private sealed class DaysFile { public CampaignDay[] Days; }

    [Test]
    public void BuildCallbackAcceptsShippedCampaign()
    {
        Assert.DoesNotThrow(() => new CampaignBuildValidator().OnPreprocessBuild(null));
    }

    [Test]
    public void AnnualReachabilityRejectsMissingFlagProducer()
    {
        var campaign = CampaignContentLoader.LoadAnnual();
        var calendar = AnnualCalendarContentLoader.LoadRuntime();
        var races = RaceContentLoader.LoadRuntime();
        var routeChoice = Array.Find(campaign.Actions, action => action.Id == "evt_annual_route_select")
            .Choices[0];
        routeChoice.Effects.RouteFlag = null;

        var error = Assert.Throws<InvalidOperationException>(() =>
            CampaignBuildValidator.ValidateAnnualReachability(campaign, calendar, races));
        StringAssert.Contains("route_bikeshop_annual", error.Message);
    }

    [Test]
    public void AnnualReachabilityRejectsUnattainableRouteStep()
    {
        var campaign = CampaignContentLoader.LoadAnnual();
        var calendar = AnnualCalendarContentLoader.LoadRuntime();
        var races = RaceContentLoader.LoadRuntime();
        var ending = Array.Find(calendar.Endings, value => value.Id == "annual_confession_bikeshop");
        Array.Find(ending.Conditions, value => value.Type == "route_step").Value = 99;

        var error = Assert.Throws<InvalidOperationException>(() =>
            CampaignBuildValidator.ValidateAnnualReachability(campaign, calendar, races));
        StringAssert.Contains("hero_bikeshop:99", error.Message);
    }

    [Test]
    public void VisualAuditReportsOnlyUnresolvedUniqueReferences()
    {
        var campaign = new CampaignDefinition
        {
            Days = new[]
            {
                new CampaignDay
                {
                    MorningBackgroundId = "present_bg",
                    AfternoonBackgroundId = "missing_bg",
                    AfternoonCharacterId = "missing_character",
                    AfternoonOutfitId = "casual",
                    AfternoonExpressionId = "smile"
                }
            },
            Actions = new[]
            {
                new ActionDefinition
                {
                    BackgroundId = "missing_bg",
                    CharacterId = "present_character",
                    OutfitId = "shop",
                    ExpressionId = "neutral",
                    Effects = new CampaignEffects(),
                    Pages = new[] { "test" }
                }
            }
        };

        var missing = CampaignBuildValidator.FindMissingVisualAssets(
            new[] { campaign },
            id => id == "present_bg",
            (id, outfit, expression) => id == "present_character");

        CollectionAssert.AreEqual(new[]
        {
            "background:missing_bg",
            "character:missing_character/casual/smile"
        }, missing);
    }

    [TestCase("missing", "Content/PrototypeRules")]
    [TestCase("empty", "Content/PrototypeRules")]
    [TestCase("json", "Content/PrototypeRules")]
    [TestCase("duplicate", "duplicate action ID")]
    [TestCase("reference", "Missing action reference")]
    [TestCase("number", "Invalid campaign rules")]
    public void InvalidInputStopsBuildWithActionableReason(string defect, string reason)
    {
        var files = new Dictionary<string, string>();
        foreach (var name in new[] { "PrototypeCampaign", "PrototypeRules", "PrototypeActions", "PrototypeDays" })
            files.Add("Content/" + name, Resources.Load<TextAsset>("Content/" + name).text);
        var definition = CampaignContentLoader.LoadPrototype();
        switch (defect)
        {
            case "missing": files.Remove("Content/PrototypeRules"); break;
            case "empty": files["Content/PrototypeRules"] = " "; break;
            case "json": files["Content/PrototypeRules"] = "{broken"; break;
            case "duplicate":
                definition.Actions[1].Id = definition.Actions[0].Id;
                files["Content/PrototypeActions"] = JsonUtility.ToJson(new ActionsFile { Actions = definition.Actions });
                break;
            case "reference":
                definition.Days[0].ActionIds[0] = "missing-action";
                files["Content/PrototypeDays"] = JsonUtility.ToJson(new DaysFile { Days = definition.Days });
                break;
            case "number":
                definition.Rules.MaxStamina = 0;
                files["Content/PrototypeRules"] = JsonUtility.ToJson(definition.Rules);
                break;
        }
        var error = Assert.Throws<BuildFailedException>(() =>
            CampaignBuildValidator.ValidateContent(key => files.TryGetValue(key, out var value) ? value : null));
        StringAssert.Contains(CampaignContentLoader.PrototypeResourceKey, error.Message);
        StringAssert.Contains(reason, error.Message);
    }

    [Test]
    public void FontAuditReportsCharactersOutsideTheJapaneseFont()
    {
        var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(FontAtlasTools.RegularAssetPath);

        var missing = FontAtlasTools.FindMissingCharacters(font, "亜あ\uE000\uE000 \n");

        CollectionAssert.AreEqual(new[] { '\uE000' }, missing);
        StringAssert.Contains("狐の依頼", FontAtlasTools.CollectGameText());
    }
}
