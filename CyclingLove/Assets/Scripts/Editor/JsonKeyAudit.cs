using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CyclingLove.Editor
{
    public static class JsonKeyAudit
    {
        public static string[] FindUnknownKeys(Type type, string json) =>
            Walk(type, JToken.Parse(json), "$").ToArray();

        private static IEnumerable<string> Walk(Type type, JToken token, string path)
        {
            var elementType = ElementType(type);
            if (elementType != null)
            {
                if (token is JArray array)
                    for (var i = 0; i < array.Count; i++)
                        foreach (var unknown in Walk(elementType, array[i], $"{path}[{i}]"))
                            yield return unknown;
                yield break;
            }
            if (token is not JObject obj || !IsSerializableObject(type)) yield break;
            var fields = SerializedFields(type);
            foreach (var property in obj.Properties())
            {
                if (!fields.TryGetValue(property.Name, out var field))
                {
                    yield return $"{path}.{property.Name}";
                    continue;
                }
                foreach (var unknown in Walk(field.FieldType, property.Value, $"{path}.{property.Name}"))
                    yield return unknown;
            }
        }

        private static Type ElementType(Type type)
        {
            if (type.IsArray) return type.GetElementType();
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) ? type.GetGenericArguments()[0] : null;
        }

        private static bool IsSerializableObject(Type type) =>
            !type.IsPrimitive && !type.IsEnum && type != typeof(string) && type.IsClass;

        private static Dictionary<string, FieldInfo> SerializedFields(Type type)
        {
            var fields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
                foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if ((field.IsPublic || field.IsDefined(typeof(SerializeField), false)) && !field.IsDefined(typeof(NonSerializedAttribute), false))
                        fields.TryAdd(field.Name, field);
            return fields;
        }
    }
}
