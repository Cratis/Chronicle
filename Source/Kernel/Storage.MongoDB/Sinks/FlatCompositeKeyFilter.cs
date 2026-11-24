// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Matches a flat key by its complete root tuple, never by a subset or a synthetic Id property.
/// </summary>
internal static class FlatCompositeKeyFilter
{
    /// <summary>
    /// Creates the exact root tuple filter with schema-converted scalar values.
    /// </summary>
    /// <param name="key">The complete flat key.</param>
    /// <param name="converter">The read model's converter.</param>
    /// <returns>The root tuple equality filter.</returns>
    internal static BsonDocument Create(FlatCompositeKey key, IMongoDBConverter converter)
    {
        var filter = new BsonDocument();
        foreach (var (part, value) in key.Parts.OrderBy(part => part.Key, StringComparer.Ordinal))
        {
            filter[part] = converter.ToBsonValue(value, new PropertyPath(part));
        }

        return filter;
    }
}
