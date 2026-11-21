// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_seeding_dormant_subscriptions;

public class when_the_observer_already_subscribed : given.a_queue_with_a_stored_durable_subscription
{
    async Task Because()
    {
        await _queue.Subscribe(_observerKey, [_eventType]);
        await _queue.SeedDormantSubscriptions();
    }

    [Fact] async Task should_keep_one_subscription() => (await _queue.GetSubscriptions()).Count.ShouldEqual(1);
    [Fact] void should_not_hold_it_as_dormant() => _queue.DormantCount.ShouldEqual(0);
}
