// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_observer_filters;

/// <summary>
/// The filters apply per event, across partitions: each partition is delivered only its tagged events, in global
/// order, and a partition left with no tagged events is not delivered anything.
/// </summary>
public class and_events_are_spread_across_partitions : given.a_performing_job_step_with_observer_filters
{
    void Establish() => _eventCursor.Current.Returns([
        CreateTaggedEvent(1UL, "module", new Tag("audited")),
        CreateTaggedEvent(2UL, "feature"),
        CreateTaggedEvent(3UL, "module"),
        CreateTaggedEvent(4UL, "other", new Tag("audited")),
        CreateTaggedEvent(5UL, "feature")
    ]);

    Task Because() => PerformStep();

    [Fact] void should_deliver_two_batches() => _handledBatches.Count.ShouldEqual(2);
    [Fact] void should_deliver_to_the_module_partition_first() => _handledBatches[0].Partition.ShouldEqual((Key)"module");
    [Fact] void should_deliver_only_the_tagged_module_event() => _handledBatches[0].SequenceNumbers.ToArray().ShouldEqual([1UL]);
    [Fact] void should_deliver_to_the_other_partition_last() => _handledBatches[1].Partition.ShouldEqual((Key)"other");
    [Fact] void should_deliver_only_the_tagged_event_of_the_other_partition() => _handledBatches[1].SequenceNumbers.ToArray().ShouldEqual([4UL]);
    [Fact] void should_not_deliver_anything_to_the_partition_without_tagged_events() => _handledBatches.Exists(_ => _.Partition == (Key)"feature").ShouldBeFalse();
    [Fact] void should_report_the_last_delivered_event_as_handled() => _result.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)4UL);
    [Fact] void should_report_having_read_up_to_the_last_event() => _result.LastScannedEventSequenceNumber.ShouldEqual((EventSequenceNumber)5UL);
}
