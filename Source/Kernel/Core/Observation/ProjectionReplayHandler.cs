// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleReplayForObserver"/> for projections.
/// </summary>
/// <param name="projections"><see cref="IProjectionsManager"/> for managing projections.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="projectionPipelineManager"><see cref="IProjectionPipelineManager"/> for managing projection pipelines.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/> for <see cref="Configuration.ChronicleOptions"/>.</param>
/// <param name="logger">The logger.</param>
public class ProjectionReplayHandler(
    IProjectionsManager projections,
    IGrainFactory grainFactory,
    IStorage storage,
    IProjectionPipelineManager projectionPipelineManager,
    IOptions<Configuration.ChronicleOptions> options,
    ILogger<ProjectionReplayHandler> logger) : ICanHandleReplayForObserver
{
    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> BeginReplayFor(ObserverDetails observerDetails) => await DoWorkOnPipeline(
        observerDetails,
        async projection =>
        {
            var namespaceStorage = storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace);
            var replayManager = grainFactory.GetReadModelReplayManager(
                observerDetails.Key.EventStore,
                observerDetails.Key.Namespace,
                projection.ReadModel.Identifier);
            await replayManager.ApplyRetentionPolicy(await VersionsToKeepFor(observerDetails));
            var replayContexts = namespaceStorage.ReplayContexts;
            return await replayContexts.Establish(new(projection.ReadModel.Identifier, projection.ReadModel.LatestGeneration), projection.ReadModel.ContainerName);
        },
        async (pipeline, _, context) =>
        {
            await pipeline.BeginReplay(context);
            return Result<ICanHandleReplayForObserver.Error>.Success();
        });

    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> ResumeReplayFor(ObserverDetails observerDetails) => await DoWorkOnPipeline(
        observerDetails,
        async projection =>
        {
            var namespaceStorage = storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace);
            return await namespaceStorage.ReplayContexts.TryGet(projection.ReadModel.Identifier);
        },
        async (pipeline, projection, context) =>
        {
            await pipeline.ResumeReplay(context);
            var namespaceStorage = storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace);
            return Result<ICanHandleReplayForObserver.Error>.Success();
        });

    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> EndReplayFor(ObserverDetails observerDetails) => await DoWorkOnPipeline(
        observerDetails,
        async projection =>
        {
            var namespaceStorage = storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace);
            return await namespaceStorage.ReplayContexts.TryGet(projection.ReadModel.Identifier);
        },
        async (pipeline, projection, context) =>
        {
            var failedPartitions = (await pipeline.EndReplay(context)).ToArray();
            await ProjectionBulkFailures.Record(grainFactory, observerDetails, failedPartitions);
            if (failedPartitions.Length > 0)
            {
                return ICanHandleReplayForObserver.Error.Unknown;
            }

            var namespaceStorage = storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace);
            var replayManager = grainFactory.GetReadModelReplayManager(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection.ReadModel.Identifier);
            await replayManager.Replayed(observerDetails.Key.ObserverId, context);

            // Nothing can revert a kernel-owned read model, so the generation this replay superseded has no use.
            // Keeping it, as is done for what an application owns, leaves one more collection behind per replay.
            if (await IsKernelOwned(observerDetails))
            {
                await replayManager.ApplyRetentionPolicy(0);
            }

            await namespaceStorage.ReplayContexts.Evict(projection.ReadModel.Identifier);
            return Result<ICanHandleReplayForObserver.Error>.Success();
        },

        // The context is gone because another silo has already ended this replay and promoted the read model. This
        // silo's sink is still writing to the replay container, which nothing reads any more - it has to stop.
        pipeline => pipeline.LeaveReplay());

    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> FlushReplayFor(ObserverDetails observerDetails)
    {
        try
        {
            if (!CanHandle(observerDetails))
            {
                return ICanHandleReplayForObserver.Error.CannotHandle;
            }

            if (!projections.TryGet(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Key.ObserverId, out var projection))
            {
                logger.ProjectionMissing(observerDetails.Key);
                return ICanHandleReplayForObserver.Error.Unknown;
            }

            if (projection.Scope == ProjectionScope.Global)
            {
                throw new GlobalProjectionReplayNotSupported();
            }

            var pipeline = await projectionPipelineManager.GetFor(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection);
            var failedPartitions = (await pipeline.EndBulk()).ToArray();
            await ProjectionBulkFailures.Record(grainFactory, observerDetails, failedPartitions);
            return failedPartitions.Length > 0
                ? ICanHandleReplayForObserver.Error.Unknown
                : Result<ICanHandleReplayForObserver.Error>.Success();
        }
        catch (Exception ex) when (ex is not GlobalProjectionReplayNotSupported)
        {
            logger.Failed(ex, observerDetails.Key.ObserverId, observerDetails.Type);
            return ICanHandleReplayForObserver.Error.Unknown;
        }
    }

    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> BeginReplayPartitionFor(ObserverDetails observerDetails, Key partition) =>
        CheckPartitionReplay(observerDetails);

    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> EndReplayPartitionFor(ObserverDetails observerDetails, Key partition) =>
        CheckPartitionReplay(observerDetails);

    /// <inheritdoc/>
    public async Task<BulkFlushResult> FlushFor(ObserverDetails observerDetails)
    {
        if (!CanHandle(observerDetails) || !projections.TryGet(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Key.ObserverId, out var projection))
        {
            return new(null, []);
        }

        var pipeline = await projectionPipelineManager.GetFor(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection);
        var result = await pipeline.FlushBulk();
        await ProjectionBulkFailures.Record(grainFactory, observerDetails, result.FailedPartitions);
        return result;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Storage.Sinks.FailedPartition>> EndBulkFor(ObserverDetails observerDetails, CancellationToken cancellationToken)
    {
        if (!CanHandle(observerDetails) || !projections.TryGet(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Key.ObserverId, out var projection))
        {
            return [];
        }

        // Unlike replay finalization, shutdown must retain the sink and must not promote its container.
        var pipeline = await projectionPipelineManager.GetFor(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection);
        return await pipeline.EndBulk(cancellationToken);
    }

    static bool CanHandle(ObserverDetails observerDetails) => observerDetails.Type == ObserverType.Projection;

    async Task<int> VersionsToKeepFor(ObserverDetails observerDetails) =>
        await IsKernelOwned(observerDetails) ? 0 : options.Value.ReadModels.ReplayedVersionsToKeep;

    async Task<bool> IsKernelOwned(ObserverDetails observerDetails)
    {
        var definitions = storage.GetEventStore(observerDetails.Key.EventStore).Projections;
        return await definitions.Has(observerDetails.Key.ObserverId.Value) &&
            (await definitions.Get(observerDetails.Key.ObserverId.Value)).IsKernelOwned;
    }

    async Task<Result<ICanHandleReplayForObserver.Error>> CheckPartitionReplay(ObserverDetails observerDetails)
    {
        await GlobalProjectionReplay.EnsureSupported(storage, observerDetails.Key, observerDetails.Type);
        if (!CanHandle(observerDetails))
        {
            return Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle);
        }

        var projection = await projections.GetCurrent(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Key.ObserverId);
        if (projection is null)
        {
            return Result.Failed(ICanHandleReplayForObserver.Error.Unknown);
        }

        if (projection.Scope == ProjectionScope.Global)
        {
            throw new GlobalProjectionReplayNotSupported();
        }

        return Result<ICanHandleReplayForObserver.Error>.Success();
    }

    async Task<Result<ICanHandleReplayForObserver.Error>> DoWorkOnPipeline(
        ObserverDetails observerDetails,
        Func<IProjection, Task<Result<ReplayContext, GetContextError>>> getContext,
        Func<IProjectionPipeline, IProjection, ReplayContext, Task<Result<ICanHandleReplayForObserver.Error>>> doWork,
        Func<IProjectionPipeline, Task>? onMissingContext = null)
    {
        try
        {
            if (!CanHandle(observerDetails))
            {
                return ICanHandleReplayForObserver.Error.CannotHandle;
            }

            var projection = await projections.GetCurrent(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Key.ObserverId);
            if (projection is null)
            {
                logger.ProjectionMissing(observerDetails.Key);
                return ICanHandleReplayForObserver.Error.Unknown;
            }

            if (projection.Scope == ProjectionScope.Global)
            {
                throw new GlobalProjectionReplayNotSupported();
            }

            var getReplayContext = await getContext(projection);
            var pipeline = await projectionPipelineManager.GetFor(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection);
            if (getReplayContext.TryPickT1(out _, out var replayContext))
            {
                if (onMissingContext is not null)
                {
                    await onMissingContext(pipeline);
                }

                return ICanHandleReplayForObserver.Error.CouldNotGetReplayContext;
            }

            return await doWork(pipeline, projection, replayContext);
        }
        catch (Exception ex) when (ex is not GlobalProjectionReplayNotSupported)
        {
            logger.Failed(ex, observerDetails.Key.ObserverId, observerDetails.Type);
            return ICanHandleReplayForObserver.Error.Unknown;
        }
    }
}
