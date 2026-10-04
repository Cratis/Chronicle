// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleReplayForObserver"/> for reducers.
/// </summary>
/// <remarks>
/// A full replay rebuilds a reducer's read model the way it rebuilds a projection's: into a replay container that starts
/// out empty and replaces the read model's container when the replay ends. The reducer pipeline takes its sink from the
/// same per-silo cache as this handler, so putting that sink into replay mode on every silo is what makes the replayed
/// events fold from nothing rather than onto the documents the replay is meant to rebuild - which the watermark guard
/// would otherwise refuse to overwrite, leaving the replay a silent no-op (#4335).
/// </remarks>
/// <param name="reducerMediator"><see cref="IReducerMediator"/> for notifying connected clients.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for resolving read model definitions and replay managers.</param>
/// <param name="storage"><see cref="IStorage"/> for reducer definitions, replay contexts and sinks.</param>
/// <param name="options">The <see cref="Configuration.ChronicleOptions"/>.</param>
/// <param name="logger">The logger.</param>
public class ReducerReplayHandler(
    IReducerMediator reducerMediator,
    IGrainFactory grainFactory,
    IStorage storage,
    IOptions<Configuration.ChronicleOptions> options,
    ILogger<ReducerReplayHandler> logger) : ICanHandleReplayForObserver
{
    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> BeginReplayFor(ObserverDetails observerDetails)
    {
        var result = await DoWorkOnSink(
            observerDetails,
            async readModel =>
            {
                var replayManager = grainFactory.GetReadModelReplayManager(observerDetails.Key.EventStore, observerDetails.Key.Namespace, readModel.Identifier);
                await replayManager.ApplyRetentionPolicy(options.Value.ReadModels.ReplayedVersionsToKeep);
                return await ReplayContextsFor(observerDetails).Establish(new(readModel.Identifier, readModel.LatestGeneration), readModel.ContainerName);
            },
            async (sink, _, context) =>
            {
                await sink.BeginReplay(context);
                return Result<ICanHandleReplayForObserver.Error>.Success();
            });

        if (!result.TryGetError(out _))
        {
            reducerMediator.OnBeginReplay(ReducerIdFor(observerDetails), observerDetails.Key.EventStore, observerDetails.Key.Namespace);
        }

        return result;
    }

    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> ResumeReplayFor(ObserverDetails observerDetails) => DoWorkOnSink(
        observerDetails,
        readModel => ReplayContextsFor(observerDetails).TryGet(readModel.Identifier),
        async (sink, _, context) =>
        {
            await sink.ResumeReplay(context);
            return Result<ICanHandleReplayForObserver.Error>.Success();
        });

    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> EndReplayFor(ObserverDetails observerDetails)
    {
        var result = await DoWorkOnSink(
            observerDetails,
            readModel => ReplayContextsFor(observerDetails).TryGet(readModel.Identifier),
            async (sink, readModel, context) =>
            {
                var failedPartitions = (await sink.EndReplay(context)).ToArray();
                await ProjectionBulkFailures.Record(grainFactory, observerDetails, failedPartitions);
                if (failedPartitions.Length > 0)
                {
                    return ICanHandleReplayForObserver.Error.Unknown;
                }

                var replayManager = grainFactory.GetReadModelReplayManager(observerDetails.Key.EventStore, observerDetails.Key.Namespace, readModel.Identifier);
                await replayManager.Replayed(observerDetails.Key.ObserverId, context);
                await ReplayContextsFor(observerDetails).Evict(readModel.Identifier);
                return Result<ICanHandleReplayForObserver.Error>.Success();
            },

            // The context is gone because another silo has already ended this replay and promoted the read model. This
            // silo's sink is still writing to the replay container, which nothing reads any more - it has to stop.
            sink => sink.LeaveReplay());

        if (!result.TryGetError(out var error) || error != ICanHandleReplayForObserver.Error.CannotHandle)
        {
            reducerMediator.OnEndReplay(ReducerIdFor(observerDetails), observerDetails.Key.EventStore, observerDetails.Key.Namespace);
        }

        return result;
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

            var readModel = await ReadModelFor(observerDetails);
            if (readModel is null)
            {
                return Result<ICanHandleReplayForObserver.Error>.Success();
            }

            var sink = await SinkFor(observerDetails, readModel);
            var failedPartitions = (await sink.EndBulk()).ToArray();
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
    public async Task<Result<ICanHandleReplayForObserver.Error>> AbandonReplayFor(ObserverDetails observerDetails)
    {
        var result = await DoWorkOnSink(
            observerDetails,
            readModel => ReplayContextsFor(observerDetails).TryGet(readModel.Identifier),
            async (sink, readModel, _) =>
            {
                await Abandon(sink);
                await ReplayContextsFor(observerDetails).Evict(readModel.Identifier);
                return Result<ICanHandleReplayForObserver.Error>.Success();
            },

            // Another silo has already evicted the context; this silo still has to stop writing to the replay container.
            Abandon);

        if (!result.TryGetError(out var error) || error != ICanHandleReplayForObserver.Error.CannotHandle)
        {
            reducerMediator.OnEndReplay(ReducerIdFor(observerDetails), observerDetails.Key.EventStore, observerDetails.Key.Namespace);
        }

        return result;
    }

    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> BeginReplayPartitionFor(ObserverDetails observerDetails, Key partition)
    {
        if (!CanHandle(observerDetails))
        {
            return Task.FromResult(Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle));
        }

        reducerMediator.OnBeginReplayPartition(ReducerIdFor(observerDetails), observerDetails.Key.EventStore, observerDetails.Key.Namespace, partition);
        return Task.FromResult(Result<ICanHandleReplayForObserver.Error>.Success());
    }

    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> EndReplayPartitionFor(ObserverDetails observerDetails, Key partition)
    {
        if (!CanHandle(observerDetails))
        {
            return Task.FromResult(Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle));
        }

        reducerMediator.OnEndReplayPartition(ReducerIdFor(observerDetails), observerDetails.Key.EventStore, observerDetails.Key.Namespace, partition);
        return Task.FromResult(Result<ICanHandleReplayForObserver.Error>.Success());
    }

    static bool CanHandle(ObserverDetails observerDetails) => observerDetails.Type == ObserverType.Reducer;

    /// <summary>
    /// Leaves replay mode without writing anything the replay rebuilt to the read model's own container.
    /// </summary>
    /// <remarks>
    /// Leaving replay mode writes whatever is still held back to the read model's own container, which is right when the
    /// replay was promoted by another silo and wrong here: an abandoned replay's held-back writes are partial state.
    /// Flushing them into the replay container first, while still in replay mode, leaves nothing to leak. The replay
    /// container itself is cleared by the next replay before it writes.
    /// </remarks>
    /// <param name="sink">The sink to take out of replay mode.</param>
    /// <returns>Awaitable task.</returns>
    static async Task Abandon(ISink sink)
    {
        await sink.EndBulk();
        await sink.LeaveReplay();
    }

    static ReducerId ReducerIdFor(ObserverDetails observerDetails) => new(observerDetails.Key.ObserverId.Value);

    IReplayContexts ReplayContextsFor(ObserverDetails observerDetails) =>
        storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace).ReplayContexts;

    async Task<ReadModelDefinition?> ReadModelFor(ObserverDetails observerDetails)
    {
        var reducers = storage.GetEventStore(observerDetails.Key.EventStore).Reducers;
        var reducerId = ReducerIdFor(observerDetails);
        if (!await reducers.Has(reducerId))
        {
            return null;
        }

        var reducer = await reducers.Get(reducerId);

        // Resolved exactly as the reducer pipeline resolves it, so the sink cache hands back the very instance the
        // pipeline reads and writes through.
        return await grainFactory.GetGrain<IReadModel>(new ReadModelGrainKey(reducer.ReadModel, observerDetails.Key.EventStore)).GetDefinition();
    }

    Task<ISink> SinkFor(ObserverDetails observerDetails, ReadModelDefinition readModel) =>
        storage.GetEventStore(observerDetails.Key.EventStore).GetNamespace(observerDetails.Key.Namespace).Sinks.GetFor(readModel);

    async Task<Result<ICanHandleReplayForObserver.Error>> DoWorkOnSink(
        ObserverDetails observerDetails,
        Func<ReadModelDefinition, Task<Result<ReplayContext, GetContextError>>> getContext,
        Func<ISink, ReadModelDefinition, ReplayContext, Task<Result<ICanHandleReplayForObserver.Error>>> doWork,
        Func<ISink, Task>? onMissingContext = null)
    {
        try
        {
            if (!CanHandle(observerDetails))
            {
                return ICanHandleReplayForObserver.Error.CannotHandle;
            }

            var readModel = await ReadModelFor(observerDetails);
            if (readModel is null)
            {
                return Result<ICanHandleReplayForObserver.Error>.Success();
            }

            var getReplayContext = await getContext(readModel);
            var sink = await SinkFor(observerDetails, readModel);
            if (getReplayContext.TryPickT1(out _, out var replayContext))
            {
                if (onMissingContext is not null)
                {
                    await onMissingContext(sink);
                }

                return ICanHandleReplayForObserver.Error.CouldNotGetReplayContext;
            }

            return await doWork(sink, readModel, replayContext);
        }
        catch (Exception ex)
        {
            logger.Failed(ex, observerDetails.Key.ObserverId, observerDetails.Type);
            return ICanHandleReplayForObserver.Error.Unknown;
        }
    }
}
