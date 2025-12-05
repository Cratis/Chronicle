// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="Chronicle.Projections.IProjectionFutures"/> that forwards to projection grains.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionFutures"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting projection grains.</param>
public class ProjectionFutures(IGrainFactory grainFactory) : Chronicle.Projections.IProjectionFutures
{
    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionFuture>> GetFutures(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId projectionId)
    {
        var projection = grainFactory.GetGrain<IProjection>(new ProjectionKey(projectionId, eventStore));
        return projection.GetFutures(@namespace);
    }

    /// <inheritdoc/>
    public Task AddFuture(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId projectionId, ProjectionFuture future)
    {
        var projection = grainFactory.GetGrain<IProjection>(new ProjectionKey(projectionId, eventStore));
        return projection.AddFuture(@namespace, future);
    }

    /// <inheritdoc/>
    public Task ResolveFuture(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId projectionId, ProjectionFutureId futureId)
    {
        var projection = grainFactory.GetGrain<IProjection>(new ProjectionKey(projectionId, eventStore));
        return projection.ResolveFuture(@namespace, futureId);
    }
}
