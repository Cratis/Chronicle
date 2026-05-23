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
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
#pragma warning disable CA1001 // _handleLock is owned by a long-lived cached pipeline; the manager evicts and replaces the entire pipeline instance when the projection definition changes.
public class ProjectionPipeline(
    EngineProjection projection,
    ISink sink,
    IChangesetStorage changesetStorage,
    IObjectComparer objectComparer,
    IEnumerable<ICanPerformProjectionPipelineStep> steps,
    ILogger<ProjectionPipeline> logger) : IProjectionPipeline
{
    /// <summary>
    /// Serializes <see cref="Handle"/> calls per pipeline.
    /// </summary>
    /// <remarks>
    /// The pipeline performs a read-modify-write cycle against the sink for each event:
    /// SetInitialState reads the current state, HandleEvent computes the changeset, and
    /// SaveChanges writes the new state back. Multiple concurrent Handle() calls — which
    /// happen when catch-up runs per-partition steps in parallel for a projection whose
    /// key collapses all partitions into a single read-model document (e.g. UsingConstantKey,
    /// or a Join that resolves to the same root) — would race on this cycle and produce
    /// lost updates. Serializing Handle() per pipeline (one pipeline per projection) keeps
    /// the read-modify-write atomic without coupling the sink to the projection model.
    /// </remarks>
    readonly SemaphoreSlim _handleLock = new(1, 1);

    /// <inheritdoc/>
    public async Task BeginReplay(ReplayContext context)
    {
        await changesetStorage.BeginReplay(projection.ReadModel.ContainerName);
        await sink.BeginReplay(context);
    }

    /// <inheritdoc/>
    public Task ResumeReplay(ReplayContext context) => sink.ResumeReplay(context);

    /// <inheritdoc/>
    public async Task EndReplay(ReplayContext context)
    {
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
        await _handleLock.WaitAsync();
        try
        {
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
        finally
        {
            _handleLock.Release();
        }
    }
}
