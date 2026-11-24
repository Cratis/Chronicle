// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Setup.Serialization.for_KernelSerializers.given;

/// <summary>
/// Compares two payload instances member by member and reports where they differ.
/// </summary>
/// <param name="ignoredMembers">Names of members that are left out of the comparison.</param>
public class PayloadComparer(IEnumerable<string> ignoredMembers)
{
    const int MaxDepth = 12;
    readonly HashSet<string> _ignoredMembers = [.. ignoredMembers];

    /// <summary>
    /// Find the differences between an expected and an actual instance.
    /// </summary>
    /// <param name="expected">The instance that was sent.</param>
    /// <param name="actual">The instance that came out.</param>
    /// <returns>The paths that differ, empty when the instances carry the same data.</returns>
    public IReadOnlyList<string> Differences(object? expected, object? actual)
    {
        var differences = new List<string>();
        Compare("$", expected, actual, differences, 0);
        return differences;
    }

    void Compare(string path, object? expected, object? actual, List<string> differences, int depth)
    {
        if (expected is null || actual is null)
        {
            if (expected is not null || actual is not null)
            {
                differences.Add($"{path}: expected {Describe(expected)} but was {Describe(actual)}");
            }
            return;
        }

        if (depth > MaxDepth || differences.Count > 20)
        {
            return;
        }

        var type = expected.GetType();

        // The JSON codec reads values of an expando it has no schema for as JsonElement; the data is the same.
        if (actual is JsonElement element && expected is not JsonElement)
        {
            if (JsonSerializer.Serialize(expected) != element.GetRawText())
            {
                differences.Add($"{path}: expected {expected} but was {element.GetRawText()}");
            }
            return;
        }

        var bothCollections = expected is IEnumerable and not string && actual is IEnumerable and not string;
        if (type != actual.GetType() && !bothCollections)
        {
            differences.Add($"{path}: expected type {type.Name} but was {actual.GetType().Name}");
            return;
        }

        if (expected is JsonNode expectedNode)
        {
            if (expectedNode.ToJsonString() != ((JsonNode)actual).ToJsonString())
            {
                differences.Add($"{path}: json differs");
            }
            return;
        }

        if (expected is JsonSchema expectedSchema)
        {
            if (expectedSchema.ToJson() != ((JsonSchema)actual).ToJson())
            {
                differences.Add($"{path}: schema differs");
            }
            return;
        }

        if (expected is Exception expectedException)
        {
            if (expectedException.Message != ((Exception)actual).Message)
            {
                differences.Add($"{path}: exception message differs");
            }
            return;
        }

        if (IsValue(type))
        {
            if (!expected.Equals(actual))
            {
                differences.Add($"{path}: expected {expected} but was {actual}");
            }
            return;
        }

        if (expected is IDictionary<string, object?> expectedExpando && expected is ExpandoObject)
        {
            CompareDictionaries(path, expectedExpando.ToDictionary(_ => (object)_.Key, _ => _.Value), ((IDictionary<string, object?>)actual).ToDictionary(_ => (object)_.Key, _ => _.Value), differences, depth);
            return;
        }

        if (expected is IDictionary expectedDictionary)
        {
            CompareDictionaries(path, ToDictionary(expectedDictionary), ToDictionary((IDictionary)actual), differences, depth);
            return;
        }

        if (expected is IEnumerable expectedItems && actual is IEnumerable actualItems && type.GetInterfaces().Any(_ => _.IsGenericType && (_.GetGenericTypeDefinition() == typeof(IDictionary<,>) || _.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))))
        {
            CompareDictionaries(path, ToDictionary(expectedItems), ToDictionary(actualItems), differences, depth);
            return;
        }

        if (expected is IEnumerable sequence && actual is IEnumerable otherSequence)
        {
            var left = sequence.Cast<object?>().ToList();
            var right = otherSequence.Cast<object?>().ToList();
            if (left.Count != right.Count)
            {
                differences.Add($"{path}: expected {left.Count} items but was {right.Count}");
                return;
            }

            // Sets have no order; match what is the same data regardless of position.
            var unordered = type.Name.Contains("Set");
            for (var i = 0; i < left.Count; i++)
            {
                if (unordered)
                {
                    var matches = right.Exists(_ => Differences(left[i], _).Count == 0);
                    if (!matches)
                    {
                        differences.Add($"{path}[{i}]: no matching item");
                    }
                }
                else
                {
                    Compare($"{path}[{i}]", left[i], right[i], differences, depth + 1);
                }
            }
            return;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(_ => _.GetIndexParameters().Length == 0 && _.GetMethod is not null && !_ignoredMembers.Contains(_.Name)))
        {
            Compare($"{path}.{property.Name}", property.GetValue(expected), property.GetValue(actual), differences, depth + 1);
        }
    }

    void CompareDictionaries(string path, IDictionary<object, object?> expected, IDictionary<object, object?> actual, List<string> differences, int depth)
    {
        if (expected.Count != actual.Count)
        {
            differences.Add($"{path}: expected {expected.Count} entries but was {actual.Count}");
            return;
        }

        foreach (var (key, value) in expected)
        {
            if (!actual.TryGetValue(key, out var other))
            {
                // Keys that are objects compare by reference inside; find the same key by what it carries.
                var match = actual.Keys.FirstOrDefault(_ => Differences(key, _).Count == 0);
                if (match is null)
                {
                    differences.Add($"{path}[{key}]: missing ({string.Join(", ", actual.Keys.Take(1).SelectMany(_ => Differences(key, _)))})");
                    continue;
                }
                other = actual[match];
            }
            Compare($"{path}[{key}]", value, other, differences, depth + 1);
        }
    }

    static Dictionary<object, object?> ToDictionary(IDictionary dictionary)
    {
        var result = new Dictionary<object, object?>();
        foreach (DictionaryEntry entry in dictionary)
        {
            result[entry.Key] = entry.Value;
        }
        return result;
    }

    static Dictionary<object, object?> ToDictionary(IEnumerable pairs)
    {
        var result = new Dictionary<object, object?>();
        foreach (var pair in pairs)
        {
            var pairType = pair!.GetType();
            result[pairType.GetProperty("Key")!.GetValue(pair)!] = pairType.GetProperty("Value")!.GetValue(pair);
        }
        return result;
    }

    static bool IsValue(Type type) =>
        type.IsPrimitive ||
        type.IsEnum ||
        type == typeof(string) ||
        type == typeof(decimal) ||
        type == typeof(Guid) ||
        type == typeof(DateTime) ||
        type == typeof(DateTimeOffset) ||
        type == typeof(TimeSpan) ||
        type == typeof(DateOnly) ||
        type == typeof(TimeOnly) ||
        type == typeof(Uri) ||
        typeof(Type).IsAssignableFrom(type) ||
        type.Namespace == "Orleans.Runtime" ||
        IsConcept(type);

    static bool IsConcept(Type type)
    {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition().Name.StartsWith("ConceptAs", StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    static string Describe(object? value) => value is null ? "null" : value.GetType().Name;
}
