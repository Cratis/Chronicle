// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_a_page_starts_after_a_missing_matching_event : given.a_queue_verifying_live_gaps
{
    void Establish()
    {
        _history = [MatchingAt(102)];
        _observer.GetSubscription().Returns(ObserverSubscription.Unsubscribed with { SubscriberType = typeof(IObserverPartitionPageSubscriber) });
    }

    Task Because() => Deliver(MatchingAt(105, "P"), MatchingAt(106, "Q"));

    [Fact] void should_not_deliver_any_partition() => _observer.DidNotReceive().Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] void should_not_call_the_page_subscriber() => _observer.DidNotReceive().HandlePage(Arg.Any<IReadOnlyList<ObserverPartitionBatch>>());
    [Fact] void should_request_catch_up_once() => _observer.Received(1).CatchUp();
}
