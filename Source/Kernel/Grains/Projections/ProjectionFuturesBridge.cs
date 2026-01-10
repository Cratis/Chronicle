// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Grains.Projections;

// TODO: Remove this bridge when we merge the "engine" version into same project as the Grains.

/// <summary>
/// Represents an implementation of <see cref="Chronicle.Projections.IProjectionFutures"/> that forwards to projection grains.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting projection grains.</param>
public class ProjectionFuturesBridge(IGrainFactory grainFactory) : Chronicle.Projections.IProjectionFutures
{
    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionFuture>> GetFutures(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId projectionId)
    {
        var projection = grainFactory.GetGrain<IProjectionFutures>(new ProjectionFuturesKey(projectionId, eventStore, @namespace));
        return projection.GetFutures();
    }

    /// <inheritdoc/>
    public Task AddFuture(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId projectionId, ProjectionFuture future)
    {
        var projection = grainFactory.GetGrain<IProjectionFutures>(new ProjectionFuturesKey(projectionId, eventStore, @namespace));
        return projection.AddFuture(future);
    }

    /// <inheritdoc/>
    public Task ResolveFuture(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId projectionId, ProjectionFutureId futureId)
    {
        var projection = grainFactory.GetGrain<IProjectionFutures>(new ProjectionFuturesKey(projectionId, eventStore, @namespace));
        return projection.ResolveFuture(futureId);
    }
}
