// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_the_failure_has_no_page_position : given.a_page_job_step
{
    void Establish()
    {
        _eventCursor.Current.Returns([CreateEvent(7UL, "module")]);
        _pageSubscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "boom") with { FailedPartition = (Key)"feature" });
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_keep_the_inferred_failure_position() => _observer.Received(1).PartitionFailed((Key)"module", (EventSequenceNumber)7UL, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), FailureKind.Handling);
    [Fact] void should_report_exactly_one_partition_failure() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.PartitionFailed)).ShouldEqual(1);
    [Fact] void should_not_advance_the_checkpoint() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
