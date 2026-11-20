// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_verifying_live_subscription_after_a_silo_departure;

public class and_the_departure_window_is_open : given.an_observing_observer
{
    void Establish()
    {
        _appendedEventsQueues.IsSubscribed(_observerKey).Returns(false);
        _observer.OpenDepartureChecksForTesting();
        _clock.Advance(TimeSpan.FromSeconds(1));
    }

    async Task Because() => await _observer.RunDepartureCheckForTesting();

    [Fact] void should_keep_checking() => _observer.HasDepartureChecksScheduled.ShouldBeTrue();
    [Fact] void should_resubscribe_the_observer_to_its_queue() => ShouldHaveResubscribed();
}
