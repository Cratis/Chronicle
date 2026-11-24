// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_the_subscription_observes_every_type : given.a_queue_verifying_live_gaps
{
    async Task Establish()
    {
        var @event = MatchingAt(102);
        _history = [@event with { Context = @event.Context with { EventType = new("another-type", EventTypeGeneration.First) } }];
        await _queue.SubscribeToAllEventTypes(_observerKey, LiveFilters);
    }

    Task Because() => Deliver(MatchingAt(105));

    [Fact] void should_not_narrow_the_range_to_declared_types() => _events.Received(1).GetRange(
        (EventSequenceNumber)102UL,
        (EventSequenceNumber)104UL,
        null,
        null,
        Arg.Any<IEnumerable<Tag>>(),
        null,
        null,
        Arg.Any<CancellationToken>());
    [Fact] void should_recover_a_missing_event_of_any_type() => _observer.Received(1).CatchUp();
    [Fact] void should_not_deliver_past_the_gap() => _handledEvents.ShouldEqual(0);
}
