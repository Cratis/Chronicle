// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Collections.Immutable;
using System.Dynamic;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Execution;
using Cratis.Monads;
using KernelAppendedEvent = KernelConcepts::Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelAuditing = KernelConcepts::Cratis.Chronicle.Concepts.Auditing;
using KernelConcept = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;
using KernelIdentities = KernelConcepts::Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventSequenceStorage"/> for use with the
/// kernel event sequence during testing.
/// </summary>
/// <param name="eventSequenceId">The <c>EventSequenceId</c> this storage serves.</param>
internal sealed class InMemoryEventSequenceStorage(
    KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId eventSequenceId) : IEventSequenceStorage
{
    readonly List<KernelAppendedEvent> _events = [];

    /// <summary>
    /// Gets the events stored in this storage instance.
    /// </summary>
    internal IReadOnlyList<KernelAppendedEvent> Events => _events;

    /// <inheritdoc/>
    public Task<EventSequenceState> GetState() =>
        Task.FromResult(new EventSequenceState
        {
            SequenceNumber = _events.Count == 0
                ? 0UL
                : _events.Max(_ => _.Context.SequenceNumber.Value) + 1
        });

    /// <inheritdoc/>
    public Task SaveState(EventSequenceState state) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<KernelEvents::EventCount> GetCount(
        KernelEvents::EventSequenceNumber? lastEventSequenceNumber = null,
        IEnumerable<KernelEvents::EventType>? eventTypes = null)
    {
        var filtered = Filter(_events, null, null, null, null, eventTypes);
        if (lastEventSequenceNumber is not null)
        {
            filtered = filtered.Where(_ => _.Context.SequenceNumber <= lastEventSequenceNumber);
        }

        return Task.FromResult((KernelEvents::EventCount)filtered.Count());
    }

    /// <inheritdoc/>
    public Task<Result<KernelAppendedEvent, DuplicateEventSequenceNumber>> Append(
        KernelEvents::EventSequenceNumber sequenceNumber,
        KernelEvents::EventSourceType eventSourceType,
        KernelEvents::EventSourceId eventSourceId,
        KernelEvents::EventStreamType eventStreamType,
        KernelEvents::EventStreamId eventStreamId,
        KernelEvents::EventType eventType,
        CorrelationId correlationId,
        IEnumerable<KernelAuditing::Causation> causation,
        IEnumerable<KernelIdentities::IdentityId> causedByChain,
        IEnumerable<KernelEvents::Tag> tags,
        DateTimeOffset occurred,
        IDictionary<KernelEvents::EventTypeGeneration, ExpandoObject> content,
        IDictionary<KernelEvents::EventTypeGeneration, KernelEvents::EventHash> contentHashes)
    {
        if (_events.Exists(_ => _.Context.SequenceNumber == sequenceNumber))
        {
            var nextAvailable = (KernelEvents::EventSequenceNumber)(_events.Max(_ => _.Context.SequenceNumber.Value) + 1);
            return Task.FromResult(Result<KernelAppendedEvent, DuplicateEventSequenceNumber>.Failed(new DuplicateEventSequenceNumber(nextAvailable)));
        }

        var appended = BuildAppendedEvent(sequenceNumber, eventSourceType, eventSourceId, eventStreamType, eventStreamId, eventType, correlationId, causation, tags, occurred, content);
        _events.Add(appended);

        return Task.FromResult(Result<KernelAppendedEvent, DuplicateEventSequenceNumber>.Success(appended));
    }

    /// <inheritdoc/>
    public Task<Result<IEnumerable<KernelAppendedEvent>, DuplicateEventSequenceNumber>> AppendMany(
        IEnumerable<EventToAppendToStorage> events)
    {
        var appended = new List<KernelAppendedEvent>();

        foreach (var e in events)
        {
            if (_events.Exists(_ => _.Context.SequenceNumber == e.SequenceNumber))
            {
                var nextAvailable = (KernelEvents::EventSequenceNumber)(_events.Max(_ => _.Context.SequenceNumber.Value) + 1);
                return Task.FromResult(Result<IEnumerable<KernelAppendedEvent>, DuplicateEventSequenceNumber>.Failed(new DuplicateEventSequenceNumber(nextAvailable)));
            }

            var content = new Dictionary<KernelEvents::EventTypeGeneration, ExpandoObject>
            {
                { KernelEvents::EventTypeGeneration.First, e.Content }
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
                content);

            _events.Add(appendedEvent);
            appended.Add(appendedEvent);
        }

        return Task.FromResult(Result<IEnumerable<KernelAppendedEvent>, DuplicateEventSequenceNumber>.Success(appended));
    }

    /// <inheritdoc/>
    public Task Revise(
        KernelEvents::EventSequenceNumber sequenceNumber,
        KernelEvents::EventType eventType,
        CorrelationId correlationId,
        IEnumerable<KernelAuditing::Causation> causation,
        IEnumerable<KernelIdentities::IdentityId> causedByChain,
        DateTimeOffset occurred,
        ExpandoObject content,
        KernelEvents::EventHash hash) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<KernelAppendedEvent> Redact(
        KernelEvents::EventSequenceNumber sequenceNumber,
        KernelEvents::RedactionReason reason,
        CorrelationId correlationId,
        IEnumerable<KernelAuditing::Causation> causation,
        IEnumerable<KernelIdentities::IdentityId> causedByChain,
        DateTimeOffset occurred) =>
        Task.FromResult(_events.FirstOrDefault(_ => _.Context.SequenceNumber == sequenceNumber)
            ?? throw new InvalidOperationException($"No event at sequence number {sequenceNumber}"));

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEvents::EventType>> Redact(
        KernelEvents::EventSourceId eventSourceId,
        KernelEvents::RedactionReason reason,
        IEnumerable<KernelEvents::EventType>? eventTypes,
        CorrelationId correlationId,
        IEnumerable<KernelAuditing::Causation> causation,
        IEnumerable<KernelIdentities::IdentityId> causedByChain,
        DateTimeOffset occurred) =>
        Task.FromResult(Enumerable.Empty<KernelEvents::EventType>());

    /// <inheritdoc/>
    public Task<KernelEvents::EventSequenceNumber> GetHeadSequenceNumber(
        IEnumerable<KernelEvents::EventType>? eventTypes = null,
        KernelEvents::EventSourceId? eventSourceId = null)
    {
        var filtered = Filter(_events, eventSourceId, null, null, null, eventTypes).ToList();
        return filtered.Count == 0
            ? Task.FromResult(KernelEvents::EventSequenceNumber.Unavailable)
            : Task.FromResult(filtered.Min(_ => _.Context.SequenceNumber)!);
    }

    /// <inheritdoc/>
    public Task<KernelEvents::EventSequenceNumber> GetTailSequenceNumber(
        IEnumerable<KernelEvents::EventType>? eventTypes = null,
        KernelEvents::EventSourceId? eventSourceId = null,
        KernelEvents::EventSourceType? eventSourceType = null,
        KernelEvents::EventStreamId? eventStreamId = null,
        KernelEvents::EventStreamType? eventStreamType = null)
    {
        var filtered = Filter(_events, eventSourceId, eventSourceType, eventStreamType, eventStreamId, eventTypes).ToList();
        return filtered.Count == 0
            ? Task.FromResult(KernelEvents::EventSequenceNumber.Unavailable)
            : Task.FromResult(filtered.Max(_ => _.Context.SequenceNumber)!);
    }

    /// <inheritdoc/>
    public Task<TailEventSequenceNumbers> GetTailSequenceNumbers(IEnumerable<KernelEvents::EventType> eventTypes)
    {
        var eventTypeList = eventTypes.ToImmutableList();
        var tail = _events.Count == 0 ? KernelEvents::EventSequenceNumber.Unavailable : _events.Max(_ => _.Context.SequenceNumber)!;
        var filtered = Filter(_events, null, null, null, null, eventTypeList).ToList();
        var tailForEventTypes = filtered.Count == 0 ? KernelEvents::EventSequenceNumber.Unavailable : filtered.Max(_ => _.Context.SequenceNumber)!;
        return Task.FromResult(new TailEventSequenceNumbers(eventSequenceId, eventTypeList, tail, tailForEventTypes));
    }

    /// <inheritdoc/>
    public Task<IImmutableDictionary<KernelEvents::EventType, KernelEvents::EventSequenceNumber>> GetTailSequenceNumbersForEventTypes(
        IEnumerable<KernelEvents::EventType> eventTypes)
    {
        var result = eventTypes.ToImmutableDictionary(
            et => et,
            et =>
            {
                var matching = _events.FindAll(_ => _.Context.EventType == et);
                return matching.Count == 0
                    ? KernelEvents::EventSequenceNumber.Unavailable
                    : matching.Max(_ => _.Context.SequenceNumber)!;
            });

        return Task.FromResult<IImmutableDictionary<KernelEvents::EventType, KernelEvents::EventSequenceNumber>>(result);
    }

    /// <inheritdoc/>
    public Task<KernelEvents::EventSequenceNumber> GetNextSequenceNumberGreaterOrEqualThan(
        KernelEvents::EventSequenceNumber sequenceNumber,
        IEnumerable<KernelEvents::EventType>? eventTypes = null,
        KernelEvents::EventSourceId? eventSourceId = null)
    {
        var filtered = Filter(_events, eventSourceId, null, null, null, eventTypes)
            .Where(_ => _.Context.SequenceNumber >= sequenceNumber)
            .MinBy(_ => _.Context.SequenceNumber);

        return Task.FromResult(filtered?.Context.SequenceNumber ?? KernelEvents::EventSequenceNumber.Unavailable);
    }

    /// <inheritdoc/>
    public Task<bool> HasEventsFor(KernelEvents::EventSourceId eventSourceId) =>
        Task.FromResult(_events.Exists(_ => _.Context.EventSourceId == eventSourceId));

    /// <inheritdoc/>
    public Task<Catch<Option<KernelAppendedEvent>>> TryGetLastEventBefore(
        KernelEvents::EventTypeId eventTypeId,
        KernelEvents::EventSourceId eventSourceId,
        KernelEvents::EventSequenceNumber currentSequenceNumber)
    {
        var found = _events
            .Where(_ =>
                _.Context.EventType.Id == eventTypeId &&
                _.Context.EventSourceId == eventSourceId &&
                _.Context.SequenceNumber < currentSequenceNumber)
            .MaxBy(_ => _.Context.SequenceNumber);

        return Task.FromResult<Catch<Option<KernelAppendedEvent>>>(
            found is not null
                ? (Option<KernelAppendedEvent>)found
                : Option<KernelAppendedEvent>.None());
    }

    /// <inheritdoc/>
    public Task<KernelAppendedEvent> GetEventAt(KernelEvents::EventSequenceNumber sequenceNumber) =>
        Task.FromResult(_events.FirstOrDefault(_ => _.Context.SequenceNumber == sequenceNumber)
            ?? throw new InvalidOperationException($"No event at sequence number {sequenceNumber}"));

    /// <inheritdoc/>
    public Task<Option<KernelAppendedEvent>> TryGetLastInstanceOfAny(
        KernelEvents::EventSourceId eventSourceId,
        IEnumerable<KernelEvents::EventTypeId> eventTypes)
    {
        var eventTypeSet = eventTypes.ToHashSet();
        var found = _events
            .Where(_ =>
                _.Context.EventSourceId == eventSourceId &&
                eventTypeSet.Contains(_.Context.EventType.Id))
            .MaxBy(_ => _.Context.SequenceNumber);

        return Task.FromResult(found is not null
            ? (Option<KernelAppendedEvent>)found
            : Option<KernelAppendedEvent>.None());
    }

    /// <inheritdoc/>
    public Task<IEventCursor> GetFromSequenceNumber(
        KernelEvents::EventSequenceNumber sequenceNumber,
        KernelEvents::EventSourceId? eventSourceId = default,
        KernelEvents::EventStreamType? eventStreamType = default,
        KernelEvents::EventStreamId? eventStreamId = default,
        IEnumerable<KernelEvents::EventType>? eventTypes = default,
        CancellationToken cancellationToken = default)
    {
        var filtered = Filter(_events, eventSourceId, null, eventStreamType, eventStreamId, eventTypes)
            .Where(_ => _.Context.SequenceNumber >= sequenceNumber)
            .OrderBy(_ => _.Context.SequenceNumber)
            .ToList();

        return Task.FromResult<IEventCursor>(new InMemoryEventCursor(filtered));
    }

    /// <inheritdoc/>
    public Task<IEventCursor> GetRange(
        KernelEvents::EventSequenceNumber start,
        KernelEvents::EventSequenceNumber end,
        KernelEvents::EventSourceId? eventSourceId = default,
        IEnumerable<KernelEvents::EventType>? eventTypes = default,
        CancellationToken cancellationToken = default)
    {
        var filtered = Filter(_events, eventSourceId, null, null, null, eventTypes)
            .Where(_ => _.Context.SequenceNumber >= start && _.Context.SequenceNumber <= end)
            .OrderBy(_ => _.Context.SequenceNumber)
            .ToList();

        return Task.FromResult<IEventCursor>(new InMemoryEventCursor(filtered));
    }

    /// <inheritdoc/>
    public Task<IEventCursor> GetEventsWithLimit(
        KernelEvents::EventSequenceNumber start,
        int limit,
        KernelEvents::EventSourceId? eventSourceId = default,
        KernelEvents::EventStreamType? eventStreamType = default,
        KernelEvents::EventStreamId? eventStreamId = default,
        IEnumerable<KernelEvents::EventType>? eventTypes = default,
        CancellationToken cancellationToken = default)
    {
        var filtered = Filter(_events, eventSourceId, null, eventStreamType, eventStreamId, eventTypes)
            .Where(_ => _.Context.SequenceNumber >= start)
            .OrderBy(_ => _.Context.SequenceNumber)
            .Take(limit)
            .ToList();

        return Task.FromResult<IEventCursor>(new InMemoryEventCursor(filtered));
    }

    /// <inheritdoc/>
    public Task ReplaceGenerationContent(
        KernelEvents::EventSequenceNumber sequenceNumber,
        IDictionary<KernelEvents::EventTypeGeneration, ExpandoObject> content) => Task.CompletedTask;

    static IEnumerable<KernelAppendedEvent> Filter(
        IEnumerable<KernelAppendedEvent> events,
        KernelEvents::EventSourceId? eventSourceId,
        KernelEvents::EventSourceType? eventSourceType,
        KernelEvents::EventStreamType? eventStreamType,
        KernelEvents::EventStreamId? eventStreamId,
        IEnumerable<KernelEvents::EventType>? eventTypes)
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

    static KernelAppendedEvent BuildAppendedEvent(
        KernelEvents::EventSequenceNumber sequenceNumber,
        KernelEvents::EventSourceType eventSourceType,
        KernelEvents::EventSourceId eventSourceId,
        KernelEvents::EventStreamType eventStreamType,
        KernelEvents::EventStreamId eventStreamId,
        KernelEvents::EventType eventType,
        CorrelationId correlationId,
        IEnumerable<KernelAuditing::Causation> causation,
        IEnumerable<KernelEvents::Tag> tags,
        DateTimeOffset occurred,
        IDictionary<KernelEvents::EventTypeGeneration, ExpandoObject> content)
    {
        var eventContext = new KernelEvents::EventContext(
            eventType,
            eventSourceType,
            eventSourceId,
            eventStreamType,
            eventStreamId,
            sequenceNumber,
            occurred,
            KernelConcept::EventStoreName.NotSet,
            KernelConcept::EventStoreNamespaceName.NotSet,
            correlationId,
            causation,
            KernelIdentities::Identity.System,
            tags,
            KernelEvents::EventHash.NotSet);

        var eventContent = content.TryGetValue(KernelEvents::EventTypeGeneration.First, out var firstGenContent)
            ? firstGenContent
            : content.Values.FirstOrDefault() ?? new ExpandoObject();

        return new KernelAppendedEvent(eventContext, eventContent);
    }
}
