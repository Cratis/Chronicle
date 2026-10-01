// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleReplayForObserver"/> for reducers.
/// </summary>
/// <param name="reducerMediator"><see cref="IReducerMediator"/> for notifying connected clients.</param>
/// <param name="reducerPipelineFactory"><see cref="IReducerPipelineFactory"/> for creating pipelines.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="options">The Chronicle options.</param>
/// <param name="logger">The logger.</param>
public class ReducerReplayHandler(
    IReducerMediator reducerMediator,
    IReducerPipelineFactory reducerPipelineFactory,
    IGrainFactory grainFactory,
    IStorage storage,
    IOptions<Configuration.ChronicleOptions> options,
    ILogger<ReducerReplayHandler> logger) : ICanHandleReplayForObserver
{
    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> BeginReplayFor(ObserverDetails observerDetails) => DoWorkOnPipeline(
        observerDetails,
        async pipeline =>
        {
            var namespaceStorage = storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace);
            var replayManager = grainFactory.GetReadModelReplayManager(observerDetails.Key.EventStore, observerDetails.Key.Namespace, pipeline.ReadModel.Identifier);
            await replayManager.ApplyRetentionPolicy(options.Value.ReadModels.ReplayedVersionsToKeep);
            return await namespaceStorage.ReplayContexts.Establish(
                new(pipeline.ReadModel.Identifier, pipeline.ReadModel.LatestGeneration), pipeline.ReadModel.ContainerName);
        },
        async (pipeline, context) =>
        {
            await pipeline.BeginReplay(context);

            // Reducer batches are checkpointed independently. Their writes must be durable before a batch is
            // acknowledged, rather than held back until the entire replay ends (or lost when it resumes).
            var failedPartitions = (await pipeline.EndBulk()).ToArray();
            await ProjectionBulkFailures.Record(grainFactory, observerDetails, failedPartitions);
            if (failedPartitions.Length > 0)
            {
                return ICanHandleReplayForObserver.Error.Unknown;
            }

            reducerMediator.OnBeginReplay(new ReducerId(observerDetails.Key.ObserverId.Value), observerDetails.Key.EventStore, observerDetails.Key.Namespace);
            return Result<ICanHandleReplayForObserver.Error>.Success();
        });

    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> ResumeReplayFor(ObserverDetails observerDetails) => DoWorkOnPipeline(
        observerDetails,
        pipeline => storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace).ReplayContexts.TryGet(pipeline.ReadModel.Identifier),
        async (pipeline, context) =>
        {
            await pipeline.Sink.ResumeReplay(context);
            var failedPartitions = (await pipeline.EndBulk()).ToArray();
            await ProjectionBulkFailures.Record(grainFactory, observerDetails, failedPartitions);
            return failedPartitions.Length > 0
                ? ICanHandleReplayForObserver.Error.Unknown
                : Result<ICanHandleReplayForObserver.Error>.Success();
        });

    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> EndReplayFor(ObserverDetails observerDetails)
    {
        try
        {
            return await DoWorkOnPipeline(
                observerDetails,
                pipeline => storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace).ReplayContexts.TryGet(pipeline.ReadModel.Identifier),
                async (pipeline, context) =>
                {
                    if (observerDetails.ReplayAborted || observerDetails.ReplayAlreadyFinalized)
                    {
                        await pipeline.Sink.LeaveReplay();
                        if (observerDetails.ReplayAborted)
                        {
                            await storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace).ReplayContexts.Evict(pipeline.ReadModel.Identifier);
                        }

                        return Result<ICanHandleReplayForObserver.Error>.Success();
                    }

                    var failedPartitions = (await pipeline.EndReplay(context with { AllowEmptyResult = observerDetails.ReplaySucceededWithEvents })).ToArray();
                    await ProjectionBulkFailures.Record(grainFactory, observerDetails, failedPartitions);
                    if (failedPartitions.Length > 0)
                    {
                        return ICanHandleReplayForObserver.Error.Unknown;
                    }

                    var namespaceStorage = storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace);
                    var replayManager = grainFactory.GetReadModelReplayManager(observerDetails.Key.EventStore, observerDetails.Key.Namespace, pipeline.ReadModel.Identifier);
                    await replayManager.Replayed(observerDetails.Key.ObserverId, context);
                    await namespaceStorage.ReplayContexts.Evict(pipeline.ReadModel.Identifier);
                    return Result<ICanHandleReplayForObserver.Error>.Success();
                },
                pipeline => pipeline.Sink.LeaveReplay());
        }
        finally
        {
            if (CanHandle(observerDetails))
            {
                reducerMediator.OnEndReplay(new ReducerId(observerDetails.Key.ObserverId.Value), observerDetails.Key.EventStore, observerDetails.Key.Namespace);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> FlushReplayFor(ObserverDetails observerDetails)
    {
        try
        {
            if (!CanHandle(observerDetails))
            {
                return ICanHandleReplayForObserver.Error.CannotHandle;
            }

            var definition = await storage.GetEventStore(observerDetails.Key.EventStore).Reducers.Get(new ReducerId(observerDetails.Key.ObserverId.Value));
            var pipeline = await reducerPipelineFactory.Create(observerDetails.Key.EventStore, observerDetails.Key.Namespace, definition);
            var failedPartitions = (await pipeline.EndBulk()).ToArray();
            await ProjectionBulkFailures.Record(grainFactory, observerDetails, failedPartitions);
            return failedPartitions.Length > 0
                ? ICanHandleReplayForObserver.Error.Unknown
                : Result<ICanHandleReplayForObserver.Error>.Success();
        }
        catch (Exception ex)
        {
            logger.Failed(ex, observerDetails.Key.ObserverId, observerDetails.Type);
            return ICanHandleReplayForObserver.Error.Unknown;
        }
    }

    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> BeginReplayPartitionFor(ObserverDetails observerDetails, Key partition)
    {
        if (!CanHandle(observerDetails))
        {
            return Task.FromResult(Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle));
        }

        reducerMediator.OnBeginReplayPartition(
            new ReducerId(observerDetails.Key.ObserverId.Value),
            observerDetails.Key.EventStore,
            observerDetails.Key.Namespace,
            partition);

        return Task.FromResult(Result<ICanHandleReplayForObserver.Error>.Success());
    }

    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> EndReplayPartitionFor(ObserverDetails observerDetails, Key partition)
    {
        if (!CanHandle(observerDetails))
        {
            return Task.FromResult(Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle));
        }

        reducerMediator.OnEndReplayPartition(
            new ReducerId(observerDetails.Key.ObserverId.Value),
            observerDetails.Key.EventStore,
            observerDetails.Key.Namespace,
            partition);

        return Task.FromResult(Result<ICanHandleReplayForObserver.Error>.Success());
    }

    static bool CanHandle(ObserverDetails observerDetails) => observerDetails.Type == ObserverType.Reducer;

    async Task<Result<ICanHandleReplayForObserver.Error>> DoWorkOnPipeline(
        ObserverDetails observerDetails,
        Func<IReducerPipeline, Task<Result<ReplayContext, GetContextError>>> getContext,
        Func<IReducerPipeline, ReplayContext, Task<Result<ICanHandleReplayForObserver.Error>>> doWork,
        Func<IReducerPipeline, Task>? onMissingContext = null)
    {
        try
        {
            if (!CanHandle(observerDetails))
            {
                return ICanHandleReplayForObserver.Error.CannotHandle;
            }

            var eventStoreStorage = storage.GetEventStore(observerDetails.Key.EventStore);
            var definition = await eventStoreStorage.Reducers.Get(new ReducerId(observerDetails.Key.ObserverId.Value));
            var pipeline = await reducerPipelineFactory.Create(observerDetails.Key.EventStore, observerDetails.Key.Namespace, definition);
            var getReplayContext = await getContext(pipeline);
            if (getReplayContext.TryPickT1(out _, out var replayContext))
            {
                if (onMissingContext is not null)
                {
                    await onMissingContext(pipeline);
                }

                return ICanHandleReplayForObserver.Error.CouldNotGetReplayContext;
            }

            return await doWork(pipeline, replayContext);
        }
        catch (Exception ex)
        {
            logger.Failed(ex, observerDetails.Key.ObserverId, observerDetails.Type);
            return ICanHandleReplayForObserver.Error.Unknown;
        }
    }
}
