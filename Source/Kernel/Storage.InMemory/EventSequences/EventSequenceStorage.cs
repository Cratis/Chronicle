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
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventSequenceStorage"/> for the
/// kernel event sequence.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the storage serves.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the storage serves.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> this storage serves.</param>
/// <param name="identityStorage">The <see cref="IIdentityStorage"/> for resolving the identity chain that caused an append.</param>
/// <param name="effectReceipts">The namespace-wide receipt state, when owned by a namespace storage.</param>
public partial class EventSequenceStorage(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    EventSequenceId eventSequenceId,
    IIdentityStorage identityStorage,
    Occurrences.EffectReceiptState? effectReceipts) : IEventSequenceStorage, IEventPublicationStorage
{
    static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    readonly List<AppendedEvent> _events = [];
    readonly Dictionary<EventSequenceNumber, IdentityId[]> _originalCausedByChains = [];
    readonly Dictionary<EventSequenceNumber, StoredEventMetadata> _metadata = [];
    readonly Dictionary<EventSequenceNumber, uint> _appendedGenerations = [];
    readonly Lock _lock = new();
    EventSequenceNumber? _constraintIndexWatermark;
    EventSequenceNumber? _constraintIndexCatchUpBelow;
    Concepts.Events.Constraints.ConstraintName[]? _constraintIndexCatchUpRefusing;
    EventSequenceMutationEpoch _mutationEpoch = EventSequenceMutationEpoch.Initial;

    /// <summary>
    /// Initializes storage with its original constructor contract and a private receipt scope.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="eventSequenceId">The destination sequence.</param>
    /// <param name="identityStorage">The identity resolver.</param>
    public EventSequenceStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, IIdentityStorage identityStorage)
        : this(eventStore, @namespace, eventSequenceId, identityStorage, null)
    {
    }

    /// <summary>
    /// Gets the events stored in this storage instance.
    /// </summary>
    public IReadOnlyList<AppendedEvent> Events
    {
        get
        {
            lock (_lock)
            {
                return _events.Select(WithGenerationMetadata).ToImmutableList();
            }
        }
    }

    /// <inheritdoc/>
    public bool SupportsRevisionTracking => true;

    /// <summary>
    /// Gets the generation of the original append, independently of the generation currently read.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number of the event.</param>
    /// <returns>The appended generation, or null if it was not recorded.</returns>
    public uint? GetAppendedGeneration(EventSequenceNumber sequenceNumber)
    {
        lock (_lock)
        {
            return _appendedGenerations.TryGetValue(sequenceNumber, out var generation) ? generation : null;
        }
    }

    /// <inheritdoc/>
    public string SerializeContentForVerification(ExpandoObject content, JsonSchema schema) => Serialize(content);

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
                    : _events.Max(_ => _.Context.SequenceNumber.Value) + 1,
                ConstraintIndexWatermark = _constraintIndexWatermark,
                ConstraintIndexCatchUpBelow = _constraintIndexCatchUpBelow,
                ConstraintIndexCatchUpRefusing = _constraintIndexCatchUpRefusing?.ToArray(),
                MutationEpoch = _mutationEpoch
            });
        }
    }

    /// <inheritdoc/>
    public Task SaveState(EventSequenceState state)
    {
        lock (_lock)
        {
            _constraintIndexWatermark = state.ConstraintIndexWatermark;
            _constraintIndexCatchUpBelow = state.ConstraintIndexCatchUpBelow;
            var refusing = state.ConstraintIndexCatchUpRefusing?.ToArray();
            _constraintIndexCatchUpRefusing = refusing?.Length > 0 ? refusing : null;
            _mutationEpoch = state.MutationEpoch;
        }

        return Task.CompletedTask;
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
        Subject? subject = null) =>
        await Append(sequenceNumber, eventSourceType, eventSourceId, eventStreamType, eventStreamId, eventType, correlationId, causation, causedByChain, tags, occurred, content, contentHashes, subject, []);

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
        Subject? subject,
        IReadOnlyCollection<NamedTag> namedTags)
    {
        var result = await AppendMany([new EventToAppendToStorage(sequenceNumber, eventSourceType, eventSourceId, eventStreamType, eventStreamId, eventType, correlationId, causation, causedByChain, tags, occurred, new ExpandoObject(), EventHash.NotSet, subject)
        {
            GenerationalContent = content,
            ContentHashes = contentHashes,
            NamedTags = namedTags
        }]);
        return result.TryGetResult(out var appended) ? appended.Single() : result.AsT1;
    }

    /// <inheritdoc/>
    public Task<Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>> AppendManyWithNamedTags(IEnumerable<EventToAppendToStorage> events) => AppendMany(events);

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>> AppendMany(
        IEnumerable<EventToAppendToStorage> events)
    {
        var result = await AppendBatch(events, false);
        return result.TryGetResult(out var appended) ? Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(appended) : result.AsT1.AsT0;
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
        var causedBy = await identityStorage.GetFor(causedByChain).ConfigureAwait(false);
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
                causedBy,
                occurred,
                Serialize(content));

            // Persistent providers revise content, not the stored append metadata. Keep the metadata index unchanged.
            _events[index] = original with
            {
                Context = original.Context with { EventType = eventType, Hash = hash },
                Content = content,
                OriginalContent = original.IsRevised ? original.OriginalContent : FirstGenerationContent(original),
                Revisions = [.. original.Revisions, revision]
            };
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<StoredEventMetadata>> GetMetadataAt(IEnumerable<EventSequenceNumber> sequenceNumbers, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var result = new List<StoredEventMetadata>();
            foreach (var locator in sequenceNumbers.Distinct().OrderBy(_ => _.Value))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_metadata.TryGetValue(locator, out var metadata))
                {
                    result.Add(metadata);
                }
            }
            return Task.FromResult<IReadOnlyList<StoredEventMetadata>>(result);
        }
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

            // Surface the highest available generation, so observers and projections consuming a newer
            // generation receive the migrated content — and report that generation on the event type,
            // exactly as the persistent providers resolve a stored event with several generations.
            var highestGeneration = content.Keys.MaxBy(_ => _.Value) ?? original.Context.EventType.Generation;

            _events[index] = original with
            {
                Context = original.Context with { EventType = new EventType(original.Context.EventType.Id, highestGeneration, false) },
                Content = content.TryGetValue(highestGeneration, out var migrated) ? migrated : original.Content,
                GenerationalContent = content.ToDictionary(_ => (int)_.Key.Value, _ => Serialize(_.Value))
            };
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Renders event content as the JSON string form kept alongside an event for its original and
    /// generational content.
    /// </summary>
    /// <remarks>
    /// Deliberately uses this type's own options rather than the shared global ones: those are mutable
    /// process-wide state that another component may still be configuring, and touching them mid-flight
    /// throws "JsonSerializerOptions instance is read-only or has already been used".
    /// </remarks>
    /// <param name="content">The content to render.</param>
    /// <returns>The content as a JSON string.</returns>
    static string Serialize(ExpandoObject content) => JsonSerializer.Serialize(content, _serializerOptions);

    static string FirstGenerationContent(AppendedEvent appended) =>
        appended.GenerationalContent.TryGetValue((int)EventTypeGeneration.First.Value, out var first) ? first : string.Empty;

    static ImmutableArray<Causation> SnapshotCausation(IEnumerable<Causation> chain) =>
        chain.Select(cause => cause with { Properties = cause.Properties.ToImmutableDictionary() }).ToImmutableArray();

    static AppendedEvent ToAppendAcknowledgment(
        AppendedEvent stored,
        EventType eventType,
        IDictionary<EventTypeGeneration, ExpandoObject> content,
        IDictionary<EventTypeGeneration, EventHash> contentHashes) => stored with
        {
            Context = stored.Context with
            {
                EventType = eventType,
                Hash = contentHashes.TryGetValue(eventType.Generation, out var hash) ? hash : EventHash.NotSet
            },
            Content = content.TryGetValue(eventType.Generation, out var value) ? value : content.Values.FirstOrDefault() ?? new ExpandoObject()
        };

    static StoredEventMetadata BuildMetadata(AppendedEvent appended, IEnumerable<IdentityId> chain)
    {
        var context = appended.Context;
        return new StoredEventMetadata(
            context.SequenceNumber,
            context.EventType.Id,
            context.EventSourceType,
            context.EventSourceId,
            context.EventStreamType,
            context.EventStreamId,
            context.Occurred,
            context.CorrelationId,
            SnapshotCausation(context.Causation),
            chain.ToImmutableArray(),
            context.Tags.ToImmutableArray(),
            context.Subject,
            context.EventSource);
    }

    void TrackMetadata(AppendedEvent appended, IEnumerable<IdentityId> chain) => _metadata[appended.Context.SequenceNumber] = BuildMetadata(appended, chain);

    AppendedEvent BuildAppendedEvent(
        EventSequenceNumber sequenceNumber,
        EventSourceType eventSourceType,
        EventSourceId eventSourceId,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        EventType eventType,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        IEnumerable<Tag> tags,
        DateTimeOffset occurred,
        IDictionary<EventTypeGeneration, ExpandoObject> content,
        IDictionary<EventTypeGeneration, EventHash> contentHashes,
        Subject? subject = null,
        IReadOnlyCollection<NamedTag>? namedTags = null,
        EventSourceName? eventSource = null,
        EventSequenceNumber? compensationFor = null)
    {
        // Default reads use the highest stored generation; append acknowledgments retain the appended one.
        var highestGeneration = content.Keys.MaxBy(_ => _.Value) ?? eventType.Generation;
        var eventContext = new EventContext(
            eventType with { Generation = highestGeneration },
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
            causedBy,
            tags,
            contentHashes.TryGetValue(highestGeneration, out var hash) ? hash : EventHash.NotSet,
            Subject: subject?.IsSet is true ? subject : new Subject(eventSourceId.Value))
        {
            NamedTags = namedTags ?? [],
            EventSource = eventSource ?? EventSourceName.NotSet,
            CompensationFor = compensationFor,
            AppendedGeneration = eventType.Generation
        };

        var eventContent = content.TryGetValue(highestGeneration, out var generationContent)
            ? generationContent
            : content.Values.FirstOrDefault() ?? new ExpandoObject();

        return new AppendedEvent(eventContext, eventContent)
        {
            GenerationalContent = content.ToDictionary(kvp => (int)kvp.Key.Value, kvp => Serialize(kvp.Value)),
            GenerationalHashes = contentHashes.ToDictionary(_ => (int)_.Key.Value, _ => _.Value)
        };
    }

    async Task<(EventToAppendToStorage[] Events, List<Identity> CausedByPerEvent)> SnapshotBatch(IEnumerable<EventToAppendToStorage> events)
    {
        var causedByPerEvent = new List<Identity>();
        try
        {
            var eventsToAppend = events.Select(e => e with
            {
                CausedByChain = e.CausedByChain.ToArray(),
                Causation = SnapshotCausation(e.Causation),
                Tags = e.Tags.ToArray(),
                ContentHashes = new Dictionary<EventTypeGeneration, EventHash>(e.ContentHashes)
            }).ToArray();
            foreach (var e in eventsToAppend)
            {
                causedByPerEvent.Add(await identityStorage.GetFor(e.CausedByChain).ConfigureAwait(false));
            }

            return (eventsToAppend, causedByPerEvent);
        }
        catch (Exception error)
        {
            throw new EventsNotCommitted(error);
        }
    }

    DuplicateEventSequenceNumber? FindDuplicateSequenceNumber(EventToAppendToStorage[] eventsToAppend)
    {
        var sequenceNumbers = new HashSet<EventSequenceNumber>();
        if (!eventsToAppend.Any(@event => !sequenceNumbers.Add(@event.SequenceNumber) || _events.Exists(existing => existing.Context.SequenceNumber == @event.SequenceNumber))) return null;

        var nextAvailable = _events.Count == 0
            ? EventSequenceNumber.First
            : (EventSequenceNumber)(_events.Max(@event => @event.Context.SequenceNumber.Value) + 1);
        return new DuplicateEventSequenceNumber(nextAvailable);
    }

    (List<AppendedEvent> Stored, StoredEventMetadata[] Metadata) BuildBatch(EventToAppendToStorage[] eventsToAppend, List<Identity> causedByPerEvent, List<AppendedEvent> appended)
    {
        var stored = new List<AppendedEvent>();
        try
        {
            for (var index = 0; index < eventsToAppend.Length; index++)
            {
                var e = eventsToAppend[index];
                var appendedEvent = BuildAppendedEvent(
                    e.SequenceNumber,
                    e.EventSourceType,
                    e.EventSourceId,
                    e.EventStreamType,
                    e.EventStreamId,
                    e.EventType,
                    e.CorrelationId,
                    e.Causation,
                    causedByPerEvent[index],
                    e.Tags,
                    e.Occurred,
                    e.GenerationalContent,
                    e.ContentHashes,
                    e.Subject,
                    e.NamedTags,
                    e.EventSource,
                    e.CompensationFor);

                stored.Add(appendedEvent);
                appended.Add(ToAppendAcknowledgment(appendedEvent, e.EventType, e.GenerationalContent, e.ContentHashes));
            }
            var metadata = stored.Select((e, index) => BuildMetadata(e, eventsToAppend[index].CausedByChain)).ToArray();
            return (stored, metadata);
        }
        catch (Exception error)
        {
            throw new EventsNotCommitted(error);
        }
    }
}
