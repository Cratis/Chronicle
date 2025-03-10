// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Pipelines;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleReplayForObserver"/> for projections.
/// </summary>
/// <param name="projections"><see cref="IProjections"/> for managing projections.</param>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="projectionPipelineManager"><see cref="IProjectionPipelineManager"/> for managing projection pipelines.</param>
public class ProjectionReplayHandler(
    IProjections projections,
    IStorage storage,
    IProjectionPipelineManager projectionPipelineManager) : ICanHandleReplayForObserver
{
    /// <inheritdoc/>
    public Task<bool> CanHandle(ObserverDetails observerDetails) => Task.FromResult(observerDetails.Type == ObserverType.Projection);

    /// <inheritdoc/>
    public async Task BeginReplayFor(ObserverDetails observerDetails)
    {
        if (projections.TryGet(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Key.ObserverId, out var projection))
        {
            var replayContexts = storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace).ReplayContexts;
            var context = await replayContexts.Establish(projection.Model.Name);
            var pipeline = projectionPipelineManager.GetFor(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection);
            await pipeline.BeginReplay(context);
        }
    }

    /// <inheritdoc/>
    public async Task EndReplayFor(ObserverDetails observerDetails)
    {
        if (projections.TryGet(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Key.ObserverId, out var projection))
        {
            var namespaceStorage = storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace);
            var contextResult = await namespaceStorage.ReplayContexts.TryGet(projection.Model.Name);
            await contextResult.Match(
                async context =>
                {
                    var pipeline = projectionPipelineManager.GetFor(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection);
                    await pipeline.EndReplay(context);
                    await namespaceStorage.ReplayedModels.Replayed(observerDetails.Key.ObserverId, context);
                    await namespaceStorage.ReplayContexts.Evict(projection.Model.Name);
                },
                _ => Task.CompletedTask);
        }
    }
}
