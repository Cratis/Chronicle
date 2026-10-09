// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;

namespace Cratis.Chronicle.Sinks;

/// <summary>
/// Represents an <see cref="ISink"/> that publishes folded state as NEW events on an event sequence.
/// </summary>
/// <remarks>
/// <para>
/// The published event IS the durable state: the current state of a key is the content of the last event published for
/// it in the current occurrence. State and publication therefore commit in one atomic append — there is no window between
/// a state write and an append. Every step carries a deterministic publication identity built from store, namespace,
/// destination, observer, replay occurrence, target key and input position, and the append is idempotent on that identity,
/// so a retried or redelivered step returns the already committed event instead of creating another instance.
/// </para>
/// <para>
/// A mapping change replays under a new occurrence: state starts empty and new public instances are appended. History is
/// never rewritten and never removed. Removal of state cannot be expressed as a public fact and fails explicitly.
/// </para>
/// </remarks>
/// <param name="eventStore">The <see cref="EventStoreName"/> the sink belongs to.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the sink belongs to.</param>
/// <param name="definition">The <see cref="ReadModelDefinition"/> of the target.</param>
/// <param name="configuration">The <see cref="EventSequenceSinkConfiguration"/>.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for reaching the destination sequence.</param>
/// <param name="storage">The <see cref="IStorage"/> used for reading published state.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/>.</param>
public class EventSequenceSink(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    ReadModelDefinition definition,
    EventSequenceSinkConfiguration configuration,
    IGrainFactory grainFactory,
    IStorage storage,
    IExpandoObjectConverter expandoObjectConverter) : ISink
{
    /// <summary>
    /// The occurrence used when no replay has happened.
    /// </summary>
    public const string InitialOccurrence = "initial";

    /// <summary>
    /// The causation type linking a published event to the processing step that produced it.
    /// </summary>
    public static readonly CausationType CausationType = new("EventSequenceSink");

    const string OccurrenceProperty = "occurrence";
    const string InputPositionProperty = "inputPosition";
    const string PublicationProperty = "publication";
    const string ObserverProperty = "observer";

    string? _replayOccurrence;

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.EventSequence;

    /// <inheritdoc/>
    public async Task<ExpandoObject?> FindOrDefault(Key key)
    {
        var last = await FindLast(key);
        if (last is null) return null;

        // A replay folds from empty state: state published under another occurrence is not input to it.
        if (_replayOccurrence is not null && OccurrenceOf(last) != _replayOccurrence) return null;

        // Pipelines add bookkeeping to the state they are handed; never let that reach the stored event.
        return last.Content.Clone();
    }

    /// <inheritdoc/>
    public Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue) => Task.FromResult(Option<Key>.None());

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber) =>
        ApplyChanges(key, changeset, eventSequenceNumber, SinkWriteMode.Always);

    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, SinkWriteMode mode)
    {
        if (changeset.HasBeenRemoved() || changeset.HasRemoved())
        {
            return [Failed(key, eventSequenceNumber, "Removal cannot be published: public events are facts and are never rewritten or retracted.")];
        }

        if (!changeset.HasChanges) return [];

        var last = await FindLast(key);
        var occurrence = _replayOccurrence ?? (last is null ? InitialOccurrence : OccurrenceOf(last));

        if (mode == SinkWriteMode.OnlyWhenAdvancingWatermark && last is not null && OccurrenceOf(last) == occurrence && InputPositionOf(last) >= eventSequenceNumber.Value)
        {
            // Already published for this position or a later one: redelivery, nothing new to say.
            return [];
        }

        var source = changeset.Incoming;
        var schema = definition.GetTargetSchema();
        var content = expandoObjectConverter.ToJsonObject(changeset.CurrentState.WithoutBookkeeping(schema), schema);
        var destination = configuration.Destination;
        var keyText = key.Value?.ToString() ?? string.Empty;
        var inputPosition = eventSequenceNumber.Value.ToString();
        var observer = ObserverName();

        var publicationId = PublicationIdentity.For(eventStore.Value, @namespace.Value, destination.Value, observer, occurrence, keyText, inputPosition);

        var causation = source.Context.Causation.Append(new Causation(
            source.Context.Occurred,
            CausationType,
            new Dictionary<string, string>
            {
                [ObserverProperty] = observer,
                [OccurrenceProperty] = occurrence,
                [InputPositionProperty] = inputPosition,
                [PublicationProperty] = publicationId
            })).ToArray();

        var subject = source.Context.Subject is { IsSet: true } ? source.Context.Subject : null;
        var fingerprint = PublicationIdentity.Fingerprint(
            configuration.EventType.ToString(),
            content.ToJsonString(),
            subject?.Value ?? string.Empty,
            source.Context.CorrelationId.ToString(),
            source.Context.CausedBy.Subject,
            string.Join(';', causation.Select(_ => $"{_.Type}:{string.Join(',', _.Properties.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}"))}")));

        var @event = new EventToAppend(
            EventSourceType.Default,
            new EventSourceId(keyText),
            EventStreamType.All,
            EventStreamId.Default,
            configuration.EventType,
            [],
            content,
            source.Context.Occurred,
            subject);

        var result = await EventSequence(destination).AppendPublication(publicationId, fingerprint, @event, source.Context.CorrelationId, causation, source.Context.CausedBy);
        if (result.IsSuccess) return [];

        return [Failed(key, eventSequenceNumber, DescribeFailure(result))];
    }

    /// <inheritdoc/>
    public Task BeginBulk() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndBulk() => Task.FromResult<IEnumerable<FailedPartition>>([]);

    /// <inheritdoc/>
    public Task BeginReplay(ReplayContext context)
    {
        _replayOccurrence = OccurrenceFor(context);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ResumeReplay(ReplayContext context)
    {
        _replayOccurrence = OccurrenceFor(context);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndReplay(ReplayContext context)
    {
        // The occurrence stays effective through the state published in it; it is rediscovered from that state.
        _replayOccurrence = null;
        return Task.FromResult<IEnumerable<FailedPartition>>([]);
    }

    /// <inheritdoc/>
    public Task LeaveReplay()
    {
        _replayOccurrence = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Remove(ReadModelContainerName containerName) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task PrepareInitialRun() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EnsureIndexes() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<ReadModelInstances> GetInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50) =>
        Task.FromResult(new ReadModelInstances([], 0));

    /// <inheritdoc/>
    public IObservable<IEnumerable<ExpandoObject>> ObserveInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50) =>
        Observable.Empty<IEnumerable<ExpandoObject>>();

    /// <summary>
    /// Gets the replay occurrence for a context. The revert container name carries a unique suffix that is persisted with
    /// the replay context, so the occurrence is stable across a resumed replay and a restart. The start time is not used
    /// because providers store it with different precision.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/>.</param>
    /// <returns>The occurrence text.</returns>
    static string OccurrenceFor(ReplayContext context) => $"{context.ContainerName}@{context.RevertContainerName}";

    static string? Property(AppendedEvent @event, string name)
    {
        var link = @event.Context.Causation.LastOrDefault(_ => _.Type == CausationType);
        return link is not null && link.Properties.TryGetValue(name, out var value) ? value : null;
    }

    static string OccurrenceOf(AppendedEvent @event) => Property(@event, OccurrenceProperty) ?? InitialOccurrence;

    static ulong InputPositionOf(AppendedEvent @event) => ulong.TryParse(Property(@event, InputPositionProperty), out var position) ? position : 0;

    static string DescribeFailure(AppendResult result) =>
        string.Join(
            "; ",
            result.Errors.Select(_ => _.Value)
                .Concat(result.ConstraintViolations.Select(_ => _.Message.Value))
                .Concat(result.HasConcurrencyViolations ? ["Concurrency violation"] : []));

    static FailedPartition Failed(Key key, EventSequenceNumber sequenceNumber, string reason) => new(key, sequenceNumber) { Reason = reason };

    string ObserverName() => $"{definition.ObserverType}/{definition.ObserverIdentifier}/{definition.Identifier}";

    IEventSequence EventSequence(Concepts.EventSequences.EventSequenceId destination) =>
        grainFactory.GetEventSequence(destination, eventStore, @namespace);

    /// <summary>
    /// Finds the last event of the target type for the key that THIS observer published. Events of the same type
    /// published by anything else - another observer or an ordinary append - are not this target's state.
    /// </summary>
    /// <param name="key">The <see cref="Key"/> of the target instance.</param>
    /// <returns>The last event this observer published for the key, or null.</returns>
    async Task<AppendedEvent?> FindLast(Key key)
    {
        var sequenceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(configuration.Destination);
        var eventSourceId = new EventSourceId(key.Value?.ToString() ?? string.Empty);
        var observer = ObserverName();
        var last = await sequenceStorage.TryGetLastInstanceOfAny(eventSourceId, [configuration.EventType.Id]);
        while (last.TryGetValue(out var found))
        {
            if (Property(found, ObserverProperty) == observer) return found;

            var before = await sequenceStorage.TryGetLastEventBefore(configuration.EventType.Id, eventSourceId, found.Context.SequenceNumber);
            last = before.Match(option => option, exception => throw exception);
        }

        return null;
    }
}
