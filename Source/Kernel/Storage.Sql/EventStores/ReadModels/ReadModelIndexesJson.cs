// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.EventStores.ReadModels;

/// <summary>
/// Stores read-model index declarations as an array of objects, each holding a properties array of paths, so composite and ordered indexes
/// can be added later without another migration. Arrays of plain path strings are accepted when reading.
/// </summary>
internal static class ReadModelIndexesJson
{
    const string PropertiesKey = "properties";
    const string PathKey = "path";

    /// <summary>
    /// Serialize index declarations.
    /// </summary>
    /// <param name="indexes">The indexes to serialize.</param>
    /// <returns>The JSON representation.</returns>
    internal static string Serialize(IEnumerable<IndexDefinition> indexes)
    {
        var array = new JsonArray();
        foreach (var index in indexes)
        {
            array.Add(new JsonObject
            {
                [PropertiesKey] = new JsonArray(new JsonObject { [PathKey] = index.PropertyPath.Path })
            });
        }

        return array.ToJsonString();
    }

    /// <summary>
    /// Deserialize index declarations, accepting the object form and plain path strings.
    /// </summary>
    /// <param name="json">The stored JSON, or <see langword="null"/> for definitions stored before indexes existed.</param>
    /// <returns>The index declarations.</returns>
    internal static IndexDefinition[] Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || JsonNode.Parse(json) is not JsonArray array)
        {
            return [];
        }

        return array.Select(PathOf).OfType<string>().Select(path => new IndexDefinition(new PropertyPath(path))).ToArray();
    }

    static string? PathOf(JsonNode? node) => node switch
    {
        JsonValue value when value.GetValueKind() == JsonValueKind.String => value.GetValue<string>(),
        JsonObject index when index[PropertiesKey] is JsonArray { Count: > 0 } properties => properties[0]?[PathKey]?.GetValue<string>(),
        _ => null
    };
}
