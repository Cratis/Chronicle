// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Maps the property names of forwarded event content to the casing of the target event store's schema.
/// </summary>
/// <remarks>
/// Schema validation is case sensitive, so a source schema that differs from the target only in property casing
/// would otherwise never be delivered. A name is only renamed when the schema has no property with that exact
/// name and exactly one property matching it case-insensitively; anything else is left untouched so validation
/// reports it.
/// </remarks>
internal static class EventStoreSubscriptionContentCasing
{
    /// <summary>
    /// Renames the properties of the content, recursively, to the property names of the schema. The content is modified in place.
    /// </summary>
    /// <param name="content">The content to map.</param>
    /// <param name="schema">The target schema.</param>
    /// <returns>The same <paramref name="content"/> instance.</returns>
    internal static JsonObject Map(JsonObject content, JsonSchema schema)
    {
        var properties = GetProperties(schema);
        if (properties.Count == 0)
        {
            return content;
        }

        var entries = content.ToArray();
        var renames = new List<(string From, string To)>();
        foreach (var (name, value) in entries)
        {
            var target = Resolve(name, properties, entries);
            if (properties.TryGetValue(target, out var property) && value is not null)
            {
                MapNode(value, property.ActualTypeSchema);
            }

            if (target != name)
            {
                renames.Add((name, target));
            }
        }

        if (renames.Count > 0)
        {
            var renamed = renames.ToDictionary(_ => _.From, _ => _.To);
            content.Clear();
            foreach (var (name, value) in entries)
            {
                content[renamed.GetValueOrDefault(name, name)] = value;
            }
        }

        return content;
    }

    static void MapNode(JsonNode node, JsonSchema schema)
    {
        switch (node)
        {
            case JsonObject nested:
                Map(nested, schema);
                break;
            case JsonArray array:
                var itemSchema = schema.Item?.ActualTypeSchema;
                if (itemSchema is not null)
                {
                    foreach (var item in array.OfType<JsonObject>())
                    {
                        Map(item, itemSchema);
                    }
                }

                break;
        }
    }

    static string Resolve(string name, Dictionary<string, JsonSchema> properties, KeyValuePair<string, JsonNode?>[] entries)
    {
        if (properties.ContainsKey(name))
        {
            return name;
        }

        var candidates = properties.Keys.Where(_ => string.Equals(_, name, StringComparison.OrdinalIgnoreCase)).ToArray();
        var ambiguousInContent = entries.Count(_ => string.Equals(_.Key, name, StringComparison.OrdinalIgnoreCase)) > 1;
        return candidates.Length == 1 && !ambiguousInContent ? candidates[0] : name;
    }

    static Dictionary<string, JsonSchema> GetProperties(JsonSchema schema)
    {
        var properties = new Dictionary<string, JsonSchema>();
        var actual = schema.ActualTypeSchema;
        foreach (var source in new[] { actual }.Concat(actual.AllOf.Select(_ => _.ActualSchema)))
        {
            foreach (var (name, property) in source.Properties)
            {
                properties.TryAdd(name, property);
            }
        }

        return properties;
    }
}
