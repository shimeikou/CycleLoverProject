using System;
using UnityEngine;

namespace CyclingLove.Services
{
    public static class JsonContentReader
    {
        // Lets build checks inspect raw JSON, because JsonUtility silently ignores unknown keys.
        public static event Action<string, Type, string> Parsed;

        public static T Load<T>(string resourceKey) where T : class => Parse<T>(resourceKey, ReadResourceText(resourceKey));

        public static string ReadResourceText(string resourceKey)
        {
            var source = Resources.Load<TextAsset>(resourceKey);
            return source == null ? null : source.text;
        }

        public static T Parse<T>(string resourceKey, string source) where T : class
        {
            if (string.IsNullOrWhiteSpace(resourceKey)) throw new InvalidOperationException("Missing content resource key");
            if (source == null) throw new InvalidOperationException("Content resource missing: " + resourceKey);
            if (string.IsNullOrWhiteSpace(source)) throw new InvalidOperationException("Empty content: " + resourceKey);
            Parsed?.Invoke(resourceKey, typeof(T), source);
            try { return JsonUtility.FromJson<T>(source) ?? throw new InvalidOperationException("Empty content: " + resourceKey); }
            catch (ArgumentException ex) { throw new InvalidOperationException("Invalid JSON content: " + resourceKey, ex); }
        }
    }
}
