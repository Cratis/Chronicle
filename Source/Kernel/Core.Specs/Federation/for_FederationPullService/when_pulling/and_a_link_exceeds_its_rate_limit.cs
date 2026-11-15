// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_a_link_exceeds_its_rate_limit : given.a_pull_service
{
    FederatedEventBatch _first;
    FederatedEventBatch _throttled;
    FederatedEventBatch _afterWaiting;

    void Establish()
    {
        Configure(new FederationLimits { MaxEventsPerSecond = 2 });
        _grants.Add(GrantForCaller());
        RegisterEventType("order-placed", EventTypeVisibility.Public);
        for (ulong sequenceNumber = 0; sequenceNumber < 5; sequenceNumber++)
        {
            Append("order-placed", sequenceNumber);
        }
    }

    async Task Because()
    {
        _first = await Pull();
        _throttled = await Pull(from: 5);
        _time.Advance(TimeSpan.FromSeconds(5));
        _afterWaiting = await Pull(from: 5);
    }

    [Fact] void should_serve_the_pull_that_started_within_budget() => _first.Events.Count().ShouldEqual(5);
    [Fact] void should_throttle_the_next_pull() => _throttled.Status.ShouldEqual(FederatedEventBatchStatus.Throttled);
    [Fact] void should_return_no_events_when_throttled() => _throttled.Events.ShouldBeEmpty();
    [Fact] void should_leave_the_position_unchanged_when_throttled() => _throttled.NextSequenceNumber.ShouldEqual(5UL);
    [Fact] void should_record_the_throttling() => _status.Received(1).RecordThrottled("link-a");
    [Fact] void should_serve_again_once_the_budget_has_refilled() => _afterWaiting.Status.ShouldEqual(FederatedEventBatchStatus.Ok);
}
