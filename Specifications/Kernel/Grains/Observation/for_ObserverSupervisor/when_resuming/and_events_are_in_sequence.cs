// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.when_resuming;

public class and_events_are_in_sequence : given.an_observer_and_two_event_types_and_one_event_in_sequence
{
    async Task Establish()
    {
        state.FailPartition(event_source_id, EventSequenceNumber.First, Array.Empty<string>(), string.Empty);
        await observer.Subscribe<ObserverSubscriber>(event_types);
    }

    async Task Because()
    {
        await observer.TryResumePartition(event_source_id);
        await observers[0].OnNextAsync(appended_event);
    }

    [Fact] void should_forward_event_to_observer_subscriber() => subscriber.Verify(_ => _.OnNext(appended_event, IsAny<ObserverSubscriberContext>()), Once);
    [Fact] void should_set_last_handled_to_event_sequence_number_from_the_event() => state_on_write.LastHandled.Value.ShouldEqual(appended_event.Metadata.SequenceNumber.Value);
    [Fact] void should_unsubscribe_stream_when_finished() => subscription_handles[0].Verify(_ => _.UnsubscribeAsync(), Once);
}
