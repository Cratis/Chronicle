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
        if (CannotReconcileSubscription() || !await NeedsSubscriptionRecovery(eventTypes)) return _subscription.EventTypes;
        await ReconcileSubscription<TObserverSubscriber>(type, MergeSubscribedEventTypes(eventTypes), siloAddress, false, subscriberArgs: subscriberArgs, isReplayable: isReplayable, filters: filters);

        return _subscription.EventTypes;
    }

    /// <inheritdoc/>
    public async Task ReconcileSubscription<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        bool subscribesToAllEvents,
        EventGenerationDelivery generationDelivery = EventGenerationDelivery.Compatibility,
        object? subscriberArgs = default,
        bool isReplayable = true,
        ObserverFilters? filters = default)
        where TObserverSubscriber : IObserverSubscriber
    {
        // Unlike explicit registration, this serialized operation has no authority to release quarantine,
        // replace recovery owed to an operator clearance, or reload a stored cursor over live progress.
        if (CannotReconcileSubscription()) return;
        var types = subscribesToAllEvents ? [] : eventTypes.ToArray();
        await ValidateGenerationDelivery(types, generationDelivery);
        if (CannotReconcileSubscription()) return;
        var schemas = await storage.GetEventStore(_observerKey.EventStore).EventTypes.GetFor(types);
        if (CannotReconcileSubscription()) return;

        try
        {
            await BeginAlertLifecycle();
            if (CannotReconcileSubscription()) return;
            _eventTypeSchemas = schemas.ToDictionary(schema => schema.Type);
            _subscriptionSetupFailed = true;

            if (subscribesToAllEvents)
            {
                await SetUpAllEventsSubscription<TObserverSubscriber>(type, siloAddress, subscriberArgs, isReplayable, reconcile: true);
            }
            else
            {
                await SetUpSubscription<TObserverSubscriber>(type, types, siloAddress, subscriberArgs, isReplayable, filters, generationDelivery, reconcile: true);
            }

            // Keep the current state through awaited setup so failure callbacks can still establish quarantine.
            // Disconnected deliberately refuses ordinary quarantine transitions.
            if (CannotReconcileSubscription()) return;
            await TransitionTo<Disconnected>();
            if (CannotReconcileSubscription()) return;
            await RecoverAfterSubscribing(leavesQuarantine: false);
            _subscriptionSetupFailed = await GetCurrentState() is not (Observing or States.Replay);
        }
        catch
        {
            _subscriptionSetupFailed = true;
            throw;
        }
    }

    bool CannotReconcileSubscription() =>
        _removed || IsRetired || State.AlertDisposition == AlertDisposition.Retired ||
        IsQuarantined || State.RunningState == ObserverRunningState.Quarantined ||
        _recoverSubscriptionAfterQuarantine || _retryRecoveryAfterQuarantine;

    EventType[] MergeSubscribedEventTypes(IEnumerable<EventType> eventTypes) => eventTypes.Concat(_subscription.EventTypes)
        .GroupBy(eventType => eventType.Id)
        .Select(group => group.OrderByDescending(eventType => eventType.Generation.Value).First())
        .ToArray();
}
