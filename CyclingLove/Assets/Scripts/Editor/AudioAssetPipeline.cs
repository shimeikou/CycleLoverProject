using System;
using System.Collections.Generic;
using System.Linq;
using System.Service;
using CyclingLove.Audio;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace CyclingLove.Editor
{
    public sealed class AudioAssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (!AudioAssetPipeline.IsManagedAudioPath(assetPath)) return;
            AudioAssetPipeline.ApplyImporterDefaults((AudioImporter)assetImporter, assetPath);
        }
    }

    public static class AudioAssetPipeline
    {
        private const string GroupPrefix = "Audio - ";

        public static bool IsManagedAudioPath(string path) =>
            !string.IsNullOrWhiteSpace(path) &&
            path.StartsWith("Assets/Content/", StringComparison.Ordinal) &&
            path.Contains("/Audio/", StringComparison.Ordinal) &&
            path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);

        public static void ApplyImporterDefaults(AudioImporter importer, string path)
        {
            if (importer == null || !IsManagedAudioPath(path)) return;
            var isBgm = path.Contains("/Audio/BGM/", StringComparison.Ordinal);
            var isAmbient = path.Contains("/Audio/Ambient/", StringComparison.Ordinal);
            var isSe = path.Contains("/Audio/SE/", StringComparison.Ordinal);
            var isBuiltInSe = isSe && path.Contains("/BuiltIn/", StringComparison.Ordinal);
            var settings = importer.defaultSampleSettings;

            if (isBgm || isAmbient)
            {
                importer.forceToMono = false;
                importer.loadInBackground = true;
                settings.preloadAudioData = false;
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = isBgm ? 0.70f : 0.60f;
            }
            else if (isSe)
            {
                importer.forceToMono = true;
                importer.loadInBackground = false;
                settings.preloadAudioData = true;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = isBuiltInSe
                    ? AudioCompressionFormat.PCM
                    : AudioCompressionFormat.ADPCM;
                settings.quality = 1f;
            }

            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
        }

        [MenuItem("Tools/Cycling Love/Audio/Sync Present Assets to Addressables")]
        public static void SyncPresentAssets()
        {
            var catalog = AudioContentLoader.LoadRuntime();
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                throw new InvalidOperationException("Addressables settings are missing");

            var synchronized = 0;
            var missing = 0;
            foreach (var definition in catalog.Entries)
            {
                var guid = AssetDatabase.AssetPathToGUID(definition.AssetPath);
                if (string.IsNullOrWhiteSpace(guid))
                {
                    missing++;
                    continue;
                }

                var group = FindOrCreateGroup(settings, definition.PackId);
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.address = definition.Address;
                entry.SetLabel("audio", true, true, false);
                entry.SetLabel("pack_" + definition.PackId, true, true, false);
                synchronized++;
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"Audio Addressables sync complete: {synchronized} present, {missing} awaiting assets");
        }

        [MenuItem("Tools/Cycling Love/Audio/Audit Present Assets")]
        public static void AuditPresentAssets()
        {
            var catalog = AudioContentLoader.LoadRuntime();
            var errors = ValidatePresentAssets(catalog);
            var present = catalog.Entries.Count(entry => !string.IsNullOrWhiteSpace(
                AssetDatabase.AssetPathToGUID(entry.AssetPath)));
            if (errors.Count == 0)
                Debug.Log($"Audio asset audit passed: {present} present, {catalog.Entries.Length - present} awaiting assets");
            else
                Debug.LogError("Audio asset audit failed:\n" + string.Join("\n", errors));
        }

        public static void ValidatePresentAssetsForBuild()
        {
            var errors = ValidatePresentAssets(AudioContentLoader.LoadRuntime());
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("; ", errors));
        }

        private static List<string> ValidatePresentAssets(AudioContentCatalog catalog)
        {
            var errors = new List<string>();
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (var definition in catalog.Entries)
            {
                var guid = AssetDatabase.AssetPathToGUID(definition.AssetPath);
                if (string.IsNullOrWhiteSpace(guid)) continue;
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(definition.AssetPath);
                if (clip == null)
                {
                    errors.Add($"Not an AudioClip [{definition.Id}]: {definition.AssetPath}");
                    continue;
                }

                var addressable = settings?.FindAssetEntry(guid);
                var expectedGroup = GroupPrefix + definition.PackId;
                if (addressable == null || addressable.address != definition.Address ||
                    addressable.parentGroup?.Name != expectedGroup)
                    errors.Add($"Run audio Addressables sync [{definition.Id}]");

                if (AssetImporter.GetAtPath(definition.AssetPath) is AudioImporter importer &&
                    !MatchesImporterDefaults(importer, definition.Kind, definition.PackId))
                    errors.Add($"Reimport audio to apply defaults [{definition.Id}]");
            }
            return errors;
        }

        private static bool MatchesImporterDefaults(AudioImporter importer, string kind, string packId)
        {
            var settings = importer.defaultSampleSettings;
            if (kind == AudioContentKinds.Bgm || kind == AudioContentKinds.Ambient)
                return !importer.forceToMono && importer.loadInBackground && !settings.preloadAudioData &&
                       settings.loadType == AudioClipLoadType.Streaming &&
                       settings.compressionFormat == AudioCompressionFormat.Vorbis;
            return importer.forceToMono && !importer.loadInBackground && settings.preloadAudioData &&
                   settings.loadType == AudioClipLoadType.DecompressOnLoad &&
                   settings.compressionFormat == (packId == "builtin_core"
                       ? AudioCompressionFormat.PCM
                       : AudioCompressionFormat.ADPCM);
        }

        private static AddressableAssetGroup FindOrCreateGroup(
            AddressableAssetSettings settings, string packId)
        {
            var groupName = GroupPrefix + packId;
            var group = settings.FindGroup(groupName);
            if (group != null) return group;
            group = settings.CreateGroup(groupName, false, false, false, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var bundleSchema = group.GetSchema<BundledAssetGroupSchema>();
            if (bundleSchema != null)
                bundleSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            return group;
        }
    }
}
