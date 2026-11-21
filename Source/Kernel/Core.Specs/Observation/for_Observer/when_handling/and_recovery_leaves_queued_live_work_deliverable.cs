// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// Recovering a failed partition leaves subsequent queued live work deliverable without pinning shared progress.
/// </summary>
public class and_recovery_leaves_queued_live_work_deliverable : given.an_observer_with_skip_detection
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 102UL };
        await _observer.PartitionFailed("P", 102UL, ["Failed handler"], string.Empty);
        _subscriber.OnNext((Key)"Q", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(105UL));
        _subscriber.OnNext((Key)"R", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(106UL));
        EventSequenceDoesNotHaveNextEvent(102UL);
    }

    async Task Because()
    {
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 105UL)]);
        await _observer.FailedPartitionRecovered("P", 102UL);
        await _observer.Handle("R", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 106UL)]);
    }

    [Fact]
    void should_deliver_the_queued_event_to_the_subscriber() => _subscriber.Received(1)
        .OnNext((Key)"R", Arg.Is<IEnumerable<AppendedEvent>>(events => events.Single().Context.SequenceNumber == (EventSequenceNumber)106UL), Arg.Any<ObserverSubscriberContext>());
    [Fact]
    void should_deliver_the_other_live_event_to_the_subscriber() => _subscriber.Received(1)
        .OnNext((Key)"Q", Arg.Is<IEnumerable<AppendedEvent>>(events => events.Single().Context.SequenceNumber == (EventSequenceNumber)105UL), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_advance_the_successful_live_frontier() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)106UL);
    [Fact] void should_not_pin_the_shared_cursor_to_the_recovered_failure() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)107UL);
}
