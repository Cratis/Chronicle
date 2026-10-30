// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_live_subscription_after_zero_work;

public class and_an_append_commits_while_reading_the_tail : given.an_observer_rejoining_during_backoff
{
    readonly TaskCompletionSource _readingTail = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<EventSequenceNumber> _tail = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _subscribedBeforeTail;
    EventSequenceNumber _positionDuringRejoin;
    EventSequenceNumber _positionAfterRejoin;

    void Establish()
    {
        _eventSequence.GetTailSequenceNumber().Returns(_ =>
        {
            _subscribedBeforeTail = _queued;
            _readingTail.TrySetResult();
            return _tail.Task;
        });
    }

    async Task Because()
    {
        var rejoin = CompletePendingJobs();
        await _readingTail.Task;
        await DeliverLive(30L);
        _positionDuringRejoin = (await _observer.GetState()).NextEventSequenceNumber;

        // The tail read started before the append committed and still returns its older, low value.
        _tail.SetResult(_nextSequenceNumber - 1);
        await rejoin;
        _positionAfterRejoin = (await _observer.GetState()).NextEventSequenceNumber;
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)30L);
        RecordRetries();
        _clock.Advance(TimeSpan.FromSeconds(10));
        await _observer.RunWatchdogAsync();
        await CompletePendingJobs();
    }

    [Fact] void should_subscribe_before_reading_the_tail() => _subscribedBeforeTail.ShouldBeTrue();
    [Fact] async Task should_deliver_the_concurrently_appended_event() => await _subscriber.Received(1).OnNext(
        Arg.Any<Key>(), Arg.Is<IEnumerable<AppendedEvent>>(events => events.Single().Context.SequenceNumber == (EventSequenceNumber)30L), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_preserve_the_position_while_the_gap_lookup_is_pending() => _positionDuringRejoin.ShouldEqual(_nextSequenceNumber);
    [Fact] void should_not_lose_the_recovery_position_to_a_stale_low_tail() => _positionAfterRejoin.ShouldEqual(_nextSequenceNumber);
    [Fact] void should_retry_from_the_preserved_position() => _retries.Single().FromEventSequenceNumber.ShouldEqual(_nextSequenceNumber);
    [Fact] void should_keep_the_queue_subscribed() => _queued.ShouldBeTrue();
}
