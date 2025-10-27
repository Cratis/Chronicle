// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Represents the implementation of <see cref="IEventSequenceStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The event store name.</param>
/// <param name="namespace">The namespace name.</param>
/// <param name="eventSequenceId">The identifier for the event sequence.</param>
/// <param name="database">The <see cref="IDatabase"/> for storage operations.</param>
/// <param name="identityStorage">The <see cref="IIdentityStorage"/> for managing identities.</param>
/// <param name="logger">The <see cref="ILogger{EventSequenceStorage}"/> for logging.</param>
public class EventSequenceStorage(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    EventSequenceId eventSequenceId,
    IDatabase database,
    IIdentityStorage identityStorage,
    ILogger<EventSequenceStorage> logger) : IEventSequenceStorage
{
    /// <inheritdoc/>
    public async Task<EventSequenceState> GetState()
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var entry = await scope.DbContext.EventSequenceStates
            .FirstOrDefaultAsync(s => s.EventSequenceId == eventSequenceId.Value);

        if (entry is null)
        {
            return new EventSequenceState();
        }

        return EventSequenceStateEntryConverter.ToEventSequenceState(entry);
    }

    /// <inheritdoc/>
    public async Task SaveState(EventSequenceState state)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var entry = EventSequenceStateEntryConverter.ToEventSequenceStateEntry(state, eventSequenceId);
        var existingEntry = await scope.DbContext.EventSequenceStates
            .FirstOrDefaultAsync(s => s.EventSequenceId == eventSequenceId.Value);

        if (existingEntry is not null)
        {
            existingEntry.SequenceNumber = entry.SequenceNumber;
            existingEntry.TailSequenceNumberPerEventType = entry.TailSequenceNumberPerEventType;
            scope.DbContext.EventSequenceStates.Update(existingEntry);
        }
        else
        {
            scope.DbContext.EventSequenceStates.Add(entry);
        }

        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<EventCount> GetCount(EventSequenceNumber? lastEventSequenceNumber = null, IEnumerable<EventType>? eventTypes = null)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var query = scope.DbContext.EventSequenceEvents
            .Where(e => e.EventSequenceId == eventSequenceId.Value);

        if (lastEventSequenceNumber is not null)
        {
            query = query.Where(e => e.SequenceNumber <= lastEventSequenceNumber.Value);
        }

        if (eventTypes?.Any() == true)
        {
            var eventTypeIds = eventTypes.Select(et => et.Id.Value).ToArray();
            query = query.Where(e => eventTypeIds.Contains(e.Type));
        }

        return await query.CountAsync();
    }

    /// <inheritdoc/>
    public async Task<Result<AppendedEvent, DuplicateEventSequenceNumber>> Append(
        EventSequenceNumber sequenceNumber,
        EventSourceType eventSourceType,
        EventSourceId eventSourceId,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        EventType eventType,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred,
        ExpandoObject content)
    {
        try
        {
            await using var scope = await database.Namespace(eventStore, @namespace);

            // Check if sequence number already exists
            var existingEvent = await scope.DbContext.EventSequenceEvents
                .FirstOrDefaultAsync(e => e.EventSequenceId == eventSequenceId.Value && e.SequenceNumber == sequenceNumber.Value);

            if (existingEvent is not null)
            {
                return new DuplicateEventSequenceNumber(sequenceNumber);
            }

            var eventEntry = EventEntryConverter.ToEventEntry(
                eventSequenceId.Value,
                sequenceNumber,
                eventSourceType,
                eventSourceId,
                eventStreamType,
                eventStreamId,
                eventType,
                correlationId,
                causation,
                causedByChain,
                occurred,
                content);

            scope.DbContext.EventSequenceEvents.Add(eventEntry);
            await scope.DbContext.SaveChangesAsync();

            // Create AppendedEvent
            var eventContext = new EventContext(
                eventType,
                eventSourceType,
                eventSourceId,
                eventStreamType,
                eventStreamId,
                sequenceNumber,
                occurred,
                eventStore,
                @namespace,
                correlationId,
                causation,
                await identityStorage.GetFor(causedByChain));

            return new AppendedEvent(eventContext, content);
        }
        catch (Exception ex)
        {
            logger.FailedToAppendEvent(ex, sequenceNumber, eventSequenceId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task Compensate(EventSequenceNumber sequenceNumber, EventType eventType, CorrelationId correlationId, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred, ExpandoObject content)
    {
        // This method would implement compensation (undo/reversal) of a specific event
        logger.CompensateNotImplemented();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> Redact(EventSequenceNumber sequenceNumber, RedactionReason reason, CorrelationId correlationId, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var eventEntry = await scope.DbContext.EventSequenceEvents
            .FirstOrDefaultAsync(e =>
                e.EventSequenceId == eventSequenceId.Value &&
                e.SequenceNumber == sequenceNumber.Value)
            ?? throw new InvalidOperationException($"Event with sequence number {sequenceNumber} not found in event sequence {eventSequenceId}");

        // Replace content with redaction marker
        eventEntry.Content = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "redacted", true },
            { "reason", reason.Value },
            { "redactedAt", occurred }
        });
        scope.DbContext.EventSequenceEvents.Update(eventEntry);
        await scope.DbContext.SaveChangesAsync();

        // Return the redacted event
        var eventType = EventEntryConverter.GetEventType(eventEntry);
        var content = EventEntryConverter.GetContentForGeneration(eventEntry, eventType.Generation);
        var eventCausation = EventEntryConverter.GetCausation(eventEntry);
        var eventCausedBy = EventEntryConverter.GetCausedBy(eventEntry);

        var eventMetadata = new EventContext(
            eventType,
            new EventSourceType(eventEntry.EventSourceType),
            new EventSourceId(eventEntry.EventSourceId),
            new EventStreamType(eventEntry.EventStreamType),
            new EventStreamId(eventEntry.EventStreamId),
            new EventSequenceNumber(eventEntry.SequenceNumber),
            eventEntry.Occurred,
            eventStore,
            @namespace,
            new CorrelationId(Guid.Parse(eventEntry.CorrelationId)),
            eventCausation,
            await identityStorage.GetFor(eventCausedBy));

        return new AppendedEvent(eventMetadata, content);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventType>> Redact(EventSourceId eventSourceId, RedactionReason reason, IEnumerable<EventType>? eventTypes, CorrelationId correlationId, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred)
    {
        // This method would redact all events for a given event source
        logger.BulkRedactionNotImplemented();
        return Task.FromResult(Enumerable.Empty<EventType>());
    }

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> GetHeadSequenceNumber(IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var query = scope.DbContext.EventSequenceEvents
            .Where(e => e.EventSequenceId == eventSequenceId.Value);

        if (eventTypes?.Any() == true)
        {
            var eventTypeIds = eventTypes.Select(et => et.Id.Value).ToArray();
            query = query.Where(e => eventTypeIds.Contains(e.Type));
        }

        if (eventSourceId?.IsSpecified == true)
        {
            query = query.Where(e => e.EventSourceId == eventSourceId.Value);
        }

        var firstEvent = await query
            .OrderBy(e => e.SequenceNumber)
            .FirstOrDefaultAsync();

        return firstEvent?.SequenceNumber is null
            ? EventSequenceNumber.Unavailable
            : new EventSequenceNumber(firstEvent.SequenceNumber);
    }

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> GetTailSequenceNumber(
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null,
        EventSourceType? eventSourceType = null,
        EventStreamId? eventStreamId = null,
        EventStreamType? eventStreamType = null)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var query = scope.DbContext.EventSequenceEvents
            .Where(e => e.EventSequenceId == eventSequenceId.Value);

        if (eventTypes?.Any() == true)
        {
            var eventTypeIds = eventTypes.Select(et => et.Id.Value).ToArray();
            query = query.Where(e => eventTypeIds.Contains(e.Type));
        }

        if (eventSourceId?.IsSpecified == true)
        {
            query = query.Where(e => e.EventSourceId == eventSourceId.Value);
        }

        if (eventSourceType?.IsDefaultOrUnspecified == false)
        {
            query = query.Where(e => e.EventSourceType == eventSourceType.Value);
        }

        if (eventStreamId?.IsDefault == false)
        {
            query = query.Where(e => e.EventStreamId == eventStreamId.Value);
        }

        if (eventStreamType?.IsAll == false)
        {
            query = query.Where(e => e.EventStreamType == eventStreamType.Value);
        }

        var lastEvent = await query
            .OrderByDescending(e => e.SequenceNumber)
            .FirstOrDefaultAsync();

        return lastEvent?.SequenceNumber is null
            ? EventSequenceNumber.Unavailable
            : new EventSequenceNumber(lastEvent.SequenceNumber);
    }

    /// <inheritdoc/>
    public Task<TailEventSequenceNumbers> GetTailSequenceNumbers(IEnumerable<EventType> eventTypes)
    {
        // This method is complex to implement efficiently in SQL
        logger.GetTailSequenceNumbersNotImplemented();
        return Task.FromResult(new TailEventSequenceNumbers(
            eventSequenceId,
            eventTypes.ToImmutableList(),
            EventSequenceNumber.Unavailable,
            EventSequenceNumber.Unavailable));
    }

    /// <inheritdoc/>
    public Task<IImmutableDictionary<EventType, EventSequenceNumber>> GetTailSequenceNumbersForEventTypes(IEnumerable<EventType> eventTypes)
    {
        // This method is complex to implement efficiently in SQL
        logger.GetTailSequenceNumbersForEventTypesNotImplemented();
        return Task.FromResult<IImmutableDictionary<EventType, EventSequenceNumber>>(ImmutableDictionary<EventType, EventSequenceNumber>.Empty);
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumberGreaterOrEqualThan(EventSequenceNumber sequenceNumber, IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null)
    {
        // This method is for finding the next event sequence number after a given one
        logger.GetNextSequenceNumberGreaterOrEqualThanNotImplemented();
        return Task.FromResult(EventSequenceNumber.Unavailable);
    }

    /// <inheritdoc/>
    public async Task<bool> HasEventsFor(EventSourceId eventSourceId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        return await scope.DbContext.EventSequenceEvents
            .AnyAsync(e => e.EventSequenceId == eventSequenceId.Value && e.EventSourceId == eventSourceId.Value);
    }

    /// <inheritdoc/>
    public Task<Catch<Option<AppendedEvent>>> TryGetLastEventBefore(EventTypeId eventTypeId, EventSourceId eventSourceId, EventSequenceNumber currentSequenceNumber)
    {
        // This method finds the last event of a specific type for an event source before a given sequence number
        logger.TryGetLastEventBeforeNotImplemented();
        return Task.FromResult<Catch<Option<AppendedEvent>>>(Option<AppendedEvent>.None());
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> GetEventAt(EventSequenceNumber sequenceNumber)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var eventEntry = await scope.DbContext.EventSequenceEvents
            .FirstOrDefaultAsync(e => e.EventSequenceId == eventSequenceId.Value && e.SequenceNumber == sequenceNumber.Value);

        if (eventEntry is null)
        {
            throw new InvalidOperationException($"Event with sequence number {sequenceNumber} not found in event sequence {eventSequenceId}");
        }

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
            eventStore,
            @namespace,
            new CorrelationId(Guid.Parse(eventEntry.CorrelationId)),
            causation,
            await identityStorage.GetFor(causedBy));

        return new AppendedEvent(eventMetadata, content);
    }

    /// <inheritdoc/>
    public Task<Option<AppendedEvent>> TryGetLastInstanceOfAny(EventSourceId eventSourceId, IEnumerable<EventTypeId> eventTypes)
    {
        // This method finds the last instance of any of the specified event types for an event source
        logger.TryGetLastInstanceOfAnyNotImplemented();
        return Task.FromResult(Option<AppendedEvent>.None());
    }

    /// <inheritdoc/>
    public Task<IEventCursor> GetFromSequenceNumber(EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = default, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, IEnumerable<EventType>? eventTypes = default, CancellationToken cancellationToken = default)
    {
        // This method creates a cursor for reading events from a specific sequence number
        logger.GetFromSequenceNumberNotImplemented();
        return Task.FromResult<IEventCursor>(new EmptyEventCursor());
    }

    /// <inheritdoc/>
    public Task<IEventCursor> GetRange(EventSequenceNumber start, EventSequenceNumber end, EventSourceId? eventSourceId = default, IEnumerable<EventType>? eventTypes = default, CancellationToken cancellationToken = default)
    {
        // This method creates a cursor for reading events within a specific range
        logger.GetRangeNotImplemented();
        return Task.FromResult<IEventCursor>(new EmptyEventCursor());
    }
}
