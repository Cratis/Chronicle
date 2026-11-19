// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_verifying_live_subscription_after_a_silo_departure;

public class and_the_observer_is_quarantined : given.an_observing_observer
{
    async Task Establish()
    {
        await _observer.TransitionTo<QuarantinedObserver>();
        _appendedEventsQueues.IsSubscribed(_observerKey).Returns(false);
        _appendedEventsQueues.ClearReceivedCalls();
    }

    async Task Because() => await _observer.VerifyLiveSubscriptionForTesting();

    [Fact] async Task should_not_ask_the_queue() => await _appendedEventsQueues.DidNotReceive().IsSubscribed(_observerKey);
    [Fact] void should_not_resubscribe_the_observer() => ShouldNotHaveResubscribed();
    [Fact] async Task should_stay_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeTrue();
}
