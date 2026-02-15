// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that stores deferred futures.
/// </summary>
/// <param name="projectionFutures"><see cref="IProjectionFutures"/> for managing futures.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class StoreFutures(
    IProjectionFutures projectionFutures,
    ILogger<StoreFutures> logger) : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
    {
        // Store any deferred futures that were created during processing
        foreach (var future in context.DeferredFutures)
        {
            await projectionFutures.AddFuture(future);
            logger.StoredFuture(future.Id, future.ProjectionId);
        }

        return context;
    }
}
