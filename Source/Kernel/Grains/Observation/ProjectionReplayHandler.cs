// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Pipelines;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleReplayForObserver"/> for projections.
/// </summary>
/// <param name="projections"><see cref="IProjectionsManager"/> for managing projections.</param>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="projectionPipelineManager"><see cref="IProjectionPipelineManager"/> for managing projection pipelines.</param>
/// <param name="logger">The logger.</param>
public class ProjectionReplayHandler(
    IProjectionsManager projections,
    IStorage storage,
    IProjectionPipelineManager projectionPipelineManager,
    ILogger<ProjectionReplayHandler> logger) : ICanHandleReplayForObserver
{
    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> BeginReplayFor(ObserverDetails observerDetails) => await DoWorkOnPipeline(
        observerDetails,
        async projection =>
        {
            var replayContexts = storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace).ReplayContexts;
            return await replayContexts.Establish(projection.Model.Name);
        },
        (pipeline, _, context) => pipeline.BeginReplay(context));

    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> ResumeReplayFor(ObserverDetails observerDetails) => await DoWorkOnPipeline(
        observerDetails,
        async projection =>
        {
            var namespaceStorage = storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace);
            return await namespaceStorage.ReplayContexts.TryGet(projection.Model.Name);
        },
        async (pipeline, projection, context) =>
        {
            await pipeline.ResumeReplay(context);
            var namespaceStorage = storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace);
        });

    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> EndReplayFor(ObserverDetails observerDetails) => await DoWorkOnPipeline(
        observerDetails,
        async projection =>
        {
            var namespaceStorage = storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace);
            return await namespaceStorage.ReplayContexts.TryGet(projection.Model.Name);
        },
        async (pipeline, projection, context) =>
        {
            await pipeline.EndReplay(context);
            var namespaceStorage = storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace);
            await namespaceStorage.ReplayedModels.Replayed(observerDetails.Key.ObserverId, context);
            await namespaceStorage.ReplayContexts.Evict(projection.Model.Name);
        });

    static bool CanHandle(ObserverDetails observerDetails) => observerDetails.Type == ObserverType.Projection;

    async Task<Result<ICanHandleReplayForObserver.Error>> DoWorkOnPipeline(
        ObserverDetails observerDetails,
        Func<IProjection, Task<Result<ReplayContext, GetContextError>>> getContext,
        Func<IProjectionPipeline, IProjection, ReplayContext, Task> doWork)
    {
        try
        {
            if (!CanHandle(observerDetails))
            {
                return ICanHandleReplayForObserver.Error.CannotHandle;
            }

            if (!projections.TryGet(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Key.ObserverId, out var projection))
            {
                return Result<ICanHandleReplayForObserver.Error>.Success();
            }

            var getReplayContext = await getContext(projection);
            if (getReplayContext.TryPickT1(out _, out var replayContext))
            {
                return ICanHandleReplayForObserver.Error.CouldNotGetReplayContext;
            }
            var pipeline = projectionPipelineManager.GetFor(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection);
            await doWork(pipeline, projection, replayContext);
            return Result<ICanHandleReplayForObserver.Error>.Success();
        }
        catch (Exception ex)
        {
            logger.Failed(ex, observerDetails.Key.ObserverId, observerDetails.Type);
            return ICanHandleReplayForObserver.Error.Unknown;
        }
    }
}
