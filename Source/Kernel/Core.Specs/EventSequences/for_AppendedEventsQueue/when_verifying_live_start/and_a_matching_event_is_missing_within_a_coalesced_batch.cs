// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_a_matching_event_is_missing_within_a_coalesced_batch : given.a_queue_verifying_live_gaps
{
    void Establish() => _history = [MatchingAt(102), MatchingAt(103)];

    Task Because() => Deliver(MatchingAt(102), MatchingAt(105));

    [Fact] void should_not_deliver_either_live_event_past_the_hole() => _handledEvents.ShouldEqual(0);
    [Fact] void should_request_catch_up() => _observer.Received(1).CatchUp();
}
