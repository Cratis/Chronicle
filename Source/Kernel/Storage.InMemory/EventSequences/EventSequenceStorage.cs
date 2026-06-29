// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventSequenceStorage"/> for the
/// kernel event sequence.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the storage serves.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the storage serves.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> this storage serves.</param>
public class EventSequenceStorage(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    EventSequenceId eventSequenceId) : IEventSequenceStorage
{
    readonly List<AppendedEvent> _events = [];
    readonly object _lock = new();

    /// <summary>
    /// Gets the events stored in this storage instance.
    /// </summary>
    public IReadOnlyList<AppendedEvent> Events
    {
        get
        {
            lock (_lock)
            {
                return _events.ToImmutableList();
            }
        }
    }

    /// <inheritdoc/>
    public Task EnsureIndexes() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<EventSequenceState> GetState()
    {
        lock (_lock)
        {
            return Task.FromResult(new EventSequenceState
            {
                SequenceNumber = _events.Count == 0
                    ? 0UL
                    : _events.Max(_ => _.Context.SequenceNumber.Value) + 1
            });
        }
    }

    /// <inheritdoc/>
    public Task SaveState(EventSequenceState state) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<EventCount> GetCount(
        EventSequenceNumber? lastEventSequenceNumber = null,
        IEnumerable<EventType>? eventTypes = null)
    {
        var snapshot = Events;
        var filtered = Filter(snapshot, null, null, null, null, eventTypes);
        if (lastEventSequenceNumber is not null)
        {
            filtered = filtered.Where(_ => _.Context.SequenceNumber <= lastEventSequenceNumber);
        }

        return Task.FromResult((EventCount)filtered.Count());
    }

    /// <inheritdoc/>
    public Task<Result<AppendedEvent, DuplicateEventSequenceNumber>> Append(
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
        lock (_lock)
        {
            if (_events.Exists(_ => _.Context.SequenceNumber == sequenceNumber))
            {
                var nextAvailable = (EventSequenceNumber)(_events.Max(_ => _.Context.SequenceNumber.Value) + 1);
                return Task.FromResult(Result<AppendedEvent, DuplicateEventSequenceNumber>.Failed(new DuplicateEventSequenceNumber(nextAvailable)));
            }

            var appended = BuildAppendedEvent(sequenceNumber, eventSourceType, eventSourceId, eventStreamType, eventStreamId, eventType, correlationId, causation, tags, occurred, content, subject);
            _events.Add(appended);

            return Task.FromResult(Result<AppendedEvent, DuplicateEventSequenceNumber>.Success(appended));
        }
    }

    /// <inheritdoc/>
    public Task<Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>> AppendMany(
        IEnumerable<EventToAppendToStorage> events)
    {
        var appended = new List<AppendedEvent>();

        lock (_lock)
        {
            foreach (var e in events)
            {
                if (_events.Exists(_ => _.Context.SequenceNumber == e.SequenceNumber))
                {
                    var nextAvailable = (EventSequenceNumber)(_events.Max(_ => _.Context.SequenceNumber.Value) + 1);
                    return Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Failed(new DuplicateEventSequenceNumber(nextAvailable)));
                }

                var content = new Dictionary<EventTypeGeneration, ExpandoObject>
                {
                    { EventTypeGeneration.First, e.Content }
                };

                var appendedEvent = BuildAppendedEvent(
                    e.SequenceNumber,
                    e.EventSourceType,
                    e.EventSourceId,
                    e.EventStreamType,
                    e.EventStreamId,
                    e.EventType,
                    e.CorrelationId,
                    e.Causation,
                    e.Tags,
                    e.Occurred,
                    content,
                    e.Subject);

                _events.Add(appendedEvent);
                appended.Add(appendedEvent);
            }
        }

        return Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(appended));
    }

    /// <inheritdoc/>
    public Task Revise(
        EventSequenceNumber sequenceNumber,
        EventType eventType,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred,
        ExpandoObject content,
        EventHash hash) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<AppendedEvent> Redact(
        EventSequenceNumber sequenceNumber,
        RedactionReason reason,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred) =>
        Task.FromResult(Events.FirstOrDefault(_ => _.Context.SequenceNumber == sequenceNumber)
            ?? throw new InvalidOperationException($"No event at sequence number {sequenceNumber}"));

    /// <inheritdoc/>
    public Task<IEnumerable<EventType>> Redact(
        EventSourceId eventSourceId,
        RedactionReason reason,
        IEnumerable<EventType>? eventTypes,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred) =>
        Task.FromResult(Enumerable.Empty<EventType>());

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetHeadSequenceNumber(
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null)
    {
        var filtered = Filter(Events, eventSourceId, null, null, null, eventTypes).ToList();
        return filtered.Count == 0
            ? Task.FromResult(EventSequenceNumber.Unavailable)
            : Task.FromResult(filtered.Min(_ => _.Context.SequenceNumber)!);
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber(
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null,
        EventSourceType? eventSourceType = null,
        EventStreamId? eventStreamId = null,
        EventStreamType? eventStreamType = null)
    {
        var filtered = Filter(Events, eventSourceId, eventSourceType, eventStreamType, eventStreamId, eventTypes).ToList();
        return filtered.Count == 0
            ? Task.FromResult(EventSequenceNumber.Unavailable)
            : Task.FromResult(filtered.Max(_ => _.Context.SequenceNumber)!);
    }

    /// <inheritdoc/>
    public Task<TailEventSequenceNumbers> GetTailSequenceNumbers(IEnumerable<EventType> eventTypes)
    {
        var snapshot = Events;
        var eventTypeList = eventTypes.ToImmutableList();
        var tail = snapshot.Count == 0 ? EventSequenceNumber.Unavailable : snapshot.Max(_ => _.Context.SequenceNumber)!;
        var filtered = Filter(snapshot, null, null, null, null, eventTypeList).ToList();
        var tailForEventTypes = filtered.Count == 0 ? EventSequenceNumber.Unavailable : filtered.Max(_ => _.Context.SequenceNumber)!;
        return Task.FromResult(new TailEventSequenceNumbers(eventSequenceId, eventTypeList, tail, tailForEventTypes));
    }

    /// <inheritdoc/>
    public Task<IImmutableDictionary<EventType, EventSequenceNumber>> GetTailSequenceNumbersForEventTypes(
        IEnumerable<EventType> eventTypes)
    {
        var snapshot = Events;
        var result = eventTypes.ToImmutableDictionary(
            et => et,
            et =>
            {
                var matching = snapshot.Where(_ => _.Context.EventType == et).ToList();
                return matching.Count == 0
                    ? EventSequenceNumber.Unavailable
                    : matching.Max(_ => _.Context.SequenceNumber)!;
            });

        return Task.FromResult<IImmutableDictionary<EventType, EventSequenceNumber>>(result);
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumberGreaterOrEqualThan(
        EventSequenceNumber sequenceNumber,
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null)
    {
        var filtered = Filter(Events, eventSourceId, null, null, null, eventTypes)
            .Where(_ => _.Context.SequenceNumber >= sequenceNumber)
            .MinBy(_ => _.Context.SequenceNumber);

        return Task.FromResult(filtered?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable);
    }

    /// <inheritdoc/>
    public Task<bool> HasEventsFor(EventSourceId eventSourceId) =>
        Task.FromResult(Events.Any(_ => _.Context.EventSourceId == eventSourceId));

    /// <inheritdoc/>
    public Task<Catch<Option<AppendedEvent>>> TryGetLastEventBefore(
        EventTypeId eventTypeId,
        EventSourceId eventSourceId,
        EventSequenceNumber currentSequenceNumber)
    {
        var found = Events
            .Where(_ =>
                _.Context.EventType.Id == eventTypeId &&
                _.Context.EventSourceId == eventSourceId &&
                _.Context.SequenceNumber < currentSequenceNumber)
            .MaxBy(_ => _.Context.SequenceNumber);

        return Task.FromResult<Catch<Option<AppendedEvent>>>(
            found is not null
                ? (Option<AppendedEvent>)found
                : Option<AppendedEvent>.None());
    }

    /// <inheritdoc/>
    public Task<AppendedEvent> GetEventAt(EventSequenceNumber sequenceNumber) =>
        Task.FromResult(Events.FirstOrDefault(_ => _.Context.SequenceNumber == sequenceNumber)
            ?? throw new InvalidOperationException($"No event at sequence number {sequenceNumber}"));

    /// <inheritdoc/>
    public Task<Option<AppendedEvent>> TryGetLastInstanceOfAny(
        EventSourceId eventSourceId,
        IEnumerable<EventTypeId> eventTypes)
    {
        var eventTypeSet = eventTypes.ToHashSet();
        var found = Events
            .Where(_ =>
                _.Context.EventSourceId == eventSourceId &&
                eventTypeSet.Contains(_.Context.EventType.Id))
            .MaxBy(_ => _.Context.SequenceNumber);

        return Task.FromResult(found is not null
            ? (Option<AppendedEvent>)found
            : Option<AppendedEvent>.None());
    }

    /// <inheritdoc/>
    public Task<IEventCursor> GetFromSequenceNumber(
        EventSequenceNumber sequenceNumber,
        EventSourceId? eventSourceId = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? eventTypes = default,
        CancellationToken cancellationToken = default)
    {
        var filtered = Filter(Events, eventSourceId, null, eventStreamType, eventStreamId, eventTypes)
            .Where(_ => _.Context.SequenceNumber >= sequenceNumber)
            .OrderBy(_ => _.Context.SequenceNumber)
            .ToList();

        return Task.FromResult<IEventCursor>(new EventCursor(filtered));
    }

    /// <inheritdoc/>
    public Task<IEventCursor> GetRange(
        EventSequenceNumber start,
        EventSequenceNumber end,
        EventSourceId? eventSourceId = default,
        IEnumerable<EventType>? eventTypes = default,
        CancellationToken cancellationToken = default)
    {
        var filtered = Filter(Events, eventSourceId, null, null, null, eventTypes)
            .Where(_ => _.Context.SequenceNumber >= start && _.Context.SequenceNumber <= end)
            .OrderBy(_ => _.Context.SequenceNumber)
            .ToList();

        return Task.FromResult<IEventCursor>(new EventCursor(filtered));
    }

    /// <inheritdoc/>
    public Task<IEventCursor> GetEventsWithLimit(
        EventSequenceNumber start,
        int limit,
        EventSourceId? eventSourceId = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? eventTypes = default,
        CancellationToken cancellationToken = default)
    {
        var filtered = Filter(Events, eventSourceId, null, eventStreamType, eventStreamId, eventTypes)
            .Where(_ => _.Context.SequenceNumber >= start)
            .OrderBy(_ => _.Context.SequenceNumber)
            .Take(limit)
            .ToList();

        return Task.FromResult<IEventCursor>(new EventCursor(filtered));
    }

    /// <inheritdoc/>
    public Task ReplaceGenerationContent(
        EventSequenceNumber sequenceNumber,
        IDictionary<EventTypeGeneration, ExpandoObject> content) => Task.CompletedTask;

    static IEnumerable<AppendedEvent> Filter(
        IEnumerable<AppendedEvent> events,
        EventSourceId? eventSourceId,
        EventSourceType? eventSourceType,
        EventStreamType? eventStreamType,
        EventStreamId? eventStreamId,
        IEnumerable<EventType>? eventTypes)
    {
        if (eventSourceId is not null)
        {
            events = events.Where(_ => _.Context.EventSourceId == eventSourceId);
        }

        if (eventSourceType is not null)
        {
            events = events.Where(_ => _.Context.EventSourceType == eventSourceType);
        }

        if (eventStreamType is not null)
        {
            events = events.Where(_ => _.Context.EventStreamType == eventStreamType);
        }

        if (eventStreamId is not null)
        {
            events = events.Where(_ => _.Context.EventStreamId == eventStreamId);
        }

        if (eventTypes is not null)
        {
            var typeSet = eventTypes.ToHashSet();
            events = events.Where(_ => typeSet.Contains(_.Context.EventType));
        }

        return events;
    }

    AppendedEvent BuildAppendedEvent(
        EventSequenceNumber sequenceNumber,
        EventSourceType eventSourceType,
        EventSourceId eventSourceId,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        EventType eventType,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<Tag> tags,
        DateTimeOffset occurred,
        IDictionary<EventTypeGeneration, ExpandoObject> content,
        Subject? subject = null)
    {
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
            Identity.System,
            tags,
            EventHash.NotSet,
            Subject: subject?.IsSet is true ? subject : new Subject(eventSourceId.Value));

        var eventContent = content.TryGetValue(EventTypeGeneration.First, out var firstGenContent)
            ? firstGenContent
            : content.Values.FirstOrDefault() ?? new ExpandoObject();

        return new AppendedEvent(eventContext, eventContent);
    }
}
