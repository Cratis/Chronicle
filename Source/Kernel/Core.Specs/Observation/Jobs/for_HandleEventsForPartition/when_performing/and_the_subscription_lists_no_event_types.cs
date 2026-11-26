// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_the_subscription_lists_no_event_types : given.a_performing_job_step
{
    EventType _eventType;

    void Establish()
    {
        _eventType = new(new EventTypeId("some-event-type"), EventTypeGeneration.First);
        _observer.GetEventTypes().Returns(Task.FromResult(EventTypeSet.From([_eventType])));
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_resolve_the_event_types_from_the_observer() => _observer.Received(1).GetEventTypes();
    [Fact] void should_read_the_schemas_of_the_resolved_event_types() => _eventTypesStorage.Received(1).GetFor(Arg.Is<IEnumerable<EventType>>(types => types.SequenceEqual(new[] { _eventType })));
}
