// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_marking_dormant;

public class when_the_observer_subscribes_again : given.a_queue_with_a_stored_durable_subscription
{
    async Task Because() => await _queue.Subscribe(_observerKey, [_eventType]);

    [Fact] void should_no_longer_be_dormant() => _queue.DormantCount.ShouldEqual(0);
    [Fact] async Task should_replace_the_entry() => (await _queue.GetSubscriptions()).Count.ShouldEqual(1);
}
