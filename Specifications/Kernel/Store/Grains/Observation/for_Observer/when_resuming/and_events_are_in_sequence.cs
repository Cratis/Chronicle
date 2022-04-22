// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_resuming;

public class and_events_are_in_sequence : given.an_observer_and_two_event_types_and_one_event_in_sequence
{
    void Establish()
    {
        state.FailPartition(event_source_id, 42, Array.Empty<string>(), string.Empty);
    }

    async Task Because()
    {
        await observer.TryResumePartition(event_source_id);
        await observers[0].OnNextAsync(appended_event);
    }

    [Fact] void should_forward_event_to_observer_stream() => observer_stream.Verify(_ => _.OnNextAsync(appended_event, IsAny<StreamSequenceToken>()), Once());
    [Fact] void should_set_offset_to_next_event_sequence() => state_on_write.NextEventSequenceNumber.Value.ShouldEqual(appended_event.Metadata.SequenceNumber.Value + 1);
    [Fact] void should_set_last_handled_to_event_sequence_number_from_the_event() => state_on_write.LastHandled.Value.ShouldEqual(appended_event.Metadata.SequenceNumber.Value);
    [Fact] void should_unsubscribe_stream_when_finished() => subscription_handles[0].Verify(_ => _.UnsubscribeAsync(), Once());
}
