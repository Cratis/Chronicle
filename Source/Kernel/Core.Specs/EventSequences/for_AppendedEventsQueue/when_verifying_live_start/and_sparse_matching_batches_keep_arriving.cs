// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_sparse_matching_batches_keep_arriving : given.a_queue_verifying_live_gaps
{
    void Establish() => _history = [EventAt(102)];

    async Task Because()
    {
        await Deliver(MatchingAt(105));
        await Deliver(MatchingAt(110));
        await Deliver(MatchingAt(120));
    }

    [Fact] void should_deliver_every_matching_batch() => _handledEvents.ShouldEqual(3);
    [Fact] void should_read_the_cursor_only_at_the_subscription_boundary() => _observer.Received(1).GetState();
    [Fact] void should_scan_history_only_once() => _events.Received(1).GetFromSequenceNumber((EventSequenceNumber)102UL, cancellationToken: Arg.Any<CancellationToken>());
    [Fact] void should_never_start_catch_up_for_excluded_events() => _observer.DidNotReceive().CatchUp();
}
