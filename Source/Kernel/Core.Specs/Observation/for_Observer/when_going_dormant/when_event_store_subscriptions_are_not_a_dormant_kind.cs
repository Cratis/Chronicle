// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant;

public class when_event_store_subscriptions_are_not_a_dormant_kind : given.a_subscribed_event_store_subscription_observer
{
    protected override Observers CreateObserversConfig() => new()
    {
        WriteCoalescingInterval = 0,
        IdleDeactivationAfter = 0,
        DormantAfter = DormantAfter,
        DurableSubscriptions = Mode,
        DormantKinds = [ObserverSubscriptionKind.ClientReactor, ObserverSubscriptionKind.ClientReducer, ObserverSubscriptionKind.Projection]
    };

    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod * 2);
    }

    [Fact] void should_keep_the_activation() => ReleaseRequests.ShouldEqual(0);
}
