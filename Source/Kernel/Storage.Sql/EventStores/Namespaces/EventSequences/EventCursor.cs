// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventCursor"/> for SQL-based event storage.
/// </summary>
/// <param name="query">The queryable source of events.</param>
/// <param name="scope">The <see cref="DbContextScope{EventSequenceDbContext}"/> that owns the query context.</param>
/// <param name="eventStore">The event store name.</param>
/// <param name="namespace">The namespace name.</param>
/// <param name="identityStorage">Identity storage for resolving identities.</param>
/// <param name="eventTypesStorage">Storage used to look up event type schemas for schema-aware content deserialization.</param>
/// <param name="expandoObjectConverter">The schema-aware <see cref="IExpandoObjectConverter"/>.</param>
/// <param name="batchSize">Number of events to fetch per batch.</param>
/// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
public class EventCursor(
    IQueryable<EventEntry> query,
    DbContextScope<EventSequenceDbContext> scope,
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IIdentityStorage identityStorage,
    IEventTypesStorage eventTypesStorage,
    IExpandoObjectConverter expandoObjectConverter,
    int batchSize = 100,
    CancellationToken cancellationToken = default) : IEventCursor
{
    readonly IQueryable<EventEntry> _query = query;
    readonly DbContextScope<EventSequenceDbContext> _scope = scope;
    readonly EventStoreName _eventStore = eventStore;
    readonly EventStoreNamespaceName _namespace = @namespace;
    readonly IIdentityStorage _identityStorage = identityStorage;
    readonly IEventTypesStorage _eventTypesStorage = eventTypesStorage;
    readonly IExpandoObjectConverter _expandoObjectConverter = expandoObjectConverter;
    readonly CancellationToken _cancellationToken = cancellationToken;
    readonly int _batchSize = batchSize;
    int _currentOffset;
    bool _hasMore = true;

    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> Current { get; private set; } = [];

    /// <inheritdoc/>
    public async Task<bool> MoveNext()
    {
        if (!_hasMore || _cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        var eventEntries = await _query
            .OrderBy(e => e.SequenceNumber)
            .Skip(_currentOffset)
            .Take(_batchSize)
            .ToListAsync(_cancellationToken);

        if (eventEntries.Count == 0)
        {
            _hasMore = false;
            Current = [];
            return false;
        }

        var appendedEvents = new List<AppendedEvent>();
        foreach (var eventEntry in eventEntries)
        {
            var eventType = EventEntryConverter.GetEventType(eventEntry);
            var content = await ResolveContent(eventEntry, eventType);
            var causation = EventEntryConverter.GetCausation(eventEntry);
            var causedBy = EventEntryConverter.GetCausedBy(eventEntry);

            var eventMetadata = new EventContext(
                eventType,
                eventEntry.EventSourceType,
                eventEntry.EventSourceId,
                eventEntry.EventStreamType,
                eventEntry.EventStreamId,
                new EventSequenceNumber(eventEntry.SequenceNumber),
                eventEntry.Occurred,
                _eventStore,
                _namespace,
                new CorrelationId(Guid.Parse(eventEntry.CorrelationId)),
                causation,
                await _identityStorage.GetFor(causedBy),
                [],
                EventEntryConverter.GetHashForGeneration(eventEntry, eventType.Generation),
                Subject: EventEntryConverter.ResolveSubject(eventEntry));

            var generationalContent = EventEntryConverter.GetAllGenerationalContent(eventEntry);
            appendedEvents.Add(new AppendedEvent(eventMetadata, content) { GenerationalContent = generationalContent });
        }

        Current = appendedEvents;
        _currentOffset += eventEntries.Count;
        _hasMore = eventEntries.Count == _batchSize;

        return true;
    }

    /// <inheritdoc/>
    public void Dispose() => _scope.DisposeAsync().AsTask().GetAwaiter().GetResult();

    async Task<ExpandoObject> ResolveContent(EventEntry entry, EventType eventType)
    {
        if (eventType.Id == Cratis.Chronicle.Concepts.Events.GlobalEventTypes.Redaction)
        {
            return EventEntryConverter.GetContentForGeneration(entry, eventType.Generation);
        }

        if (!await _eventTypesStorage.HasFor(eventType.Id, eventType.Generation))
        {
            return EventEntryConverter.GetContentForGeneration(entry, eventType.Generation);
        }

        var jsonObject = EventEntryConverter.GetContentJsonForGeneration(entry, eventType.Generation);
        if (jsonObject is null)
        {
            return new ExpandoObject();
        }

        var schema = await _eventTypesStorage.GetFor(eventType.Id, eventType.Generation);
        return _expandoObjectConverter.ToExpandoObject(jsonObject, schema.Schema);
    }
}
