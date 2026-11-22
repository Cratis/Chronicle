// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberExtensions.when_calling_an_unpartitioned_subscriber;

public class and_the_batch_has_been_delivered : given.an_unpartitioned_subscriber
{
    async Task Because() => await _subscriber.OnNextWithin(TimeSpan.FromMinutes(1), "some-partition", _events, new ObserverSubscriberContext(null));

    [Fact] void should_not_retain_a_gate_for_the_subscriber() => ObserverSubscriberExtensions.HasUnpartitionedGateFor(_subscriber).ShouldBeFalse();
}
