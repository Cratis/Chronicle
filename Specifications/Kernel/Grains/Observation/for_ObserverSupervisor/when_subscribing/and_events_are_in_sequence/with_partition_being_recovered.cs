// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.when_subscribing.and_events_are_in_sequence;

public class with_partition_being_recovered : given.an_observer_and_two_event_types_and_one_event_in_sequence
{
    void Establish() => state.StartRecoveringPartition(event_source_id);

    [Fact] void should_not_forward_event_to_observer_subscriber() => subscriber.Verify(_ => _.OnNext(appended_event), Never);
}
