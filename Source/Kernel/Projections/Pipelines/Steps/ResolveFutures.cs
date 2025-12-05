// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
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
/// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> for accessing event sequences.</param>
/// <param name="sink"><see cref="ISink"/> for querying the read model.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class ResolveFutures(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IProjectionFutures projectionFutures,
    IEventSequenceStorage eventSequenceStorage,
    ISink sink,
    ILogger<ResolveFutures> logger) : ICanPerformProjectionPipelineStep
{
    HandleEvent? _handleEventStep;

    /// <summary>
    /// Sets the HandleEvent step to use for reprocessing deferred events.
    /// </summary>
    /// <param name="handleEventStep">The <see cref="HandleEvent"/> step instance.</param>
    public void SetHandleEventStep(HandleEvent handleEventStep)
    {
        _handleEventStep = handleEventStep;
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

        // HandleEvent step must be set before we can resolve futures
        if (_handleEventStep is null)
        {
            logger.HandleEventStepNotSet();
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
                    // Process the future event directly through HandleEvent with the current context
                    // The current context contains the changeset with the parent already added
                    // HandleEvent will navigate through the projection hierarchy and add the child
                    // to the parent that exists in the changeset's CurrentState
                    var futureContext = context with { Event = future.Event };
                    var resultContext = await _handleEventStep.Perform(projection, futureContext);

                    // If the event was successfully processed (no deferred futures created),
                    // then the parent data exists in the changeset and we resolved the future
                    if (!resultContext.IsDeferred)
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
