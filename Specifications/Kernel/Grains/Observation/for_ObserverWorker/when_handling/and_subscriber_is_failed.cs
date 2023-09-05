// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverWorker.when_handling;

public class and_subscriber_is_failed : given.an_observer_worker
{
    AppendedEvent @event;
    EventSequenceNumber sequence_number;

    void Establish()
    {
        state.RunningState = ObserverRunningState.Active;
        sequence_number = (ulong)Random.Shared.Next();
        state.NextEventSequenceNumber = sequence_number;
        @event = new AppendedEvent(EventMetadata.EmptyWithEventSequenceNumber(sequence_number), EventContext.Empty with { EventSourceId = Guid.NewGuid() }, new System.Dynamic.ExpandoObject());
        subscriber
            .Setup(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>()))
            .Returns((IEnumerable<AppendedEvent> e, ObserverSubscriberContext _) => Task.FromResult(ObserverSubscriberResult.Failed(e.First().Metadata.SequenceNumber)));
    }

    Task Because() => worker.Handle(@event);

    [Fact] void should_fail_the_partition() => state.IsPartitionFailed(@event.Context.EventSourceId).ShouldBeTrue();
    [Fact] void should_move_the_sequence_number() => state.NextEventSequenceNumber.ShouldEqual(sequence_number + 1);
}
