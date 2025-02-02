// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.ProjectionEngine;
using Cratis.Chronicle.ProjectionEngine.Pipelines;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleReplayForObserver"/> for projections.
/// </summary>
/// <param name="projectionManager"><see cref="IProjectionManager"/> for managing projections.</param>
/// <param name="projectionPipelineManager"><see cref="IProjectionPipelineManager"/> for managing projection pipelines.</param>
public class ProjectionReplayHandler(
    IProjectionManager projectionManager,
    IProjectionPipelineManager projectionPipelineManager) : ICanHandleReplayForObserver
{
    /// <inheritdoc/>
    public Task<bool> CanHandle(ObserverDetails observerDetails) => Task.FromResult(observerDetails.Type == ObserverType.Projection);

    /// <inheritdoc/>
    public async Task BeginReplayFor(ObserverDetails observerDetails)
    {
        if (projectionManager.TryGet(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Identifier, out var projection))
        {
            var pipeline = projectionPipelineManager.GetFor(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection);
            await pipeline.BeginReplay();
        }
    }

    /// <inheritdoc/>
    public async Task EndReplayFor(ObserverDetails observerDetails)
    {
        if (projectionManager.TryGet(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Identifier, out var projection))
        {
            var pipeline = projectionPipelineManager.GetFor(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection);
            await pipeline.EndReplay();
        }
    }
}
