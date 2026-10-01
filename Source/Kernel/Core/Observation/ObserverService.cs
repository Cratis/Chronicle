// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
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
[Reentrant]
public class ObserverService(
    GrainId grainId,
    Silo silo,
    IInstancesOf<ICanHandleReplayForObserver> replayHandlers,
    IInstancesOf<ICanHandleCatchupForObserver> catchupHandlers,
    ILoggerFactory loggerFactory) : GrainService(grainId, silo, loggerFactory), IObserverService
{
    /// <inheritdoc/>
    public async Task BeginReplayFor(ObserverDetails observerDetails)
    {
        if (observerDetails.Type is not (ObserverType.Projection or ObserverType.Reducer))
        {
            await ForEachReplayHandler(handler => handler.BeginReplayFor(observerDetails));
            return;
        }

        var results = await Task.WhenAll(replayHandlers.Select(handler => handler.BeginReplayFor(observerDetails)));
        EnsureReplayStarted(results);
    }

    /// <inheritdoc/>
    public async Task ResumeReplayFor(ObserverDetails observerDetails) => await ResumeReplayForHandlers(replayHandlers, observerDetails);

    /// <inheritdoc/>
    public async Task EndReplayFor(ObserverDetails observerDetails) => _ = await TryFinalizeReplayFor(observerDetails);

    /// <inheritdoc/>
    public async Task<bool> TryFinalizeReplayFor(ObserverDetails observerDetails)
    {
        var results = await Task.WhenAll(replayHandlers.Select(handler => handler.EndReplayFor(observerDetails)));
        return EnsureReplayFinalized(results);
    }

    /// <inheritdoc/>
    public async Task<bool> FlushReplayFor(ObserverDetails observerDetails)
    {
        var results = await Task.WhenAll(replayHandlers.Select(handler => handler.FlushReplayFor(observerDetails)));
        return results.All(result => !result.TryGetError(out var error) || error == ICanHandleReplayForObserver.Error.CannotHandle);
    }

    /// <inheritdoc/>
    public async Task BeginReplayPartitionFor(ObserverDetails observerDetails, Key partition) => await ForEachReplayHandler(handler => handler.BeginReplayPartitionFor(observerDetails, partition));

    /// <inheritdoc/>
    public async Task EndReplayPartitionFor(ObserverDetails observerDetails, Key partition) => await ForEachReplayHandler(handler => handler.EndReplayPartitionFor(observerDetails, partition));

    /// <inheritdoc/>
    public async Task BeginCatchupFor(ObserverDetails observerDetails) => await ForEachCatchupHandler(handler => handler.BeginCatchupFor(observerDetails));

    /// <inheritdoc/>
    public async Task ResumeCatchupFor(ObserverDetails observerDetails) => await ForEachCatchupHandler(handler => handler.ResumeCatchupFor(observerDetails));

    /// <inheritdoc/>
    public async Task EndCatchupFor(ObserverDetails observerDetails)
    {
        var results = await Task.WhenAll(catchupHandlers.Select(handler => handler.EndCatchupFor(observerDetails)));
        EnsureCatchupFinalized(results);
    }

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
    /// Reattach every replay handler before the job resumes its steps.
    /// </summary>
    /// <param name="handlers">The replay handlers on this silo.</param>
    /// <param name="observerDetails">The observer being resumed.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task ResumeReplayForHandlers(IEnumerable<ICanHandleReplayForObserver> handlers, ObserverDetails observerDetails)
    {
        var results = await Task.WhenAll(handlers.Select(handler => handler.ResumeReplayFor(observerDetails)));
        if (observerDetails.Type is ObserverType.Projection or ObserverType.Reducer)
        {
            EnsureReplayStarted(results);
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
