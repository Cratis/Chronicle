// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Grpc;
using Cratis.Monads;
using Orleans.Concurrency;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Defines the event sequence.
/// </summary>
[KeyedBy<EventSequenceKey>]
[BelongsTo(WellKnownServices.EventSequenceQueries)]
public interface IEventSequence : IGrainWithStringKey
{
    /// <summary>
    /// Rehydrate the event sequence.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Rehydrate();

    /// <summary>
    /// Wait for preceding append calls to finish executing on this sequence.
    /// </summary>
    /// <returns>Awaitable task. Failure or timeout does not confirm that preceding appends have finished.</returns>
    /// <remarks>
    /// This call must not interleave with appends. A caller's response timeout does not cancel an executing append;
    /// awaiting this barrier before reading storage prevents acknowledging history while that append can still commit.
    /// This is an execution barrier, not a reservation against future appends or a storage transaction fence.
    /// </remarks>
    Task DrainAppends();

    /// <summary>
    /// Re-read the constraints registered for the event store and rebuild the validators from them.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Called before a reindex of this sequence starts, so that the sequence maintains the index of a newly covered
    /// constraint for every append from this moment on - and every append before it is covered by the reindex, which
    /// reads the sequence's events. It is deliberately not interleaved: an append in progress completes, including its
    /// index update, before the validators are replaced.
    /// </remarks>
    Task RefreshConstraints();

    /// <summary>
    /// Get the next sequence number.
    /// </summary>
    /// <returns>Next sequence number.</returns>
    /// <remarks>
    /// Served concurrently with an in-flight append rather than queued behind it. The sequence number is only advanced
    /// after the append is durable, so the value returned during an append is the one from before it commits.
    /// </remarks>
    [AlwaysInterleave]
    Task<EventSequenceNumber> GetNextSequenceNumber();

    /// <summary>
    /// Get the sequence number of the last (tail) event in the sequence.
    /// </summary>
    /// <returns>Tail sequence number.</returns>
    /// <remarks>
    /// Served concurrently with an in-flight append rather than queued behind it. The sequence number is only advanced
    /// after the append is durable, so the value returned during an append is the one from before it commits.
    /// </remarks>
    [AlwaysInterleave]
    Task<EventSequenceNumber> GetTailSequenceNumber();

    /// <summary>
    /// Get the sequence number of the tail event in the sequence filtered by event types.
    /// </summary>
    /// <param name="eventTypes">Event types to filter on.</param>
    /// <returns>Tail sequence number.</returns>
    /// <remarks>
    /// The method will filter down on the event types and give you the highest sequence number for the given event types.
    /// </remarks>
    [AlwaysInterleave]
    Task<EventSequenceNumber> GetTailSequenceNumberForEventTypes(IEnumerable<EventType> eventTypes);

    /// <summary>
    /// Get the next sequence number greater or equal to a specific sequence number with optionally filtered on event types and event source id.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number to search from.</param>
    /// <param name="eventTypes">Optional event types to get for.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to get for. It won't filter by this if omitted.</param>
    /// <returns>
    /// <p>The last sequence number.</p>
    /// <p>If providing event types, this will give the last sequence number from the selection of event types.</p>
    /// <p>If no event is found, it will return <see cref="EventSequenceNumber.Unavailable"/>.</p>
    /// </returns>
    [AlwaysInterleave]
    Task<Result<EventSequenceNumber, GetSequenceNumberError>> GetNextSequenceNumberGreaterOrEqualTo(
        EventSequenceNumber sequenceNumber,
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null);

    /// <summary>
    /// Publishes one immutable durable intent through normal validation, compliance and event migration.
    /// </summary>
    /// <param name="publicationId">The opaque deterministic identity persisted with the intent, scoped to this destination.</param>
    /// <param name="intentFingerprint">The persisted digest of the plaintext intent and all append context.</param>
    /// <param name="event">The event from the durable intent.</param>
    /// <param name="correlationId">The original correlation identity.</param>
    /// <param name="causation">The original causation chain.</param>
    /// <param name="causedBy">The original identity chain.</param>
    /// <returns>The existing or newly committed sequence number, or an explicit append failure.</returns>
    /// <remarks>
    /// Kernel-owned publication only; this does not expose a client registration or gRPC API.
    /// A retry must reuse the same persisted intent. Replay must allocate a new occurrence identity.
    /// </remarks>
    Task<AppendResult> AppendPublication(string publicationId, string intentFingerprint, EventToAppend @event, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy);

    /// <summary>
    /// Appends an event to the event sequence.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> of the event source.</param>
    /// <param name="event">The event to append.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the event.</param>
    /// <param name="causation">Collection of <see cref="Causation"/> for the event.</param>
    /// <param name="causedBy">The <see cref="Identity"/> of the entity that caused the event.</param>
    /// <param name="tags">Collection of <see cref="Tag"/> for the event.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> to specify the type of the event source. Defaults to <see cref="EventSourceType.Default"/>.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> to specify the type of the event stream. Defaults to <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> to specify the identifier of the event stream. Defaults to <see cref="EventStreamId.Default"/>.</param>
    /// <returns>An <see cref="AppendResult"/> indicating the result of the append operation.</returns>
    Task<AppendResult> Append(
        EventSourceId eventSourceId,
        object @event,
        CorrelationId? correlationId = default,
        IEnumerable<Causation>? causation = default,
        Identity? causedBy = default,
        IEnumerable<Tag>? tags = default,
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default);

    /// <summary>
    /// Append a single event to the event store.
    /// </summary>
    /// <param name="eventSourceType">The <see cref="EventSourceType"/> to append for.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="eventStreamType">the <see cref="EventStreamType"/> to append to.</param>
    /// <param name="eventStreamId">The <see cref="EventStreamId"/> to append to.</param>
    /// <param name="eventType">The <see cref="EventType">type of event</see> to append.</param>
    /// <param name="content">The JSON payload of the event.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the event.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedBy">The person, system or service that caused the event, defined by <see cref="Identity"/>.</param>
    /// <param name="tags">Collection of <see cref="Tag"/> for the event.</param>
    /// <param name="concurrencyScope">The <see cref="ConcurrencyScope"/>.</param>
    /// <param name="occurred">Optional occurred time. If null, the server will set it to approximately the time of append.</param>
    /// <param name="subject">Optional <see cref="Subject"/> identifying the target the event is about. Used as the identity for compliance concerns such as PII encryption keys. When omitted, the <paramref name="eventSourceId"/> is used as the subject.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task<AppendResult> Append(
        EventSourceType eventSourceType,
        EventSourceId eventSourceId,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        EventType eventType,
        JsonObject content,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        IEnumerable<Tag> tags,
        ConcurrencyScope concurrencyScope,
        DateTimeOffset? occurred = null,
        Subject? subject = null);

    /// <summary>
    /// Appends an event carrying structured named tags.
    /// </summary>
    /// <param name="eventSourceType">The event source type.</param>
    /// <param name="eventSourceId">The event source id.</param>
    /// <param name="eventStreamType">The stream type.</param>
    /// <param name="eventStreamId">The stream id.</param>
    /// <param name="eventType">The event type.</param>
    /// <param name="content">The event content.</param>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="causation">The causation chain.</param>
    /// <param name="causedBy">The identity.</param>
    /// <param name="tags">The legacy tags.</param>
    /// <param name="concurrencyScope">The concurrency scope.</param>
    /// <param name="occurred">The occurrence time.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="namedTags">The named tags.</param>
    /// <param name="eventSource">Optional name of the registered event source definition the event is appended through.</param>
    /// <returns>The append result.</returns>
    Task<AppendResult> Append(
        EventSourceType eventSourceType,
        EventSourceId eventSourceId,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        EventType eventType,
        JsonObject content,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        IEnumerable<Tag> tags,
        ConcurrencyScope concurrencyScope,
        DateTimeOffset? occurred,
        Subject? subject,
        IReadOnlyCollection<NamedTag> namedTags,
        EventSourceName? eventSource = null);

    /// <summary>
    /// Append a single event to the event store.
    /// </summary>
    /// <param name="events">Collection of <see cref="EventToAppend">events</see> to append.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the event.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedBy">The person, system or service that caused the events, defined by <see cref="Identity"/>.</param>
    /// <param name="concurrencyScopes">The <see cref="ConcurrencyScopes"/>.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task<AppendManyResult> AppendMany(
        IEnumerable<EventToAppend> events,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        ConcurrencyScopes concurrencyScopes);

    /// <summary>
    /// Revise a specific event in the event store.
    /// </summary>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> of the event to revise.</param>
    /// <param name="eventType">The <see cref="EventType">type of event</see> to revise.</param>
    /// <param name="content">The JSON payload of the event.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the event.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedBy">The person, system or service that caused the revision, defined by <see cref="Identity"/>.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    /// <remarks>
    /// The type of the event has to be the same as the original event at the sequence number.
    /// Its generational information is taken into account when revising.
    /// </remarks>
    Task Revise(
        EventSequenceNumber sequenceNumber,
        EventType eventType,
        JsonObject content,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy);

    /// <summary>
    /// Redact an event at a specific sequence number.
    /// </summary>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to redact.</param>
    /// <param name="reason">Reason for redacting.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the event.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedBy">The person, system or service that caused the redaction, defined by <see cref="Identity"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Redact(
        EventSequenceNumber sequenceNumber,
        RedactionReason reason,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy);

    /// <summary>
    /// Redact all events for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to redact.</param>
    /// <param name="reason">Reason for redacting.</param>
    /// <param name="eventTypes">Optionally any specific event types.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the event.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedBy">The person, system or service that caused the redaction, defined by <see cref="Identity"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Redact(
        EventSourceId eventSourceId,
        RedactionReason reason,
        IEnumerable<EventType> eventTypes,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy);

    /// <summary>
    /// Complete a stream so that no further events can be appended to it.
    /// </summary>
    /// <param name="eventStreamType">The <see cref="EventStreamType"/> identifying the stream's type.</param>
    /// <param name="eventStreamId">The <see cref="EventStreamId"/> identifying the stream within the type.</param>
    /// <returns>A <see cref="Result{TSuccess, TError}"/> containing the tail <see cref="EventSequenceNumber"/> at the moment of completion on success, or a <see cref="CompleteStreamError"/> describing why the operation was rejected.</returns>
    /// <remarks>
    /// The default stream — <see cref="EventStreamType.All"/> paired with the default <see cref="EventStreamId"/> — can never be completed and will return
    /// <see cref="CompleteStreamError.DefaultStreamCannotBeCompleted"/>. Completing an already-completed stream returns
    /// <see cref="CompleteStreamError.AlreadyCompleted"/> and leaves the stream in its completed state.
    /// </remarks>
    Task<Result<EventSequenceNumber, CompleteStreamError>> CompleteStream(EventStreamType eventStreamType, EventStreamId eventStreamId);

    /// <summary>
    /// Manually close a nonempty scope in the same grain turn as its optional tail check.
    /// </summary>
    /// <param name="scope">The scope to close.</param>
    /// <param name="expectedTailSequenceNumber">An optional expected scope tail.</param>
    /// <returns>The sequence tail at closure, or a completion error.</returns>
    /// <remarks>
    /// A covering manual closure returns AlreadyCompleted without writing. Coverage only by event-owned
    /// closures still writes a manual closure, so a later reopening event cannot undo the manual decision.
    /// </remarks>
    Task<Result<EventSequenceNumber, CompleteStreamError>> CompleteStream(ClosedStreamScope scope, EventSequenceNumber? expectedTailSequenceNumber = default);

    /// <summary>
    /// Check whether a scope is covered by any persisted closure.
    /// </summary>
    /// <param name="scope">The scope to check.</param>
    /// <returns>True if covered.</returns>
    Task<bool> IsStreamCompleted(ClosedStreamScope scope);

    /// <summary>
    /// Check whether or not the supplied stream has been completed.
    /// </summary>
    /// <param name="eventStreamType">The <see cref="EventStreamType"/> identifying the stream's type.</param>
    /// <param name="eventStreamId">The <see cref="EventStreamId"/> identifying the stream within the type.</param>
    /// <returns>True if the stream has been completed; false otherwise.</returns>
    [Query]
    Task<bool> IsStreamCompleted(EventStreamType eventStreamType, EventStreamId eventStreamId);
}
