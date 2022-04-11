// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_resuming;

public class and_events_are_in_sequence : given.a_connected_observer_and_two_event_types_and_one_event_in_sequence
{
    const string partition = "ca517fc7-6a93-4878-9ccd-8e5b94b264d5";
    void Establish()
    {
        state.FailPartition(partition, 42, Array.Empty<string>(), string.Empty);
    }

    async Task Because() => await observer.TryResumePartition(partition);

    [Fact] void should_set_connection_id_on_partitioned_observer() => partitioned_observer.Verify(_ => _.SetConnectionId(connection_id), Once());
    [Fact] void should_forward_event_to_partitioned_observer() => partitioned_observer.Verify(_ => _.OnNext(appended_event), Once());
    [Fact] void should_set_offset_to_next_event_sequence() => state.Offset.Value.ShouldEqual(appended_event.Metadata.SequenceNumber.Value + 1);
    [Fact] void should_set_last_handled_to_next_event_sequence() => state.Offset.Value.ShouldEqual(appended_event.Metadata.SequenceNumber.Value + 1);
    [Fact] void should_set_running_state_to_active() => state.RunningState.ShouldEqual(ObserverRunningState.Active);
}
