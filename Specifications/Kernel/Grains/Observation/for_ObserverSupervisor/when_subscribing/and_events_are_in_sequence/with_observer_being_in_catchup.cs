// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.when_subscribing.and_events_are_in_sequence;

public class with_observer_being_in_catchup : given.an_observer_and_two_event_types_and_one_event_in_sequence
{
    void Establish()
    {
        state.RunningState = ObserverRunningState.CatchingUp;
    }

    async Task Because() => await observer.Subscribe<ObserverSubscriber>(event_types);

    [Fact] void should_initiate_catchup() => catch_up.Verify(_ => _.Start(IsAny<Type>()), Once);
    [Fact] void should_not_subscribe_to_stream() => sequence_stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()), Never);
    [Fact] void should_not_forward_event_to_observer_subscriber() => subscriber.Verify(_ => _.OnNext(appended_event), Never);
    [Fact] void should_not_set_offset_to_next_event_sequence() => state_on_write.NextEventSequenceNumber.Value.ShouldEqual(appended_event.Metadata.SequenceNumber.Value);
}
