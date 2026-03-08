// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Seeding;

/// <summary>
/// Represents an implementation of <see cref="IEventSeedingStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="jsonSerializerOptions">The JSON serializer options.</param>
public class EventSeedingStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database, JsonSerializerOptions jsonSerializerOptions) : IEventSeedingStorage
{
    /// <inheritdoc/>
    public async Task<EventSeeds> Get()
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var entity = await scope.DbContext.EventSeeds.FirstOrDefaultAsync();
        if (entity is null)
        {
            return new EventSeeds(
                new Dictionary<Concepts.Events.EventTypeId, IEnumerable<SeededEventEntry>>(),
                new Dictionary<Concepts.Events.EventSourceId, IEnumerable<SeededEventEntry>>());
        }

        return EventSeedingConverters.ToEventSeeds(entity, jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public async Task Save(EventSeeds data)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var entity = await scope.DbContext.EventSeeds.FirstOrDefaultAsync();
        if (entity is null)
        {
            entity = EventSeedingConverters.ToEntity(data, jsonSerializerOptions);
            scope.DbContext.EventSeeds.Add(entity);
        }
        else
        {
            var updated = EventSeedingConverters.ToEntity(data, jsonSerializerOptions);
            entity.ByEventTypeJson = updated.ByEventTypeJson;
            entity.ByEventSourceJson = updated.ByEventSourceJson;
        }

        await scope.DbContext.SaveChangesAsync();
    }
}
