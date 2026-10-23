// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_the_first_event_fails : given.a_page_job_step
{
    void Establish() => _pageSubscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
        .Returns(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "boom") with { FailedPartition = (Key)"module", FailedAt = 1UL });

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_report_exactly_one_partition_failure() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.PartitionFailed)).ShouldEqual(1);
    [Fact] void should_not_count_any_events() => _observer.DidNotReceive().ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Any<IReadOnlyList<HandledEventsReport>>());
    [Fact] void should_attribute_the_failure_to_its_partition_and_sequence() => _observer.Received(1).PartitionFailed((Key)"module", (EventSequenceNumber)1UL, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), FailureKind.Handling);
    [Fact] void should_checkpoint_only_the_successful_prefix() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_fall_back_to_partition_calls() => _handledBatches.ShouldBeEmpty();
}
