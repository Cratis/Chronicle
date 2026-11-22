// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Supervision;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Re-verifies the live subscription of an observer after a silo leaves the cluster.
/// </summary>
/// <remarks>
/// <para>
/// The appended-events queues are grains of their own, and their subscriptions live only in memory. A silo that leaves
/// takes the queues it hosted with it, so an observer on a silo that remains can keep believing it is observing while
/// the queue that replaced its old one, activated afresh on another silo, holds no subscription for it. Nothing reaches
/// such an observer: it is up to date and kept alive, so neither the watchdog's stranded-subscription rescue, which
/// only looks at an observer that is behind, nor a reactivation brings it back before an event goes missing.
/// </para>
/// <para>
/// The <see cref="SiloSupervisor"/> of the silo hears the departure and, for a while after it, asks every resident
/// observer through <see cref="IObserver.VerifyLiveSubscriptionAfterDeparture"/> whether its queue still holds the
/// subscription. The observer re-routes through <see cref="Routing"/> when it does not, the same recovery the watchdog
/// uses. An intact subscription is left alone, and an observer that is not settled in <see cref="Observing"/> -
/// replaying, catching up, failed, quarantined or between states - is not touched: what drives it forward re-subscribes
/// it on its way back into <see cref="Observing"/>.
/// </para>
/// </remarks>
public partial class Observer
{
    async Task VerifyLiveSubscription()
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        if (!IsSettledLiveObserver() || !await IsObservingWithNoTransitionInProgress())
        {
            return;
        }

        if (!await QueueLostSubscription())
        {
            return;
        }

        logger.RestoringLiveSubscriptionAfterSiloDeparture(State.NextEventSequenceNumber);
        await TransitionTo<Routing>();
    }

    bool IsSettledLiveObserver() =>
        IsObserverLive() &&
        KeyOwnsDefinition &&
        _subscription.IsSubscribed &&
        State.RunningState == ObserverRunningState.Active;
}
