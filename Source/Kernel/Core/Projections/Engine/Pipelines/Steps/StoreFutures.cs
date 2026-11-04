// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that stores deferred futures.
/// </summary>
public class StoreFutures : ICanPerformProjectionPipelineStep
{
    readonly IProjectionFuturesStore _projectionFutures;
    readonly ProjectionFuturesTracker _futuresTracker;
    readonly ILogger<StoreFutures> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreFutures"/> class.
    /// </summary>
    /// <param name="projectionFutures"><see cref="IProjectionFutures"/> for managing futures.</param>
    /// <param name="futuresTracker"><see cref="ProjectionFuturesTracker"/> shared with the <see cref="ResolveFutures"/> step.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public StoreFutures(IProjectionFutures projectionFutures, ProjectionFuturesTracker futuresTracker, ILogger<StoreFutures> logger)
        : this(futuresTracker, new ProjectionFuturesStore(projectionFutures), logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreFutures"/> class.
    /// </summary>
    /// <param name="futuresTracker"><see cref="ProjectionFuturesTracker"/> shared with the <see cref="ResolveFutures"/> step.</param>
    /// <param name="futuresStore"><see cref="IProjectionFuturesStore"/> for managing futures.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    internal StoreFutures(ProjectionFuturesTracker futuresTracker, IProjectionFuturesStore futuresStore, ILogger<StoreFutures> logger)
    {
        _projectionFutures = futuresStore;
        _futuresTracker = futuresTracker;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
    {
        if (context.IsUnresolvable)
        {
            return context;
        }

        // Store any deferred futures that were created during processing
        foreach (var future in context.DeferredFutures)
        {
            var pending = projection.GenerationDelivery == EventGenerationDelivery.Pinned
                ? await _projectionFutures.AddPinnedFuture(future)
                : await _projectionFutures.AddFuture(future);
            if (pending > 0)
            {
                _futuresTracker.HasPending = true;
            }
            _logger.StoredFuture(future.Id, future.ProjectionId);
        }

        return context;
    }
}
