// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_routing_controlled_delivery;

public class and_the_selected_target_has_its_own_generation_pin : given.a_controlled_job_step_with_observer_filters
{
    EventType _pin;
    EventType[] _releasedPins = [];
    EventSequenceNumber[] _releasedEvents = [];

    async Task Establish()
    {
        _pin = new(_e.Id, 2);
        var subscription = await _observer.GetSubscription();
        _observer.GetSubscription().Returns(subscription with
        {
            Targets =
            [
                new(SiloAddress.Zero) { DeclaredEventTypes = [_a] },
                new(SiloAddress.Zero) { DeclaredEventTypes = [_pin], GenerationDelivery = EventGenerationDelivery.Pinned }
            ]
        });
        _performState.EventObservationState = EventObservationState.None;
        _eventCursor.Current.Returns([CreateTypedEvent(_e, 2), CreateTypedEvent(_a, 10), CreateTypedEvent(_e, 11)]);
        _eventGenerationRelease.Release(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call =>
            {
                _releasedPins = call.Arg<IEnumerable<EventType>>().ToArray();
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _releasedEvents = events.Select(@event => @event.Context.SequenceNumber).ToArray();
                return events;
            });
    }

    async Task Because() => await PerformStep();

    [Fact] void should_release_using_only_the_selected_targets_pin() => _releasedPins.ShouldContainOnly(_pin);
    [Fact] void should_apply_the_fence_before_generation_release() => _releasedEvents.ShouldEqual([new EventSequenceNumber(11)]);
    [Fact] void should_preserve_order_across_different_delivery_modes() => _handledBatches.SelectMany(batch => batch).ShouldEqual([new EventSequenceNumber(10), new(11)]);
}
