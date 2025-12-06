// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Projections.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that stores deferred futures.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StoreFutures"/> class.
/// </remarks>
/// <param name="eventStore">The <see cref="EventStoreName"/> for the event store.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> for the namespace.</param>
/// <param name="projectionFutures"><see cref="IProjectionFutures"/> for managing futures.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class StoreFutures(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IProjectionFutures projectionFutures,
    ILogger<StoreFutures> logger) : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(EngineProjection projection, ProjectionEventContext context)
    {
        // Store any deferred futures that were created during processing
        foreach (var future in context.DeferredFutures)
        {
            await projectionFutures.AddFuture(eventStore, @namespace, projection.Identifier, future);
            logger.StoredFuture(future.Id, future.ProjectionId);
        }

        return context;
    }
}
