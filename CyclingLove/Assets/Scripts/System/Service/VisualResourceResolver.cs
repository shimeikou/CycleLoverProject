using System.Collections.Generic;
using UnityEngine;

namespace CyclingLove.Services
{
    /// <summary>
    /// Keeps the current Resources layout out of presentation code. Replace this
    /// adapter when visual assets move to Addressables or external AssetBundles.
    /// </summary>
    public static class VisualResourceResolver
    {
        public static Sprite LoadBackground(string backgroundId)
        {
            return string.IsNullOrWhiteSpace(backgroundId)
                ? null
                : Resources.Load<Sprite>($"Images/Backgrounds/{backgroundId}");
        }

        public static Sprite LoadCharacter(string characterId, string outfitId, string expressionId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return null;

            var expression = string.IsNullOrWhiteSpace(expressionId) ? "neutral" : expressionId;
            foreach (var path in CharacterPaths(characterId, outfitId, expression))
            {
                var sprite = Resources.Load<Sprite>(path);
                if (sprite != null) return sprite;
            }
            if (expression != "neutral")
            {
                foreach (var path in CharacterPaths(characterId, outfitId, "neutral"))
                {
                    var sprite = Resources.Load<Sprite>(path);
                    if (sprite != null) return sprite;
                }
            }
            var legacyExpression = Resources.Load<Sprite>($"Images/Characters/{characterId}_{expression}");
            if (legacyExpression != null) return legacyExpression;
            return expression == "neutral"
                ? null
                : Resources.Load<Sprite>($"Images/Characters/{characterId}_neutral");
        }

        private static IEnumerable<string> CharacterPaths(string characterId, string outfitId, string expressionId)
        {
            if (!string.IsNullOrWhiteSpace(outfitId))
            {
                yield return $"Images/Characters/{characterId}/{outfitId}/{expressionId}";
                yield return $"Images/Characters/{characterId}_{outfitId}_{expressionId}";
            }
        }
    }
}
