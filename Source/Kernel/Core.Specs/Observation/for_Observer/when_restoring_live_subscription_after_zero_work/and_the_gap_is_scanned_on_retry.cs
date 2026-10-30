// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_live_subscription_after_zero_work;

public class and_the_gap_is_scanned_on_retry : given.an_observer_rejoining_during_backoff
{
    EventSequenceNumber _positionBeforeRetry;
    EventSequenceNumber _positionAfterScan;

    async Task Because()
    {
        await CompletePendingJobs();
        await DeliverLive(30L);
        _positionBeforeRetry = (await _observer.GetState()).NextEventSequenceNumber;
        RecordRetries();
        _clock.Advance(TimeSpan.FromSeconds(10));
        await _observer.RunWatchdogAsync();
        await CompletePendingJobs();

        // The retry's reader delivers the missed event and reports its contiguous scan frontier.
        await DeliverLive(10L);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(EventSequenceNumber.Unavailable);
        await _observer.CaughtUp(JobId.New(), 10L, 30L);
        await DeliverLive(31L);
        _positionAfterScan = (await _observer.GetState()).NextEventSequenceNumber;
    }

    [Fact] void should_not_advance_past_the_gap_on_newer_live_success() => _positionBeforeRetry.ShouldEqual(_nextSequenceNumber);
    [Fact] void should_retry_from_the_oldest_unresolved_position() => _retries.Single().FromEventSequenceNumber.ShouldEqual(_nextSequenceNumber);
    [Fact] async Task should_deliver_the_gap_event() => await _subscriber.Received(1).OnNext(
        Arg.Any<Key>(), Arg.Is<IEnumerable<AppendedEvent>>(events => events.Single().Context.SequenceNumber == (EventSequenceNumber)10L), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_release_the_gap_after_the_scan_covers_it() => _positionAfterScan.ShouldEqual((EventSequenceNumber)32L);
}
