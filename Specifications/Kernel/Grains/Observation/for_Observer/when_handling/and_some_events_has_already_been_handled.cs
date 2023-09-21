// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.when_handling;

public class and_some_events_has_already_been_handled : given.an_observer_with_subscription
{
    readonly IEnumerable<AppendedEvent> events = new[]
    {
        AppendedEvent.EmptyWithEventSequenceNumber(42UL),
        AppendedEvent.EmptyWithEventSequenceNumber(43UL),
    };

    void Establish()
    {
        state.NextEventSequenceNumber = 43UL;
        state.LastHandled = 42UL;
        subscriber.Setup(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>())).Returns(Task.FromResult(ObserverSubscriberResult.Ok(43UL)));
    }

    async Task Because() => await observer.Handle("Something", events);

    [Fact] void should_forward_only_one_to_subscriber() => subscriber.Verify(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>()), Once());
    [Fact] void should_forward_last_event_to_subscriber() => subscriber.Verify(_ => _.OnNext(new[] { events.Last() }, IsAny<ObserverSubscriberContext>()), Once());
    [Fact] void should_not_set_next_sequence_number() => state.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)44UL);
    [Fact] void should_not_set_last_handled_event_sequence_number() => state.LastHandled.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_write_state_once() => written_states.Count.ShouldEqual(1);
}
