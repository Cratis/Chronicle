// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_AbandonedDeliveryDrain;

public class when_the_subscriber_is_idle : given.a_captured_delivery
{
    async Task Because() => await Drain();

    [Fact] void should_drain_the_original_activation() => _subscriber.Received(1).Drain();
    [Fact] void should_not_wait_for_the_hold_deadline() => (_clock.GetUtcNow() < _fence.ReleaseBy).ShouldBeTrue();
}
