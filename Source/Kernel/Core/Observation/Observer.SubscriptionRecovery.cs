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
        if (_removed || State.RunningState == ObserverRunningState.Quarantined)
        {
            return false;
        }

        var currentState = await GetCurrentState();
        if (currentState is Observing or States.Replay)
        {
            return !_subscription.EventTypes.ToHashSet().SetEquals(eventTypes);
        }

        return currentState is Disconnected or Routing or CatchingUpInFlight &&
            (!_subscription.IsSubscribed || _subscriptionSetupFailed);
    }

    /// <inheritdoc/>
    public async Task RecoverStalledSubscription<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs = null,
        bool isReplayable = true,
        ObserverFilters? filters = null)
        where TObserverSubscriber : IObserverSubscriber
    {
        // This is a non-interleaved grain request, not a check followed by a queued Subscribe. In particular,
        // a Subscribe that was still running when reconciliation requested recovery finishes before this decision.
        if (!await NeedsSubscriptionRecovery(eventTypes))
        {
            return;
        }

        // Keep the failure indication until setup completes, including when refreshing a healthy observer's
        // event types. A failed refresh must remain repairable even after installing the new subscription.
        _subscriptionSetupFailed = true;

        // A failed entry write leaves the state machine in CatchingUpInFlight with its scheduled Routing
        // discarded. That state cannot enter itself. Disconnected is a legal, progress-preserving way out.
        await TransitionTo<Disconnected>();

        // Do not reload stale progress or call LeaveQuarantineForSubscription. Reconciliation has no authority
        // to release quarantine; ordinary application Subscribe retains its existing behavior.
        await SubscribeToEventTypes<TObserverSubscriber>(type, eventTypes, siloAddress, subscriberArgs, isReplayable, filters, recovering: true);
        _subscriptionSetupFailed = await GetCurrentState() is not (Observing or States.Replay);
    }
}
