// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.Events;
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
    public Task RetryPartition(RetryPartition command, CallContext context = default) =>
        grainFactory.GetObserver(command).TryStartRecoverJobForFailedPartition(command.Partition);

    /// <inheritdoc/>
    public Task Replay(Replay command, CallContext context = default) =>
        grainFactory.GetObserver(command).Replay();

    /// <inheritdoc/>
    public Task ReplayPartition(ReplayPartition command, CallContext context = default) =>
        grainFactory.GetObserver(command).ReplayPartition(command.Partition);

    /// <inheritdoc/>
    public async Task<ObserverInformation> GetObserverInformation(GetObserverInformationRequest request, CallContext context = default)
    {
        var observer = grainFactory.GetObserver(request);
        var state = await observer.GetState();
        var subscribed = await observer.IsSubscribed();
        return new ObserverInformation
        {
            Id = request.ObserverId,
            EventSequenceId = state.EventSequenceId,
            Type = state.Type.ToContract(),
            EventTypes = state.EventTypes.ToContract(),
            NextEventSequenceNumber = state.NextEventSequenceNumber,
            LastHandledEventSequenceNumber = state.LastHandledEventSequenceNumber,
            RunningState = state.RunningState.ToContract(),
            IsSubscribed = subscribed
        };
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverInformation>> GetObservers(AllObserversRequest request, CallContext context = default)
    {
        var observers = await storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace).Observers.GetAll();
        return observers.ToContract();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<ObserverInformation>> ObserveObservers(AllObserversRequest request, CallContext context = default) =>
        storage
            .GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace).Observers
            .ObserveAll()
            .CompletedBy(context.CancellationToken)
            .Select(_ => _.ToContract());
}
