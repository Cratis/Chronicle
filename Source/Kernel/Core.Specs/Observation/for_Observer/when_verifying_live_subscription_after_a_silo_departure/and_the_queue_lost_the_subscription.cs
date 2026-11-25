// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_verifying_live_subscription_after_a_silo_departure;

/// <summary>
/// The queue that held the subscription went away with the silo that hosted it, and its replacement holds nothing for
/// this observer. The observer is up to date, so the watchdog's rescue of a behind observer would never see it.
/// </summary>
public class and_the_queue_lost_the_subscription : given.an_observing_observer
{
    void Establish() => _appendedEventsQueues.IsSubscribed(_observerKey).Returns(false);

    async Task Because() => await _observer.VerifyLiveSubscriptionAfterDeparture();

    [Fact] void should_resubscribe_the_observer_to_its_queue() => ShouldHaveResubscribed();
    [Fact] async Task should_be_observing_again() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_leave_the_observer_active() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
}
