// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_a_stale_only_batch_precedes_a_new_batch : given.a_queue_verifying_live_gaps
{
    void Establish() => _history = [MatchingAt(102)];

    async Task Because()
    {
        await Deliver(MatchingAt(101));
        await Deliver(MatchingAt(105));
    }

    [Fact] void should_bypass_only_the_stale_batch() => _handledEvents.ShouldEqual(1);
    [Fact] void should_check_the_cursor_again_for_the_new_batch() => _observer.Received(2).GetState();
    [Fact] void should_read_only_the_new_batchs_gap() => _events.Received(1).GetRange(
        (EventSequenceNumber)102UL,
        (EventSequenceNumber)104UL,
        null,
        Arg.Any<IEnumerable<EventType>>(),
        Arg.Any<IEnumerable<Tag>>(),
        null,
        null,
        Arg.Any<CancellationToken>());
    [Fact] void should_request_catch_up_for_the_new_batch() => _observer.Received(1).CatchUp();
}
