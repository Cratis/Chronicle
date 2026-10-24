// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.EventStores.ReadModels;

/// <summary>
/// Stores ordered index declarations, accepting legacy path strings and property objects without directions.
/// </summary>
internal static class ReadModelIndexesJson
{
    static readonly JsonSerializerOptions _options = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    /// <summary>
    /// Serializes all ordered index properties.
    /// </summary>
    /// <param name="indexes">The index definitions.</param>
    /// <returns>The storage JSON.</returns>
    internal static string Serialize(IEnumerable<IndexDefinition> indexes) =>
        JsonSerializer.Serialize(indexes.Select(index => new { properties = index.Properties.Select(property => new { path = property.Path.Path, descending = property.Descending ? true : (bool?)null }) }), _options);

    /// <summary>
    /// Reads ordered or legacy index declarations.
    /// </summary>
    /// <param name="json">The stored JSON, or null.</param>
    /// <returns>The supported index definitions.</returns>
    internal static IndexDefinition[] Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || JsonNode.Parse(json) is not JsonArray array)
        {
            return [];
        }
        return array.Select(ReadIndex).OfType<IndexDefinition>().ToArray();
    }

    static IndexDefinition? ReadIndex(JsonNode? node)
    {
        if (node is JsonValue value && value.GetValueKind() == JsonValueKind.String)
        {
            return new(value.GetValue<string>());
        }
        if (node is not JsonObject index || index["properties"] is not JsonArray { Count: > 0 } properties)
        {
            return null;
        }
        var indexedProperties = new List<IndexedProperty>();
        foreach (var property in properties)
        {
            if (property is not JsonObject entry || entry["path"] is not JsonValue path || !path.TryGetValue<string>(out var text) || string.IsNullOrEmpty(text))
            {
                return null;
            }
            if (entry["descending"] is not null && (entry["descending"] is not JsonValue direction || !direction.TryGetValue<bool>(out _)))
            {
                return null;
            }
            indexedProperties.Add(new(text, entry["descending"]?.GetValue<bool>() ?? false));
        }
        return new(indexedProperties[0].Path) { Properties = indexedProperties };
    }
}
