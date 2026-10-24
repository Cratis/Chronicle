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

        if (_subscriptionSetupFailed) return true;

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
        if (CannotReconcileSubscription(typeof(Webhooks.IWebhookObserverSubscriber).IsAssignableFrom(typeof(TObserverSubscriber)))) return;
        var types = subscribesToAllEvents ? [] : eventTypes.ToArray();
        await ValidateGenerationDelivery(types, generationDelivery);
        if (CannotReconcileSubscription(typeof(Webhooks.IWebhookObserverSubscriber).IsAssignableFrom(typeof(TObserverSubscriber)))) return;
        var schemas = await storage.GetEventStore(_observerKey.EventStore).EventTypes.GetFor(types);
        if (CannotReconcileSubscription(typeof(Webhooks.IWebhookObserverSubscriber).IsAssignableFrom(typeof(TObserverSubscriber)))) return;

        try
        {
            await BeginAlertLifecycle();
            if (CannotReconcileSubscription(typeof(Webhooks.IWebhookObserverSubscriber).IsAssignableFrom(typeof(TObserverSubscriber)))) return;
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

            await RecoverReconciledSubscription(typeof(Webhooks.IWebhookObserverSubscriber).IsAssignableFrom(typeof(TObserverSubscriber)));
        }
        catch
        {
            _subscriptionSetupFailed = true;
            throw;
        }
    }

    async Task RecoverReconciledSubscription(bool allowQuarantinedWebhook)
    {
        // Keep the current state through awaited setup so failure callbacks can still establish quarantine.
        // Disconnected deliberately refuses ordinary quarantine transitions.
        if (CannotReconcileSubscription(allowQuarantinedWebhook)) return;
        if (IsQuarantined)
        {
            _subscriptionSetupFailed = false;
            await ReconcileWebhookProbe();
            return;
        }
        if (allowQuarantinedWebhook && !await ValidateStoredWebhookTarget()) return;
        await TransitionTo<Disconnected>();
        if (CannotReconcileSubscription(allowQuarantinedWebhook)) return;
        await RecoverAfterSubscribing(leavesQuarantine: false);

        // Do not clear a stranded entry merely because recovery returned without throwing.
        // Healthy catch-up and deferred routing need not have finished for setup to be established.
        _subscriptionSetupFailed = await GetCurrentState() is CatchingUpInFlight;
    }

    bool CannotReconcileSubscription(bool allowQuarantinedWebhook = false) =>
        _removed || IsRetired || State.AlertDisposition == AlertDisposition.Retired ||
        (!allowQuarantinedWebhook && (IsQuarantined || State.RunningState == ObserverRunningState.Quarantined)) ||
        _recoverSubscriptionAfterQuarantine || _retryRecoveryAfterQuarantine;

    EventType[] MergeSubscribedEventTypes(IEnumerable<EventType> eventTypes) => eventTypes.Concat(_subscription.EventTypes)
        .GroupBy(eventType => eventType.Id)
        .Select(group => group.OrderByDescending(eventType => eventType.Generation.Value).First())
        .ToArray();
}
