// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Monads;

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
    /// Both single-event and batch append operations
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
    /// Snapshots an event using this store's serializer and additional-information providers exactly once.
    /// </summary>
    /// <param name="event">The event to prepare.</param>
    /// <returns>Content reusable for append and verification on this sequence instance.</returns>
    /// <exception cref="PreparedEventsNotSupported">This implementation does not support preparation.</exception>
    Task<PreparedEvent> Prepare(object @event) => throw new PreparedEventsNotSupported();

    /// <summary>
    /// Appends prepared content without serializing the event or running providers again.
    /// </summary>
    /// <param name="eventSourceId">The event source.</param>
    /// <param name="preparedEvent">The content prepared by this sequence.</param>
    /// <param name="eventStreamType">Optional stream type.</param>
    /// <param name="eventStreamId">Optional stream id.</param>
    /// <param name="eventSourceType">Optional source type.</param>
    /// <param name="correlationId">Optional correlation id.</param>
    /// <param name="tags">Optional tags.</param>
    /// <param name="concurrencyScope">Optional concurrency scope.</param>
    /// <param name="occurred">Optional occurrence time.</param>
    /// <param name="subject">Optional subject overriding the prepared event's subject.</param>
    /// <returns>The append outcome.</returns>
    /// <exception cref="PreparedEventsNotSupported">This implementation does not support prepared appends.</exception>
    /// <exception cref="PreparedEventBelongsToAnotherSequence">The content belongs to another sequence instance.</exception>
    Task<AppendResult> AppendPrepared(
        EventSourceId eventSourceId,
        PreparedEvent preparedEvent,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default) => throw new PreparedEventsNotSupported();

    /// <summary>
    /// Compares prepared content with the complete, strictly released content at an exact sequence number.
    /// </summary>
    /// <param name="sequenceNumber">The event to verify.</param>
    /// <param name="preparedEvent">Content prepared by this sequence, in the generation to compare.</param>
    /// <param name="eventSourceId">Optional event source to require.</param>
    /// <returns>Equal, different, or unavailable. Unavailable is never proof of a duplicate.</returns>
    /// <exception cref="PreparedEventBelongsToAnotherSequence">The content belongs to another sequence instance.</exception>
    Task<ContentVerificationResult> VerifyContent(EventSequenceNumber sequenceNumber, PreparedEvent preparedEvent, EventSourceId? eventSourceId = default) =>
        Task.FromResult(ContentVerificationResult.Unavailable);

    /// <summary>
    /// Get all events for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to get for.</param>
    /// <param name="filterEventTypes">Collection of <see cref="EventType"/> to get for.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> to narrow to. Omit, or pass <see cref="EventStreamType.All"/>, to not narrow.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> to narrow to. Omit, or pass <see cref="EventStreamId.Default"/>, to not narrow.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> to narrow to. Omit, or pass <see cref="EventSourceType.Default"/> or <see cref="EventSourceType.Unspecified"/>, to not narrow.</param>
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
    /// <param name="subject">Optional <see cref="Subject"/> identifying the target the events are about. Used as the identity for compliance concerns such as PII encryption keys.</param>
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
        DateTimeOffset? occurred = default,
        Subject? subject = default);

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
    /// Append a single event with structured named tags.
    /// </summary>
    /// <param name="eventSourceId">The event source.</param>
    /// <param name="event">The event.</param>
    /// <param name="namedTags">Structured named tags for the event.</param>
    /// <param name="eventStreamType">Optional stream type.</param>
    /// <param name="eventStreamId">Optional stream id.</param>
    /// <param name="eventSourceType">Optional source type.</param>
    /// <param name="correlationId">Optional correlation id.</param>
    /// <param name="tags">Optional plain tags.</param>
    /// <param name="concurrencyScope">Optional concurrency scope.</param>
    /// <param name="occurred">Optional occurred time.</param>
    /// <param name="subject">Optional subject.</param>
    /// <returns>The append result.</returns>
    /// <exception cref="NamedTagsNotSupported">The implementation cannot append nonempty named tags.</exception>
    Task<AppendResult> AppendWithNamedTags(
        EventSourceId eventSourceId,
        object @event,
        IEnumerable<NamedTag> namedTags,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default) =>
        namedTags.Any()
            ? throw new NamedTagsNotSupported(GetType())
            : Append(eventSourceId, @event, eventStreamType, eventStreamId, eventSourceType, correlationId, tags, concurrencyScope, occurred, subject);

    /// <summary>
    /// Append events for one source with structured named tags.
    /// </summary>
    /// <param name="eventSourceId">The event source.</param>
    /// <param name="events">The events.</param>
    /// <param name="namedTags">Structured named tags for every event.</param>
    /// <param name="eventStreamType">Optional stream type.</param>
    /// <param name="eventStreamId">Optional stream id.</param>
    /// <param name="eventSourceType">Optional source type.</param>
    /// <param name="correlationId">Optional correlation id.</param>
    /// <param name="tags">Optional plain tags.</param>
    /// <param name="concurrencyScope">Optional concurrency scope.</param>
    /// <param name="occurred">Optional occurred time.</param>
    /// <param name="subject">Optional subject.</param>
    /// <returns>The batch result.</returns>
    /// <exception cref="NamedTagsNotSupported">The implementation cannot append nonempty named tags.</exception>
    Task<AppendManyResult> AppendManyWithNamedTags(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        IEnumerable<NamedTag> namedTags,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default) =>
        namedTags.Any()
            ? throw new NamedTagsNotSupported(GetType())
            : AppendMany(eventSourceId, events, eventStreamType, eventStreamId, eventSourceType, correlationId, tags, concurrencyScope, occurred, subject);

    /// <summary>
    /// Append events for several sources with structured named tags.
    /// </summary>
    /// <param name="events">The events, including their own named tags.</param>
    /// <param name="namedTags">Structured named tags shared by all events.</param>
    /// <param name="correlationId">Optional correlation id.</param>
    /// <param name="tags">Optional plain tags.</param>
    /// <param name="concurrencyScopes">Optional concurrency scopes.</param>
    /// <returns>The batch result.</returns>
    /// <exception cref="NamedTagsNotSupported">The implementation cannot append nonempty named tags.</exception>
    Task<AppendManyResult> AppendManyWithNamedTags(
        IEnumerable<EventForEventSourceId> events,
        IEnumerable<NamedTag> namedTags,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        IDictionary<EventSourceId, ConcurrencyScope>? concurrencyScopes = default)
    {
        var materializedEvents = events.ToArray();
        if (namedTags.Any() || materializedEvents.Any(_ => _.NamedTags.Any()))
        {
            throw new NamedTagsNotSupported(GetType());
        }

        return AppendMany(materializedEvents, correlationId, tags, concurrencyScopes);
    }

    /// <summary>
    /// Revise a specific event in the event sequence with new content.
    /// </summary>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> of the event to revise.</param>
    /// <param name="event">The event holding the revised content.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    /// <remarks>
    /// The type of <paramref name="event"/> has to be the same as the original event at the sequence number.
    /// Its generational information is taken into account when revising.
    /// </remarks>
    Task Revise(EventSequenceNumber sequenceNumber, object @event);

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

    /// <summary>
    /// Complete a stream so that no further events can be appended to it.
    /// </summary>
    /// <param name="eventStreamType">The <see cref="EventStreamType"/> identifying the stream's type.</param>
    /// <param name="eventStreamId">The <see cref="EventStreamId"/> identifying the stream within the type.</param>
    /// <returns>A <see cref="Result{TSuccess, TError}"/> containing the tail <see cref="EventSequenceNumber"/> at the moment of completion on success, or a <see cref="CompleteStreamError"/> describing why the operation was rejected.</returns>
    /// <remarks>
    /// The default stream — <see cref="EventStreamType.All"/> paired with the default <see cref="EventStreamId"/> — can never be completed and will return
    /// <see cref="CompleteStreamError.DefaultStreamCannotBeCompleted"/>. Completing an already-completed stream returns
    /// <see cref="CompleteStreamError.AlreadyCompleted"/> and leaves the stream in its completed state. After a successful completion any subsequent
    /// append targeting the same stream results in a constraint violation of type <c language="csharp">StreamClosed</c>.
    /// </remarks>
    Task<Result<EventSequenceNumber, CompleteStreamError>> CompleteStream(EventStreamType eventStreamType, EventStreamId eventStreamId);
}
