// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_verifying_live_subscription_after_a_silo_departure;

public class and_the_departure_window_has_closed : given.an_observing_observer
{
    bool _scheduledWhileOpen;

    void Establish()
    {
        _appendedEventsQueues.IsSubscribed(_observerKey).Returns(false);
        _observer.OpenDepartureChecksForTesting();
        _scheduledWhileOpen = _observer.HasDepartureChecksScheduled;
        _clock.Advance(TimeSpan.FromSeconds(new Configuration.Clients().SubscriptionSettleSeconds + 1));
    }

    async Task Because() => await _observer.RunDepartureCheckForTesting();

    [Fact] void should_have_scheduled_checks_when_the_silo_left() => _scheduledWhileOpen.ShouldBeTrue();
    [Fact] void should_stop_checking() => _observer.HasDepartureChecksScheduled.ShouldBeFalse();
    [Fact] async Task should_not_ask_the_queue() => await _appendedEventsQueues.DidNotReceive().IsSubscribed(_observerKey);
    [Fact] void should_not_resubscribe_the_observer() => ShouldNotHaveResubscribed();
}
