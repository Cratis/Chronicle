// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

using ReadModelsConfiguration = Cratis.Chronicle.Configuration.ReadModels;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_the_page_result_is_deferred : given.a_page_job_step
{
    Catch<JobStepResult> _result;

    protected override ReadModelsConfiguration CreateReadModelsConfig() => new() { MaxBatchSize = 2 };

    void Establish() => _pageSubscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
        .Returns(call =>
        {
            _pages.Add(call.Arg<IEnumerable<AppendedEvent>>().ToArray());
            return new ObserverSubscriberResult(ObserverSubscriberState.Deferred, 1UL, ["not ok"], string.Empty)
            {
                FailedPartition = (Key)"feature",
                FailedAt = 2UL
            };
        });

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_fail_the_step() => _result.Match(result => result.TryGetError(out _), _ => false).ShouldBeTrue();
    [Fact] void should_stop_dispatching_pages() => _pages.Count.ShouldEqual(1);
    [Fact] void should_report_the_page_failure() => _observer.Received(1).PartitionFailed((Key)"feature", (EventSequenceNumber)2UL, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), FailureKind.Handling);
    [Fact] void should_report_exactly_one_partition_failure() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.PartitionFailed)).ShouldEqual(1);
    [Fact] void should_checkpoint_only_the_successful_prefix() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)1UL);
    [Fact] void should_count_only_the_handled_prefix() => _observer.Received(1).ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Is<IReadOnlyList<HandledEventsReport>>(reports => reports.Count == 1 && reports[0].Partition == (Key)"module" && reports[0].HandledUpTo == (EventSequenceNumber)1UL && reports[0].Counts.Values.Single() == (EventCount)1UL));
}
