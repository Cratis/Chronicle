// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_reminded;

public class and_two_failed_partitions : given.an_observer_with_event_types_a_reminder_and_two_failing_partitions
{
    void Establish() => event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, event_types, null)).Returns(Task.FromResult((EventSequenceNumber)83));

    async Task Because() => await observer.ReceiveReminder(Observer.RecoverReminder, new TickStatus());

    [Fact] void should_start_recovering_first_partition() => state.IsRecoveringPartition(first_partition).ShouldBeTrue();
    [Fact] void should_start_recovering_second_partition() => state.IsRecoveringPartition(second_partition).ShouldBeTrue();
    [Fact] void should_subscribe_to_sequences_stream_for_first_partition() => subscribed_tokens[0].Partition.Value.ShouldEqual(first_partition);
    [Fact] void should_subscribe_to_sequences_stream_for_second_partitions() => subscribed_tokens[1].Partition.Value.ShouldEqual(second_partition);
    [Fact] void should_subscribe_with_offset_at_first_failed_partition_sequence_number() => subscribed_tokens[0].SequenceNumber.ShouldEqual((long)state.FailedPartitions.ToArray()[0].SequenceNumber.Value);
    [Fact] void should_subscribe_with_event_types_for_first_failed_partition() => subscribed_tokens[0].EventTypes.ShouldEqual(event_types);
    [Fact] void should_subscribe_with_offset_at_second_failed_partition_sequence_number() => subscribed_tokens[1].SequenceNumber.ShouldEqual((long)state.FailedPartitions.ToArray()[1].SequenceNumber.Value);
    [Fact] void should_subscribe_with_event_types_for_second_failed_partition() => subscribed_tokens[1].EventTypes.ShouldEqual(event_types);
}
