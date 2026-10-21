using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace CyclingLove.Editor
{
    public static class FontAtlasTools
    {
        public const string RegularAssetPath = "Assets/Font/BIZUDPGothic/BIZUDPGothic-Regular SDF.asset";
        private const string CharacterListPath = "Assets/Font/CustomCharacters.txt";
        private const int SamplingPointSize = 32;
        private const int AtlasPadding = 4;
        private const int AtlasSize = 4096;

        [MenuItem("Tools/Cycling Love/Font/Build BIZ UDPGothic Asset")]
        public static void BuildFromMenu()
        {
            var missing = BuildStaticAsset("Assets/Font/BIZUDPGothic/BIZUDPGothic-Regular.ttf", RegularAssetPath);
            if (missing.Length == 0) Debug.Log("Built " + RegularAssetPath);
            else Debug.LogWarning($"Built {RegularAssetPath}; {missing.Length} characters are not in the source font or did not fit: {missing}");
        }

        public static string BuildStaticAsset(string sourcePath, string assetPath)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath) ??
                throw new InvalidOperationException("Source font is missing: " + sourcePath);
            var characters = new string(File.ReadAllText(CharacterListPath).Concat(CollectGameText())
                .Where(value => !char.IsControl(value) && !char.IsSurrogate(value))
                .Distinct()
                .OrderBy(value => value)
                .ToArray());

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (font == null)
            {
                font = TMP_FontAsset.CreateFontAsset(source, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA,
                    AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, false);
                font.name = Path.GetFileNameWithoutExtension(assetPath);
                font.atlasTexture.name = font.name + " Atlas";
                font.material.name = font.name + " Material";
                AssetDatabase.CreateAsset(font, assetPath);
                AssetDatabase.AddObjectToAsset(font.atlasTexture, font);
                AssetDatabase.AddObjectToAsset(font.material, font);
            }
            else
            {
                var dynamic = new SerializedObject(font);
                dynamic.FindProperty("m_SourceFontFile").objectReferenceValue = source;
                dynamic.FindProperty("m_AtlasPopulationMode").intValue = (int)AtlasPopulationMode.Dynamic;
                dynamic.ApplyModifiedPropertiesWithoutUndo();
                font.ClearFontAssetData(true);
            }

            font.TryAddCharacters(characters, out var missing);

            var serialized = new SerializedObject(font);
            serialized.FindProperty("m_AtlasPopulationMode").intValue = (int)AtlasPopulationMode.Static;
            serialized.FindProperty("m_SourceFontFile").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var atlas = new SerializedObject(font.atlasTexture);
            atlas.FindProperty("m_IsReadable").boolValue = false;
            atlas.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(font.atlasTexture);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            return missing ?? string.Empty;
        }

        [MenuItem("Tools/Cycling Love/Font/Audit Game Text Glyphs")]
        public static void AuditFromMenu()
        {
            var missing = FindMissingCharacters(LoadFontAsset(), CollectGameText());
            if (missing.Length == 0) Debug.Log("Every game text character is in the Japanese font asset or its fallbacks.");
            else Debug.LogWarning($"Japanese font asset is missing {missing.Length} characters: {new string(missing)}");
        }

        public static void WarnForMissingGlyphs()
        {
            var missing = FindMissingCharacters(LoadFontAsset(), CollectGameText());
            if (missing.Length == 0) return;
            Debug.LogWarning(
                $"Japanese font asset is missing {missing.Length} characters used by game text; they will not render: {new string(missing)}");
        }

        public static char[] FindMissingCharacters(TMP_FontAsset font, string text)
        {
            if (font == null) throw new ArgumentNullException(nameof(font));
            return (text ?? string.Empty)
                .Where(value => !char.IsControl(value) && !char.IsSurrogate(value) && !char.IsWhiteSpace(value))
                .Distinct()
                .Where(value => !font.HasCharacter(value, searchFallbacks: true))
                .OrderBy(value => value)
                .ToArray();
        }

        public static string CollectGameText()
        {
            var builder = new StringBuilder();
            foreach (var path in Directory.GetFiles("Assets/Resources/Content", "*.json"))
                builder.Append(File.ReadAllText(path));
            // Comments are excluded so that only strings that can reach the screen are audited.
            var literal = new Regex("\"(?:[^\"\\\\]|\\\\.)*\"");
            foreach (var path in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories)
                         .Where(value => !value.Replace('\\', '/').Contains("/Editor/")))
                foreach (Match match in literal.Matches(File.ReadAllText(path)))
                    builder.Append(match.Value);
            return builder.ToString();
        }

        private static TMP_FontAsset LoadFontAsset() =>
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RegularAssetPath) ??
            throw new InvalidOperationException("Japanese font asset is missing: " + RegularAssetPath);
    }
}
