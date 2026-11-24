// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_stale_and_new_events_are_coalesced : given.a_queue_verifying_live_gaps
{
    void Establish() => _history = [MatchingAt(102)];

    Task Because() => Deliver(MatchingAt(101), MatchingAt(105));

    [Fact] void should_not_deliver_past_the_missing_cursor_event() => _handledEvents.ShouldEqual(0);
    [Fact] void should_request_catch_up() => _observer.Received(1).CatchUp();
}
