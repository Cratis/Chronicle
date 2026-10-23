// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing_a_bucket;

public class and_a_page_reports_an_authoritative_failure : given.a_page_job_step
{
    void Establish()
    {
        _performState.Bucket = new(0, 1, 1);
        _eventCursor.Current.Returns([CreateEvent(1UL, "module"), CreateEvent(2UL, "feature"), CreateEvent(3UL, "module"), CreateEvent(4UL, "feature"), CreateEvent(5UL, "module")]);
        _pageSubscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(call =>
        {
            var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
            return events.Any(@event => @event.Context.SequenceNumber == (EventSequenceNumber)2UL)
                ? ObserverSubscriberResult.Failed((EventSequenceNumber)1UL, "bad feature") with { FailedPartition = (Key)"feature", FailedAt = 2UL }
                : ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber);
        });
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_continue_with_only_the_unhandled_healthy_suffix() => _pageSubscriber.Received(1).OnNextPage(Arg.Is<IEnumerable<AppendedEvent>>(events => events.Select(@event => @event.Context.SequenceNumber.Value).SequenceEqual(new ulong[] { 3, 5 })), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_report_the_failed_partition() => _observer.Received(1).PartitionFailed((Key)"feature", (EventSequenceNumber)2UL, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), FailureKind.Handling, failurePolicy: Arg.Any<WebhookDeliveryFailurePolicy>());
    [Fact] void should_checkpoint_the_later_healthy_events() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)5UL);
    [Fact] void should_not_bypass_page_dispatch() => _handledBatches.ShouldBeEmpty();
}
