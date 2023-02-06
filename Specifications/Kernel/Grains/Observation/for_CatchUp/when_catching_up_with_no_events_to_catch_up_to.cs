// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Specifications;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_CatchUp;

public class when_catching_up_with_no_events_to_catch_up_to : given.a_catch_up_job
{
    void Establish()
    {
        event_sequence_storage_provider
            .Setup(_ => _.GetFromSequenceNumber(IsAny<EventSequenceId>(), IsAny<EventSequenceNumber>(), IsAny<EventSourceId>(), IsAny<IEnumerable<EventType>>()))
            .Returns(Task.FromResult<IEventCursor>(new EventCursorForSpecifications(Enumerable.Empty<AppendedEvent>())));

        timer_registry
            .Setup(_ => _.RegisterTimer(grain, IsAny<Func<object, Task>>(), IsAny<object>(), IsAny<TimeSpan>(), IsAny<TimeSpan>()))
            .Returns((Grain __, Func<object, Task> callback, object state, TimeSpan ___, TimeSpan ____) =>
            {
                callback(state);
                return Task.CompletedTask;
            });
    }

    Task Because() => catch_up.Start(typeof(ObserverSubscriber));

    [Fact] void should_notify_supervisor_that_catch_up_is_complete() => supervisor.Verify(_ => _.NotifyCatchUpComplete(), Once);
}
