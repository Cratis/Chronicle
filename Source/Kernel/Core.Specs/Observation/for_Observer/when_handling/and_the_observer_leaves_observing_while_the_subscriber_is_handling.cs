// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// The observer leaves the observing state - which awaits unsubscribing from the queue - while a batch is with the
/// subscriber. Leaving is handed the state with the partition in flight and returns it after the batch cleared its
/// marker; the marker must stay removed (#4972).
/// </summary>
public class and_the_observer_leaves_observing_while_the_subscriber_is_handling : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partition = "partition";

    TaskCompletionSource<ObserverSubscriberResult> _subscriberResult;
    TaskCompletionSource _unsubscribeStarted;
    TaskCompletionSource _unsubscribeRelease;

    async Task Establish()
    {
        _subscriberResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _unsubscribeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _unsubscribeRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(_ => _subscriberResult.Task);
        _eventSequence.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);
        _appendedEventsQueues.Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>()).Returns(_ =>
        {
            _unsubscribeStarted.TrySetResult();
            return _unsubscribeRelease.Task;
        });
        await _observer.TransitionTo<Observing>();
    }

    async Task Because()
    {
        var handling = _observer.Handle(_partition, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);
        var leaving = _observer.TransitionTo<Routing>();
        await _unsubscribeStarted.Task;

        _subscriberResult.SetResult(ObserverSubscriberResult.Ok(42UL));
        await handling;

        _unsubscribeRelease.SetResult();
        await leaving;
    }

    [Fact] void should_not_leave_the_partition_in_flight() => _stateStorage.State.InFlightPartitions.ShouldBeEmpty();
    [Fact] void should_count_the_handled_event() => _stateStorage.State.HandledEventCount.ShouldEqual((EventCount)1UL);
}
