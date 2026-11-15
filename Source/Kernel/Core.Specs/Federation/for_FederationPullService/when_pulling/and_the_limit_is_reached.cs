// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_the_limit_is_reached : given.a_pull_service
{
    FederatedEventBatch _batch;

    void Establish()
    {
        _grants.Add(GrantForCaller());
        RegisterEventType("order-placed", EventTypeVisibility.Public);
        for (ulong sequenceNumber = 0; sequenceNumber < 5; sequenceNumber++)
        {
            Append("order-placed", sequenceNumber);
        }
    }

    async Task Because() => _batch = await Pull(from: 1, limit: 2);

    [Fact] void should_deliver_up_to_the_limit() => _batch.Events.Select(_ => _.SequenceNumber).ShouldContainOnly(1UL, 2UL);
    [Fact] void should_continue_right_after_the_last_delivered_event() => _batch.NextSequenceNumber.ShouldEqual(3UL);
    [Fact] void should_report_the_tail_so_the_consumer_can_compute_lag() => _batch.TailSequenceNumber.ShouldEqual(4UL);
}
