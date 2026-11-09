// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_the_consumer_filters_event_types : given.a_pull_service
{
    FederatedEventBatch _batch;

    void Establish()
    {
        _grants.Add(GrantForCaller("link-a", false, "order-placed", "order-shipped"));
        RegisterEventType("order-placed", EventTypeVisibility.Public);
        RegisterEventType("order-shipped", EventTypeVisibility.Public);
        RegisterEventType("order-cancelled", EventTypeVisibility.Public);
        Append("order-placed", 0);
        Append("order-shipped", 1);
        Append("order-cancelled", 2);
    }

    async Task Because() => _batch = await Pull(eventTypes: ["order-shipped", "order-cancelled"]);

    [Fact] void should_deliver_only_what_is_both_requested_and_granted() => _batch.Events.Select(_ => _.SequenceNumber).ShouldContainOnly(1UL);
    [Fact] void should_advance_past_the_unwanted_events() => _batch.NextSequenceNumber.ShouldEqual(3UL);
}
