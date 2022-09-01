// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_resuming;

public class and_partition_is_failed_after_sequence_of_events_in_sequence_from_stream : given.an_observer_and_two_event_types_and_one_event_in_sequence
{
    void Establish()
    {
        state.FailPartition(event_source_id, 42, Array.Empty<string>(), string.Empty);
        state.NextEventSequenceNumber = 43;
    }

    async Task Because()
    {
        await observer.TryResumePartition(event_source_id);
        await observers[0].OnNextAsync(appended_event);
    }

    [Fact] void should_not_forward_event_to_observer_stream() => observer_stream.Verify(_ => _.OnNextAsync(appended_event, IsAny<StreamSequenceToken>()), Never());
    [Fact] void should_not_set_offset_to_next_event_sequence() => state_on_write.NextEventSequenceNumber.Value.ShouldEqual(43U);
}
