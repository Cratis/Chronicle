// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_the_subscriber_fails_after_partial_progress : given.a_performing_job_step
{
    Catch<JobStepResult> _result;

    void Establish()
    {
        _eventCursor.Current.Returns([
            AppendedEvent.EmptyWithEventSequenceNumber(42),
            AppendedEvent.EmptyWithEventSequenceNumber(43)]);
        _observerSubscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(Task.FromResult(new ObserverSubscriberResult(ObserverSubscriberState.Failed, new(42), ["boom"], "stack")));
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_report_the_failure_at_the_first_unhandled_event() => _observer.Received(1).PartitionFailed(Arg.Any<Key>(), new(43), Arg.Any<IEnumerable<string>>(), "stack", FailureKind.Handling);
    [Fact] void should_record_the_handled_prefix() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(new EventSequenceNumber(42));
    [Fact] void should_count_only_the_handled_prefix() => _observer.Received(1).ReportHandledEvents(Arg.Any<Key>(), Arg.Is<IReadOnlyDictionary<EventTypeId, EventCount>>(counts => counts.Values.Sum(count => (long)count.Value) == 1), Arg.Any<JobStepId>(), new(42));
    [Fact] void should_fail_without_cancelling() => _result.Match(result => result.TryGetError(out var error) && !error.Cancelled, _ => false).ShouldBeTrue();
}
