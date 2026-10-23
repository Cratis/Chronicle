// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_an_event_fails : given.a_page_job_step
{
    void Establish() => _pageSubscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
        .Returns(ObserverSubscriberResult.Failed((EventSequenceNumber)1UL, "boom") with { FailedPartition = (Key)"feature", FailedAt = 2UL });

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_attribute_the_failure_to_its_partition_and_sequence() => _observer.Received(1).PartitionFailed((Key)"feature", (EventSequenceNumber)2UL, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), FailureKind.Handling);
    [Fact] void should_checkpoint_only_the_successful_prefix() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)1UL);
    [Fact] void should_not_fall_back_to_partition_calls() => _handledBatches.ShouldBeEmpty();
}
