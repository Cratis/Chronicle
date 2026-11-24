// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_seeding_dormant_subscriptions;

public class when_a_client_reactor_is_stored : given.a_queue_with_a_stored_durable_subscription
{
    [Fact] async Task should_keep_the_subscription() => (await _queue.GetSubscriptions()).Single().ObserverKey.ShouldEqual(_observerKey);
    [Fact] void should_hold_it_as_dormant() => _queue.DormantCount.ShouldEqual(1);
    [Fact] async Task should_take_the_event_types_from_the_definition() => (await _queue.GetSubscriptions()).Single().EventTypes.Select(_ => _.Id).ShouldContainOnly(_eventType.Id);
}
