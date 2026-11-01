// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_routing_controlled_delivery;

public class and_the_selected_target_is_pinned : given.a_controlled_observer
{
    EventType _pin;
    EventType[] _releasedPins = [];
    EventSequenceNumber[] _releasedEvents = [];

    void Establish()
    {
        _pin = new(event_type.Id, 2);
        subscription = subscription with
        {
            Targets = [new(SiloAddress.Zero) { DeclaredEventTypes = [_pin], GenerationDelivery = EventGenerationDelivery.Pinned }]
        };
        _observer.SetSubscription(subscription);
        _eventGenerationRelease.Release(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call =>
            {
                _releasedPins = call.Arg<IEnumerable<EventType>>().ToArray();
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _releasedEvents = events.Select(@event => @event.Context.SequenceNumber).ToArray();
                return events;
            });
    }

    Task Because() => _observer.Handle(partition, [EventAt(event_type, 10), EventAt(added_type, 11)]);

    [Fact] void should_release_against_the_selected_targets_pin() => _releasedPins.ShouldContainOnly(_pin);
    [Fact] void should_not_release_the_uncovered_event() => _releasedEvents.ShouldEqual([new EventSequenceNumber(10)]);
    [Fact] void should_hold_before_the_uncovered_event() => _failedPartitionsState.Partitions.Single().RecoveryCursor!.Position.ShouldEqual((EventSequenceNumber)11UL);
}
