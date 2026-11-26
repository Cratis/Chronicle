// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant;

public class when_the_subscriber_is_an_event_store_subscription : given.a_subscribed_event_store_subscription_observer
{
    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod);
    }

    [Fact] void should_release_the_activation() => ReleaseRequests.ShouldEqual(1);
    [Fact] void should_have_recorded_the_subscription_as_an_event_store_subscription() => Recorded?.Kind.ShouldEqual(ObserverSubscriptionKind.EventStoreSubscription);
}
