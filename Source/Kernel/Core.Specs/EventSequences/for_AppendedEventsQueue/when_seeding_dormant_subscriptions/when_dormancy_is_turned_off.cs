// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_seeding_dormant_subscriptions;

public class when_dormancy_is_turned_off : given.a_queue_with_a_stored_durable_subscription
{
    protected override int DormantAfter => 0;

    [Fact] async Task should_not_keep_anything() => (await _queue.GetSubscriptions()).ShouldBeEmpty();
}
