// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_subscribing.and_events_are_in_sequence;

public class with_successful_observation : given.an_observer_and_two_event_types_and_one_event_in_sequence
{
    async Task Because()
    {
        await observer.Subscribe(event_types, observer_namespace);
        await observers[0].OnNextAsync(appended_event);
    }

    [Fact] void should_forward_event_to_observer_stream() => observer_stream.Verify(_ => _.OnNextAsync(appended_event, IsAny<StreamSequenceToken>()), Once());
    [Fact] void should_set_current_namespace_in_state() => state_on_write.CurrentNamespace.ShouldEqual(observer_namespace);
    [Fact] void should_set_offset_to_next_event_sequence() => state_on_write.NextEventSequenceNumber.Value.ShouldEqual(appended_event.Metadata.SequenceNumber.Value + 1);
    [Fact] void should_set_last_handled_to_event_sequence_number_from_event() => state_on_write.NextEventSequenceNumber.Value.ShouldEqual(appended_event.Metadata.SequenceNumber.Value);
    [Fact] void should_set_running_state_to_active() => state_on_write.RunningState.ShouldEqual(ObserverRunningState.Active);
}
