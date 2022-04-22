// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_subscribing;

public class and_observer_has_been_put_into_rewind_state : given.an_observer_and_two_event_types
{
    async Task Establish()
    {
        state.NextEventSequenceNumber = 42;
        state.CurrentNamespace = ObserverNamespace.NotSet;
        await observer.Rewind();
    }

    async Task Because() => await observer.Subscribe(event_types, Guid.NewGuid().ToString());

    [Fact] void should_set_state_to_replaying() => state.RunningState.ShouldEqual(ObserverRunningState.Replaying);
    [Fact] void should_subscribe_to_sequences_stream() => sequence_stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()), Once());
    [Fact] void should_subscribe_with_offset_at_beginning() => subscribed_token.SequenceNumber.ShouldEqual((long)EventSequenceNumber.First.Value);
    [Fact] void should_subscribe_with_event_types() => subscribed_token.EventTypes.ShouldEqual(event_types);
}
