// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
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
/// <param name="eventTypesStorage">The <see cref="IEventTypesStorage"/> for resolving event type schemas.</param>
/// <param name="expandoObjectConverter">The schema-aware <see cref="IExpandoObjectConverter"/>.</param>
/// <param name="logger">The <see cref="ILogger{EventSequenceStorage}"/> for logging.</param>
public class EventSequenceStorage(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    EventSequenceId eventSequenceId,
    IDatabase database,
    IIdentityStorage identityStorage,
    IEventTypesStorage eventTypesStorage,
    IExpandoObjectConverter expandoObjectConverter,
    ILogger<EventSequenceStorage> logger) : IEventSequenceStorage
{
    /// <summary>
    /// Truncate a <see cref="DateTimeOffset"/> to microsecond precision (10 .NET ticks).
    /// </summary>
    /// <param name="value">The value to truncate.</param>
    /// <returns>The truncated <see cref="DateTimeOffset"/> with the same offset.</returns>
    /// <remarks>
    /// PostgreSQL's <c>timestamp</c> stores microsecond precision and drops the lower .NET
    /// tick digit on write. Applying it on append keeps the value the projection observes
    /// equal to what GetForEventSourceIdAndEventTypes returns later, so specs that compare
    /// event.Occurred.Ticks to a projected value do not flake when DateTime.UtcNow happens
    /// to land off a microsecond boundary.
    /// </remarks>
    public static DateTimeOffset TruncateToMicrosecond(DateTimeOffset value) =>
        new(value.Ticks - (value.Ticks % (TimeSpan.TicksPerMillisecond / 1000)), value.Offset);

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

            // Truncate to the precision the database will actually preserve so the value the
            // projection observes (via the returned AppendedEvent) round-trips byte-identical
            // with what GetForEventSourceIdAndEventTypes returns later. PostgreSQL truncates
            // .NET 100ns ticks to microseconds; without this, the read-back drops the lower
            // tick digit while the projection's read-model JSON retains it, and any spec that
            // compares event.Occurred.Ticks to the projected value (e.g. FromEvery setting
            // LastUpdated from the EventContext) flakes whenever DateTime.UtcNow happens to
            // sit off a microsecond boundary. The cap is microsecond because that is the
            // narrowest precision a Chronicle-supported relational provider preserves; SQLite
            // (TEXT) and MSSQL (datetime2) keep more precision but stay correct under the
            // tighter cap.
            occurred = TruncateToMicrosecond(occurred);

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
                contentHashes,
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

            var generationalContent = EventEntryConverter.BuildGenerationalContent(content);
            return new AppendedEvent(eventContext, returnContent) { GenerationalContent = generationalContent };
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
                // Same precision-cap rationale as the single Append path: keep the value the
                // projection observes equal to what GetForEventSourceIdAndEventTypes returns
                // later. See the comment in Append for details.
                var truncatedOccurred = TruncateToMicrosecond(eventToAppend.Occurred);

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
                    truncatedOccurred,
                    eventToAppend.Content,
                    eventToAppend.Hash);

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
                    truncatedOccurred,
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

        // If the event has already been redacted (either by an earlier call or by a racing
        // caller — e.g. the kernel's EventSequencesReactor reacting to the same
        // EventRedactionRequested system event), return the AppendedEvent with EventType ==
        // Redaction so the upstream caller in EventSequence.Redact can skip the duplicate
        // RewindPartitionForAffectedObservers. Doing the duplicate rewind would replay any
        // observer subscribed to EventRedacted for a redaction that was already applied and
        // already triggered its own rewind, producing duplicate EventRedacted notifications.
        if (eventEntry.Type == GlobalEventTypes.Redaction)
        {
            return await BuildAppendedEventFromRedactionEntry(eventEntry);
        }

        // Capture the original event type BEFORE we overwrite it. The kernel uses the
        // returned AppendedEvent's event type to find which observers must replay the
        // affected partition (RewindPartitionForAffectedObservers), so it must be the
        // pre-redaction type — observers subscribe to the original type, not the synthetic
        // Redaction marker that replaces it in storage. This matches the MongoDB behavior.
        var originalEventType = EventEntryConverter.GetEventType(eventEntry);
        var redactionContent = EventEntryConverter.CreateRedactionContent(originalEventType.Id.Value, reason, correlationId, causation, causedByChain, occurred);

        eventEntry.Type = GlobalEventTypes.Redaction;
        eventEntry.Occurred = occurred;
        eventEntry.CorrelationId = correlationId.ToString();
        eventEntry.Causation = EventEntryConverter.SerializeCausation(causation);
        eventEntry.CausedBy = EventEntryConverter.SerializeCausedBy(causedByChain);
        eventEntry.Content = redactionContent;

        scope.DbContext.Events.Update(eventEntry);
        await scope.DbContext.SaveChangesAsync();

        // Return the AppendedEvent with the ORIGINAL event type so the kernel can route
        // the replay to the right observers. Content is the now-stored redaction marker,
        // but metadata carries the original type for routing.
        var content = await ResolveContent(eventEntry, originalEventType);
        var eventCausation = EventEntryConverter.GetCausation(eventEntry);
        var eventCausedBy = EventEntryConverter.GetCausedBy(eventEntry);

        var eventMetadata = new EventContext(
            originalEventType,
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
            EventEntryConverter.GetHashForGeneration(eventEntry, originalEventType.Generation),
            Subject: EventEntryConverter.ResolveSubject(eventEntry));

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
            if (eventEntry.Type == GlobalEventTypes.Redaction)
            {
                continue;
            }

            var originalEventTypeId = eventEntry.Type.Value;
            var originalEventType = EventEntryConverter.GetEventType(eventEntry);
            affectedEventTypes.Add(originalEventType);

            var redactionContent = EventEntryConverter.CreateRedactionContent(originalEventTypeId, reason, correlationId, causation, causedByChain, occurred);

            eventEntry.Type = GlobalEventTypes.Redaction;
            eventEntry.Occurred = occurred;
            eventEntry.CorrelationId = correlationId.ToString();
            eventEntry.Causation = EventEntryConverter.SerializeCausation(causation);
            eventEntry.CausedBy = EventEntryConverter.SerializeCausedBy(causedByChain);
            eventEntry.Content = redactionContent;

            scope.DbContext.Events.Update(eventEntry);
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
                eventStore,
                @namespace,
                new CorrelationId(Guid.Parse(eventEntry.CorrelationId)),
                causation,
                await identityStorage.GetFor(causedBy),
                [],
                EventEntryConverter.GetHashForGeneration(eventEntry, eventType.Generation),
                Subject: EventEntryConverter.ResolveSubject(eventEntry));

            var generationalContent = EventEntryConverter.GetAllGenerationalContent(eventEntry);
            var appendedEvent = new AppendedEvent(eventMetadata, content) { GenerationalContent = generationalContent };
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
            eventStore,
            @namespace,
            new CorrelationId(Guid.Parse(eventEntry.CorrelationId)),
            causation,
            await identityStorage.GetFor(causedBy),
            [],
            EventEntryConverter.GetHashForGeneration(eventEntry, eventType.Generation),
            Subject: EventEntryConverter.ResolveSubject(eventEntry));

        var generationalContentAt = EventEntryConverter.GetAllGenerationalContent(eventEntry);
        return new AppendedEvent(eventMetadata, content) { GenerationalContent = generationalContentAt };
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
            eventStore,
            @namespace,
            new CorrelationId(Guid.Parse(eventEntry.CorrelationId)),
            causation,
            await identityStorage.GetFor(causedBy),
            [],
            EventEntryConverter.GetHashForGeneration(eventEntry, eventType.Generation),
            Subject: EventEntryConverter.ResolveSubject(eventEntry));

        var generationalContentLast = EventEntryConverter.GetAllGenerationalContent(eventEntry);
        return new AppendedEvent(eventMetadata, content) { GenerationalContent = generationalContentLast };
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

        return new EventCursor(query, scope, eventStore, @namespace, identityStorage, eventTypesStorage, expandoObjectConverter, 100, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEventCursor> GetRange(EventSequenceNumber start, EventSequenceNumber end, EventSourceId? eventSourceId = default, IEnumerable<EventType>? eventTypes = default, CancellationToken cancellationToken = default)
    {
        var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var startValue = start.Value;
        var query = scope.DbContext.Events.Where(e => e.SequenceNumber >= startValue);

        // EventSequenceNumber.Max (ulong.MaxValue - 1) means "no upper bound". The SQL column
        // is BIGINT (signed int64) so the ulong value wraps to -2 when EF binds the parameter,
        // and the original Where clause matched zero rows. Skip the upper-bound predicate
        // entirely when the caller signals "unbounded" by passing Max.
        if (end != EventSequenceNumber.Max)
        {
            var endValue = end.Value;
            query = query.Where(e => e.SequenceNumber <= endValue);
        }

        if (eventSourceId?.IsSpecified == true)
        {
            query = query.Where(e => e.EventSourceId == eventSourceId);
        }

        if (eventTypes?.Any() == true)
        {
            var eventTypeIds = eventTypes.Select(et => et.Id).ToArray();
            query = query.Where(e => eventTypeIds.Contains(e.Type));
        }

        return new EventCursor(query, scope, eventStore, @namespace, identityStorage, eventTypesStorage, expandoObjectConverter, 100, cancellationToken);
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

        return new EventCursor(query, scope, eventStore, @namespace, identityStorage, eventTypesStorage, expandoObjectConverter, 100, cancellationToken);
    }

    /// <summary>
    /// Resolves the content of an event using schema-aware deserialization when an event type schema is registered.
    /// This is required so that schema-typed values such as <c>ConceptAs&lt;Guid&gt;</c> read back as <see cref="Guid"/>
    /// rather than the raw JSON string — without it, identity comparisons in the projection engine (key resolvers,
    /// deferred-future resolution, array-indexer matching) silently mismatch when one side is a string and the
    /// other is a Guid. Redaction events and event types without a registered schema fall back to raw deserialization.
    /// </summary>
    /// <param name="entry">The <see cref="EventEntry"/> to read content from.</param>
    /// <param name="eventType">The <see cref="EventType"/> identifying the schema to apply.</param>
    /// <returns>The deserialized content as an <see cref="ExpandoObject"/>.</returns>
    async Task<ExpandoObject> ResolveContent(EventEntry entry, EventType eventType)
    {
        if (eventType.Id == GlobalEventTypes.Redaction)
        {
            return EventEntryConverter.GetContentForGeneration(entry, eventType.Generation);
        }

        if (!await eventTypesStorage.HasFor(eventType.Id, eventType.Generation))
        {
            return EventEntryConverter.GetContentForGeneration(entry, eventType.Generation);
        }

        var jsonObject = EventEntryConverter.GetContentJsonForGeneration(entry, eventType.Generation);
        if (jsonObject is null)
        {
            return new ExpandoObject();
        }

        var schema = await eventTypesStorage.GetFor(eventType.Id, eventType.Generation);
        return expandoObjectConverter.ToExpandoObject(jsonObject, schema.Schema);
    }

    async Task<AppendedEvent> BuildAppendedEventFromRedactionEntry(EventEntry redactionEntry)
    {
        var redactionEventType = EventEntryConverter.GetEventType(redactionEntry);
        var content = await ResolveContent(redactionEntry, redactionEventType);
        var eventCausation = EventEntryConverter.GetCausation(redactionEntry);
        var eventCausedBy = EventEntryConverter.GetCausedBy(redactionEntry);

        var eventMetadata = new EventContext(
            redactionEventType,
            redactionEntry.EventSourceType,
            redactionEntry.EventSourceId,
            redactionEntry.EventStreamType,
            redactionEntry.EventStreamId,
            new EventSequenceNumber(redactionEntry.SequenceNumber),
            redactionEntry.Occurred,
            eventStore,
            @namespace,
            new CorrelationId(Guid.Parse(redactionEntry.CorrelationId)),
            eventCausation,
            await identityStorage.GetFor(eventCausedBy),
            [],
            EventEntryConverter.GetHashForGeneration(redactionEntry, redactionEventType.Generation),
            Subject: EventEntryConverter.ResolveSubject(redactionEntry));

        return new AppendedEvent(eventMetadata, content);
    }
}
