// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_live_subscription_after_zero_work;

public class and_relevant_events_arrived_during_the_gap : given.an_observer_rejoining_during_backoff
{
    EventSequenceNumber _positionAfterLiveDelivery;
    EventSequenceNumber _positionAfterExcludedDelivery;
    bool _lookedForGapAfterSubscribing;
    bool _restoredAfterDirectCatchUp;
    int _jobsBeforeDeadline;
    int _warningsBeforeDeadline;

    void Establish()
    {
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(_nextSequenceNumber, Arg.Any<IEnumerable<EventType>>())
            .Returns(_ =>
            {
                _lookedForGapAfterSubscribing = _queued;
                return (EventSequenceNumber)10L;
            });
    }

    async Task Because()
    {
        await CompletePendingJobs();
        await DeliverLive(30L);
        _positionAfterLiveDelivery = (await _observer.GetState()).NextEventSequenceNumber;
        var otherType = new EventType("ae0b77b3-79a1-4d55-9d75-293633a21011", EventTypeGeneration.First);
        await _observer.Handle("excluded", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(otherType, 40L)]);
        _positionAfterExcludedDelivery = (await _observer.GetState()).NextEventSequenceNumber;
        RecordRetries();
        _queued = false;
        await _observer.CatchUp();
        _restoredAfterDirectCatchUp = _queued;
        _clock.Advance(TimeSpan.FromSeconds(10) - TimeSpan.FromTicks(1));
        await _observer.RunWatchdogAsync();
        await CompletePendingJobs();
        _jobsBeforeDeadline = _preparedJobs.Count;
        _warningsBeforeDeadline = NoStepsWarnings;
        _clock.Advance(TimeSpan.FromTicks(1));
        for (var pass = 0; pass < 5; pass++)
        {
            await _observer.RunWatchdogAsync();
            await CompletePendingJobs();
        }
    }

    [Fact] void should_check_for_gap_events_after_rejoining_despite_backoff() => _lookedForGapAfterSubscribing.ShouldBeTrue();
    [Fact] void should_not_skip_the_gap_when_a_newer_live_event_is_handled() => _positionAfterLiveDelivery.ShouldEqual(_nextSequenceNumber);
    [Fact] void should_not_skip_the_gap_when_a_newer_live_event_is_excluded() => _positionAfterExcludedDelivery.ShouldEqual(_nextSequenceNumber);
    [Fact] async Task should_deliver_the_newer_live_event() => await _subscriber.Received(1).OnNext(
        Arg.Any<Key>(), Arg.Is<IEnumerable<AppendedEvent>>(events => events.Single().Context.SequenceNumber == (EventSequenceNumber)30L), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_restore_the_queue_when_catch_up_is_requested_directly_during_backoff() => _restoredAfterDirectCatchUp.ShouldBeTrue();
    [Fact] void should_not_start_another_job_before_the_deadline() => _jobsBeforeDeadline.ShouldEqual(3);
    [Fact] void should_not_warn_before_the_deadline() => _warningsBeforeDeadline.ShouldEqual(3);
    [Fact] void should_retry_once_at_the_exact_deadline() => _retries.Count.ShouldEqual(1);
    [Fact] void should_scan_the_gap_on_the_deadline_retry() => _retries.Single().FromEventSequenceNumber.ShouldEqual(_nextSequenceNumber);
    [Fact] void should_keep_zero_key_jobs_bounded() => _preparedJobs.Count.ShouldEqual(4);
    [Fact] async Task should_preserve_the_gap_after_another_zero_key_lap() => (await _observer.GetState()).NextEventSequenceNumber.ShouldEqual(_nextSequenceNumber);
}
