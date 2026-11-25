// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Matches a flat key by its complete, sorted identity document using the _id index.
/// </summary>
internal static class FlatCompositeKeyFilter
{
    /// <summary>
    /// Creates an exact identity filter with schema-converted scalar values.
    /// </summary>
    /// <param name="key">The complete flat key.</param>
    /// <param name="converter">The read model's converter.</param>
    /// <returns>The indexed identity equality filter.</returns>
    internal static FilterDefinition<BsonDocument> Create(FlatCompositeKey key, IMongoDBConverter converter) =>
        Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(key));
}
