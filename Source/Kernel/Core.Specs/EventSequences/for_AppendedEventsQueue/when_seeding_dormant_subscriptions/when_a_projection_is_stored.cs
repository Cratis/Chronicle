// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_seeding_dormant_subscriptions;

public class when_a_projection_is_stored : given.a_queue_with_a_stored_durable_subscription
{
    protected override ObserverSubscriptionKind StoredKind => ObserverSubscriptionKind.Projection;

    [Fact] async Task should_keep_it() => (await _queue.GetSubscriptions()).Count.ShouldEqual(1);
}
