// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_seeding_dormant_subscriptions;

public class when_subscriptions_are_only_shadowed : given.a_queue_with_a_stored_durable_subscription
{
    protected override DurableSubscriptionMode Mode => DurableSubscriptionMode.Shadow;

    [Fact] async Task should_not_keep_anything() => (await _queue.GetSubscriptions()).ShouldBeEmpty();
}
