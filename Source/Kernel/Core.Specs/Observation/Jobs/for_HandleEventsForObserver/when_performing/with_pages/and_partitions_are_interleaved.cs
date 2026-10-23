// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_partitions_are_interleaved : given.a_page_job_step
{
    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_dispatch_one_page() => _pages.Count.ShouldEqual(1);
    [Fact] void should_preserve_global_order() => _pages[0].Select(_ => _.Context.SequenceNumber).ShouldEqual(new EventSequenceNumber[] { 1, 2, 3 });
    [Fact] void should_not_call_the_partition_method() => _handledBatches.ShouldBeEmpty();
    [Fact] void should_checkpoint_the_page() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_report_each_partitions_maximum() => _observer.Received(1).ReportHandledEventsBatch(Arg.Any<Cratis.Orleans.Jobs.JobStepId>(), Arg.Is<IReadOnlyList<HandledEventsReport>>(reports => reports.Any(r => r.Partition == (Key)"module" && r.HandledUpTo == (EventSequenceNumber)3UL) && reports.Any(r => r.Partition == (Key)"feature" && r.HandledUpTo == (EventSequenceNumber)2UL)));
}
