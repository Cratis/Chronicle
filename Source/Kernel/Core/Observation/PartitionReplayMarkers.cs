// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IPartitionReplayMarkers"/>.
/// </summary>
/// <param name="projections"><see cref="IProjectionsManager"/> for finding the projection.</param>
/// <param name="projectionPipelineManager"><see cref="IProjectionPipelineManager"/> for finding its pipeline.</param>
public class PartitionReplayMarkers(
    IProjectionsManager projections,
    IProjectionPipelineManager projectionPipelineManager) : IPartitionReplayMarkers
{
    /// <inheritdoc/>
    public async Task ClearFor(ObserverDetails observerDetails, Key partition)
    {
        if (observerDetails.Type != ObserverType.Projection)
        {
            return;
        }

        var key = observerDetails.Key;
        var projection = await projections.GetCurrent(key.EventStore, key.Namespace, key.ObserverId);
        if (projection is null || projection.MarkedJoins.Count == 0)
        {
            return;
        }

        var pipeline = await projectionPipelineManager.GetFor(key.EventStore, key.Namespace, projection);
        await pipeline.ClearAppliedEventMarkers(key.Namespace, partition);
    }
}
