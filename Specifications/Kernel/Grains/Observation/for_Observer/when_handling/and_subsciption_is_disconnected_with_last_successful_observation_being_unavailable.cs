// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.when_handling;

public class and_subsciption_is_disconnected_with_last_successful_observation_being_unavailable : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish()
    {
        subscriber.Setup(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>())).Returns(Task.FromResult(ObserverSubscriberResult.Disconnected(EventSequenceNumber.Unavailable)));
        state.NextEventSequenceNumber = 33UL;
        state.LastHandledEventSequenceNumber = 34UL;
    }

    async Task Because() => await observer.Handle("Something", new[] { AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 43UL) });

    [Fact] void should_not_set_next_sequence_number() => state.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)33UL);
    [Fact] void should_not_set_last_handled_event_sequence_number() => state.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)34UL);
    [Fact] void should_not_write_state() => written_states.Count.ShouldEqual(0);
}
