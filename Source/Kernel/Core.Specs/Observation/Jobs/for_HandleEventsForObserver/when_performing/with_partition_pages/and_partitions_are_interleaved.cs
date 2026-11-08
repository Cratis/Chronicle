// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_partition_pages;

public class and_partitions_are_interleaved : given.a_partition_page_job_step
{
    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_deliver_everything_in_one_page() => _pages.Count.ShouldEqual(1);
    [Fact] void should_deliver_one_batch_per_partition() => _pages[0].Count.ShouldEqual(2);
    [Fact] void should_keep_the_events_of_a_partition_together() => _pages[0].Single(_ => _.Partition.Equals((Key)"module")).Events.Count.ShouldEqual(2);
    [Fact] void should_not_deliver_a_partition_at_a_time() => _handledBatches.ShouldBeEmpty();
    [Fact] void should_not_fail_any_partition() => _observer.DidNotReceive().PartitionFailed(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>());
    [Fact] void should_report_the_handled_events_of_each_partition() => _observer.Received().ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Is<IReadOnlyList<HandledEventsReport>>(reports => reports.Count == 2));
}
