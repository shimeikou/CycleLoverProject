using System.Linq;
using System;
using CyclingLove.Audio;
using CyclingLove.Calendar;
using CyclingLove.Flow;
using CyclingLove.Services;
using CyclingLove.Session;
using NUnit.Framework;
using Scene.Init;
using UnityEngine;

public sealed class AudioContentTests
{
    private static readonly string[] RequiredIds =
    {
        "bgm_daily_morning", "bgm_daily_afternoon", "bgm_daily_evening",
        "bgm_route_bikeshop", "bgm_route_cafe", "bgm_route_cousin",
        "bgm_route_announcer", "bgm_route_online_rival", "bgm_race", "bgm_fox",
        "bgm_confession", "bgm_ending_incomplete",
        "amb_bikeshop", "amb_cafe", "amb_town", "amb_coast", "amb_mountain", "amb_shrine",
        "se_ui_move", "se_ui_confirm", "se_ui_cancel", "se_page", "se_save",
        "se_bicycle_freewheel", "se_bicycle_brake", "se_race_start",
        "se_camera_shutter", "se_notification", "se_fox_bell"
    };

    [Test]
    public void RuntimeCatalogContainsEveryPlannedLogicalId()
    {
        var catalog = AudioContentLoader.LoadRuntime();

        CollectionAssert.AreEquivalent(RequiredIds, catalog.Entries.Select(entry => entry.Id));
        Assert.AreEqual(12, catalog.Entries.Count(entry => entry.Kind == AudioContentKinds.Bgm));
        Assert.AreEqual(6, catalog.Entries.Count(entry => entry.Kind == AudioContentKinds.Ambient));
        Assert.AreEqual(11, catalog.Entries.Count(entry => entry.Kind == AudioContentKinds.Se));
    }

    [Test]
    public void CharacterAndBackgroundRulesOverrideTimeFallbackIndependently()
    {
        var cue = AudioContentLoader.LoadRuntime().Resolve(new GameSceneRequest
        {
            Timing = ActionTiming.Morning,
            CharacterId = "hero_cafe_master",
            BackgroundId = "cafe_interior_day"
        });

        Assert.AreEqual("bgm_route_cafe", cue.BgmId);
        Assert.AreEqual("amb_cafe", cue.AmbientId);
        Assert.IsNull(cue.EnterSeId);
    }

    [Test]
    public void RaceRuleOverridesCharacterMusicAndKeepsLocationAmbient()
    {
        var cue = AudioContentLoader.LoadRuntime().Resolve(new GameSceneRequest
        {
            Timing = ActionTiming.Evening,
            NodeId = "annual_bikeshop_race_final",
            CharacterId = "hero_bikeshop",
            BackgroundId = "mountain_finish_evening"
        });

        Assert.AreEqual("bgm_race", cue.BgmId);
        Assert.AreEqual("amb_mountain", cue.AmbientId);
        Assert.AreEqual("se_race_start", cue.EnterSeId);
    }

    [Test]
    public void CatalogRejectsRuleWithWrongAudioKind()
    {
        var catalog = new AudioContentCatalog
        {
            Version = 1,
            Entries = new[]
            {
                new AudioContentEntry
                {
                    Id = "se_only", Kind = AudioContentKinds.Se, Address = "audio/se_only",
                    PackId = "test", AssetPath = "Assets/Content/Test/Audio/SE/se_only.wav",
                    Loop = false, Volume = 1f
                }
            },
            Rules = new[]
            {
                new AudioPresentationRule { Id = "bad", Priority = 1, BgmId = "se_only" }
            }
        };

        Assert.Throws<InvalidOperationException>(catalog.Validate);
    }

    [Test]
    public void AudioSettingsSnapshotIsIndependentAndValuesAreNormalized()
    {
        var settings = new AudioSettingsData
        {
            MasterVolume = 1.4f,
            BgmVolume = -0.2f,
            AmbientVolume = 0.45f,
            SeVolume = 0.8f,
            Muted = true
        };

        settings.Normalize();
        var snapshot = settings.Snapshot();
        snapshot.AmbientVolume = 0.1f;

        Assert.AreEqual(1f, settings.MasterVolume);
        Assert.AreEqual(0f, settings.BgmVolume);
        Assert.AreEqual(0.45f, settings.AmbientVolume);
        Assert.AreEqual(0.1f, snapshot.AmbientVolume);
        Assert.IsTrue(snapshot.Muted);
    }

    [Test]
    public void AudioSettingsPanelBuildsControlsAndForwardsPreviewApplyAndCancel()
    {
        var canvasObject = new GameObject("TestCanvas", typeof(Canvas));
        try
        {
            var panel = AudioSettingsPanel.Create(canvasObject.transform);
            AudioSettingsData preview = null;
            AudioSettingsData applied = null;
            var canceled = false;
            panel.Show(new AudioSettingsData(), value => preview = value,
                value => applied = value, () => canceled = true);

            var sliders = panel.GetComponentsInChildren<UnityEngine.UI.Slider>(true);
            Assert.AreEqual(4, sliders.Length);
            Assert.AreEqual(1, panel.GetComponentsInChildren<UnityEngine.UI.Toggle>(true).Length);
            Assert.AreEqual(3, panel.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length);

            var master = sliders.Single(slider => slider.name == "MasterSlider");
            master.value = 0.25f;
            Assert.AreEqual(0.25f, preview.MasterVolume);

            var apply = panel.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                .Single(button => button.name == "ApplyButton");
            apply.onClick.Invoke();
            Assert.AreEqual(0.25f, applied.MasterVolume);
            Assert.IsFalse(panel.IsOpen);

            panel.Show(new AudioSettingsData(), _ => { }, _ => { }, () => canceled = true);
            var cancel = panel.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                .Single(button => button.name == "CancelButton");
            cancel.onClick.Invoke();
            Assert.IsTrue(canceled);
            Assert.IsFalse(panel.IsOpen);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(canvasObject);
        }
    }
}
