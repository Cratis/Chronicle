// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_the_observer_leaves_observing_while_uninteresting_events_advance : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partition = "partition";

    TaskCompletionSource _unsubscribeStarted;
    TaskCompletionSource _unsubscribeRelease;

    async Task Establish()
    {
        _unsubscribeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _unsubscribeRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
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
        var leaving = _observer.TransitionTo<Routing>();
        await _unsubscribeStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _observer.Handle(_partition, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(EventType.Unknown, 42UL)]);

        _unsubscribeRelease.SetResult();
        await leaving;
    }

    [Fact] void should_not_leave_the_partition_in_flight() => _stateStorage.State.InFlightPartitions.ShouldBeEmpty();
    [Fact] void should_not_count_uninteresting_events() => _stateStorage.State.HandledEventCount.ShouldEqual(EventCount.Zero);
    [Fact] void should_keep_the_next_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_not_mark_uninteresting_events_as_handled() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_keep_the_tail_position() => _stateStorage.State.TailEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
