// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_CatchUp;

public class when_catching_up_with_two_events_to_catch_up_with : given.a_catch_up_worker_with_two_pending_events
{
    Task Because() => catch_up.Start(new(GrainId, ObserverKey.Parse(GrainKeyExtension), event_types, typeof(ObserverSubscriber), subscriber_args));

    [Fact] void should_call_on_next_for_first_event() => subscriber.Verify(_ => _.OnNext(Is<IEnumerable<AppendedEvent>>(m => m.First() == first_appended_event), new(subscriber_args)), Once);
    [Fact] void should_call_on_next_for_second_event() => subscriber.Verify(_ => _.OnNext(Is<IEnumerable<AppendedEvent>>(m => m.First() == second_appended_event), new(subscriber_args)), Once);
    [Fact] void should_notify_supervisor_that_catch_up_is_complete() => supervisor.Verify(_ => _.NotifyCatchUpComplete(IsAny<IEnumerable<FailedPartition>>()), Once);
}
