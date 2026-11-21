// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_verifying_live_subscription_after_a_silo_departure;

public class and_the_subscription_is_intact : given.an_observing_observer
{
    async Task Because() => await _observer.VerifyLiveSubscriptionForTesting();

    [Fact] async Task should_ask_the_queue_whether_it_holds_the_subscription() => await _appendedEventsQueues.Received(1).IsSubscribed(_observerKey);
    [Fact] void should_not_resubscribe_the_observer() => ShouldNotHaveResubscribed();
    [Fact] void should_not_start_catching_up() => ShouldNotHaveStartedCatchup();
    [Fact] async Task should_stay_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
}
