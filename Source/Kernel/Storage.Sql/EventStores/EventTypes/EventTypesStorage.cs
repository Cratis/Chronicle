// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes;

/// <summary>
/// Represents an implementation of <see cref="IEventTypesStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class EventTypesStorage(EventStoreName eventStore, IDatabase database) : IEventTypesStorage
{
    ConcurrentBag<EventType> _eventTypes = new();

    /// <inheritdoc/>
    public async Task Populate()
    {
        await using var scope = await database.EventStore(eventStore);
        _eventTypes = new ConcurrentBag<EventType>(await scope.DbContext.EventTypes.ToListAsync());
    }

    /// <inheritdoc/>
    public async Task Register(Concepts.Events.EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code)
    {
        await using var scope = await database.EventStore(eventStore);

        var existingEventType = _eventTypes
            .FirstOrDefault(_ => _.Id == type.Id && _.Schemas.ContainsKey(type.Generation));

        if (existingEventType is not null)
        {
            var existingSchema = await JsonSchema.FromJsonAsync(existingEventType.Schemas[type.Generation]);
            if (existingSchema.ToJson() == schema.ToJson())
            {
                return;
            }
        }

        var eventSchema = new EventTypeSchema(type, owner, source, schema);
        if (_eventTypes.Any(_ => _.Id == type.Id))
        {
            _eventTypes = new ConcurrentBag<EventType>(_eventTypes.Where(_ => _.Id != type.Id));
        }
        var eventType = eventSchema.ToSql();
        _eventTypes.Add(eventType);

        await scope.DbContext.EventTypes.Upsert(eventType);
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task Register(EventTypeDefinition definition)
    {
        await using var scope = await database.EventStore(eventStore);

        var eventType = definition.ToSql();

        _eventTypes = new ConcurrentBag<EventType>(_eventTypes.Where(_ => _.Id != definition.Id));
        _eventTypes.Add(eventType);

        await scope.DbContext.EventTypes.Upsert(eventType);
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeDefinition>> GetAllDefinitions()
    {
        await using var scope = await database.EventStore(eventStore);
        var eventTypes = await scope.DbContext.EventTypes.ToListAsync();
        return eventTypes.ConvertAll(static eventType => eventType.ToDefinition());
    }

    /// <inheritdoc/>
    public async Task<EventTypeDefinition> GetDefinition(EventTypeId eventTypeId)
    {
        var eventType = await GetSpecificEventType(eventTypeId);
        if (eventType is null)
        {
            var schema = await GetFor(eventTypeId);
            return new EventTypeDefinition(
                eventTypeId,
                EventTypeOwner.None,
                false,
                [new EventTypeGenerationDefinition(schema.Type.Generation, schema.Schema)],
                []);
        }

        return eventType.ToDefinition();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes()
    {
        await using var scope = await database.EventStore(eventStore);
        var eventTypes = await scope.DbContext.EventTypes.ToListAsync();
        return eventTypes.Select(_ => _.ToKernel());
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventTypeSchema>> ObserveLatestForAllEventTypes() => LiveQuery.Observe(GetLatestForAllEventTypes);

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventTypeId> eventTypeIds)
    {
        var ids = eventTypeIds.ToList();
        await using var scope = await database.EventStore(eventStore);
        var eventTypes = await scope.DbContext.EventTypes.Where(e => ids.Contains(e.Id)).ToListAsync();
        return eventTypes.Select(_ => _.ToKernel());
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<Concepts.Events.EventType> eventTypes)
    {
        var eventTypesList = eventTypes.ToList();
        var ids = eventTypesList.ConvertAll(et => et.Id);
        await using var scope = await database.EventStore(eventStore);
        var storedTypes = await scope.DbContext.EventTypes.Where(e => ids.Contains(e.Id)).ToListAsync();
        var storedTypeMap = storedTypes.ToDictionary(s => s.Id);
        return eventTypesList
            .Where(et => storedTypeMap.ContainsKey(et.Id))
            .Select(et => storedTypeMap[et.Id].ToKernel(et.Generation));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetAllGenerationsForEventType(Concepts.Events.EventType eventType)
    {
        var storedEventType = await GetSpecificEventType(eventType.Id);
        if (storedEventType is null)
        {
            return [];
        }

        return storedEventType.Schemas.Select(kvp => storedEventType.ToKernel(new EventTypeGeneration(kvp.Key)));
    }

    /// <inheritdoc/>
    public async Task<EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = null)
    {
        generation ??= EventTypeGeneration.First;
        if (_eventTypes.Any(_ => _.Id == type && _.Schemas.ContainsKey(generation)))
        {
            return _eventTypes.First(_ => _.Id == type && _.Schemas.ContainsKey(generation)).ToKernel(generation);
        }
        var eventType = await GetSpecificEventType(type) ?? throw new UnknownEventType(eventStore, type);
        if (eventType.Schemas.Count == 0)
        {
            throw new MissingEventSchemaForEventType(
                eventStore,
                type,
                generation);
        }

        return eventType.ToKernel();
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = null)
    {
        generation ??= EventTypeGeneration.First;
        if (_eventTypes.Any(_ => _.Id == type && _.Schemas.ContainsKey(generation)))
        {
            // Verify the cache is consistent with the database. After an external database
            // reset (e.g. between integration test classes), the cache may still hold entries
            // for event types that no longer exist in the backing store.
            var dbEventType = await GetSpecificEventType(type);
            if (dbEventType?.Schemas.ContainsKey(generation) != true)
            {
                _eventTypes = new ConcurrentBag<EventType>(_eventTypes.Where(_ => _.Id != type));
                return false;
            }

            return true;
        }

        var eventType = await GetSpecificEventType(type);
        return eventType?.Schemas.ContainsKey(generation) ?? false;
    }

    async Task<EventType?> GetSpecificEventType(EventTypeId eventTypeId)
    {
        var eventTypeIdValue = eventTypeId.Value;
        await using var scope = await database.EventStore(eventStore);
        return await scope.DbContext.EventTypes.FirstOrDefaultAsync(e => e.Id == eventTypeIdValue);
    }
}
