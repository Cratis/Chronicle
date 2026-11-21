// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant;

public class when_a_delivery_is_fenced : given.a_subscribed_client_observer
{
    async Task Establish()
    {
        var target = new DeliveryFenceTarget(typeof(IClientOwnedObserverSubscriber).AssemblyQualifiedName!, "key", "a-connection", SiloAddress.Zero.ToParsableString());
        await _observer.PartitionFailedWithDeliveryFence("partition", 42UL, ["timeout"], string.Empty, FailureKind.Timeout, new(DeliveryFence: new(_clock.GetUtcNow().AddDays(1), [target])));
    }

    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod * 2);
    }

    [Fact] void should_keep_the_activation() => ReleaseRequests.ShouldEqual(0);
}
