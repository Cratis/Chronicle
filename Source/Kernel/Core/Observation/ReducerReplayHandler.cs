// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Notifies reducer clients of replay boundaries. The job owns the isolated target and its publication;
/// a silo notification must never switch a live sink or promote a partially rebuilt model.
/// </summary>
/// <param name="reducerMediator">The connected reducer clients.</param>
public class ReducerReplayHandler(IReducerMediator reducerMediator) : ICanHandleReplayForObserver
{
    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> BeginReplayFor(ObserverDetails observerDetails) => Notify(
        observerDetails,
        () => reducerMediator.OnBeginReplay(new ReducerId(observerDetails.Key.ObserverId.Value), observerDetails.Key.EventStore, observerDetails.Key.Namespace));

    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> ResumeReplayFor(ObserverDetails observerDetails) => BeginReplayFor(observerDetails);

    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> EndReplayFor(ObserverDetails observerDetails) => Notify(
        observerDetails,
        () => reducerMediator.OnEndReplay(new ReducerId(observerDetails.Key.ObserverId.Value), observerDetails.Key.EventStore, observerDetails.Key.Namespace));

    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> BeginReplayPartitionFor(ObserverDetails observerDetails, Key partition) => Notify(
        observerDetails,
        () => reducerMediator.OnBeginReplayPartition(new ReducerId(observerDetails.Key.ObserverId.Value), observerDetails.Key.EventStore, observerDetails.Key.Namespace, partition));

    /// <inheritdoc/>
    public Task<Result<ICanHandleReplayForObserver.Error>> EndReplayPartitionFor(ObserverDetails observerDetails, Key partition) => Notify(
        observerDetails,
        () => reducerMediator.OnEndReplayPartition(new ReducerId(observerDetails.Key.ObserverId.Value), observerDetails.Key.EventStore, observerDetails.Key.Namespace, partition));

    static Task<Result<ICanHandleReplayForObserver.Error>> Notify(ObserverDetails details, Action notify)
    {
        if (details.Type != ObserverType.Reducer)
        {
            return Task.FromResult(Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle));
        }

        notify();
        return Task.FromResult(Result<ICanHandleReplayForObserver.Error>.Success());
    }
}
