// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_the_observer_subscribes_again_behind_after_dormancy : given.a_queue_verifying_live_gaps
{
    async Task Establish()
    {
        _history = [MatchingAt(102)];
        _observer.CatchUp().Returns(_ =>
        {
            _calls.Enqueue("catch_up");
            return Task.CompletedTask;
        });
        await ReleaseAndSubscribeAgain();
    }

    Task Because() => Deliver(MatchingAt(105));

    [Fact] void should_resume_through_catch_up_without_delivering_past_the_gap() => _calls.ToArray().ShouldEqual(new[] { "catch_up" });
    [Fact] void should_not_advance_through_live_delivery() => _handledEvents.ShouldEqual(0);
}
