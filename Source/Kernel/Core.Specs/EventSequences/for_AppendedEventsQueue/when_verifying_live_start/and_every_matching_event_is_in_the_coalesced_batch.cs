// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_every_matching_event_is_in_the_coalesced_batch : given.a_queue_verifying_live_gaps
{
    void Establish() => _history = [MatchingAt(102), EventAt(103)];

    Task Because() => Deliver(MatchingAt(102), MatchingAt(105));

    [Fact] void should_deliver_both_matching_events() => _handledEvents.ShouldEqual(2);
    [Fact] void should_not_treat_a_present_event_as_a_gap() => _observer.DidNotReceive().CatchUp();
    [Fact] void should_not_warn_about_a_gap() => LiveStartGapWarnings.ShouldBeEmpty();
}
