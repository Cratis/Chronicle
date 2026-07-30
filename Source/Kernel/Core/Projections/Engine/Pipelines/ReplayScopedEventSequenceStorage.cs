// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Dynamic;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Represents an <see cref="IEventSequenceStorage"/> decorator that memoizes the parent- and
/// creation-event lookups used by hierarchical key resolution for the duration of a replay session.
/// </summary>
/// <remarks>
/// During a replay the event sequence is treated as immutable, so a lookup for a given
/// (<see cref="EventSourceId"/>, event types) pair resolves to the same result for every event in the
/// session. Replaying a hierarchy repeats the same parent/creation-event lookups once per child event;
/// caching them collapses those repeats to a single storage query per distinct pair.
/// <para>
/// Only the position-independent lookups (<see cref="GetHeadSequenceNumber"/> and
/// <see cref="TryGetLastInstanceOfAny"/>) are cached — they read only the immutable event log. Every
/// other member, and all sink-dependent resolution, passes straight through so live behavior is
/// unchanged. The caches are consulted only while a session is active and are cleared at both session
/// boundaries, so a cached value can never leak into live event handling (see <see cref="IReplayScopedCache"/>).
/// </para>
/// <para>
/// Concurrency: replay handling for a projection is serialized by the pipeline handle lock, so cache
/// population is effectively single-threaded; <see cref="ConcurrentDictionary{TKey, TValue}"/> plus a
/// volatile activation flag keep it safe for the old-pipeline/new-pipeline overlap that occurs around
/// eviction. Growth is bounded by the number of distinct lookup arguments seen during one session and
/// released when the session ends.
/// </para>
/// </remarks>
/// <param name="inner">The underlying <see cref="IEventSequenceStorage"/> that performs the actual work.</param>
internal sealed class ReplayScopedEventSequenceStorage(IEventSequenceStorage inner) : IEventSequenceStorage, IReplayScopedCache
{
    readonly ConcurrentDictionary<string, Task<EventSequenceNumber>> _headSequenceNumbers = new();
    readonly ConcurrentDictionary<string, Task<Option<AppendedEvent>>> _lastInstances = new();
    volatile bool _active;

    /// <inheritdoc/>
    public void BeginReplaySession()
    {
        _headSequenceNumbers.Clear();
        _lastInstances.Clear();
        _active = true;
    }

    /// <inheritdoc/>
    public void EndReplaySession()
    {
        _active = false;
        _headSequenceNumbers.Clear();
        _lastInstances.Clear();
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetHeadSequenceNumber(IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null)
    {
        if (!_active)
        {
            return inner.GetHeadSequenceNumber(eventTypes, eventSourceId);
        }

        var eventTypesList = eventTypes?.ToArray();
        var cacheKey = BuildKey(eventSourceId, eventTypesList?.Select(_ => $"{_.Id.Value}:{_.Generation.Value}"));
        return _headSequenceNumbers.GetOrAdd(
            cacheKey,
            static (_, arg) => arg.inner.GetHeadSequenceNumber(arg.eventTypesList, arg.eventSourceId),
            (inner, eventTypesList, eventSourceId));
    }

    /// <inheritdoc/>
    public Task<Option<AppendedEvent>> TryGetLastInstanceOfAny(EventSourceId eventSourceId, IEnumerable<EventTypeId> eventTypes)
    {
        if (!_active)
        {
            return inner.TryGetLastInstanceOfAny(eventSourceId, eventTypes);
        }

        var eventTypesList = eventTypes.ToArray();
        var cacheKey = BuildKey(eventSourceId, eventTypesList.Select(_ => _.Value));
        return _lastInstances.GetOrAdd(
            cacheKey,
            static (_, arg) => arg.inner.TryGetLastInstanceOfAny(arg.eventSourceId, arg.eventTypesList),
            (inner, eventSourceId, eventTypesList));
    }

    /// <inheritdoc/>
    public Task EnsureIndexes() => inner.EnsureIndexes();

    /// <inheritdoc/>
    public Task<EventSequenceState> GetState() => inner.GetState();

    /// <inheritdoc/>
    public Task SaveState(EventSequenceState state) => inner.SaveState(state);

    /// <inheritdoc/>
    public Task<EventCount> GetCount(EventSequenceNumber? lastEventSequenceNumber = null, IEnumerable<EventType>? eventTypes = null, IEnumerable<Tag>? tags = null) =>
        inner.GetCount(lastEventSequenceNumber, eventTypes, tags);

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
        Subject? subject = null) =>
        inner.Append(sequenceNumber, eventSourceType, eventSourceId, eventStreamType, eventStreamId, eventType, correlationId, causation, causedByChain, tags, occurred, content, contentHashes, subject);

    /// <inheritdoc/>
    public Task<Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>> AppendMany(IEnumerable<EventToAppendToStorage> events) =>
        inner.AppendMany(events);

    /// <inheritdoc/>
    public Task Revise(EventSequenceNumber sequenceNumber, EventType eventType, CorrelationId correlationId, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred, ExpandoObject content, EventHash hash) =>
        inner.Revise(sequenceNumber, eventType, correlationId, causation, causedByChain, occurred, content, hash);

    /// <inheritdoc/>
    public Task<AppendedEvent> Redact(EventSequenceNumber sequenceNumber, RedactionReason reason, CorrelationId correlationId, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred) =>
        inner.Redact(sequenceNumber, reason, correlationId, causation, causedByChain, occurred);

    /// <inheritdoc/>
    public Task<IEnumerable<EventType>> Redact(EventSourceId eventSourceId, RedactionReason reason, IEnumerable<EventType>? eventTypes, CorrelationId correlationId, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred) =>
        inner.Redact(eventSourceId, reason, eventTypes, correlationId, causation, causedByChain, occurred);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber(
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null,
        EventSourceType? eventSourceType = null,
        EventStreamId? eventStreamId = null,
        EventStreamType? eventStreamType = null) =>
        inner.GetTailSequenceNumber(eventTypes, eventSourceId, eventSourceType, eventStreamId, eventStreamType);

    /// <inheritdoc/>
    public Task<TailEventSequenceNumbers> GetTailSequenceNumbers(IEnumerable<EventType> eventTypes) =>
        inner.GetTailSequenceNumbers(eventTypes);

    /// <inheritdoc/>
    public Task<IImmutableDictionary<EventType, EventSequenceNumber>> GetTailSequenceNumbersForEventTypes(IEnumerable<EventType> eventTypes) =>
        inner.GetTailSequenceNumbersForEventTypes(eventTypes);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumberGreaterOrEqualThan(EventSequenceNumber sequenceNumber, IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null) =>
        inner.GetNextSequenceNumberGreaterOrEqualThan(sequenceNumber, eventTypes, eventSourceId);

    /// <inheritdoc/>
    public Task<bool> HasEventsFor(EventSourceId eventSourceId) => inner.HasEventsFor(eventSourceId);

    /// <inheritdoc/>
    public Task<Catch<Option<AppendedEvent>>> TryGetLastEventBefore(EventTypeId eventTypeId, EventSourceId eventSourceId, EventSequenceNumber currentSequenceNumber) =>
        inner.TryGetLastEventBefore(eventTypeId, eventSourceId, currentSequenceNumber);

    /// <inheritdoc/>
    public Task<AppendedEvent> GetEventAt(EventSequenceNumber sequenceNumber) => inner.GetEventAt(sequenceNumber);

    /// <inheritdoc/>
    public Task<IEventCursor> GetFromSequenceNumber(EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = default, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, IEnumerable<EventType>? eventTypes = default, IEnumerable<Tag>? tags = default, CancellationToken cancellationToken = default) =>
        inner.GetFromSequenceNumber(sequenceNumber, eventSourceId, eventStreamType, eventStreamId, eventTypes, tags, cancellationToken);

    /// <inheritdoc/>
    public Task<IEventCursor> GetRange(EventSequenceNumber start, EventSequenceNumber end, EventSourceId? eventSourceId = default, IEnumerable<EventType>? eventTypes = default, IEnumerable<Tag>? tags = default, CancellationToken cancellationToken = default) =>
        inner.GetRange(start, end, eventSourceId, eventTypes, tags, cancellationToken);

    /// <inheritdoc/>
    public Task<IEventCursor> GetEventsWithLimit(
        EventSequenceNumber start,
        int limit,
        EventSourceId? eventSourceId = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? eventTypes = default,
        IEnumerable<Tag>? tags = default,
        CancellationToken cancellationToken = default) =>
        inner.GetEventsWithLimit(start, limit, eventSourceId, eventStreamType, eventStreamId, eventTypes, tags, cancellationToken);

    /// <inheritdoc/>
    public Task ReplaceGenerationContent(EventSequenceNumber sequenceNumber, IDictionary<EventTypeGeneration, ExpandoObject> content) =>
        inner.ReplaceGenerationContent(sequenceNumber, content);

    static string BuildKey(EventSourceId? eventSourceId, IEnumerable<string>? eventTypeTokens)
    {
        var source = eventSourceId?.Value ?? "*";
        var types = eventTypeTokens is null ? "*" : string.Join(',', eventTypeTokens.Order(StringComparer.Ordinal));
        return $"{source}|{types}";
    }
}
