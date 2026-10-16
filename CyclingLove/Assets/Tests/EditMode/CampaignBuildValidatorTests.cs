using System;
using System.Collections.Generic;
using System.Service;
using CyclingLove.Content;
using CyclingLove.Editor;
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
}
