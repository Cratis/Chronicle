// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Services.Events;
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
    const int ObserverCompletionPollingDelayMs = 50;

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
    public async Task<IEnumerable<ObserverInformation>> GetObservers(AllObserversRequest request, CallContext context = default)
    {
        var namespaceStorage = storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace);
        var observerStates = await namespaceStorage.Observers.GetObservers();
        return await Task.WhenAll(observerStates.Select(async state =>
        {
            var observer = grainFactory.GetObserver(request.EventStore, request.Namespace, state.ObserverId);
            var definition = await observer.GetDefinition();
            return (definition, state).ToContract();
        }));
    }

    /// <inheritdoc/>
    public async Task<WaitForObserverCompletionResponse> WaitForCompletion(WaitForObserverCompletionRequest request, CallContext context = default)
    {
        while (true)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var observers = (await GetObservers(
                new AllObserversRequest
                {
                    EventStore = request.EventStore,
                    Namespace = request.Namespace
                },
                context))
                .Where(_ => _.EventSequenceId == request.EventSequenceId)
                .ToArray();

            if (observers.Length == 0)
            {
                return new WaitForObserverCompletionResponse
                {
                    IsSuccess = true
                };
            }

            var observerIds = observers.Select(_ => (Concepts.Observation.ObserverId)_.Id).ToArray();
            var failedPartitions = await storage
                .GetEventStore(request.EventStore)
                .GetNamespace(request.Namespace)
                .FailedPartitions
                .GetFor(observerIds);
            var failedObserverIds = failedPartitions.Partitions.Select(_ => _.ObserverId.Value).ToHashSet(StringComparer.Ordinal);
            if (observers.All(_ =>
                _.LastHandledEventSequenceNumber >= request.TailEventSequenceNumber ||
                failedObserverIds.Contains(_.Id)))
            {
                return new WaitForObserverCompletionResponse
                {
                    IsSuccess = !failedPartitions.Partitions.Any(),
                    FailedPartitions = failedPartitions.Partitions.ToContract().ToArray()
                };
            }

            await Task.Delay(ObserverCompletionPollingDelayMs, context.CancellationToken);
        }
    }

    /// <inheritdoc/>
    public Task ClearObserverQuarantine(ClearObserverQuarantine command, CallContext context = default) =>
        grainFactory.GetObserver(command).ClearObserverQuarantine();

    /// <inheritdoc/>
    public async Task<ObserverInformation> GetObserverInformation(GetObserverInformationRequest request, CallContext context = default)
    {
        var observer = grainFactory.GetObserver(request);
        var definition = await observer.GetDefinition();
        var state = await observer.GetState();
        var subscribed = await observer.IsSubscribed();
        return new ObserverInformation
        {
            Id = request.ObserverId,
            EventSequenceId = definition.EventSequenceId,
            Type = definition.Type.ToContract(),
            Owner = definition.Owner.ToContract(),
            EventTypes = definition.EventTypes.ToContract(),
            NextEventSequenceNumber = state.NextEventSequenceNumber,
            LastHandledEventSequenceNumber = state.LastHandledEventSequenceNumber,
            TailEventSequenceNumber = state.TailEventSequenceNumber,
            HandledEventCount = state.HandledEventCount,
            RunningState = state.RunningState.ToContract(),
            IsSubscribed = subscribed,
            IsReplayable = definition.IsReplayable
        };
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<ObserverInformationResponse>> ObserveObservers(ObserveObserversRequest request, CallContext callContext = default) =>
        Chronicle.Observation.ObserverInformation.ObserveObservers(request.EventStore, request.Namespace, storage)
            .CompletedBy(callContext.CancellationToken)
            .Select(observers => (IEnumerable<ObserverInformationResponse>)observers.Select(o => ToResponse(o)).ToList());

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverInformation>> GetReplayableObserversForEventTypes(GetReplayableObserversForEventTypesRequest request, CallContext context = default)
    {
        var eventTypes = request.EventTypes.ToChronicle();
        var observerDefinitions = await storage.GetEventStore(request.EventStore).Observers.GetReplayableObserversForEventTypes(eventTypes);
        var observerStates = await storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace).Observers.GetAll();
        var observers =
            from definition in observerDefinitions
            join state in observerStates on definition.Identifier equals state.Identifier
            select (definition, state);
        return observers.ToContract();
    }

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
