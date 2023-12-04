// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.when_handling;

public class and_subscription_is_disconnected_with_last_successful_observation_being_unavailable : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish()
    {
        subscriber.Setup(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>())).Returns(Task.FromResult(ObserverSubscriberResult.Disconnected(EventSequenceNumber.Unavailable)));

        state_storage.State = state_storage.State with
        {
            NextEventSequenceNumber = 33UL,
            LastHandledEventSequenceNumber = 34UL
        };
    }

    async Task Because() => await observer.Handle("Something", new[] { AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 43UL) });

    [Fact] void should_not_set_next_sequence_number() => state_storage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)33UL);
    [Fact] void should_not_set_last_handled_event_sequence_number() => state_storage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)34UL);
    [Fact] void should_not_write_state() => silo.StorageStats().Writes.ShouldEqual(0);
}
