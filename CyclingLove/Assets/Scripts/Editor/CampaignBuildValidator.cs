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
            Debug.Log("Campaign data validation passed: prototype, regression, annual, equipment, bike visuals, races, calendar, and audio");
        }

        // The optional reader permits invalid-input tests without editing production assets.
        public static void ValidateContent(Func<string, string> readText = null)
        {
            if (readText != null)
            {
                ValidateSource(CampaignContentLoader.PrototypeResourceKey,
                    () => CampaignContentLoader.Load(CampaignContentLoader.PrototypeResourceKey, readText));
                return;
            }

            ValidateSource(CampaignContentLoader.PrototypeResourceKey, CampaignContentLoader.LoadPrototype);
            ValidateSource(CampaignContentLoader.RuntimeResourceKey, CampaignContentLoader.LoadRuntime);
            ValidateSource(CampaignContentLoader.AnnualResourceKey, CampaignContentLoader.LoadAnnual);
            ValidateSource(EquipmentContentLoader.RuntimeResourceKey, EquipmentContentLoader.LoadRuntime);
            ValidateSource(BikeVisualContentLoader.RuntimeResourceKey, () => BikeVisualContentLoader.LoadRuntime());
            ValidateSource(RaceContentLoader.RuntimeResourceKey, RaceContentLoader.LoadRuntime);
            ValidateSource(AnnualCalendarContentLoader.RuntimeResourceKey, AnnualCalendarContentLoader.LoadRuntime);
            ValidateSource(AudioContentLoader.RuntimeResourceKey, AudioContentLoader.LoadRuntime);
            ValidateSource("present audio assets", () =>
            {
                AudioAssetPipeline.ValidatePresentAssetsForBuild();
                return true;
            });
        }

        private static void ValidateSource<T>(string key, Func<T> validate)
        {
            try
            {
                validate();
            }
            catch (Exception ex)
            {
                throw new BuildFailedException($"Content validation failed [{key}]: {ex.Message}");
            }
        }
    }
}
