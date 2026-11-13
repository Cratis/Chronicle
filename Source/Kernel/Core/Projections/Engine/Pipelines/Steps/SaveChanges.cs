// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that saves changes.
/// </summary>
public class SaveChanges : ICanPerformProjectionPipelineStep
{
    readonly ISink _sink;
    readonly IChangesetStorage _changesetStorage;
    readonly bool _guardWritesOnWatermark;
    readonly ILogger<SaveChanges> _logger;
    readonly IProjectionFuturesStore? _projectionFutures;
    readonly ReadModelContainerName? _changesetContainer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveChanges"/> class.
    /// </summary>
    /// <param name="sink"><see cref="ISink"/> to use.</param>
    /// <param name="changesetStorage"><see cref="IChangesetStorage"/> for storing changesets as they occur.</param>
    /// <param name="guardWritesOnWatermark">Whether a write may be made conditional on advancing the read model's watermark.</param>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    /// <param name="projectionFutures">The persisted futures to acknowledge after successful writes.</param>
    /// <param name="changesetContainer">The container changesets are recorded under, when it is not the read model's own container - as for a materialization generation's destination.</param>
    public SaveChanges(ISink sink, IChangesetStorage changesetStorage, bool guardWritesOnWatermark, ILogger<SaveChanges> logger, IProjectionFutures? projectionFutures = null, ReadModelContainerName? changesetContainer = null)
        : this(projectionFutures is null ? null : new ProjectionFuturesStore(projectionFutures), sink, changesetStorage, guardWritesOnWatermark, logger, changesetContainer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveChanges"/> class.
    /// </summary>
    /// <param name="futuresStore">The persisted futures to acknowledge after successful writes.</param>
    /// <param name="sink"><see cref="ISink"/> to use.</param>
    /// <param name="changesetStorage"><see cref="IChangesetStorage"/> for storing changesets as they occur.</param>
    /// <param name="guardWritesOnWatermark">Whether a write may be made conditional on advancing the read model's watermark.</param>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    /// <param name="changesetContainer">The container changesets are recorded under, when it is not the read model's own container - as for a materialization generation's destination.</param>
    internal SaveChanges(IProjectionFuturesStore? futuresStore, ISink sink, IChangesetStorage changesetStorage, bool guardWritesOnWatermark, ILogger<SaveChanges> logger, ReadModelContainerName? changesetContainer = null)
    {
        _sink = sink;
        _changesetStorage = changesetStorage;
        _guardWritesOnWatermark = guardWritesOnWatermark;
        _logger = logger;
        _projectionFutures = futuresStore;
        _changesetContainer = changesetContainer;
    }

    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
    {
        // Don't save if the event was deferred or the key is permanently unresolvable
        if (context.IsDeferred || context.IsUnresolvable)
        {
            _logger.NotSavingDueToDeferred(context.Event.Context.SequenceNumber);
            return context;
        }

        var hasPendingFutureSaves = context.PendingFutureSaves.Any();

        if (!context.Changeset.HasChanges && !hasPendingFutureSaves)
        {
            _logger.NotSaving(context.Event.Context.SequenceNumber);
            return context;
        }

        PendingFutureSave[] pendingSavesToFold = [];
        if (context.Changeset.HasChanges)
        {
            _logger.SavingResult(context.Event.Context.SequenceNumber);

            // Fold property-only saves into the main write only until a same-key child operation
            // needs its own write. Later updates must stay behind that operation (add before update).
            pendingSavesToFold = hasPendingFutureSaves ? SelectPendingSavesToFold(context) : [];
            foreach (var pendingSave in pendingSavesToFold)
            {
                foreach (var change in pendingSave.Changeset.Changes)
                {
                    context.Changeset.Add(change);
                }
            }

            // TODO: Return the number of affected records and pass this along to the changeset storage
            var failedPartitions = await ApplyMainChanges(projection, context, pendingSavesToFold);

            // A bulk flush may report partitions other than this event's. Every one is recorded, but only a failure
            // of this event's own partition stops the rest of the save: the other partitions' events are retried.
            foreach (var failedPartition in failedPartitions)
            {
                context.AddFailedPartition(failedPartition);
            }

            if (HasFailedOwnPartition(failedPartitions, context.Changeset, context.Key))
            {
                return context;
            }

            await ResolveSavedFutures(pendingSavesToFold);

            await _changesetStorage.Save(
                _changesetContainer ?? projection.ReadModel.ContainerName,
                context.Key,
                context.Event.Context.EventType,
                context.Event.Context.SequenceNumber,
                context.Event.Context.CorrelationId,
                context.Changeset);
        }

        await ApplyPendingFutureSaves(context, pendingSavesToFold);

        return context;
    }

    static PendingFutureSave[] SelectPendingSavesToFold(ProjectionEventContext context)
    {
        var selected = new List<PendingFutureSave>();
        foreach (var pendingSave in context.PendingFutureSaves)
        {
            if (!Equals(pendingSave.Key.Value, context.Key.Value))
            {
                continue;
            }

            if (!pendingSave.Changeset.Changes.All(change => change is PropertiesChanged<ExpandoObject>))
            {
                break;
            }

            selected.Add(pendingSave);
        }
        return selected.ToArray();
    }

    static bool HasFailedOwnPartition(IEnumerable<FailedPartition> failedPartitions, IChangeset<AppendedEvent, ExpandoObject> changeset, Key key)
    {
        var ownPartition = changeset.Incoming is { } incoming ? incoming.Context.EventSourceId.Value : key.Value;
        return failedPartitions.Any(failure => Equals(failure.EventSourceId.Value, ownPartition) || Equals(failure.EventSourceId.Value, key.Value));
    }

    Task<IEnumerable<FailedPartition>> ApplyMainChanges(IProjection projection, ProjectionEventContext context, PendingFutureSave[] foldedFutureSaves)
    {
        var mode = WriteModeFor(projection, context);
        return foldedFutureSaves.Length > 0
            ? _sink.ApplyChangesImmediately(context.Key, context.Changeset, context.Event.Context.SequenceNumber, mode)
            : _sink.ApplyChanges(context.Key, context.Changeset, context.Event.Context.SequenceNumber, mode);
    }

    async Task ApplyPendingFutureSaves(ProjectionEventContext context, PendingFutureSave[] foldedFutureSaves)
    {
        var foldedSet = foldedFutureSaves.Length > 0 ? new HashSet<PendingFutureSave>(foldedFutureSaves) : null;
        foreach (var pendingSave in context.PendingFutureSaves.Where(p => foldedSet?.Contains(p) != true))
        {
            await ApplyPendingFutureSave(context, pendingSave);
        }
    }

    async Task ApplyPendingFutureSave(ProjectionEventContext context, PendingFutureSave pendingSave)
    {
        var failedPartitions = (await _sink.ApplyChangesImmediately(pendingSave.Key, pendingSave.Changeset, context.Event.Context.SequenceNumber, SinkWriteMode.Always)).ToArray();
        foreach (var failedPartition in failedPartitions)
        {
            context.AddFailedPartition(failedPartition);
        }

        if (!HasFailedOwnPartition(failedPartitions, pendingSave.Changeset, pendingSave.Key))
        {
            await ResolveSavedFuture(pendingSave);
        }
    }

    async Task ResolveSavedFutures(IEnumerable<PendingFutureSave> pendingSaves)
    {
        foreach (var pendingSave in pendingSaves)
        {
            await ResolveSavedFuture(pendingSave);
        }
    }

    async Task ResolveSavedFuture(PendingFutureSave pendingSave)
    {
        if (pendingSave.FutureId is { } futureId)
        {
            await _projectionFutures!.ResolveFuture(futureId);
        }
    }

    /// <summary>
    /// Decides whether this write may be made conditional on advancing the read model's last handled event
    /// sequence number.
    /// </summary>
    /// <param name="projection">The <see cref="IProjection"/> being saved for.</param>
    /// <param name="context">The <see cref="ProjectionEventContext"/> for the event.</param>
    /// <returns>The <see cref="SinkWriteMode"/> to write with.</returns>
    /// <remarks>
    /// Only a projection whose every event resolves its document key to the event's own event source id has a
    /// monotonic per-document stream; anything that collapses event sources onto one document — a join, a constant
    /// key, a parent hierarchy — is deliberately written out of order and would lose events to the guard. The
    /// event that creates the instance is written unconditionally: there is no document to compare a watermark
    /// against, and a guarded write never inserts. On a redelivery of that same creating event the instance is
    /// already there, so the write is guarded and correctly does nothing.
    /// </remarks>
    SinkWriteMode WriteModeFor(IProjection projection, ProjectionEventContext context) =>
        _guardWritesOnWatermark && projection.IsEventSourceKeyed && !context.IsNewInstance
            ? SinkWriteMode.OnlyWhenAdvancingWatermark
            : SinkWriteMode.Always;
}
