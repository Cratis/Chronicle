// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.Engine.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipeline"/> for a projection that targets a graph.
/// </summary>
/// <remarks>
/// The pipeline reuses the observer, event routing, replay and failed partition handling of every projection. Where
/// the document pipeline folds an event into a changeset per read model key, this one turns it into the operations a
/// <see cref="IGraphSink"/> applies, with the event sequence number as watermark.
/// </remarks>
/// <param name="projection">The <see cref="EngineProjection"/> the pipeline is for.</param>
/// <param name="builder">The <see cref="GraphChangesetBuilder"/> that builds the operations for an event.</param>
/// <param name="sink">The <see cref="IGraphSink"/> to apply the operations to.</param>
/// <param name="handleLock">Per-projection lock that serializes <see cref="Handle"/> calls.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for the result context.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
public class GraphProjectionPipeline(
    EngineProjection projection,
    GraphChangesetBuilder builder,
    IGraphSink sink,
    ProjectionHandleLock handleLock,
    IObjectComparer objectComparer,
    ILogger<GraphProjectionPipeline> logger) : IProjectionPipeline
{
    /// <inheritdoc/>
    public Task BeginReplay(ReplayContext context) => sink.BeginReplay(projection.Identifier, context);

    /// <inheritdoc/>
    public Task ResumeReplay(ReplayContext context) => sink.ResumeReplay(projection.Identifier, context);

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndReplay(ReplayContext context) => sink.EndReplay(projection.Identifier, context);

    /// <inheritdoc/>
    public Task LeaveReplay() => sink.LeaveReplay(projection.Identifier);

    /// <inheritdoc/>
    public Task BeginBulk() => sink.BeginBulk();

    /// <inheritdoc/>
    public Task ResumeBulk() => sink.ResumeBulk();

    /// <inheritdoc/>
    public Task<BulkFlushResult> FlushBulk() => sink.FlushBulk();

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndBulk() => sink.EndBulk();

    /// <inheritdoc/>
    public async Task<ProjectionEventContext> Handle(AppendedEvent @event)
    {
        // Operations on one node or edge can come from events of many event sources, so handling is serialized per projection.
        using var handleScope = await handleLock.AcquireCoarse();
        var context = ProjectionEventContext.Empty(objectComparer, @event);
        var changeset = await builder.Build(@event);
        if (!changeset.HasChanges)
        {
            logger.NothingToApply(@event.Context.SequenceNumber);
            return context;
        }

        foreach (var failedPartition in await sink.ApplyGraphChanges(changeset, @event.Context.SequenceNumber))
        {
            context.AddFailedPartition(failedPartition);
        }

        logger.AppliedOperations(changeset.Operations.Count, @event.Context.SequenceNumber);
        return context;
    }
}
