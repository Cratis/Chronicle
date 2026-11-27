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
    [Fact] void should_warn_about_the_gap_once() => LiveStartGapWarnings.Count().ShouldEqual(1);
    [Fact] void should_identify_the_observer_in_the_warning() => LiveStartGapWarnings.Single()["ObserverKey"].ShouldEqual(_observerKey);
    [Fact] void should_name_the_cursor() => LiveStartGapWarnings.Single()["NextEventSequenceNumber"].ShouldEqual((EventSequenceNumber)102UL);
    [Fact] void should_name_the_first_eligible_live_sequence() => LiveStartGapWarnings.Single()["FirstLiveEventSequenceNumber"].ShouldEqual((EventSequenceNumber)105UL);
    [Fact] void should_name_the_last_eligible_live_sequence() => LiveStartGapWarnings.Single()["LastLiveEventSequenceNumber"].ShouldEqual((EventSequenceNumber)106UL);
    [Fact] void should_name_the_missing_sequence() => LiveStartGapWarnings.Single()["MissingEventSequenceNumber"].ShouldEqual((EventSequenceNumber)102UL);
}
