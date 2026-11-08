// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerPipeline"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerPipeline"/> class.
/// </remarks>
/// <param name="readModel">The <see cref="ReadModelDefinition"/> the sink is for.</param>
/// <param name="sink"><see cref="ISink"/> to use in pipeline.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="readModelsCompliance">The <see cref="IReadModelsCompliance"/> for encrypting and decrypting compliance and security fields.</param>
/// <param name="eventStore">The <see cref="EventStoreName"/> this pipeline belongs to.</param>
/// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> this pipeline belongs to.</param>
/// <param name="guardWritesOnWatermark">Whether a write may be made conditional on advancing the read model's watermark.</param>
/// <param name="expandoObjectConverter">The converter used to serialize committed state for watchers.</param>
/// <param name="memoryGate">Optional <see cref="ReducerMemoryGate"/> that makes reduces of large states wait for room in the memory budget.</param>
public partial class ReducerPipeline(
    ReadModelDefinition readModel,
    ISink sink,
    IObjectComparer objectComparer,
    IReadModelsCompliance readModelsCompliance,
    EventStoreName eventStore,
    EventStoreNamespaceName eventStoreNamespace,
    bool guardWritesOnWatermark = true,
    IExpandoObjectConverter? expandoObjectConverter = null,
    ReducerMemoryGate? memoryGate = null) : IReducerPipeline
{
    readonly string _memoryScope = $"{eventStore}/{eventStoreNamespace}/{readModel.Identifier}";

    readonly IExpandoObjectConverter _expandoObjectConverter = expandoObjectConverter ?? new ExpandoObjectConverter(new TypeFormats());

    /// <inheritdoc/>
    public ReadModelDefinition ReadModel { get; } = readModel;

    /// <inheritdoc/>
    public ISink Sink { get; } = sink;

    /// <inheritdoc/>
    public Task BeginReplay(ReplayContext context) => Sink.BeginReplay(context);

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndReplay(ReplayContext context) => Sink.EndReplay(context);

    /// <inheritdoc/>
    public Task BeginBulk() => Sink.BeginBulk();

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndBulk() => Sink.EndBulk();

    /// <inheritdoc/>
    public long StateSizeOf(Key partition) => memoryGate?.RememberedSizeOf(_memoryScope, partition) ?? 0;

    /// <inheritdoc/>
    public async Task<ReadModelChangeNotification?> Reduce(ReducerContext context, ReducerDelegate reducer, bool createNotification = true)
    {
        using var memoryTicket = memoryGate is null ? ReducerMemoryTicket.None : await memoryGate.Enter(_memoryScope, context.Key);
        var storedInitial = await Sink.FindOrDefault(context.Key);
        if (memoryGate is not null)
        {
            await memoryGate.Observe(memoryTicket, _memoryScope, context.Key, storedInitial);
        }

        var stored = await Release(storedInitial);

        var result = await reducer(context.Events, stored.Released);

        if (result.ObserverResult.State != ObserverSubscriberState.Ok) return null;

        var (notification, failures) = await Write(context, stored, result, createNotification);
        if (failures.Length > 0)
        {
            throw new ReducerSinkWriteFailed(failures);
        }

        return notification;
    }

    /// <summary>
    /// Releases the state a partition's read model holds for the reducer.
    /// </summary>
    /// <param name="storedInitial">The state as the sink holds it, or null when there is none.</param>
    /// <returns>The <see cref="StoredState"/> of the partition.</returns>
    async Task<StoredState> Release(ExpandoObject? storedInitial)
    {
        if (storedInitial is null)
        {
            return new(null, null, null);
        }

        ((IDictionary<string, object?>)storedInitial).TryGetValue(WellKnownProperties.Subject, out var storedSubject);
        var released = await readModelsCompliance.Release(
            eventStore,
            eventStoreNamespace,
            ReadModel.GetTargetSchema(),
            storedInitial);
        return new(storedInitial, released, storedSubject);
    }

    /// <summary>
    /// Writes the state the reducer returned for a partition.
    /// </summary>
    /// <param name="context">The <see cref="ReducerContext"/> of the partition.</param>
    /// <param name="stored">The <see cref="StoredState"/> the partition was reduced from.</param>
    /// <param name="result">The <see cref="ReducerSubscriberResult"/> the reducer returned.</param>
    /// <param name="createNotification">Whether to build the notification of the change.</param>
    /// <returns>The change to notify watchers of, if any, and the partitions the sink reported as failed.</returns>
    async Task<(ReadModelChangeNotification? Notification, FailedPartition[] Failures)> Write(ReducerContext context, StoredState stored, ReducerSubscriberResult result, bool createNotification)
    {
        if (await Prepare(context, stored, result) is not { } write)
        {
            return (null, []);
        }

        var failures = (await Sink.ApplyChanges(write.Key, write.Changeset, write.EventSequenceNumber, write.Mode)).ToArray();
        if (failures.Length > 0)
        {
            return (null, failures);
        }

        // The notification is a whole new copy of the state, so it is only built when something is watching.
        return (createNotification ? CreateNotification(context, result.ReadModelState, stored.Released, write.StoredState) : null, []);
    }

    /// <summary>
    /// Works out what to write for the state the reducer returned for a partition.
    /// </summary>
    /// <param name="context">The <see cref="ReducerContext"/> of the partition.</param>
    /// <param name="stored">The <see cref="StoredState"/> the partition was reduced from.</param>
    /// <param name="result">The <see cref="ReducerSubscriberResult"/> the reducer returned.</param>
    /// <returns>The <see cref="PreparedWrite"/>, or null when the state did not change.</returns>
    async Task<PreparedWrite?> Prepare(ReducerContext context, StoredState stored, ReducerSubscriberResult result)
    {
        var schema = ReadModel.GetTargetSchema();
        var storedInitial = stored.Stored;
        var initial = stored.Released;
        var identifier = context.Events.First().Context.ResolveComplianceIdentifier(context.Key);

        // Sinks apply encrypted differences to the stored snapshot, not the released schema-only state.
        // In-memory sinks also need its subject metadata when the subject itself has not changed.
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, context.Events.First(), storedInitial ?? new ExpandoObject());
        var storedState = storedInitial;
        if (result.ReadModelState is null)
        {
            if (initial is not null)
            {
                changeset.Add(new Removed(initial));
            }
        }
        else
        {
            var encryptedState = await readModelsCompliance.Apply(
                eventStore,
                eventStoreNamespace,
                schema,
                identifier,
                result.ReadModelState);

            storedState = encryptedState;

            // Reducer results are rebuilt from the schema and do not carry sink bookkeeping.
            // Compare a copy so removing those fields cannot mutate a sink's cached instance.
            ExpandoObject? initialForComparison = null;
            if (initial is not null)
            {
                initialForComparison = new ExpandoObject();
                var declaredProperties = schema.GetFlattenedProperties().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
                foreach (var (name, value) in initial.Where(property => !WellKnownProperties.All.Contains(property.Key) || declaredProperties.Contains(property.Key)))
                {
                    ((IDictionary<string, object?>)initialForComparison)[name] = value;
                }
                if (stored.Subject is not null)
                {
                    ((IDictionary<string, object?>)initialForComparison)[WellKnownProperties.Subject] = stored.Subject;
                }
            }

            if (!objectComparer.Compare(initialForComparison, encryptedState, out var differences))
            {
                // The comparer has no child identity for reducer-owned collections. A nested,
                // unindexed array path cannot be applied safely by sinks, so replace that collection.
                changeset.ReplaceState(encryptedState, differences.Collapse(initialForComparison, encryptedState));
            }
        }

        if (!changeset.HasChanges) return null;

        // A reducer read model is always keyed by event source id, so its per-document event stream is
        // monotonic and a redelivered batch can be recognized by the sequence number it carries. The batch
        // that creates the instance is written unconditionally — there is no document to compare against,
        // and a guarded write never inserts.
        //
        // The guard recognizes a redelivered batch by its LAST sequence number, so it neutralizes a redelivery
        // whose batches reproduce the original boundaries. A resume that re-cuts them does not: the step
        // restarts the cursor at the checkpoint's successor, and the driver's first page is a fixed document
        // count while later pages are size-limited, so resumed pages routinely straddle the watermark and the
        // already-applied prefix of such a batch is folded twice. Comparing against the last sequence number
        // rather than the first is the deliberate choice there: it double-counts a straddling batch instead of
        // dropping the part of it that is genuinely new. Closing the gap needs a per-document applied-range
        // ledger, which is a separate design.
        var mode = guardWritesOnWatermark && initial is not null
            ? SinkWriteMode.OnlyWhenAdvancingWatermark
            : SinkWriteMode.Always;
        return new(context.Key, changeset, context.Events.Last().Context.SequenceNumber, mode, storedState);
    }

    ReadModelChangeNotification CreateNotification(ReducerContext context, ExpandoObject? result, ExpandoObject? initial, ExpandoObject? storedState)
    {
        var schema = ReadModel.GetTargetSchema();
        var changeType = (result, initial) switch
        {
            (null, _) => ReadModelChangeType.Removed,
            (_, null) => ReadModelChangeType.Added,
            _ => ReadModelChangeType.Modified
        };
        var state = storedState ?? new ExpandoObject();
        var model = _expandoObjectConverter.ToJsonObject(state, schema);
        ReadModelChangeBookkeeping.CopyLegacySubjectLineage(state, model);
        var (subject, subjects) = ReadModelChangeBookkeeping.LineageOf(state);
        var last = context.Events.Last().Context;
        return new ReadModelChangeNotification(
            context.Key.ToString(),
            model,
            new ReadModelChangeContext(changeType, last.SequenceNumber, last.Occurred, last.CorrelationId)
            {
                Subject = subject,
                Subjects = subjects
            });
    }

    /// <summary>
    /// Represents the state a partition's read model holds when it is reduced.
    /// </summary>
    /// <param name="Stored">The state as the sink holds it, or null when there is none.</param>
    /// <param name="Released">The state released for the reducer, or null when there is none.</param>
    /// <param name="Subject">The subject the stored state carries, if any.</param>
    sealed record StoredState(ExpandoObject? Stored, ExpandoObject? Released, object? Subject);

    /// <summary>
    /// Represents what to write for a partition.
    /// </summary>
    /// <param name="Key">The <see cref="Key"/> of the partition.</param>
    /// <param name="Changeset">The changes to apply.</param>
    /// <param name="EventSequenceNumber">The sequence number of the last event reduced.</param>
    /// <param name="Mode">The <see cref="SinkWriteMode"/> to write with.</param>
    /// <param name="StoredState">The state the sink holds once the write is applied.</param>
    sealed record PreparedWrite(Key Key, Changeset<AppendedEvent, ExpandoObject> Changeset, EventSequenceNumber EventSequenceNumber, SinkWriteMode Mode, ExpandoObject? StoredState)
    {
        /// <summary>
        /// Gets the write as a <see cref="SinkPageWrite"/>.
        /// </summary>
        public SinkPageWrite AsPageWrite => new(Key, Changeset, EventSequenceNumber, Mode);
    }
}
