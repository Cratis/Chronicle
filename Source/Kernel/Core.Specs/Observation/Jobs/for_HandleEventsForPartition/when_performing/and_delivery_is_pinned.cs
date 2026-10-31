// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_delivery_is_pinned : given.a_performing_job_step
{
    readonly EventType _pin = new("person-registered", 2);
    EventType[] _releasedPins = [];

    async Task Establish()
    {
        _observer.GetSubscription().Returns((await _observer.GetSubscription()) with { GenerationDelivery = EventGenerationDelivery.Pinned });
        _performState.EventTypes = [_pin];
        _eventCursor.Current.Returns([AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new(_pin.Id, 1), first_event_sequence_number)]);
        _eventGenerationRelease.Release(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns(call =>
        {
            _releasedPins = call.Arg<IEnumerable<EventType>>().ToArray();
            return call.Arg<IEnumerable<AppendedEvent>>().ToArray();
        });
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);
    [Fact] void should_pass_the_retry_pins_to_selection() => _releasedPins.ShouldContainOnly(_pin);
    [Fact] async Task should_not_use_compatibility_release() => await _eventCompliance.DidNotReceive().Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>());
}
