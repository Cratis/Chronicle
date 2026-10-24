// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Jobs;

public partial class HandleEventsForPartition
{
    IReadOnlyDictionary<SiloAddress, Guid[]>? _bulkWindowPin;
    ObserverDetails? _bulkObserver;
    bool _bulkReplay;

    async Task<IEventCursor> OpenDurableRange(
        HandleEventsForPartitionState currentState,
        ObserverSubscription subscription,
        IEnumerable<EventType> eventTypesToRead,
        CancellationToken cancellationToken)
    {
        var eventSequenceStorage = GetEventSequenceStorage(
            currentState.ObserverKey.EventStore,
            currentState.ObserverKey.Namespace,
            currentState.ObserverKey.EventSequenceId);
        await PinBulkWindows(currentState, subscription);
        return await eventSequenceStorage.GetRange(
            currentState.LastSuccessfullyHandledEventSequenceNumber == EventSequenceNumber.Unavailable
                ? currentState.StartEventSequenceNumber
                : currentState.LastSuccessfullyHandledEventSequenceNumber.Next(),
            currentState.EndEventSequenceNumber,
            _eventSourceId,
            eventTypesToRead,
            cancellationToken: cancellationToken);
    }

    async Task PinBulkWindows(HandleEventsForPartitionState currentState, ObserverSubscription subscription)
    {
        _bulkWindowPin = null;
        _bulkObserver = null;
        if (!subscription.IsCollapsingProjection && !subscription.SubscriberType.IsAssignableTo(typeof(IProjectionObserverSubscriber))) return;

        _bulkObserver = new(currentState.ObserverKey, ObserverType.Projection);
        _bulkReplay = currentState.EventObservationState.HasFlag(EventObservationState.Replay);
        var windows = await observerServiceClient.FlushAllFor(_bulkObserver, _bulkReplay);
        _bulkWindowPin = windows.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
    }

    async Task ConfirmBulkWindowDurability()
    {
        if (_bulkWindowPin is null) return;
        var windows = await observerServiceClient.FlushAllFor(_bulkObserver!, _bulkReplay);
        if (!_bulkWindowPin.Keys.Union(windows.Keys).All(silo =>
            (_bulkWindowPin.GetValueOrDefault(silo) ?? []).ToHashSet().SetEquals(windows.GetValueOrDefault(silo) ?? [])))
        {
            throw new BulkWindowLost();
        }
    }
}
