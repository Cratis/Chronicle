// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_verifying_live_subscription_after_a_silo_departure;

public class and_the_queue_lost_the_subscription_when_the_supervisor_asks : given.an_observing_observer
{
    void Establish() => _appendedEventsQueues.IsSubscribed(_observerKey).Returns(false);

    async Task Because() => await _observer.VerifyLiveSubscriptionAfterDeparture();

    [Fact] void should_resubscribe_the_observer_to_its_queue() => ShouldHaveResubscribed();
}
