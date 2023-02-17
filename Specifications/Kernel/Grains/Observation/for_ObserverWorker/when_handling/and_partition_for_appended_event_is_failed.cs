// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverWorker.when_handling;

public class and_partition_for_appended_event_is_failed : given.an_observer_worker
{
    AppendedEvent @event;
    EventSequenceNumber sequence_number;

    async Task Establish()
    {
        state.RunningState = ObserverRunningState.Active;
        sequence_number = (ulong)Random.Shared.Next();
        state.NextEventSequenceNumber = sequence_number;
        @event = new AppendedEvent(EventMetadata.EmptyWithEventSequenceNumber(sequence_number), EventContext.Empty with { EventSourceId = Guid.NewGuid() }, new System.Dynamic.ExpandoObject());
        await worker.PartitionFailed(@event.Context.EventSourceId, @event.Metadata.SequenceNumber, Enumerable.Empty<string>(), string.Empty);
    }

    Task Because() => worker.Handle(@event);

    [Fact] void should_not_call_the_subscriber() => subscriber.VerifyNoOtherCalls();
    [Fact] void should_move_the_sequence_number() => state.NextEventSequenceNumber.ShouldEqual(sequence_number + 1);
}
