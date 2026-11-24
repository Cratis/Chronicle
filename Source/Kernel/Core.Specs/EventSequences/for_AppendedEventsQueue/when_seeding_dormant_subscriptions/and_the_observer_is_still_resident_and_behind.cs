// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_seeding_dormant_subscriptions;

public class and_the_observer_is_still_resident_and_behind : given.a_queue_verifying_live_gaps
{
    protected override bool StartsDormant => true;

    void Establish()
    {
        _history = [MatchingAt(102)];
        _observer.CatchUp().Returns(_ =>
        {
            _calls.Enqueue("catch_up");
            return Task.CompletedTask;
        });
    }

    Task Because() => Deliver(MatchingAt(105));

    [Fact] void should_resume_without_delivering_past_the_gap() => _calls.ToArray().ShouldEqual(new[] { "ensure", "catch_up" });
    [Fact] void should_not_advance_through_live_delivery() => _handledEvents.ShouldEqual(0);
}
