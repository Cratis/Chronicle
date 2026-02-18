// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.EntityFrameworkCore;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes;

/// <summary>
/// Represents an implementation of <see cref="IEventTypesStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class EventTypesStorage(EventStoreName eventStore, IDatabase database) : IEventTypesStorage, IDisposable
{
    readonly ReplaySubject<IEnumerable<EventTypeSchema>> _eventTypesSubject = new(1);
    ConcurrentBag<EventType> _eventTypes = new();

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

        _eventTypesSubject.OnNext(await GetLatestForAllEventTypes());
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes()
    {
        await using var scope = await database.EventStore(eventStore);
        var eventTypes = await scope.DbContext.EventTypes.ToListAsync();
        return eventTypes.Select(_ => _.ToKernel());
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventTypeSchema>> ObserveLatestForAllEventTypes() => _eventTypesSubject;

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

    /// <inheritdoc/>
    public void Dispose()
    {
        _eventTypesSubject?.Dispose();
        GC.SuppressFinalize(this);
    }

    async Task<EventType?> GetSpecificEventType(EventTypeId eventTypeId)
    {
        await using var scope = await database.EventStore(eventStore);
        return await scope.DbContext.EventTypes.FirstOrDefaultAsync(e => e.Id == eventTypeId);
    }
}
