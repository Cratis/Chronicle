// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverWorker.when_handling;

public class and_event_should_be_handled : given.an_observer_worker
{
    AppendedEvent @event;
    EventSequenceNumber sequence_number;

    void Establish()
    {
        state.RunningState = ObserverRunningState.Active;
        sequence_number = (ulong)Random.Shared.Next();
        state.NextEventSequenceNumber = sequence_number;
        @event = new AppendedEvent(EventMetadata.EmptyWithEventSequenceNumber(sequence_number), EventContext.Empty with { EventSourceId = Guid.NewGuid() }, new System.Dynamic.ExpandoObject());
    }

    Task Because() => worker.Handle(@event);

    [Fact] void should_call_the_subscriber() => subscriber.Verify(_ => _.OnNext(Is<IEnumerable<AppendedEvent>>(m => m.First() == @event), IsAny<ObserverSubscriberContext>()), Once);
    [Fact] void should_move_the_sequence_number() => state.NextEventSequenceNumber.ShouldEqual(sequence_number + 1);
    [Fact] void should_set_last_handled() => state.LastHandled.ShouldEqual(sequence_number);
}
