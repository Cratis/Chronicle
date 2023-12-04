// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.when_handling;

public class and_some_events_has_already_been_handled : given.an_observer_with_subscription_for_specific_event_type
{
    readonly IEnumerable<AppendedEvent> events = new[]
    {
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL),
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 43UL),
    };

    void Establish()
    {
        state_storage.State = state_storage.State with
        {
            NextEventSequenceNumber = 43UL,
            LastHandledEventSequenceNumber = 42UL
        };

        subscriber.Setup(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>())).Returns(Task.FromResult(ObserverSubscriberResult.Ok(43UL)));
    }

    async Task Because() => await observer.Handle("Something", events);

    [Fact] void should_forward_only_one_to_subscriber() => subscriber.Verify(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>()), Once());
    [Fact] void should_forward_last_event_to_subscriber() => subscriber.Verify(_ => _.OnNext(new[] { events.Last() }, IsAny<ObserverSubscriberContext>()), Once());
    [Fact] void should_not_set_next_sequence_number() => state_storage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)44UL);
    [Fact] void should_not_set_last_handled_event_sequence_number() => state_storage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_write_state_once() => silo.StorageStats().Writes.ShouldEqual(1);
}
