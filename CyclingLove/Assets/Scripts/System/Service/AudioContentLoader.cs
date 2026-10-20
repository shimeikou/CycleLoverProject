using CyclingLove.Audio;
using UnityEngine;

namespace System.Service
{
    public static class AudioContentLoader
    {
        public const string RuntimeResourceKey = "Content/AudioCatalog";

        public static AudioContentCatalog LoadRuntime()
        {
            var source = UnityEngine.Resources.Load<TextAsset>(RuntimeResourceKey);
            if (source == null || string.IsNullOrWhiteSpace(source.text))
                throw new InvalidOperationException("Audio content catalog is missing: " + RuntimeResourceKey);
            try
            {
                var catalog = JsonUtility.FromJson<AudioContentCatalog>(source.text) ??
                    throw new InvalidOperationException("Audio content catalog is empty");
                catalog.Validate();
                return catalog;
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("Invalid audio content catalog JSON", ex);
            }
        }
    }
}
