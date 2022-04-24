// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_subscribing.and_events_are_in_sequence;

public class with_partition_being_recovered : given.an_observer_and_two_event_types_and_one_event_in_sequence
{
    void Establish() => state.StartRecoveringPartition(event_source_id);

    [Fact] void should_not_forward_event_to_observer_stream() => observer_stream.Verify(_ => _.OnNextAsync(appended_event, IsAny<StreamSequenceToken>()), Never());
}
