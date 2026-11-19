// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_verifying_live_subscription_after_a_silo_departure;

public class and_the_observer_is_replaying : given.an_observing_observer
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { IsReplaying = true };
        _appendedEventsQueues.IsSubscribed(_observerKey).Returns(false);
    }

    async Task Because() => await _observer.VerifyLiveSubscriptionForTesting();

    [Fact] async Task should_not_ask_the_queue() => await _appendedEventsQueues.DidNotReceive().IsSubscribed(_observerKey);
    [Fact] void should_not_resubscribe_the_observer() => ShouldNotHaveResubscribed();
    [Fact] void should_not_start_catching_up() => ShouldNotHaveStartedCatchup();
}
