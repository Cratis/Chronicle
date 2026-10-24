// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_interleaved_partitions_fit_the_budget : given.a_bounded_replay_job_step
{
    protected override int Interval => 5;

    void Establish() => SetupPages([CreateEvent(1UL, "A"), CreateEvent(2UL, "B"), CreateEvent(3UL, "C"), CreateEvent(4UL, "A")]);

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_report_one_global_frontier() => _frontiersAtReport.ShouldEqual(new EventSequenceNumber[] { 4 });
    [Fact] void should_not_force_the_remainder_checkpoint() => _durableAtReport.Single().ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_flush_the_global_frontier_at_completion() => _performState.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)4UL);
    [Fact] void should_only_persist_the_global_frontier() => _jobStep.PersistedPositions.ShouldEqual(new EventSequenceNumber[] { 4 });
    [Fact] void should_retain_exact_partition_counts() => _reports.Select(report => report.Counts.Values.Sum(count => (int)count.Value)).ShouldEqual([2, 1, 1]);
    [Fact] void should_retain_exact_partition_maxima() => _reports.Select(report => report.HandledUpTo).ShouldEqual(new EventSequenceNumber[] { 4, 2, 3 });
}
