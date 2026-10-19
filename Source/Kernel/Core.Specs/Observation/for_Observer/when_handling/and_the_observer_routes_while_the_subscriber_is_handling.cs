// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// A catch-up report routes the observer while a batch is with the subscriber. Routing is handed the state with the
/// partition in flight and returns it after the batch completed; the batch clearing its marker and counting its
/// events must survive that (#4972).
/// </summary>
public class and_the_observer_routes_while_the_subscriber_is_handling : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partition = "partition";

    TaskCompletionSource<ObserverSubscriberResult> _subscriberResult;
    TaskCompletionSource<EventSequenceNumber> _tail;

    void Establish()
    {
        _subscriberResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _tail = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(_ => _subscriberResult.Task);
        _eventSequence.GetTailSequenceNumber().Returns(_ => _tail.Task);
    }

    async Task Because()
    {
        var handling = _observer.Handle(_partition, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);
        var routing = _observer.TransitionTo<Routing>();

        _subscriberResult.SetResult(ObserverSubscriberResult.Ok(42UL));
        await handling;

        _tail.SetResult(42UL);
        await routing;
    }

    [Fact] void should_not_leave_the_partition_in_flight() => _stateStorage.State.InFlightPartitions.ShouldBeEmpty();
    [Fact] void should_count_the_handled_event() => _stateStorage.State.HandledEventCount.ShouldEqual((EventCount)1UL);
    [Fact] void should_count_the_handled_event_for_its_type() => _stateStorage.State.HandledEventCountPerEventType[event_type.Id].ShouldEqual((EventCount)1UL);
}
