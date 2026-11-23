// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventTypes;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.EventTypes;

public partial class EventTypesStorage
{
    /// <inheritdoc/>
    public async Task<EventTypeRegistrationResult> RegisterIfAbsent(EventTypeToRegister eventType)
    {
        var collection = GetCollection();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var cursor = await collection.FindAsync(GetFilterForSpecificEventType(eventType.Definition.Id));
            var existing = await cursor.FirstOrDefaultAsync();
            var result = EventTypeRegistrationResult.Compare(eventType.Definition, existing?.ToDefinition().Generations ?? []);
            if (result.Conflict is not null)
            {
                return result;
            }

            var incoming = eventType.Definition.ToMongoDB(
                eventType.Definition.Owner, eventType.Source, EffectiveVisibility(existing, eventType.Visibility), eventType.Origin);
            var effective = Merge(existing, incoming);
            if (!Changes(existing, effective))
            {
                return result;
            }

            if (await TryInsertOrUpdate(collection, existing, effective))
            {
                Invalidate(effective.Id);
                return result with { Changed = true };
            }
        }

        throw new EventTypeRegistrationContended(eventType.Definition.Id);
    }

    async Task<bool> TryInsertOrUpdate(IMongoCollection<EventType> collection, EventType? existing, EventType effective)
    {
        if (existing is null)
        {
            try
            {
                await collection.InsertOneAsync(effective);
                return true;
            }
            catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }

        var filter = GetFilterForSpecificEventType(effective.Id) &
            Builders<EventType>.Filter.Eq(_ => _.Schemas, existing.Schemas) &
            Builders<EventType>.Filter.Eq(_ => _.Migrations, existing.Migrations);
        var updated = await collection.UpdateOneAsync(filter, BuildUpdate(existing, effective));
        return updated.MatchedCount == 1;
    }
}
