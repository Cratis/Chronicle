// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Identities;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventCursor"/> for SQL-based event storage.
/// </summary>
/// <param name="query">The queryable source of events.</param>
/// <param name="eventStore">The event store name.</param>
/// <param name="namespace">The namespace name.</param>
/// <param name="identityStorage">Identity storage for resolving identities.</param>
/// <param name="batchSize">Number of events to fetch per batch.</param>
/// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
public class EventCursor(
    IQueryable<EventEntry> query,
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IIdentityStorage identityStorage,
    int batchSize = 100,
    CancellationToken cancellationToken = default) : IEventCursor
{
    readonly IQueryable<EventEntry> _query = query;
    readonly EventStoreName _eventStore = eventStore;
    readonly EventStoreNamespaceName _namespace = @namespace;
    readonly IIdentityStorage _identityStorage = identityStorage;
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
            var content = EventEntryConverter.GetContentForGeneration(eventEntry, eventType.Generation);
            var causation = EventEntryConverter.GetCausation(eventEntry);
            var causedBy = EventEntryConverter.GetCausedBy(eventEntry);

            var eventMetadata = new EventContext(
                eventType,
                new EventSourceType(eventEntry.EventSourceType),
                new EventSourceId(eventEntry.EventSourceId),
                new EventStreamType(eventEntry.EventStreamType),
                new EventStreamId(eventEntry.EventStreamId),
                new EventSequenceNumber(eventEntry.SequenceNumber),
                eventEntry.Occurred,
                _eventStore,
                _namespace,
                new CorrelationId(Guid.Parse(eventEntry.CorrelationId)),
                causation,
                await _identityStorage.GetFor(causedBy),
                [],
                EventHash.NotSet);

            appendedEvents.Add(new AppendedEvent(eventMetadata, content));
        }

        Current = appendedEvents;
        _currentOffset += eventEntries.Count;
        _hasMore = eventEntries.Count == _batchSize;

        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing to dispose for EF Core queryables
    }
}
