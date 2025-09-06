// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.EntityFrameworkCore;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.Sql.EventTypes;

/// <summary>
/// Represents an implementation of <see cref="IEventTypesStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="dbContext">The database context.</param>
public class EventTypesStorage(EventStoreName eventStore, EventStoreDbContext dbContext) : IEventTypesStorage
{
    ConcurrentBag<EventType> _eventTypes = new();

    /// <inheritdoc/>
    public async Task Populate()
    {
        var eventTypes = await dbContext.EventTypes.ToListAsync();
        foreach (var eventType in eventTypes)
        {
            _eventTypes.Add(eventType);
        }
    }

    /// <inheritdoc/>
    public async Task Register(Concepts.Events.EventType type, JsonSchema schema)
    {
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

        var eventSchema = new EventTypeSchema(type, schema);
        if (_eventTypes.Any(_ => _.Id == type.Id))
        {
            _eventTypes = new ConcurrentBag<EventType>(_eventTypes.Where(_ => _.Id != type.Id));
        }
        var eventType = eventSchema.ToSql();
        _eventTypes.Add(eventType);
        dbContext.EventTypes.Add(eventType);
        await dbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeSchema>> GetAllGenerationsForEventType(Concepts.Events.EventType eventType) => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task<EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = null)
    {
        generation ??= EventTypeGeneration.First;
        if (_eventTypes.Any(_ => _.Id == type && _.Schemas.ContainsKey(generation)))
        {
            return _eventTypes.First(_ => _.Id == type && _.Schemas.ContainsKey(generation)).ToKernel();
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
    public Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes() => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = null)
    {
        generation ??= EventTypeGeneration.First;
        if (_eventTypes.Any(_ => _.Id == type && _.Schemas.ContainsKey(generation)))
        {
            return true;
        }

        var eventType = await GetSpecificEventType(type);
        return eventType?.Schemas.ContainsKey(generation) ?? false;
    }

    Task<EventType?> GetSpecificEventType(EventTypeId eventTypeId) =>
        dbContext.EventTypes.FirstOrDefaultAsync(e => e.Id == eventTypeId.Value);
}
