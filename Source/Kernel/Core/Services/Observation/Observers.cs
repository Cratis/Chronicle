// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
/// <param name="storage">The <see cref="IStorage"/>.</param>
internal sealed class Observers(IGrainFactory grainFactory, IStorage storage) : IObservers
{
    /// <inheritdoc/>
    public Task ReplayObserver(ReplayObserverRequest request, CallContext callContext = default) =>
        new Chronicle.Observation.ReplayObserver(request.EventStore, request.Namespace, request.ObserverId, request.EventSequenceId)
            .Handle(grainFactory);

    /// <inheritdoc/>
    public Task ReplayPartition(ReplayPartitionRequest request, CallContext callContext = default) =>
        new Chronicle.Observation.ReplayPartition(request.EventStore, request.Namespace, request.ObserverId, request.EventSequenceId, request.Partition)
            .Handle(grainFactory);

    /// <inheritdoc/>
    public Task RetryPartition(RetryPartitionRequest request, CallContext callContext = default) =>
        new Chronicle.Observation.RetryPartition(request.EventStore, request.Namespace, request.ObserverId, request.EventSequenceId, request.Partition)
            .Handle(grainFactory);

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverInformationResponse>> AllObservers(AllObserversRequest request, CallContext callContext = default)
    {
        var observers = await Chronicle.Observation.ObserverInformation.AllObservers(request.EventStore, request.Namespace, storage);
        return observers.Select(ToResponse);
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<ObserverInformationResponse>> ObserveObservers(ObserveObserversRequest request, CallContext callContext = default) =>
        Chronicle.Observation.ObserverInformation.ObserveObservers(request.EventStore, request.Namespace, storage)
            .CompletedBy(callContext.CancellationToken)
            .Select(observers => (IEnumerable<ObserverInformationResponse>)observers.Select(o => ToResponse(o)).ToList());

    static ObserverInformationResponse ToResponse(Chronicle.Observation.ObserverInformation info) => new()
    {
        Id = info.Id,
        EventSequenceId = info.EventSequenceId,
        Type = info.Type,
        Owner = info.Owner,
        EventTypes = info.EventTypes,
        NextEventSequenceNumber = info.NextEventSequenceNumber,
        LastHandledEventSequenceNumber = info.LastHandledEventSequenceNumber,
        RunningState = info.RunningState,
        IsSubscribed = info.IsSubscribed,
        IsReplayable = info.IsReplayable
    };
}
