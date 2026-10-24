// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_interleaved_partitions_cross_the_budget : given.a_bounded_replay_job_step
{
    protected override int Interval => 2;

    void Establish() => SetupPages([CreateEvent(1UL, "A"), CreateEvent(2UL, "B"), CreateEvent(3UL, "C"), CreateEvent(4UL, "A")]);

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_report_global_prefixes_not_partition_maxima() => _frontiersAtReport.ShouldEqual(new EventSequenceNumber[] { 2, 4 });
    [Fact] void should_confirm_each_prefix_before_reporting_it() => _durableAtReport.ShouldEqual(new EventSequenceNumber[] { 2, 4 });
    [Fact] void should_never_regress_durable_progress() => _durableAtReport.SequenceEqual(_durableAtReport.Order()).ShouldBeTrue();
    [Fact] void should_persist_only_monotonic_prefixes() => _jobStep.PersistedPositions.ShouldEqual(new EventSequenceNumber[] { 2, 4 });
    [Fact] void should_count_partition_a_twice() => Count("A").ShouldEqual(2);
    [Fact] void should_count_partition_b_once() => Count("B").ShouldEqual(1);
    [Fact] void should_count_partition_c_once() => Count("C").ShouldEqual(1);
    [Fact] void should_retain_partition_as_maximum() => Maximum("A").ShouldEqual((EventSequenceNumber)4UL);
    [Fact] void should_retain_partition_bs_maximum() => Maximum("B").ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_retain_partition_cs_maximum() => Maximum("C").ShouldEqual((EventSequenceNumber)3UL);

    int Count(Key partition) => _reports.Where(report => report.Partition == partition).SelectMany(report => report.Counts.Values).Sum(count => (int)count.Value);
    EventSequenceNumber Maximum(Key partition) => _reports.Where(report => report.Partition == partition).Max(report => report.HandledUpTo)!;
}
