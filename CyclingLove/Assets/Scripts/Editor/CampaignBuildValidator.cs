using System;
using System.Service;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CyclingLove.Editor
{
    public sealed class CampaignBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report) => ValidateContent();

        [MenuItem("Tools/Cycling Love/Validate Campaign Data")]
        public static void ValidateFromMenu()
        {
            ValidateContent();
            Debug.Log("Campaign data validation passed: " + CampaignContentLoader.RuntimeResourceKey);
        }

        // The optional reader permits invalid-input tests without editing production assets.
        public static void ValidateContent(Func<string, string> readText = null)
        {
            var key = readText == null ? CampaignContentLoader.RuntimeResourceKey : CampaignContentLoader.PrototypeResourceKey;
            try
            {
                if (readText == null)
                {
                    CampaignContentLoader.Load(CampaignContentLoader.PrototypeResourceKey);
                    CampaignContentLoader.Load(key);
                    CampaignContentLoader.LoadAnnual();
                    EquipmentContentLoader.LoadRuntime();
                    RaceContentLoader.LoadRuntime();
                    AnnualCalendarContentLoader.LoadRuntime();
                }
                else CampaignContentLoader.Load(key, readText);
            }
            catch (Exception ex)
            {
                throw new BuildFailedException($"Campaign data validation failed [{key}]: {ex.Message}");
            }
        }
    }
}
