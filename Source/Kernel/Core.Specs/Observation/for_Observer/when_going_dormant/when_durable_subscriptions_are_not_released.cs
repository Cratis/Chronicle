// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant;

public class when_durable_subscriptions_are_not_released : given.a_subscribed_client_observer
{
    protected override DurableSubscriptionMode Mode => DurableSubscriptionMode.Shadow;

    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod * 2);
    }

    [Fact] void should_never_release_the_activation() => ReleaseRequests.ShouldEqual(0);
}
