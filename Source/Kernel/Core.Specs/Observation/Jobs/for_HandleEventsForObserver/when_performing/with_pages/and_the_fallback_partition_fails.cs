// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_the_fallback_partition_fails : given.a_page_job_step
{
    void Establish()
    {
        _eventCompliance.Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>())
            .Returns(call => call.Arg<IEnumerable<AppendedEvent>>().Count() > 1
                ? Task.FromException<AppendedEvent[]>(new InvalidOperationException("page preparation failed"))
                : Task.FromResult(call.Arg<IEnumerable<AppendedEvent>>().ToArray()));
        _pageSubscriber.OnNext((Key)"feature", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "partition failed"));
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_not_dispatch_the_page() => _pageSubscriber.DidNotReceive().OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_report_the_exact_failing_partition() => _observer.Received(1).PartitionFailed((Key)"feature", (EventSequenceNumber)2UL, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), FailureKind.Handling);
    [Fact] void should_checkpoint_the_handled_prefix() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)1UL);
    [Fact] void should_not_dispatch_the_later_module_event() => _pageSubscriber.DidNotReceive().OnNext((Key)"module", Arg.Is<IEnumerable<AppendedEvent>>(events => events.Any(e => e.Context.SequenceNumber == (EventSequenceNumber)3UL)), Arg.Any<ObserverSubscriberContext>());
}
