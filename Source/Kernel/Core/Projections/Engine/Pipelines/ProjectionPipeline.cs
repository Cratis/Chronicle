// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipeline"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="IProjectionPipeline"/>.
/// </remarks>
/// <param name="projection">The <see cref="EngineProjection"/> the pipeline is for.</param>
/// <param name="sink"><see cref="ISink"/> to use.</param>
/// <param name="changesetStorage"><see cref="IChangesetStorage"/> for storing changesets as they occur.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="steps">Collection of <see cref="ICanPerformProjectionPipelineStep"/> to perform.</param>
/// <param name="handleLock">Per-projection lock that serializes <see cref="Handle"/> calls across all pipeline instances that share the same projection identifier.</param>
/// <param name="replayScopedCache">The <see cref="IReplayScopedCache"/> whose key-resolution caching is active only for the duration of a replay.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
public class ProjectionPipeline(
    EngineProjection projection,
    ISink sink,
    IChangesetStorage changesetStorage,
    IObjectComparer objectComparer,
    IEnumerable<ICanPerformProjectionPipelineStep> steps,
    ProjectionHandleLock handleLock,
    IReplayScopedCache replayScopedCache,
    ILogger<ProjectionPipeline> logger) : IProjectionPipeline
{
    /// <summary>
    /// Serializes the read-modify-write cycle in <see cref="Handle"/>.
    /// </summary>
    /// <remarks>
    /// The pipeline performs a read-modify-write cycle against the sink for each event:
    /// SetInitialState reads the current state, HandleEvent computes the changeset, and
    /// SaveChanges writes the new state back. Multiple concurrent Handle() calls happen when
    /// catch-up or replay runs per-partition steps in parallel, and when a replay evicts the
    /// cached pipeline while already-activated subscribers still hold the old pipeline reference.
    /// For a projection whose key collapses all partitions into a single read-model document
    /// (constant key, joins, or hierarchical parent/child resolution) those calls would race and
    /// produce lost updates or missing parent/child links, so the whole projection is serialized
    /// coarsely. For a purely event-source-keyed projection (<see cref="IProjection.IsEventSourceKeyed"/>)
    /// events for different event source ids target different documents, so handling is striped
    /// per event source id — same-key calls serialize, different-key calls run in parallel. The
    /// lock is owned by <see cref="IProjectionPipelineManager"/> so it survives pipeline eviction:
    /// both the old and new pipeline share the same stripes so the concurrent paths still serialize.
    /// </remarks>
    readonly ProjectionHandleLock _handleLock = handleLock;

    /// <inheritdoc/>
    public async Task BeginReplay(ReplayContext context)
    {
        replayScopedCache.BeginReplaySession();
        await changesetStorage.BeginReplay(projection.ReadModel.ContainerName);
        await sink.BeginReplay(context);
    }

    /// <inheritdoc/>
    public Task ResumeReplay(ReplayContext context)
    {
        replayScopedCache.BeginReplaySession();
        return sink.ResumeReplay(context);
    }

    /// <inheritdoc/>
    public async Task EndReplay(ReplayContext context)
    {
        replayScopedCache.EndReplaySession();
        await sink.EndReplay(context);
        await changesetStorage.EndReplay(projection.ReadModel.ContainerName);
    }

    /// <inheritdoc/>
    public Task BeginBulk() => sink.BeginBulk();

    /// <inheritdoc/>
    public Task EndBulk() => sink.EndBulk();

    /// <inheritdoc/>
    public async Task<ProjectionEventContext> Handle(AppendedEvent @event)
    {
        // A purely event-source-keyed projection resolves every event's key to its own event source id, so striping
        // on the event source id is striping on the resolved key: same-key handling serializes, different-key
        // handling runs in parallel. Any projection that can collapse partitions keeps the coarse lock.
        using var handleScope = projection.IsEventSourceKeyed
            ? await _handleLock.AcquireFor(@event.Context.EventSourceId)
            : await _handleLock.AcquireCoarse();

        logger.StartingPipeline(@event.Context.SequenceNumber);
        var context = ProjectionEventContext.Empty(objectComparer, @event) with
        {
            OperationType = projection.GetOperationTypeFor(@event.Context.EventType),
        };

        foreach (var step in steps)
        {
            try
            {
                context = await step.Perform(projection, context);
            }
            catch (Exception ex)
            {
                logger.ErrorPerformingStep(ex, step.GetType(), @event.Context.SequenceNumber);
                throw;
            }
        }
        logger.CompletedAllSteps(@event.Context.SequenceNumber);

        return context;
    }
}
