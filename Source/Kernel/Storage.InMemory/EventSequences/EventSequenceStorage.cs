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
using Cratis.Json;
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
        EventHash hash)
    {
        lock (_lock)
        {
            var index = _events.FindIndex(_ => _.Context.SequenceNumber == sequenceNumber);
            if (index < 0)
            {
                throw new NoEventAtSequenceNumber(eventSequenceId, sequenceNumber);
            }

            var original = _events[index];
            var revision = new EventRevision(
                eventType.Generation,
                correlationId,
                causation,
                Identity.System,
                occurred,
                Serialize(content));

            _events[index] = original with
            {
                Context = original.Context with { EventType = eventType, Hash = hash },
                Content = content,
                OriginalContent = original.IsRevised ? original.OriginalContent : Serialize(original.Content),
                Revisions = [.. original.Revisions, revision]
            };
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<AppendedEvent> Redact(
        EventSequenceNumber sequenceNumber,
        RedactionReason reason,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred)
    {
        lock (_lock)
        {
            var index = _events.FindIndex(_ => _.Context.SequenceNumber == sequenceNumber);
            if (index < 0)
            {
                throw new NoEventAtSequenceNumber(eventSequenceId, sequenceNumber);
            }

            var original = _events[index];

            // Already redacted — return it as-is so the caller can skip the duplicate rewind, matching
            // how the persistent providers signal "redaction already applied".
            if (original.Context.EventType.Id == GlobalEventTypes.Redaction)
            {
                return Task.FromResult(original);
            }

            _events[index] = Redacted(original, reason, correlationId, causation, causedByChain, occurred);

            // The pre-redaction event is returned, as the persistent providers do.
            return Task.FromResult(original);
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventType>> Redact(
        EventSourceId eventSourceId,
        RedactionReason reason,
        IEnumerable<EventType>? eventTypes,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred)
    {
        var affectedEventTypes = new HashSet<EventType>();
        var eventTypeIds = eventTypes?.Select(_ => _.Id).ToHashSet();

        lock (_lock)
        {
            for (var index = 0; index < _events.Count; index++)
            {
                var original = _events[index];
                if (original.Context.EventSourceId != eventSourceId ||
                    original.Context.EventType.Id == GlobalEventTypes.Redaction ||
                    (eventTypeIds?.Count > 0 && !eventTypeIds.Contains(original.Context.EventType.Id)))
                {
                    continue;
                }

                affectedEventTypes.Add(new EventType(original.Context.EventType.Id, EventTypeGeneration.First, false));
                _events[index] = Redacted(original, reason, correlationId, causation, causedByChain, occurred);
            }
        }

        return Task.FromResult<IEnumerable<EventType>>(affectedEventTypes);
    }

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
        IDictionary<EventTypeGeneration, ExpandoObject> content)
    {
        lock (_lock)
        {
            var index = _events.FindIndex(_ => _.Context.SequenceNumber == sequenceNumber);
            if (index < 0)
            {
                throw new NoEventAtSequenceNumber(eventSequenceId, sequenceNumber);
            }

            var original = _events[index];
            var latest = content
                .OrderByDescending(_ => _.Key.Value)
                .Select(_ => _.Value)
                .FirstOrDefault() ?? original.Content;

            _events[index] = original with
            {
                Content = latest,
                GenerationalContent = content.ToDictionary(_ => (int)_.Key.Value, _ => Serialize(_.Value))
            };
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Produces the redacted replacement for an event, mirroring how the persistent providers redact in place:
    /// the event type becomes <see cref="GlobalEventTypes.Redaction"/>, the payload is replaced with a
    /// <see cref="RedactionEventContent"/> describing what was redacted, and the auditing fields become the
    /// redaction's. The sequence number and event source are preserved.
    /// </summary>
    /// <param name="original">The event being redacted.</param>
    /// <param name="reason">The <see cref="RedactionReason"/>.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> of the redaction.</param>
    /// <param name="causation">The causation chain behind the redaction.</param>
    /// <param name="causedByChain">The identities that caused the redaction.</param>
    /// <param name="occurred">When the redaction occurred.</param>
    /// <returns>The redacted <see cref="AppendedEvent"/>.</returns>
    static AppendedEvent Redacted(
        AppendedEvent original,
        RedactionReason reason,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred)
    {
        // This provider stores a single Identity per event rather than an identity chain, so the original
        // chain cannot be carried into the redaction content; everything else mirrors the persistent providers.
        var content = new RedactionEventContent(
            reason,
            original.Context.EventType.Id,
            original.Context.Occurred,
            original.Context.CorrelationId,
            original.Context.Causation,
            causedByChain);

        return original with
        {
            Context = original.Context with
            {
                EventType = new EventType(GlobalEventTypes.Redaction, EventTypeGeneration.First, false),
                Occurred = occurred,
                CorrelationId = correlationId,
                Causation = causation,
                CausedBy = Identity.System
            },
            Content = ToExpandoObject(content),
            OriginalContent = string.Empty,
            GenerationalContent = new Dictionary<int, string>()
        };
    }

    static ExpandoObject ToExpandoObject(RedactionEventContent content)
    {
        var expando = new ExpandoObject();
        var values = (IDictionary<string, object?>)expando;
        values["reason"] = content.Reason.Value;
        values["originalEventType"] = content.OriginalEventType.Value;
        values["occurred"] = content.Occurred;
        values["correlationId"] = content.CorrelationId.Value;
        values["causation"] = content.Causation;
        values["causedBy"] = content.CausedBy;
        return expando;
    }

    static string Serialize(ExpandoObject content) => JsonSerializer.Serialize(content, Globals.JsonSerializerOptions);

    /// <summary>
    /// Narrows the events by the supplied criteria, matching how the persistent storage providers build their queries.
    /// </summary>
    /// <remarks>
    /// Each criterion carries a sentinel meaning "do not narrow on this dimension" — an unspecified
    /// <see cref="EventSourceId"/>, a default/unspecified <see cref="EventSourceType"/>,
    /// <see cref="EventStreamType.All"/>, the default <see cref="EventStreamId"/>, and an empty event type set.
    /// Callers asking for "everything" pass those sentinels rather than <see langword="null"/>, so treating a
    /// sentinel as a value to match on would narrow every event away.
    /// </remarks>
    /// <param name="events">The events to narrow.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to narrow by, or its unspecified sentinel.</param>
    /// <param name="eventSourceType">The <see cref="EventSourceType"/> to narrow by, or its default/unspecified sentinel.</param>
    /// <param name="eventStreamType">The <see cref="EventStreamType"/> to narrow by, or <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">The <see cref="EventStreamId"/> to narrow by, or its default sentinel.</param>
    /// <param name="eventTypes">The event types to narrow by, or an empty set.</param>
    /// <returns>The narrowed events.</returns>
    static IEnumerable<AppendedEvent> Filter(
        IEnumerable<AppendedEvent> events,
        EventSourceId? eventSourceId,
        EventSourceType? eventSourceType,
        EventStreamType? eventStreamType,
        EventStreamId? eventStreamId,
        IEnumerable<EventType>? eventTypes)
    {
        if (eventSourceId?.IsSpecified == true)
        {
            events = events.Where(_ => _.Context.EventSourceId == eventSourceId);
        }

        if (eventSourceType?.IsDefaultOrUnspecified == false)
        {
            events = events.Where(_ => _.Context.EventSourceType == eventSourceType);
        }

        if (eventStreamType?.IsAll == false)
        {
            events = events.Where(_ => _.Context.EventStreamType == eventStreamType);
        }

        if (eventStreamId?.IsDefault == false)
        {
            events = events.Where(_ => _.Context.EventStreamId == eventStreamId);
        }

        var typeSet = eventTypes?.ToHashSet();
        if (typeSet?.Count > 0)
        {
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
