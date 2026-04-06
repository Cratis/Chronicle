// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a mutable, in-memory implementation of <see cref="IEventSequence"/> for testing purposes.
/// </summary>
/// <remarks>
/// <para>
/// This implementation maintains a list of <see cref="AppendedEvent"/> in memory and assigns
/// monotonically increasing sequence numbers to each appended event. It is designed to be used
/// as the backing store for <see cref="EventScenario"/>.
/// </para>
/// </remarks>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> for this sequence.</param>
/// <param name="eventTypes">The <see cref="IEventTypes"/> for mapping CLR types to <see cref="EventType"/>.</param>
public class InMemoryEventSequence(EventSequenceId eventSequenceId, IEventTypes eventTypes) : IEventSequence
{
    readonly List<AppendedEvent> _events = [];
    ulong _nextSequenceNumber;

    /// <inheritdoc/>
    public EventSequenceId Id => eventSequenceId;

    /// <inheritdoc/>
    public ITransactionalEventSequence Transactional => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<AppendResult> Append(
        EventSourceId eventSourceId,
        object @event,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default)
    {
        correlationId ??= CorrelationId.New();
        var sequenceNumber = (EventSequenceNumber)_nextSequenceNumber++;
        var eventType = eventTypes.GetEventTypeFor(@event.GetType());

        _events.Add(new AppendedEvent(
            EventContext.From(
                EventStoreName.NotSet,
                EventStoreNamespaceName.NotSet,
                eventType,
                eventSourceType ?? EventSourceType.Default,
                eventSourceId,
                eventStreamType ?? EventStreamType.All,
                eventStreamId ?? EventStreamId.Default,
                sequenceNumber,
                correlationId,
                occurred),
            @event.AsExpandoObject(true)));

        return Task.FromResult(AppendResult.Success(correlationId, sequenceNumber));
    }

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendMany(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default)
    {
        correlationId ??= CorrelationId.New();
        var sequenceNumbers = new List<EventSequenceNumber>();

        foreach (var @event in events)
        {
            var sequenceNumber = (EventSequenceNumber)_nextSequenceNumber++;
            var eventType = eventTypes.GetEventTypeFor(@event.GetType());

            _events.Add(new AppendedEvent(
                EventContext.From(
                    EventStoreName.NotSet,
                    EventStoreNamespaceName.NotSet,
                    eventType,
                    eventSourceType ?? EventSourceType.Default,
                    eventSourceId,
                    eventStreamType ?? EventStreamType.All,
                    eventStreamId ?? EventStreamId.Default,
                    sequenceNumber,
                    correlationId,
                    occurred),
                @event.AsExpandoObject(true)));

            sequenceNumbers.Add(sequenceNumber);
        }

        return Task.FromResult(AppendManyResult.Success(correlationId, sequenceNumbers));
    }

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendMany(
        IEnumerable<EventForEventSourceId> events,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        IDictionary<EventSourceId, ConcurrencyScope>? concurrencyScopes = default)
    {
        correlationId ??= CorrelationId.New();
        var sequenceNumbers = new List<EventSequenceNumber>();

        foreach (var eventForSource in events)
        {
            var sequenceNumber = (EventSequenceNumber)_nextSequenceNumber++;
            var eventType = eventTypes.GetEventTypeFor(eventForSource.Event.GetType());

            _events.Add(new AppendedEvent(
                EventContext.From(
                    EventStoreName.NotSet,
                    EventStoreNamespaceName.NotSet,
                    eventType,
                    eventForSource.EventSourceType,
                    eventForSource.EventSourceId,
                    eventForSource.EventStreamType,
                    eventForSource.EventStreamId,
                    sequenceNumber,
                    correlationId,
                    eventForSource.Occurred),
                eventForSource.Event.AsExpandoObject(true)));

            sequenceNumbers.Add(sequenceNumber);
        }

        return Task.FromResult(AppendManyResult.Success(correlationId, sequenceNumbers));
    }

    /// <inheritdoc/>
    public Task<bool> HasEventsFor(EventSourceId eventSourceId) =>
        Task.FromResult(_events.Exists(_ => _.Context.EventSourceId == eventSourceId));

    /// <inheritdoc/>
    public Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(
        EventSourceId eventSourceId,
        IEnumerable<EventType> filterEventTypes,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default) =>
        Task.FromResult<IImmutableList<AppendedEvent>>(
            _events
                .Where(_ =>
                    _.Context.EventSourceId == eventSourceId &&
                    filterEventTypes.Contains(_.Context.EventType))
                .ToImmutableList());

    /// <inheritdoc/>
    public Task<IImmutableList<AppendedEvent>> GetFromSequenceNumber(
        EventSequenceNumber sequenceNumber,
        EventSourceId? eventSourceId = default,
        IEnumerable<EventType>? filterEventTypes = default) =>
        Task.FromResult<IImmutableList<AppendedEvent>>(
            _events
                .Where(_ =>
                    _.Context.SequenceNumber >= sequenceNumber &&
                    (eventSourceId is null || _.Context.EventSourceId == eventSourceId) &&
                    filterEventTypes?.Contains(_.Context.EventType) != false)
                .ToImmutableList());

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumber() =>
        Task.FromResult((EventSequenceNumber)_nextSequenceNumber);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber(
        EventSourceId? eventSourceId = default,
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? filterEventTypes = default)
    {
        var filtered = _events
            .Where(_ =>
                (eventSourceId is null || _.Context.EventSourceId == eventSourceId) &&
                (eventSourceType is null || _.Context.EventSourceType == eventSourceType) &&
                (eventStreamType is null || _.Context.EventStreamType == eventStreamType) &&
                (eventStreamId is null || _.Context.EventStreamId == eventStreamId) &&
                filterEventTypes?.Contains(_.Context.EventType) != false)
            .ToList();

        if (filtered.Count == 0)
        {
            return Task.FromResult(EventSequenceNumber.Unavailable);
        }

        return Task.FromResult(filtered.Max(_ => _.Context.SequenceNumber) ?? EventSequenceNumber.Unavailable);
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumberForObserver(Type type) =>
        GetTailSequenceNumber();

    /// <inheritdoc/>
    public Task Redact(EventSequenceNumber sequenceNumber, RedactionReason reason) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Redact(EventSourceId eventSourceId, RedactionReason reason, params Type[] clrEventTypes) => Task.CompletedTask;

    /// <summary>
    /// Seeds the sequence with pre-existing events without assigning new sequence numbers.
    /// </summary>
    /// <param name="events">The events to seed, already wrapped as <see cref="AppendedEvent"/>.</param>
    internal void Seed(IEnumerable<AppendedEvent> events)
    {
        foreach (var @event in events)
        {
            _events.Add(@event);
            var nextSequenceNumber = @event.Context.SequenceNumber.Value + 1;
            if (nextSequenceNumber > _nextSequenceNumber)
            {
                _nextSequenceNumber = nextSequenceNumber;
            }
        }
    }
}
