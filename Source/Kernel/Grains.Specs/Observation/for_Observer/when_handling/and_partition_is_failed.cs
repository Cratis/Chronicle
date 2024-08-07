// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Grains.Observation.for_Observer.when_handling;

public class and_partition_is_failed : given.an_observer_with_subscription_for_specific_event_type
{
    const string event_source_id = "Something";

    void Establish()
    {
        failed_partitions_state.Add(new FailedPartition
        {
            Partition = event_source_id,
            Attempts =
            [
                new FailedPartitionAttempt
                {
                    SequenceNumber = 42UL,
                    Messages = ["Something went wrong"],
                    StackTrace = "This is the stack trace"
                }
            ]
        });
        state_storage.State = state_storage.State with
        {
            NextEventSequenceNumber = 53UL,
            LastHandledEventSequenceNumber = 54UL
        };
    }

    async Task Because() => await observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 43UL)]);

    [Fact] void should_not_forward_to_subscriber() => subscriber.Verify(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>()), Never);
    [Fact] void should_not_set_next_sequence_number() => state_storage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)53UL);
    [Fact] void should_not_set_last_handled_event_sequence_number() => state_storage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)54UL);
    [Fact] void should_not_write_state() => storage_stats.Writes.ShouldEqual(0);
}
