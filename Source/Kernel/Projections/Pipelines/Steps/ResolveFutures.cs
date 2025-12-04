// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Projections.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that resolves pending futures.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ResolveFutures"/> class.
/// </remarks>
/// <param name="eventStore">The <see cref="EventStoreName"/> for the event store.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> for the namespace.</param>
/// <param name="projectionFutures"><see cref="IProjectionFutures"/> for managing futures.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class ResolveFutures(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IProjectionFutures projectionFutures,
    ILogger<ResolveFutures> logger) : ICanPerformProjectionPipelineStep
{
    IProjectionPipeline? _pipeline;

    /// <summary>
    /// Sets the pipeline to use for reprocessing deferred events.
    /// </summary>
    /// <param name="pipeline">The <see cref="IProjectionPipeline"/> instance.</param>
    public void SetPipeline(IProjectionPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(EngineProjection projection, ProjectionEventContext context)
    {
        // If this event was deferred, don't try to resolve futures (we have no valid data yet)
        if (context.IsDeferred)
        {
            return context;
        }

        // Attempt to resolve any pending futures now that we've processed a new event
        // Try all futures for this projection - we don't need to track specific keys
        var futures = await projectionFutures.GetFutures(eventStore, @namespace, projection.Identifier);
        if (!futures.Any())
        {
            return context;
        }

        // Pipeline must be set before we can resolve futures
        if (_pipeline is null)
        {
            logger.PipelineNotSet();
            return context;
        }

        logger.FoundPendingFutures(futures.Count(), projection.Identifier);

        // Keep trying to resolve futures until we can't resolve any more
        // This handles the case where resolving one future creates the parent data needed by another future
        var resolvedAny = true;
        while (resolvedAny)
        {
            resolvedAny = false;
            futures = await projectionFutures.GetFutures(eventStore, @namespace, projection.Identifier);

            foreach (var future in futures)
            {
                try
                {
                    // Attempt to reprocess the event - the pipeline will check if parent data now exists
                    var pipelineContext = await _pipeline.Handle(future.Event);

                    // If the event was successfully processed (no deferred futures created),
                    // then the parent data now exists and we can resolve this future
                    if (!pipelineContext.IsDeferred)
                    {
                        await projectionFutures.ResolveFuture(eventStore, @namespace, projection.Identifier, future.Id);
                        logger.ResolvedFuture(future.Id, future.ProjectionId);
                        resolvedAny = true;
                    }
                }
                catch (Exception ex)
                {
                    logger.FailedToResolveFuture(ex, future.Id, future.ProjectionId);
                }
            }
        }

        return context;
    }
}
