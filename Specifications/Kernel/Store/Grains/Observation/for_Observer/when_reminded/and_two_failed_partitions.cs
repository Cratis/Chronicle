// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_reminded;

public class and_two_failed_partitions : given.a_connected_observer_with_event_types_and_reminder
{
    const string first_partition = "0fbbbd40-1380-4a9d-bbbe-7b46b324f537";
    const string second_partition = "db5fa43e-32eb-4481-9e74-d13b7a501ab3";

    void Establish()
    {
        state.FailPartition(first_partition, 42, Array.Empty<string>(), string.Empty);
        state.FailPartition(second_partition, 43, Array.Empty<string>(), string.Empty);
        event_sequence.Setup(_ => _.GetNextSequenceNumber()).Returns(Task.FromResult((EventSequenceNumber)84));
    }

    async Task Because() => await observer.ReceiveReminder(Observer.RecoverReminder, new TickStatus());

    [Fact] void should_start_recovering_first_partition() => state.IsRecoveringPartition(first_partition);
    [Fact] void should_start_recovering_second_partition() => state.IsRecoveringPartition(second_partition);
    [Fact] void should_subscribe_to_sequences_stream_for_both_partitions() => stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()), Exactly(2));
    [Fact] void should_subscribe_with_offset_at_first_failed_partition_sequence_number() => subscribed_tokens[0].SequenceNumber.ShouldEqual((long)state.FailedPartitions.ToArray()[0].SequenceNumber.Value);
    [Fact] void should_subscribe_with_event_types_for_first_failed_partition() => subscribed_tokens[0].EventTypes.ShouldEqual(event_types);
    [Fact] void should_subscribe_with_offset_at_second_failed_partition_sequence_number() => subscribed_tokens[1].SequenceNumber.ShouldEqual((long)state.FailedPartitions.ToArray()[1].SequenceNumber.Value);
    [Fact] void should_subscribe_with_event_types_for_second_failed_partition() => subscribed_tokens[1].EventTypes.ShouldEqual(event_types);
}
