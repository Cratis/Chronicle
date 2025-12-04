// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Projections;
using Orleans;

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
    public Task<IEnumerable<ProjectionFuture>> GetFutures(EventStoreName eventStore, EventStoreNamespaceName @namespace, Key key)
    {
        var projection = grainFactory.GetGrain<IProjection>(new ProjectionKey(key.Value.ToString()!, eventStore));
        return projection.GetFutures(@namespace, key);
    }

    /// <inheritdoc/>
    public Task AddFuture(EventStoreName eventStore, EventStoreNamespaceName @namespace, Key key, ProjectionFuture future)
    {
        var projection = grainFactory.GetGrain<IProjection>(new ProjectionKey(future.ProjectionId, eventStore));
        return projection.AddFuture(@namespace, key, future);
    }

    /// <inheritdoc/>
    public Task ResolveFuture(EventStoreName eventStore, EventStoreNamespaceName @namespace, Key key, ProjectionFutureId futureId)
    {
        // We need to get the projection from the future, but we don't have access to it here.
        // We'll need to store which projection the future belongs to in the storage key.
        // For now, we'll use the key value as the projection ID - this may need refinement.
        var projection = grainFactory.GetGrain<IProjection>(new ProjectionKey(key.Value.ToString()!, eventStore));
        return projection.ResolveFuture(@namespace, key, futureId);
    }
}
