// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.in_replay;

public class and_reporting_handled_events : given.a_performing_job_step
{
    IReadOnlyList<HandledEventsReport> _reports;
    EventSequenceNumber _checkpointAtReport;

    protected override Configuration.Jobs CreateJobsConfig() => new() { StepCheckpointBatchInterval = 1 };

    void Establish() => _observer.ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Any<IReadOnlyList<HandledEventsReport>>())
        .Returns(call =>
        {
            _reports = call.ArgAt<IReadOnlyList<HandledEventsReport>>(1);
            _checkpointAtReport = _performState.LastSuccessfullyHandledEventSequenceNumber;
            return Task.CompletedTask;
        });

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_send_one_batch_for_the_page() => _observer.Received(1).ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Any<IReadOnlyList<HandledEventsReport>>());
    [Fact] void should_not_send_per_run_reports() => _observer.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IObserver.ReportHandledEvents)).ShouldEqual(0);
    [Fact] void should_record_the_page_checkpoint_before_reporting_counts() => _checkpointAtReport.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_include_each_partition_once() => _reports.Count.ShouldEqual(2);
    [Fact] void should_combine_the_repeated_partition_counts() => _reports.Single(r => r.Partition == (Key)"module").Counts.Values.Single().ShouldEqual((EventCount)2UL);
    [Fact] void should_keep_the_other_partition_count() => _reports.Single(r => r.Partition == (Key)"feature").Counts.Values.Single().ShouldEqual((EventCount)1UL);
    [Fact] void should_use_the_partition_maximum_sequence() => _reports.Single(r => r.Partition == (Key)"module").HandledUpTo.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_not_use_the_page_maximum_for_another_partition() => _reports.Single(r => r.Partition == (Key)"feature").HandledUpTo.ShouldEqual((EventSequenceNumber)2UL);
}
