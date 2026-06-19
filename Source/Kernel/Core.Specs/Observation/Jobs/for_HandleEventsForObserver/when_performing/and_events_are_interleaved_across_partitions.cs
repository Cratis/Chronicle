// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_events_are_interleaved_across_partitions : given.a_performing_job_step
{
    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_read_without_event_source_id_filter() => _eventSourceIdFilter.ShouldBeNull();
    [Fact] void should_handle_three_consecutive_partition_batches() => _handledBatches.Count.ShouldEqual(3);
    [Fact] void should_handle_first_module_partition_first() => _handledBatches[0].Partition.ShouldEqual((Key)"module");
    [Fact] void should_handle_first_module_event_first() => _handledBatches[0].SequenceNumbers[0].ShouldEqual((EventSequenceNumber)1UL);
    [Fact] void should_handle_feature_partition_second() => _handledBatches[1].Partition.ShouldEqual((Key)"feature");
    [Fact] void should_handle_feature_event_second() => _handledBatches[1].SequenceNumbers[0].ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_handle_second_module_partition_last() => _handledBatches[2].Partition.ShouldEqual((Key)"module");
    [Fact] void should_handle_second_module_event_last() => _handledBatches[2].SequenceNumbers[0].ShouldEqual((EventSequenceNumber)3UL);
}
