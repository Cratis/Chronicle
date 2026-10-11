// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_delivery_is_pinned_to_a_later_generation : given.a_performing_job_step
{
    readonly EventType _pin = new("person-registered", 2);
    EventType[] _releasedPins = [];

    async Task Establish()
    {
        var subscription = await _observer.GetSubscription();
        _observer.GetSubscription().Returns(subscription with { GenerationDelivery = EventGenerationDelivery.Pinned, EventTypes = [new(_pin.Id, 3)] });
        _performState.EventTypes = [_pin];
        _eventCursor.Current.Returns([CreateEvent(1UL, "module") with { Context = CreateEvent(1UL, "module").Context with { EventType = new(_pin.Id, 1) } }]);
        _eventGenerationRelease.Release(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns(call =>
        {
            _releasedPins = call.Arg<IEnumerable<EventType>>().ToArray();
            return call.Arg<IEnumerable<AppendedEvent>>().ToArray();
        });
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);
    [Fact] void should_use_the_jobs_pins_not_the_current_subscriptions_pins() => _releasedPins.ShouldContainOnly(_pin);
    [Fact] async Task should_not_use_compatibility_release() => await _eventCompliance.DidNotReceive().Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>());
}
