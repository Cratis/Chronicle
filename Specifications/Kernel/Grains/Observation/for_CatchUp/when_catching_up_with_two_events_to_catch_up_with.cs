// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Specifications;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_CatchUp;

public class when_catching_up_with_two_events_to_catch_up_with : given.a_catch_up_worker
{
    AppendedEvent first_appended_event;
    AppendedEvent second_appended_event;
    EventSourceId first_event_source_id;
    EventSourceId second_event_source_id;
    IEnumerable<EventType> event_types = new EventType[]
   {
        new("ad9f43ca-8d98-4669-99cd-dbd0eaaea9d9", 1),
        new("3e84ef60-c725-4b45-832d-29e3b663d7cd", 1)
   };

    void Establish()
    {
        first_event_source_id = Guid.NewGuid();
        first_appended_event = new AppendedEvent(
            new(EventSequenceNumber.First, event_types.ToArray()[0]),
            new(first_event_source_id, EventSequenceNumber.First, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TenantId.Development, CorrelationId.New(), CausationId.System, CausedBy.System),
            new ExpandoObject());

        second_event_source_id = Guid.NewGuid();
        second_appended_event = new AppendedEvent(
            new(EventSequenceNumber.First + 1, event_types.ToArray()[0]),
            new(second_event_source_id, EventSequenceNumber.First + 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TenantId.Development, CorrelationId.New(), CausationId.System, CausedBy.System),
            new ExpandoObject());

        event_sequence_storage_provider
            .Setup(_ => _.GetFromSequenceNumber(IsAny<EventSequenceId>(), IsAny<EventSequenceNumber>(), IsAny<EventSourceId>(), IsAny<IEnumerable<EventType>>()))
            .Returns(Task.FromResult<IEventCursor>(new EventCursorForSpecifications(new[] { first_appended_event, second_appended_event })));

        timer_registry
            .Setup(_ => _.RegisterTimer(grain, IsAny<Func<object, Task>>(), IsAny<object>(), IsAny<TimeSpan>(), IsAny<TimeSpan>()))
            .Returns((Grain __, Func<object, Task> callback, object state, TimeSpan ___, TimeSpan ____) =>
            {
                callback(state);
                return Task.CompletedTask;
            });
    }

    Task Because() => catch_up.Start(typeof(ObserverSubscriber));

    [Fact] void should_call_on_next_for_first_event() => subscriber.Verify(_ => _.OnNext(first_appended_event), Once);
    [Fact] void should_call_on_next_for_second_event() => subscriber.Verify(_ => _.OnNext(second_appended_event), Once);
    [Fact] void should_notify_supervisor_that_catch_up_is_complete() => supervisor.Verify(_ => _.NotifyCatchUpComplete(), Once);
}
