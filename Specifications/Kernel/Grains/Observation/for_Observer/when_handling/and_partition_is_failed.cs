// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.when_handling;

public class and_partition_is_failed : given.an_observer_with_subscription
{
    const string event_source_id = "Something";

    void Establish()
    {
        failed_partitions_state.Add(new FailedPartition
        {
            Partition = event_source_id,
            Attempts = new[]
            {
                new FailedPartitionAttempt
                {
                    SequenceNumber = 42UL,
                    Messages = new[] { "Something went wrong" },
                    StackTrace = "This is the stack trace"
                }
            }
        });
        state.NextEventSequenceNumber = 53UL;
        state.LastHandled = 54UL;
    }

    async Task Because() => await observer.Handle(event_source_id, new[] { AppendedEvent.EmptyWithEventSequenceNumber(43UL) });

    [Fact] void should_not_forward_to_subscriber() => subscriber.Verify(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>()), Never);
    [Fact] void should_not_set_next_sequence_number() => state.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)53UL);
    [Fact] void should_not_set_last_handled_event_sequence_number() => state.LastHandled.ShouldEqual((EventSequenceNumber)54UL);
    [Fact] void should_not_write_state() => written_states.Count.ShouldEqual(0);
}
