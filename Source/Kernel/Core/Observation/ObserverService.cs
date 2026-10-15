// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Monads;
using Cratis.Types;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverService"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ObserverService"/> class.
/// </remarks>
/// <param name="grainId">The <see cref="GrainId"/> for the service.</param>
/// <param name="silo">The <see cref="Silo"/> the service belongs to.</param>
/// <param name="replayHandlers">All instances of <see cref="ICanHandleReplayForObserver"/>.</param>
/// <param name="catchupHandlers">All instances of <see cref="ICanHandleCatchupForObserver"/>.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
/// <param name="stopBudget">The shared stop deadline.</param>
/// <param name="grainFactory">The grain factory for recording failed partitions.</param>
[Reentrant]
public class ObserverService(
    GrainId grainId,
    Silo silo,
    IInstancesOf<ICanHandleReplayForObserver> replayHandlers,
    IInstancesOf<ICanHandleCatchupForObserver> catchupHandlers,
    ILoggerFactory loggerFactory,
    IShutdownDrainBudget stopBudget,
    IGrainFactory grainFactory) : GrainService(grainId, silo, loggerFactory), IObserverService
{
    readonly ObserverServiceDrain _drain = new();
    readonly ILogger<ObserverService> _logger = loggerFactory.CreateLogger<ObserverService>();
    readonly ObserverBulkDrain _bulkDrain = new(grainFactory, loggerFactory.CreateLogger<ObserverService>());

    /// <inheritdoc/>
    public override async Task Stop()
    {
        await _drain.Stop(EndWindow, stopBudget.Begin());
        await base.Stop();
    }

    /// <inheritdoc/>
    public Task BeginReplayFor(ObserverDetails observerDetails) => Run(observerDetails, true, async () =>
    {
        if (!RebuildsReadModel(observerDetails))
        {
            await ForEachReplayHandler(handler => handler.BeginReplayFor(observerDetails));
            return;
        }

        var results = await Task.WhenAll(replayHandlers.Select(handler => handler.BeginReplayFor(observerDetails)));
        EnsureReplayStarted(results);
    });

    /// <inheritdoc/>
    public Task ResumeReplayFor(ObserverDetails observerDetails) => Run(observerDetails, true, () => ForEachReplayHandler(handler => handler.ResumeReplayFor(observerDetails)));

    /// <inheritdoc/>
    public async Task EndReplayFor(ObserverDetails observerDetails) => _ = await TryFinalizeReplayFor(observerDetails);

    /// <inheritdoc/>
    public Task<bool> TryFinalizeReplayFor(ObserverDetails observerDetails) => _drain.Run(observerDetails, true, async () =>
    {
        var results = await Task.WhenAll(replayHandlers.Select(handler => handler.EndReplayFor(observerDetails)));
        var finalized = EnsureReplayFinalized(results);
        _drain.Ended(observerDetails, true);
        return finalized;
    });

    /// <inheritdoc/>
    public Task AbandonReplayFor(ObserverDetails observerDetails) => Run(observerDetails, true, async () =>
    {
        var results = await Task.WhenAll(replayHandlers.Select(handler => handler.AbandonReplayFor(observerDetails)));
        EnsureReplayAbandoned(results);
        _drain.Ended(observerDetails, true);
    });

    /// <inheritdoc/>
    public Task<bool> FlushReplayFor(ObserverDetails observerDetails) => _drain.Run(observerDetails, true, async () =>
    {
        var results = await Task.WhenAll(replayHandlers.Select(handler => handler.FlushReplayFor(observerDetails)));
        return results.All(result => !result.TryGetError(out var error) || error == ICanHandleReplayForObserver.Error.CannotHandle);
    });

    /// <inheritdoc/>
    public Task BeginReplayPartitionFor(ObserverDetails observerDetails, Key partition) => Run(observerDetails, true, () => ForEachReplayHandler(handler => handler.BeginReplayPartitionFor(observerDetails, partition)));

    /// <inheritdoc/>
    public Task EndReplayPartitionFor(ObserverDetails observerDetails, Key partition) => Run(observerDetails, true, () => ForEachReplayHandler(handler => handler.EndReplayPartitionFor(observerDetails, partition)));

    /// <inheritdoc/>
    public Task BeginCatchupFor(ObserverDetails observerDetails) => Run(observerDetails, false, () => ForEachCatchupHandler(handler => handler.BeginCatchupFor(observerDetails)));

    /// <inheritdoc/>
    public Task ResumeCatchupFor(ObserverDetails observerDetails) => Run(observerDetails, false, () => ForEachCatchupHandler(handler => handler.ResumeCatchupFor(observerDetails)));

    /// <inheritdoc/>
    public Task EndCatchupFor(ObserverDetails observerDetails) => Run(observerDetails, false, async () =>
    {
        var results = await Task.WhenAll(catchupHandlers.Select(handler => handler.EndCatchupFor(observerDetails)));
        EnsureCatchupFinalized(results);
        _drain.Ended(observerDetails, false);
    });

    /// <inheritdoc/>
    public Task<Guid[]> FlushFor(ObserverDetails observerDetails, bool replay) => _drain.Run(observerDetails, replay, async () =>
    {
        var results = replay
            ? await Task.WhenAll(replayHandlers.Select(handler => handler.FlushFor(observerDetails)))
            : await Task.WhenAll(catchupHandlers.Select(handler => handler.FlushFor(observerDetails)));
        if (results.Any(result => result.FailedPartitions.Any()))
        {
            throw new ObserverFlushFailed();
        }

        return results.Where(result => result.WindowId is not null).Select(result => result.WindowId!.Value).Distinct().ToArray();
    });

    /// <summary>
    /// Ensure every applicable catch-up handler finished successfully.
    /// </summary>
    /// <param name="results">The results returned by the catch-up handlers.</param>
    /// <exception cref="CatchupFinalizationFailed">A handler reported a finalization error.</exception>
    internal static void EnsureCatchupFinalized(IEnumerable<Result<ICanHandleCatchupForObserver.Error>> results)
    {
        foreach (var result in results)
        {
            if (result.TryGetError(out var error) && error != ICanHandleCatchupForObserver.Error.CannotHandle)
            {
                throw new CatchupFinalizationFailed(error);
            }
        }
    }

    /// <summary>
    /// Gets whether a replay of an observer rebuilds a read model into a replay container that has to be promoted.
    /// </summary>
    /// <remarks>
    /// Projections and reducers both rebuild their read model from nothing during a replay. Starting such a replay has to
    /// succeed on every silo, and ending it has to flush every silo before exactly one of them promotes the result.
    /// </remarks>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>True if the replay rebuilds a read model; otherwise false.</returns>
    internal static bool RebuildsReadModel(ObserverDetails observerDetails) =>
        observerDetails.Type is ObserverType.Projection or ObserverType.Reducer;

    /// <summary>
    /// Ensure every silo that held a replay abandoned it.
    /// </summary>
    /// <param name="results">The results returned by the replay handlers.</param>
    /// <exception cref="ReplayFinalizationFailed">A handler reported a failure to abandon the replay.</exception>
    internal static void EnsureReplayAbandoned(IEnumerable<Result<ICanHandleReplayForObserver.Error>> results)
    {
        foreach (var result in results)
        {
            // Every silo is asked; one that never held the replay context has nothing to abandon.
            if (result.TryGetError(out var error) &&
                error != ICanHandleReplayForObserver.Error.CannotHandle &&
                error != ICanHandleReplayForObserver.Error.CouldNotGetReplayContext)
            {
                throw new ReplayFinalizationFailed(error);
            }
        }
    }

    /// <summary>
    /// Ensure every applicable replay handler started successfully.
    /// </summary>
    /// <param name="results">The results returned by the replay handlers.</param>
    /// <exception cref="ReplayInitializationFailed">A handler reported a failure to start replay.</exception>
    internal static void EnsureReplayStarted(IEnumerable<Result<ICanHandleReplayForObserver.Error>> results)
    {
        foreach (var result in results)
        {
            if (result.TryGetError(out var error) && error != ICanHandleReplayForObserver.Error.CannotHandle)
            {
                throw new ReplayInitializationFailed(error);
            }
        }
    }

    /// <summary>
    /// Ensure every applicable replay handler finished successfully.
    /// </summary>
    /// <param name="results">The results returned by the replay handlers.</param>
    /// <returns>Whether this silo finalized the replay.</returns>
    /// <exception cref="ReplayFinalizationFailed">A handler reported a finalization error.</exception>
    internal static bool EnsureReplayFinalized(IEnumerable<Result<ICanHandleReplayForObserver.Error>> results)
    {
        var finalized = false;
        foreach (var result in results)
        {
            // EndReplayFor is sent to every silo. Replay contexts are shared in storage; CouldNotGetReplayContext
            // means the context is gone from cache and storage, usually because another silo already finalized
            // and evicted it. Only a successful handler here proves that this silo finalized the replay.
            if (result.TryGetError(out var error))
            {
                if (error is not (ICanHandleReplayForObserver.Error.CannotHandle or ICanHandleReplayForObserver.Error.CouldNotGetReplayContext))
                {
                    throw new ReplayFinalizationFailed(error);
                }
            }
            else
            {
                finalized = true;
            }
        }

        return finalized;
    }

    Task<bool> Run(ObserverDetails observer, bool replay, Func<Task> callback) => _drain.Run(observer, replay, async () =>
    {
        await callback();
        return true;
    });

    async Task EndWindow(ObserverDetails observer, bool replay, CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.DrainDeadlinePassed(observer.Key);
            }

            var work = replay
                ? replayHandlers.Select(handler => _bulkDrain.Drain(observer, token => handler.EndBulkFor(observer, token), cancellationToken))
                : catchupHandlers.Select(handler => _bulkDrain.Drain(observer, token => handler.EndBulkFor(observer, token), cancellationToken));
            await Task.WhenAll(work);
        }
        catch (Exception exception)
        {
            _logger.DrainFailed(exception, observer.Key);
        }
    }

    async Task ForEachReplayHandler(Func<ICanHandleReplayForObserver, Task> callback)
    {
        var tasks = replayHandlers.Select(callback);
        await Task.WhenAll(tasks);
    }

    async Task ForEachCatchupHandler(Func<ICanHandleCatchupForObserver, Task> callback)
    {
        var tasks = catchupHandlers.Select(callback);
        await Task.WhenAll(tasks);
    }
}
