// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_the_subscription_has_no_fixed_event_types : given.a_performing_job_step
{
    static readonly EventType _firstEventType = new("first-event-type", 1);
    static readonly EventType _secondEventType = new("second-event-type", 1);

    IEnumerable<EventType>? _eventTypesPassedToEventRange;

    void Establish()
    {
        // The subscription for an observer subscribed to all events carries no fixed event type list (see
        // Observer.SubscribeToAllEvents) - the whole point being that it also covers event types that did not
        // exist when it subscribed. Only IObserver.GetEventTypes() can resolve the current, full set.
        _observer.GetEventTypes().Returns(Task.FromResult<IEnumerable<EventType>>([_firstEventType, _secondEventType]));

        _eventSequenceStorage.GetRange(
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSourceId?>(),
            Arg.Do<IEnumerable<EventType>>(value => _eventTypesPassedToEventRange = value),
            Arg.Any<IEnumerable<Tag>?>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(_eventCursor));
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_resolve_event_types_from_the_observer() => _observer.Received(1).GetEventTypes();
    [Fact] void should_use_the_resolved_event_types_for_reading_the_event_range() => _eventTypesPassedToEventRange.ShouldContainOnly(_firstEventType, _secondEventType);
}
