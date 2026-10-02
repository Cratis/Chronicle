// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task<bool> NeedsSubscriptionRecovery(IEnumerable<EventType> eventTypes)
    {
        if (_removed || IsRetired || State.AlertDisposition == AlertDisposition.Retired || State.RunningState == ObserverRunningState.Quarantined)
        {
            return false;
        }

        var currentState = await GetCurrentState();
        if (currentState is Observing or States.Replay)
        {
            // Reconciliation can hold a registry snapshot older than a concurrent registration's subscription.
            // Extra types and newer generations are already covered and must never trigger a narrowing refresh.
            var subscribedGenerations = _subscription.EventTypes
                .GroupBy(eventType => eventType.Id)
                .ToDictionary(group => group.Key, group => group.Max(eventType => eventType.Generation.Value));

            return eventTypes.Any(expected => !subscribedGenerations.TryGetValue(expected.Id, out var generation) ||
                generation < expected.Generation.Value);
        }

        return currentState is Disconnected or Routing or CatchingUpInFlight &&
            (!_subscription.IsSubscribed || _subscriptionSetupFailed);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventType>> SubscribeAdditively<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs = null,
        bool isReplayable = true,
        ObserverFilters? filters = null)
        where TObserverSubscriber : IObserverSubscriber
    {
        await SubscribeToEventTypes<TObserverSubscriber>(type, eventTypes, siloAddress, subscriberArgs, isReplayable, filters, additive: true, reactivateRetired: false);

        return _subscription.EventTypes;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventType>> RecoverStalledSubscription<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs = null,
        bool isReplayable = true,
        ObserverFilters? filters = null)
        where TObserverSubscriber : IObserverSubscriber
    {
        // The shared entry rechecks eligibility in this non-interleaved request, after any running setup.
        await SubscribeToEventTypes<TObserverSubscriber>(type, eventTypes, siloAddress, subscriberArgs, isReplayable, filters, additive: true, recovering: true, reactivateRetired: false);

        return _subscription.EventTypes;
    }

    EventType[] MergeSubscribedEventTypes(IEnumerable<EventType> eventTypes) => eventTypes.Concat(_subscription.EventTypes)
        .GroupBy(eventType => eventType.Id)
        .Select(group => group.OrderByDescending(eventType => eventType.Generation.Value).First())
        .ToArray();
}
