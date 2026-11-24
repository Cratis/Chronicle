// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_only_filter_excluded_events_precede_the_first_batch_after_dormancy : given.a_queue_verifying_live_gaps
{
    async Task Establish()
    {
        _history = [EventAt(102)];
        await ReleaseAndSubscribeAgain();
    }

    Task Because() => Deliver(MatchingAt(105));

    [Fact] void should_deliver_the_matching_event() => _handledEvents.ShouldEqual(1);
    [Fact] void should_not_start_catch_up() => _observer.DidNotReceive().CatchUp();
}
