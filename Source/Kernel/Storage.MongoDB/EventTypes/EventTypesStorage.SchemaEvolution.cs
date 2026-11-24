// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Strings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.EventTypes;

public partial class EventTypesStorage
{
    /// <inheritdoc/>
    public async Task<bool> TryReplaceSchema(EventTypeId eventTypeId, EventTypeGeneration generation, JsonSchema expected, JsonSchema schema)
    {
        var path = $"{nameof(EventType.Schemas).ToCamelCase()}.{generation}";
        var filter = Builders<EventType>.Filter.Eq(_ => _.Id, eventTypeId) &
            Builders<EventType>.Filter.Eq(path, BsonDocument.Parse(expected.ToJson()));
        var update = Builders<EventType>.Update.Set(path, BsonDocument.Parse(schema.ToJson()));
        var result = await GetCollection().UpdateOneAsync(filter, update).ConfigureAwait(false);
        return result.ModifiedCount > 0;
    }
}
