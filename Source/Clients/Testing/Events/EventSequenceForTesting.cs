// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Chronicle.Testing.Events;

/// <summary>
/// Represents a null implementation of <see cref="IEventSequence"/> that does nothing.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="EventSequenceForTesting"/>.
/// </remarks>
/// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
/// <param name="events">Optional events to populate the event log with.</param>
public class EventSequenceForTesting(IEventTypes eventTypes, params EventForEventSourceId[] events) : IEventSequence
{
    readonly AppendedEvent[] _events = events.Select((@event, index) => new AppendedEvent(
            EventContext.Empty with { EventType = eventTypes.GetEventTypeFor(@event.Event.GetType()), EventSourceId = @event.EventSourceId },
            @event.Event.AsExpandoObject(true))).ToArray();

    /// <inheritdoc/>
    public EventSequenceId Id => EventSequenceId.Log;

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
        ConcurrencyScope? concurrencyScope = default) => Task.FromResult(AppendResult.Success(correlationId ?? CorrelationId.New(), EventSequenceNumber.Unavailable));

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendMany(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default) => Task.FromResult(AppendManyResult.Success(correlationId ?? CorrelationId.New(), []));

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendMany(
        IEnumerable<EventForEventSourceId> events,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        IDictionary<EventSourceId, ConcurrencyScope>? concurrencyScopes = default) => Task.FromResult(AppendManyResult.Success(correlationId ?? CorrelationId.New(), []));

    /// <inheritdoc/>
    public Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(
        EventSourceId eventSourceId,
        IEnumerable<EventType> filterEventTypes,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default) => Task.FromResult<IImmutableList<AppendedEvent>>(_events.ToImmutableList());

    /// <inheritdoc/>
    public Task<IImmutableList<AppendedEvent>> GetFromSequenceNumber(
        EventSequenceNumber sequenceNumber,
        EventSourceId? eventSourceId = default,
        IEnumerable<EventType>? filterEventTypes = default) =>
        Task.FromResult<IImmutableList<AppendedEvent>>(_events.Where(_ =>
            _.Context.SequenceNumber >= sequenceNumber
            && (eventSourceId is null || _.Context.EventSourceId == eventSourceId)
            && filterEventTypes?.Contains(_.Context.EventType) != false).ToImmutableList());

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumber() => Task.FromResult(EventSequenceNumber.First);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber(
        EventSourceId? eventSourceId = default,
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? filterEventTypes = default) => Task.FromResult(EventSequenceNumber.First);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumberForObserver(Type type) => Task.FromResult(EventSequenceNumber.First);

    /// <inheritdoc/>
    public Task<bool> HasEventsFor(EventSourceId eventSourceId) => Task.FromResult(_events.Any(_ => _.Context.EventSourceId == eventSourceId));

    /// <inheritdoc/>
    public Task Redact(EventSequenceNumber sequenceNumber, RedactionReason? reason = null) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Redact(EventSourceId eventSourceId, RedactionReason? reason = null, params Type[] eventTypes) => Task.CompletedTask;
}
