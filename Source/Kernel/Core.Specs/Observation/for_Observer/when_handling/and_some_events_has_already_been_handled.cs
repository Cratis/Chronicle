// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_some_events_has_already_been_handled : given.an_observer_with_subscription_for_specific_event_type
{
    readonly IEnumerable<AppendedEvent> _events =
    [
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL),
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 43UL),
    ];

    static IEnumerable<AppendedEvent> _eventToBeHandled;

    List<AppendedEvent> _handledEvents;

    void Establish()
    {
        _eventToBeHandled = [_events.Last()];
        _stateStorage.State = _stateStorage.State with
        {
            NextEventSequenceNumber = 43UL,
            LastHandledEventSequenceNumber = 42UL
        };

        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(43UL));

        _handledEvents = [];
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(callInfo =>
            {
                var events = callInfo.Arg<IEnumerable<AppendedEvent>>();
                _handledEvents.AddRange(events);
                return ObserverSubscriberResult.Ok(events.Last().Context.SequenceNumber);
            });
    }

    async Task Because() => await _observer.Handle("Something", _events);

    [Fact] void should_forward_only_one_to_subscriber() => _subscriber.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_forward_last_event_to_subscriber() => _subscriber.Received(1).OnNext(Arg.Any<Key>(), Arg.Is<IEnumerable<AppendedEvent>>(_ => _.SequenceEqual(_eventToBeHandled)), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_handle_last_event() => _handledEvents.SequenceEqual(_eventToBeHandled).ShouldBeTrue();
    [Fact] void should_set_correct_next_sequence_number() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)44UL);
    [Fact] void should_set_correct_last_handled_event_sequence_number() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
}
