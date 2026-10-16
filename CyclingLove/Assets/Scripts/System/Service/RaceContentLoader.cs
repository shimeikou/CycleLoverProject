using CyclingLove.Racing;
using UnityEngine;

namespace System.Service
{
    public static class RaceContentLoader
    {
        public const string RuntimeResourceKey = "Content/Races";

        public static RaceCatalog LoadRuntime()
        {
            var source = UnityEngine.Resources.Load<TextAsset>(RuntimeResourceKey);
            if (source == null || string.IsNullOrWhiteSpace(source.text))
                throw new InvalidOperationException("Race catalog is missing: " + RuntimeResourceKey);
            try
            {
                var catalog = JsonUtility.FromJson<RaceCatalog>(source.text) ??
                    throw new InvalidOperationException("Race catalog is empty");
                catalog.Validate();
                return catalog;
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("Invalid race catalog JSON", ex);
            }
        }
    }
}
