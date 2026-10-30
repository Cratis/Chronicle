// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Stores ordered indexes beside the unchanged legacy path array.
/// </summary>
internal static class ReadModelIndexesBson
{
    /// <summary>
    /// Serializes ordered index definitions.
    /// </summary>
    /// <param name="indexes">The index definitions.</param>
    /// <returns>The BSON array.</returns>
    internal static BsonArray Serialize(IEnumerable<IndexDefinition> indexes) => new(indexes.Select(index => new BsonDocument("properties", new BsonArray(index.Properties.Select(property => new BsonDocument
    {
        ["path"] = property.Path.Path,
        ["descending"] = property.Descending
    })))));

    /// <summary>
    /// Reads legacy paths and ordered index definitions.
    /// </summary>
    /// <param name="indexes">The stored definitions.</param>
    /// <returns>The index definitions.</returns>
    internal static IndexDefinition[] Deserialize(BsonArray indexes) => indexes.Select(index =>
    {
        if (index.IsString)
        {
            return new IndexDefinition(index.AsString);
        }
        var properties = index["properties"].AsBsonArray.Select(property => new IndexedProperty(property["path"].AsString, property.AsBsonDocument.GetValue("descending", false).AsBoolean)).ToArray();
        if (properties.Length == 0)
        {
            throw new FormatException("An index must contain at least one property.");
        }
        return new IndexDefinition(properties[0].Path) { Properties = properties };
    }).ToArray();
}
