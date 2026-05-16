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
    public Task RetryPartition(RetryPartition command, CallContext context = default) =>
        grainFactory.GetObserver(command).TryStartRecoverJobForFailedPartition(command.Partition);

    /// <inheritdoc/>
    public Task Replay(Replay command, CallContext context = default) =>
        grainFactory.GetObserver(command).Replay();

    /// <inheritdoc/>
    public Task ReplayPartition(ReplayPartition command, CallContext context = default) =>
        grainFactory.GetObserver(command).ReplayPartition(command.Partition);

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

            var observerIds = observers.Select(_ => _.Id).ToHashSet(StringComparer.Ordinal);
            var failedPartitions = await storage
                .GetEventStore(request.EventStore)
                .GetNamespace(request.Namespace)
                .FailedPartitions
                .GetFor(default);
            var relevantFailedPartitions = failedPartitions
                .Partitions
                .Where(_ => observerIds.Contains(_.ObserverId.Value))
                .ToArray();

            if (relevantFailedPartitions.Length > 0)
            {
                return new WaitForObserverCompletionResponse
                {
                    IsSuccess = false,
                    FailedPartitions = relevantFailedPartitions.ToContract().ToArray()
                };
            }

            if (observers.All(_ => _.LastHandledEventSequenceNumber >= request.TailEventSequenceNumber))
            {
                return new WaitForObserverCompletionResponse
                {
                    IsSuccess = true
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
            RunningState = state.RunningState.ToContract(),
            IsSubscribed = subscribed,
            IsReplayable = definition.IsReplayable
        };
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverInformation>> GetObservers(AllObserversRequest request, CallContext context = default)
    {
        var observerDefinitions = await storage.GetEventStore(request.EventStore).Observers.GetAll();
        var observerStates = await storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace).Observers.GetAll();
        var observers =
            from definition in observerDefinitions
            join state in observerStates on definition.Identifier equals state.Identifier
            select (definition, state);
        return observers.ToContract();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<ObserverInformation>> ObserveObservers(AllObserversRequest request, CallContext context = default) =>
        storage
            .GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace).Observers
            .ObserveAll()
            .CompletedBy(context.CancellationToken)
            .SelectMany(async observerStates =>
            {
                // TODO: We will be formalizing these things in Grains, until then this is less than optimal.
                var observerDefinitions = await storage.GetEventStore(request.EventStore).Observers.GetAll();
                var observers =
                    from definition in observerDefinitions
                    join state in observerStates on definition.Identifier equals state.Identifier
                    select (definition, state);
                return observers.ToContract();
            });

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
}
