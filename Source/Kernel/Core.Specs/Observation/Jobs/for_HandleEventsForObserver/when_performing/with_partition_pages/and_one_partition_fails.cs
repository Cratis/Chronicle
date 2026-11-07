// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_partition_pages;

public class and_one_partition_fails : given.a_partition_page_job_step
{
    void Establish() =>
        _pageSubscriber.OnNextPartitions(Arg.Any<IReadOnlyList<ObserverPartitionBatch>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(new ObserverPageResult(new Dictionary<Key, ObserverSubscriberResult>
            {
                [(Key)"module"] = ObserverSubscriberResult.Ok(3UL),
                [(Key)"feature"] = ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "boom")
            }));

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_park_the_partition_that_failed_at_its_first_event() => _observer.Received(1).PartitionFailed((Key)"feature", (EventSequenceNumber)2UL, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), FailureKind.Handling);
    [Fact] void should_park_only_that_partition() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.PartitionFailed)).ShouldEqual(1);
    [Fact] void should_still_count_the_events_of_the_partition_that_succeeded() => _observer.Received().ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Is<IReadOnlyList<HandledEventsReport>>(reports => reports.Count == 1 && reports[0].Partition == (Key)"module" && reports[0].HandledUpTo == (EventSequenceNumber)3UL));
}
