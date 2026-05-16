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
    public Task EnsureIndexes() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task<Chronicle.Storage.EventSequences.EventSequenceState> GetState()
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var entry = await scope.DbContext.EventSequences.FirstOrDefaultAsync(s => s.EventSequenceId == eventSequenceId.Value);
        if (entry is null)
        {
            return new Chronicle.Storage.EventSequences.EventSequenceState();
        }

        return EventSequenceStateConverter.ToEventSequenceState(entry);
    }

    /// <inheritdoc/>
    public async Task SaveState(Chronicle.Storage.EventSequences.EventSequenceState state)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var entry = EventSequenceStateConverter.ToEventSequenceStateEntry(state, eventSequenceId);
        await scope.DbContext.EventSequences.Upsert(entry);
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<EventCount> GetCount(EventSequenceNumber? lastEventSequenceNumber = null, IEnumerable<EventType>? eventTypes = null)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var query = scope.DbContext.Events.AsQueryable();
        if (lastEventSequenceNumber is not null)
        {
            var lastSeqNumValue = lastEventSequenceNumber.Value;
            query = query.Where(e => e.SequenceNumber <= lastSeqNumValue);
        }

        if (eventTypes?.Any() == true)
        {
            var eventTypeIds = eventTypes.Select(et => et.Id).ToArray();
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
        IEnumerable<Tag> tags,
        DateTimeOffset occurred,
        IDictionary<EventTypeGeneration, ExpandoObject> content,
        IDictionary<EventTypeGeneration, EventHash> contentHashes,
        Subject? subject = null)
    {
        try
        {
            await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

            var seqNumValue = sequenceNumber.Value;
            var existingEvent = await scope.DbContext.Events
                .FirstOrDefaultAsync(e => e.SequenceNumber == seqNumValue);

            if (existingEvent is not null)
            {
                return new DuplicateEventSequenceNumber(sequenceNumber);
            }

            var eventEntry = EventEntryConverter.ToEventEntry(
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
                content,
                subject?.IsSet == true ? subject : null);

            scope.DbContext.Events.Add(eventEntry);
            await scope.DbContext.SaveChangesAsync();

            var returnContent = content.TryGetValue(eventType.Generation, out var value) ? value : content.Values.FirstOrDefault() ?? new ExpandoObject();

            var eventHash = contentHashes.TryGetValue(eventType.Generation, out var hash) ? hash : EventHash.NotSet;

            var resolvedSubject = subject?.IsSet == true ? subject : new Subject(eventSourceId.Value);
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
                await identityStorage.GetFor(causedByChain),
                tags,
                eventHash,
                Subject: resolvedSubject);

            return new AppendedEvent(eventContext, returnContent);
        }
        catch (Exception ex)
        {
            logger.FailedToAppendEvent(ex, sequenceNumber, eventSequenceId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task Revise(
        EventSequenceNumber sequenceNumber,
        EventType eventType,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred,
        ExpandoObject content,
        EventHash hash)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var seqNumRevise = sequenceNumber.Value;
        var eventEntry = await scope.DbContext.Events.FirstOrDefaultAsync(e => e.SequenceNumber == seqNumRevise);
        if (eventEntry is null)
        {
            return;
        }

        EventEntryConverter.UpdateContentForGeneration(eventEntry, eventType.Generation, content);
        scope.DbContext.Events.Update(eventEntry);
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task ReplaceGenerationContent(EventSequenceNumber sequenceNumber, IDictionary<EventTypeGeneration, ExpandoObject> content)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var seqNumReplace = sequenceNumber.Value;
        var eventEntry = await scope.DbContext.Events.FirstAsync(e => e.SequenceNumber == seqNumReplace);
        EventEntryConverter.ReplaceAllGenerationContent(eventEntry, content);
        scope.DbContext.Events.Update(eventEntry);
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>> AppendMany(IEnumerable<EventToAppendToStorage> events)
    {
        var eventsArray = events.ToArray();
        if (eventsArray.Length == 0)
        {
            return Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success([]);
        }

        try
        {
            await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);
            var appendedEvents = new List<AppendedEvent>();

            foreach (var eventToAppend in eventsArray)
            {
                // Check if sequence number already exists
                var appendManySeqNum = eventToAppend.SequenceNumber.Value;
                var existingEvent = await scope.DbContext.Events
                    .FirstOrDefaultAsync(e => e.SequenceNumber == appendManySeqNum);

                if (existingEvent is not null)
                {
                    return new DuplicateEventSequenceNumber(eventToAppend.SequenceNumber);
                }

                var eventEntry = EventEntryConverter.ToEventEntry(
                    eventToAppend.SequenceNumber,
                    eventToAppend.EventSourceType,
                    eventToAppend.EventSourceId,
                    eventToAppend.EventStreamType,
                    eventToAppend.EventStreamId,
                    eventToAppend.EventType,
                    eventToAppend.CorrelationId,
                    eventToAppend.Causation,
                    eventToAppend.CausedByChain,
                    eventToAppend.Occurred,
                    eventToAppend.Content);

                scope.DbContext.Events.Add(eventEntry);

                var resolvedSubject = eventToAppend.Subject?.IsSet == true
                    ? eventToAppend.Subject
                    : new Subject(eventToAppend.EventSourceId.Value);
                var eventContext = new EventContext(
                    eventToAppend.EventType,
                    eventToAppend.EventSourceType,
                    eventToAppend.EventSourceId,
                    eventToAppend.EventStreamType,
                    eventToAppend.EventStreamId,
                    eventToAppend.SequenceNumber,
                    eventToAppend.Occurred,
                    eventStore,
                    @namespace,
                    eventToAppend.CorrelationId,
                    eventToAppend.Causation,
                    await identityStorage.GetFor(eventToAppend.CausedByChain),
                    eventToAppend.Tags,
                    eventToAppend.Hash,
                    Subject: resolvedSubject);

                appendedEvents.Add(new AppendedEvent(eventContext, eventToAppend.Content));
            }

            await scope.DbContext.SaveChangesAsync();
            return Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(appendedEvents);
        }
        catch (Exception ex)
        {
            logger.FailedToAppendEvent(ex, EventSequenceNumber.Unavailable, eventSequenceId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> Redact(EventSequenceNumber sequenceNumber, RedactionReason reason, CorrelationId correlationId, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var seqNumRedact = sequenceNumber.Value;
        var eventEntry = await scope.DbContext.Events.FirstAsync(e => e.SequenceNumber == seqNumRedact);

        // Replace content with redaction marker
        eventEntry.Content = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "redacted", true },
            { "reason", reason },
            { "redactedAt", occurred }
        });
        scope.DbContext.Events.Update(eventEntry);
        await scope.DbContext.SaveChangesAsync();

        // Return the redacted event
        var eventType = EventEntryConverter.GetEventType(eventEntry);
        var content = EventEntryConverter.GetContentForGeneration(eventEntry, eventType.Generation);
        var eventCausation = EventEntryConverter.GetCausation(eventEntry);
        var eventCausedBy = EventEntryConverter.GetCausedBy(eventEntry);

        var eventMetadata = new EventContext(
            eventType,
            eventEntry.EventSourceType,
            eventEntry.EventSourceId,
            eventEntry.EventStreamType,
            eventEntry.EventStreamId,
            new EventSequenceNumber(eventEntry.SequenceNumber),
            eventEntry.Occurred,
            eventStore,
            @namespace,
            new CorrelationId(Guid.Parse(eventEntry.CorrelationId)),
            eventCausation,
            await identityStorage.GetFor(eventCausedBy),
            [],
            EventHash.NotSet);

        return new AppendedEvent(eventMetadata, content);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventType>> Redact(EventSourceId eventSourceId, RedactionReason reason, IEnumerable<EventType>? eventTypes, CorrelationId correlationId, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var query = scope.DbContext.Events.Where(e => e.EventSourceId == eventSourceId);

        if (eventTypes?.Any() == true)
        {
            var eventTypeIds = eventTypes.Select(et => et.Id).ToArray();
            query = query.Where(e => eventTypeIds.Contains(e.Type));
        }

        var eventsToRedact = await query.ToListAsync();
        var affectedEventTypes = new HashSet<EventType>();

        foreach (var eventEntry in eventsToRedact)
        {
            // Replace content with redaction marker
            eventEntry.Content = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "redacted", true },
                { "reason", reason },
                { "redactedAt", occurred }
            });
            scope.DbContext.Events.Update(eventEntry);

            var eventType = EventEntryConverter.GetEventType(eventEntry);
            affectedEventTypes.Add(eventType);
        }

        await scope.DbContext.SaveChangesAsync();
        return affectedEventTypes;
    }

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> GetHeadSequenceNumber(IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var query = scope.DbContext.Events.AsQueryable();
        if (eventTypes?.Any() == true)
        {
            var eventTypeIds = eventTypes.Select(et => et.Id).ToArray();
            query = query.Where(e => eventTypeIds.Contains(e.Type));
        }

        if (eventSourceId?.IsSpecified == true)
        {
            query = query.Where(e => e.EventSourceId == eventSourceId);
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
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var query = scope.DbContext.Events.AsQueryable();

        if (eventTypes?.Any() == true)
        {
            var eventTypeIds = eventTypes.Select(et => et.Id).ToArray();
            query = query.Where(e => eventTypeIds.Contains(e.Type));
        }

        if (eventSourceId?.IsSpecified == true)
        {
            query = query.Where(e => e.EventSourceId == eventSourceId);
        }

        if (eventSourceType?.IsDefaultOrUnspecified == false)
        {
            query = query.Where(e => e.EventSourceType == eventSourceType);
        }

        if (eventStreamId?.IsDefault == false)
        {
            query = query.Where(e => e.EventStreamId == eventStreamId);
        }

        if (eventStreamType?.IsAll == false)
        {
            query = query.Where(e => e.EventStreamType == eventStreamType);
        }

        var lastEvent = await query
            .OrderByDescending(e => e.SequenceNumber)
            .FirstOrDefaultAsync();

        return lastEvent?.SequenceNumber is null
            ? EventSequenceNumber.Unavailable
            : new EventSequenceNumber(lastEvent.SequenceNumber);
    }

    /// <inheritdoc/>
    public async Task<TailEventSequenceNumbers> GetTailSequenceNumbers(IEnumerable<EventType> eventTypes)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        // Get the overall tail (lowest sequence number in the sequence)
        var overallTail = await scope.DbContext.Events
            .MaxAsync(e => (ulong?)e.SequenceNumber);

        var overallTailSequenceNumber = overallTail.HasValue
            ? new EventSequenceNumber(overallTail.Value)
            : EventSequenceNumber.Unavailable;

        // Get the tail for the specified event types
        var eventTypeIds = eventTypes.Select(et => et.Id).ToArray();
        var tailForEventTypes = await scope.DbContext.Events
            .Where(e => eventTypeIds.Contains(e.Type))
            .MaxAsync(e => (ulong?)e.SequenceNumber);

        var tailForEventTypesSequenceNumber = tailForEventTypes.HasValue
            ? new EventSequenceNumber(tailForEventTypes.Value)
            : EventSequenceNumber.Unavailable;

        return new TailEventSequenceNumbers(
            eventSequenceId,
            eventTypes.ToImmutableList(),
            overallTailSequenceNumber,
            tailForEventTypesSequenceNumber);
    }

    /// <inheritdoc/>
    public async Task<IImmutableDictionary<EventType, EventSequenceNumber>> GetTailSequenceNumbersForEventTypes(IEnumerable<EventType> eventTypes)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var eventTypeIds = eventTypes.Select(et => et.Id).ToArray();
        var eventTypeList = eventTypes.ToList();

        // Query to get the max sequence number for each event type
        var tailSequenceNumbers = await scope.DbContext.Events
            .Where(e => eventTypeIds.Contains(e.Type))
            .GroupBy(e => e.Type)
            .Select(g => new { EventTypeId = g.Key, MaxSequenceNumber = g.Max(e => e.SequenceNumber) })
            .ToListAsync();

        var result = new Dictionary<EventType, EventSequenceNumber>();

        foreach (var eventType in eventTypeList)
        {
            var tailEntry = tailSequenceNumbers.FirstOrDefault(t => t.EventTypeId == eventType.Id);
            result[eventType] = tailEntry is not null
                ? new EventSequenceNumber(tailEntry.MaxSequenceNumber)
                : EventSequenceNumber.Unavailable;
        }

        return result.ToImmutableDictionary();
    }

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> GetNextSequenceNumberGreaterOrEqualThan(EventSequenceNumber sequenceNumber, IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var nextSeqNumValue = sequenceNumber.Value;
        var query = scope.DbContext.Events.Where(e => e.SequenceNumber >= nextSeqNumValue);

        if (eventTypes?.Any() == true)
        {
            var eventTypeIds = eventTypes.Select(et => et.Id).ToArray();
            query = query.Where(e => eventTypeIds.Contains(e.Type));
        }

        if (eventSourceId?.IsSpecified == true)
        {
            query = query.Where(e => e.EventSourceId == eventSourceId);
        }

        var nextEvent = await query
            .OrderBy(e => e.SequenceNumber)
            .FirstOrDefaultAsync();

        return nextEvent?.SequenceNumber is null
            ? EventSequenceNumber.Unavailable
            : new EventSequenceNumber(nextEvent.SequenceNumber);
    }

    /// <inheritdoc/>
    public async Task<bool> HasEventsFor(EventSourceId eventSourceId)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        return await scope.DbContext.Events.AnyAsync(e => e.EventSourceId == eventSourceId);
    }

    /// <inheritdoc/>
    public async Task<Catch<Option<AppendedEvent>>> TryGetLastEventBefore(EventTypeId eventTypeId, EventSourceId eventSourceId, EventSequenceNumber currentSequenceNumber)
    {
        try
        {
            await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

            var currentSequenceNumberValue = currentSequenceNumber.Value;
            var eventEntry = await scope.DbContext.Events
                .Where(e =>
                    e.Type == eventTypeId
                    && e.EventSourceId == eventSourceId
                    && e.SequenceNumber < currentSequenceNumberValue)
                .OrderByDescending(e => e.SequenceNumber)
                .FirstOrDefaultAsync();

            if (eventEntry is null)
            {
                return Option<AppendedEvent>.None();
            }

            var eventType = EventEntryConverter.GetEventType(eventEntry);
            var content = EventEntryConverter.GetContentForGeneration(eventEntry, eventType.Generation);
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
                eventStore,
                @namespace,
                new CorrelationId(Guid.Parse(eventEntry.CorrelationId)),
                causation,
                await identityStorage.GetFor(causedBy),
                [],
                EventHash.NotSet);

            var appendedEvent = new AppendedEvent(eventMetadata, content);
            return (Option<AppendedEvent>)appendedEvent;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> GetEventAt(EventSequenceNumber sequenceNumber)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var seqNumAt = sequenceNumber.Value;
        var eventEntry = await scope.DbContext.Events
            .FirstOrDefaultAsync(e => e.SequenceNumber == seqNumAt)
            ?? throw new InvalidOperationException($"Event with sequence number {sequenceNumber} not found in event sequence {eventSequenceId}");

        var eventType = EventEntryConverter.GetEventType(eventEntry);
        var content = EventEntryConverter.GetContentForGeneration(eventEntry, eventType.Generation);
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
            eventStore,
            @namespace,
            new CorrelationId(Guid.Parse(eventEntry.CorrelationId)),
            causation,
            await identityStorage.GetFor(causedBy),
            [],
            EventHash.NotSet);

        return new AppendedEvent(eventMetadata, content);
    }

    /// <inheritdoc/>
    public async Task<Option<AppendedEvent>> TryGetLastInstanceOfAny(EventSourceId eventSourceId, IEnumerable<EventTypeId> eventTypes)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var eventTypeIds = eventTypes.ToArray();

        var eventEntry = await scope.DbContext.Events
            .Where(e =>
                e.EventSourceId == eventSourceId
                && eventTypeIds.Contains(e.Type))
            .OrderByDescending(e => e.SequenceNumber)
            .FirstOrDefaultAsync();

        if (eventEntry is null)
        {
            return Option<AppendedEvent>.None();
        }

        var eventType = EventEntryConverter.GetEventType(eventEntry);
        var content = EventEntryConverter.GetContentForGeneration(eventEntry, eventType.Generation);
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
            eventStore,
            @namespace,
            new CorrelationId(Guid.Parse(eventEntry.CorrelationId)),
            causation,
            await identityStorage.GetFor(causedBy),
            [],
            EventHash.NotSet);

        return new AppendedEvent(eventMetadata, content);
    }

    /// <inheritdoc/>
    public async Task<IEventCursor> GetFromSequenceNumber(EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = default, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, IEnumerable<EventType>? eventTypes = default, CancellationToken cancellationToken = default)
    {
        var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var fromSeqNumValue = sequenceNumber.Value;
        var query = scope.DbContext.Events.Where(e => e.SequenceNumber >= fromSeqNumValue);

        if (eventSourceId?.IsSpecified == true)
        {
            query = query.Where(e => e.EventSourceId == eventSourceId);
        }

        if (eventStreamType?.IsAll == false)
        {
            query = query.Where(e => e.EventStreamType == eventStreamType);
        }

        if (eventStreamId?.IsDefault == false)
        {
            query = query.Where(e => e.EventStreamId == eventStreamId);
        }

        if (eventTypes?.Any() == true)
        {
            var eventTypeIds = eventTypes.Select(et => et.Id).ToArray();
            query = query.Where(e => eventTypeIds.Contains(e.Type));
        }

        return new EventCursor(query, scope, eventStore, @namespace, identityStorage, 100, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEventCursor> GetRange(EventSequenceNumber start, EventSequenceNumber end, EventSourceId? eventSourceId = default, IEnumerable<EventType>? eventTypes = default, CancellationToken cancellationToken = default)
    {
        var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var startValue = start.Value;
        var endValue = end.Value;
        var query = scope.DbContext.Events
            .Where(e =>
                e.SequenceNumber >= startValue
                && e.SequenceNumber <= endValue);

        if (eventSourceId?.IsSpecified == true)
        {
            query = query.Where(e => e.EventSourceId == eventSourceId);
        }

        if (eventTypes?.Any() == true)
        {
            var eventTypeIds = eventTypes.Select(et => et.Id).ToArray();
            query = query.Where(e => eventTypeIds.Contains(e.Type));
        }

        return new EventCursor(query, scope, eventStore, @namespace, identityStorage, 100, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEventCursor> GetEventsWithLimit(
        EventSequenceNumber start,
        int limit,
        EventSourceId? eventSourceId = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? eventTypes = default,
        CancellationToken cancellationToken = default)
    {
        var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var limitStartValue = start.Value;
        var query = scope.DbContext.Events.Where(e => e.SequenceNumber >= limitStartValue);

        if (eventSourceId?.IsSpecified == true)
        {
            query = query.Where(e => e.EventSourceId == eventSourceId);
        }

        if (eventStreamType?.IsAll == false)
        {
            query = query.Where(e => e.EventStreamType == eventStreamType);
        }

        if (eventStreamId?.IsDefault == false)
        {
            query = query.Where(e => e.EventStreamId == eventStreamId);
        }

        if (eventTypes?.Any() == true)
        {
            var eventTypeIds = eventTypes.Select(et => et.Id).ToArray();
            query = query.Where(e => eventTypeIds.Contains(e.Type));
        }

        query = query.Take(limit);

        return new EventCursor(query, scope, eventStore, @namespace, identityStorage, 100, cancellationToken);
    }
}
