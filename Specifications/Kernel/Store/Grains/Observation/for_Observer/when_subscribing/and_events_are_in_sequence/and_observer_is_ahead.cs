// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_subscribing.and_events_are_in_sequence;

public class and_observer_is_ahead : given.an_observer_and_two_event_types_and_one_event_in_sequence
{
    void Establish()
    {
        state.NextEventSequenceNumber = 42U;
        state.LastHandled = 41U;
    }

    async Task Because()
    {
        await observer.Subscribe(event_types, observer_namespace);
        await observers[0].OnNextAsync(appended_event);
    }

    [Fact] void should_not_forward_event_to_observer_stream() => observer_stream.Verify(_ => _.OnNextAsync(appended_event, IsAny<StreamSequenceToken>()), Never());
    [Fact] void should_set_current_namespace_in_state() => state_on_write.CurrentNamespace.ShouldEqual(observer_namespace);
    [Fact] void should_not_change_offset() => state_on_write.NextEventSequenceNumber.Value.ShouldEqual(42U);
    [Fact] void should_not_change_set_last_handled() => state_on_write.LastHandled.Value.ShouldEqual(41U);
}
