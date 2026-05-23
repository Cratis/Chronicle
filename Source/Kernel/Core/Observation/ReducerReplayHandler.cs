// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleReplayForObserver"/> for reducers.
/// </summary>
/// <param name="reducerMediator"><see cref="IReducerMediator"/> for notifying connected clients.</param>
public class ReducerReplayHandler(IReducerMediator reducerMediator) : ICanHandleReplayForObserver
{
    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> BeginReplayFor(ObserverDetails observerDetails)
    {
        if (!CanHandle(observerDetails))
        {
            return Task.FromResult(Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle));
        }

        reducerMediator.OnBeginReplay(
            new ReducerId(observerDetails.Key.ObserverId.Value),
            observerDetails.Key.EventStore,
            observerDetails.Key.Namespace);

        return Task.FromResult(Result<ICanHandleReplayForObserver.Error>.Success());
    }

    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> ResumeReplayFor(ObserverDetails observerDetails) =>
        CanHandle(observerDetails)
            ? Task.FromResult(Result<ICanHandleReplayForObserver.Error>.Success())
            : Task.FromResult(Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle));

    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> EndReplayFor(ObserverDetails observerDetails)
    {
        if (!CanHandle(observerDetails))
        {
            return Task.FromResult(Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle));
        }

        reducerMediator.OnEndReplay(
            new ReducerId(observerDetails.Key.ObserverId.Value),
            observerDetails.Key.EventStore,
            observerDetails.Key.Namespace);

        return Task.FromResult(Result<ICanHandleReplayForObserver.Error>.Success());
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
}
