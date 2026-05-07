// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Defines the client event sequence.
/// </summary>
public interface IEventSequence
{
    /// <summary>
    /// Gets the <see cref="EventSequenceId"/> for the event sequence.
    /// </summary>
    EventSequenceId Id { get; }

    /// <summary>
    /// Gets an observable that emits a collection of <see cref="AppendedEventWithResult"/> after each append operation.
    /// </summary>
    /// <remarks>
    /// Both <see cref="Append"/> and <see cref="AppendMany(EventSourceId, IEnumerable{object}, Events.EventStreamType?, Events.EventStreamId?, Events.EventSourceType?, CorrelationId?, System.Collections.Generic.IEnumerable{string}?, Concurrency.ConcurrencyScope?, System.DateTimeOffset?)"/>
    /// emit through this observable. A single-event append emits a collection of one element;
    /// a batch append emits the full batch. Subscribers receive the notification after the operation has completed, whether it succeeded or failed.
    /// This observable does not fire for transactional appends through <see cref="ITransactionalEventSequence"/>.
    /// </remarks>
    IObservable<IEnumerable<AppendedEventWithResult>> AppendOperations { get; }

    /// <summary>
    /// Gets the transactional event sequence.
    /// </summary>
    /// <remarks>
    /// Use this when you want to typically append events to the event sequence as a transaction that takes place in the same unit of work.
    /// This is very useful when you have disperse event sources that you want to append events to in a single transaction, typically within one request.
    /// Using this will also mean that any result will be handled by Chronicle infrastructure and bubbled up, typically when using Chronicle with ASP.NET Core.
    /// </remarks>
    ITransactionalEventSequence Transactional { get; }

    /// <summary>
    /// Get all events for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to get for.</param>
    /// <param name="filterEventTypes">Collection of <see cref="EventType"/> to get for.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> to append to. Defaults to <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> to append to. Defaults to <see cref="EventStreamId.Default"/>.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> to append to. Defaults to <see cref="EventSourceType.Default"/>.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(EventSourceId eventSourceId, IEnumerable<EventType> filterEventTypes, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default);

    /// <summary>
    /// Check if there are events for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to check for.</param>
    /// <returns>True if it has, false if not.</returns>
    Task<bool> HasEventsFor(EventSourceId eventSourceId);

    /// <summary>
    /// Get all events after and including the given <see cref="EventSequenceNumber"/> with optional <see cref="EventSourceId"/> and <see cref="IEnumerable{T}"/> of <see cref="EventType"/> for filtering.
    /// </summary>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> of the first event to get from.</param>
    /// <param name="eventSourceId">The optional <see cref="EventSourceId"/>.</param>
    /// <param name="filterEventTypes">The optional <see cref="IEnumerable{T}"/> of <see cref="EventType"/>.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    Task<IImmutableList<AppendedEvent>> GetFromSequenceNumber(EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = default, IEnumerable<EventType>? filterEventTypes = default);

    /// <summary>
    /// Get the next sequence number.
    /// </summary>
    /// <returns>Next sequence number.</returns>
    Task<EventSequenceNumber> GetNextSequenceNumber();

    /// <summary>
    /// Get the sequence number of the last (tail) event in the sequence.
    /// </summary>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to get for. If not specified, it will return the tail sequence number for all event sources.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> to get for. If not specified, it will return the tail sequence number for all event source types.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> to get for. If not specified, it will return the tail sequence number for all event stream types.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> to get for. If not specified, it will return the tail sequence number for all event streams.</param>
    /// <param name="filterEventTypes">Optional collection of <see cref="EventType"/> to filter by. If not specified, it will return the tail sequence number for all.</param>
    /// <returns>Tail sequence number.</returns>
    Task<EventSequenceNumber> GetTailSequenceNumber(
        EventSourceId? eventSourceId = default,
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? filterEventTypes = default);

    /// <summary>
    /// Get the sequence number of the last (tail) event in the sequence for a specific observer.
    /// </summary>
    /// <param name="type">Type of observer to get for.</param>
    /// <returns>Tail sequence number.</returns>
    /// <remarks>
    /// This is based on the tail of the event types the observer is interested in.
    /// </remarks>
    Task<EventSequenceNumber> GetTailSequenceNumberForObserver(Type type);

    /// <summary>
    /// Append a single event to the event store.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="event">The event.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> to append to. Defaults to <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> to append to. Defaults to <see cref="EventStreamId.Default"/>.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> to append to. Defaults to <see cref="EventSourceType.Default"/>.</param>
    /// <param name="correlationId">Optional <see cref="CorrelationId"/> of the event. Defaults to <see cref="ICorrelationIdAccessor.Current"/>.</param>
    /// <param name="tags">Optional collection of tags to associate with the event. Will be combined with any static tags from the event type.</param>
    /// <param name="concurrencyScope">Optional <see cref="ConcurrencyScope"/> to use for concurrency control. Defaults to <see cref="ConcurrencyScope.None"/>.</param>
    /// <param name="occurred">Optional <see cref="DateTimeOffset"/> specifying when the event occurred. If not set, the server will set it to approximately the time of append.</param>
    /// <param name="subject">Optional <see cref="Subject"/> identifying the target the event is about. Used as the identity for compliance concerns such as PII encryption keys. When omitted, the <paramref name="eventSourceId"/> is used as the subject.</param>
    /// <returns><see cref="AppendResult"/> with details about whether or not it succeeded and more.</returns>
    Task<AppendResult> Append(
        EventSourceId eventSourceId,
        object @event,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default);

    /// <summary>
    /// Append a collection of events to the event store as a transaction.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="events">Collection of events to append.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> to append to. Defaults to <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> to append to. Defaults to <see cref="EventStreamId.Default"/>.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> to append to. Defaults to <see cref="EventSourceType.Default"/>.</param>
    /// <param name="correlationId">Optional <see cref="CorrelationId"/> of the event. Defaults to <see cref="ICorrelationIdAccessor.Current"/>.</param>
    /// <param name="tags">Optional collection of tags to associate with all events. Will be combined with any static tags from the event types.</param>
    /// <param name="concurrencyScope">Optional <see cref="ConcurrencyScope"/> to use for concurrency control. Defaults to <see cref="ConcurrencyScope.None"/>.</param>
    /// <param name="occurred">Optional <see cref="DateTimeOffset"/> specifying when the events occurred. If not set, the server will set it to approximately the time of append.</param>
    /// <returns><see cref="AppendManyResult"/> with details about whether or not it succeeded and more.</returns>
    /// <remarks>
    /// All events will be committed as one operation for the underlying data store.
    /// </remarks>
    Task<AppendManyResult> AppendMany(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default);

    /// <summary>
    /// Append a collection of events to the event store as a transaction.
    /// </summary>
    /// <param name="events">Collection of <see cref="EventForEventSourceId"/> to append.</param>
    /// <param name="correlationId">Optional <see cref="CorrelationId"/> of the event. Defaults to <see cref="ICorrelationIdAccessor.Current"/>.</param>
    /// <param name="tags">Optional collection of tags to associate with all events. Will be combined with any static tags from the event types.</param>
    /// <param name="concurrencyScopes">Optional <see cref="IDictionary{TKey, TValue}"/> of <see cref="EventSourceId"/> and <see cref="ConcurrencyScope"/> to use for concurrency control. Defaults to an empty dictionary.</param>
    /// <returns><see cref="AppendManyResult"/> with details about whether or not it succeeded and more.</returns>
    /// <remarks>
    /// All events will be committed as one operation for the underlying data store.
    /// </remarks>
    Task<AppendManyResult> AppendMany(
        IEnumerable<EventForEventSourceId> events,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        IDictionary<EventSourceId, ConcurrencyScope>? concurrencyScopes = default);

    /// <summary>
    /// Redact an event at a specific sequence number.
    /// </summary>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to redact.</param>
    /// <param name="reason">Reason for redacting.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Redact(EventSequenceNumber sequenceNumber, RedactionReason reason);

    /// <summary>
    /// Redact all events for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to redact.</param>
    /// <param name="reason">Reason for redacting.</param>
    /// <param name="clrEventTypes">Optionally any specific event types.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Redact(EventSourceId eventSourceId, RedactionReason reason, params Type[] clrEventTypes);
}
