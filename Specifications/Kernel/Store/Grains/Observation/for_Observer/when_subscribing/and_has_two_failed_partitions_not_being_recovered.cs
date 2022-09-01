// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_subscribing;

public class and_has_two_failed_partitions_not_being_recovered : given.an_observer_and_two_event_types
{
    const string first_partition = "fc0b6dad-774b-42e3-bf34-56422c617a30";
    const string second_partition = "79870d8c-1448-4362-9c94-66c014fc51b0";

    void Establish()
    {
        state.FailPartition(first_partition, 42, Array.Empty<string>(), string.Empty);
        state.FailPartition(second_partition, 43, Array.Empty<string>(), string.Empty);
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, event_types, null)).Returns(Task.FromResult((EventSequenceNumber)44));
    }

    async Task Because() => await observer.Subscribe(event_types, observer_namespace);

    [Fact] void should_attempt_recovery_on_first_partition() => state.IsRecoveringPartition(first_partition).ShouldBeTrue();
    [Fact] void should_attempt_recovery_on_second_partition() => state.IsRecoveringPartition(second_partition).ShouldBeTrue();
    [Fact] void should_subscribe_with_offset_where_first_failed_partition() => subscribed_tokens[0].SequenceNumber.ShouldEqual((long)state.FailedPartitions.ToArray()[0].SequenceNumber.Value);
    [Fact] void should_subscribe_with_offset_where_second_failed_partition() => subscribed_tokens[1].SequenceNumber.ShouldEqual((long)state.FailedPartitions.ToArray()[1].SequenceNumber.Value);
}
