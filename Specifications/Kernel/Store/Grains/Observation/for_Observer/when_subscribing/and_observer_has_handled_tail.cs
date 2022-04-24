// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_subscribing;

public class and_observer_has_handled_tail : given.an_observer_and_two_event_types
{
    void Establish()
    {
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_types, null)).Returns(Task.FromResult((EventSequenceNumber)42u));
        state.LastHandled = 42;
        state.NextEventSequenceNumber = 43;
    }

    async Task Because() => await observer.Subscribe(event_types, observer_namespace);

    [Fact] void should_have_running_state_of_active() => state_on_write.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_subscribe_to_sequences_stream() => sequence_stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()), Once());
    [Fact] void should_subscribe_with_offset_at_beginning() => subscribed_token.SequenceNumber.ShouldEqual(43L);
}
