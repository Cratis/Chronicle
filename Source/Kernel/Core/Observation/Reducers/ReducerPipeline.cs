// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.ReadModels;
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
/// <param name="readModelsCompliance">The <see cref="IReadModelsCompliance"/> for encrypting and decrypting PII fields.</param>
/// <param name="eventStore">The <see cref="EventStoreName"/> this pipeline belongs to.</param>
/// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> this pipeline belongs to.</param>
/// <param name="guardWritesOnWatermark">Whether a write may be made conditional on advancing the read model's watermark.</param>
public class ReducerPipeline(
    ReadModelDefinition readModel,
    ISink sink,
    IObjectComparer objectComparer,
    IReadModelsCompliance readModelsCompliance,
    EventStoreName eventStore,
    EventStoreNamespaceName eventStoreNamespace,
    bool guardWritesOnWatermark = true) : IReducerPipeline
{
    /// <inheritdoc/>
    public ReadModelDefinition ReadModel { get; } = readModel;

    /// <inheritdoc/>
    public ISink Sink { get; } = sink;

    /// <inheritdoc/>
    public Task BeginReplay(ReplayContext context) => Sink.BeginReplay(context);

    /// <inheritdoc/>
    public Task EndReplay(ReplayContext context) => Sink.EndReplay(context);

    /// <inheritdoc/>
    public Task BeginBulk() => Sink.BeginBulk();

    /// <inheritdoc/>
    public Task EndBulk() => Sink.EndBulk();

    /// <inheritdoc/>
    public async Task Reduce(ReducerContext context, ReducerDelegate reducer)
    {
        var schema = ReadModel.GetSchemaForLatestGeneration();
        var initial = await Sink.FindOrDefault(context.Key);

        if (initial is not null)
        {
            initial = await readModelsCompliance.Release(
                eventStore,
                eventStoreNamespace,
                schema,
                initial);
        }

        var result = await reducer(context.Events, initial);

        if (result.ObserverResult.State != ObserverSubscriberState.Ok) return;

        var identifier = context.Events.First().Context.ResolveComplianceIdentifier(context.Key);

        var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, context.Events.First(), initial ?? new ExpandoObject());
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

            if (!objectComparer.Compare(initial, encryptedState, out var differences))
            {
                // The comparer has no child identity for reducer-owned collections. A nested,
                // unindexed array path cannot be applied safely by sinks, so replace that collection.
                changeset.Add(new PropertiesChanged<ExpandoObject>(
                    null!,
                    differences.Collapse(initial, encryptedState)));
            }
        }

        if (changeset.HasChanges)
        {
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
            var failedPartitions = await Sink.ApplyChanges(context.Key, changeset, context.Events.Last().Context.SequenceNumber, mode);

            if (failedPartitions.Any())
            {
                var firstFailure = failedPartitions.First();
                throw new InvalidOperationException($"Bulk operation failed for partition {firstFailure.EventSourceId} at sequence number {firstFailure.EventSequenceNumber}");
            }
        }
    }
}
