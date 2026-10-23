// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_the_page_call_throws : given.a_page_job_step
{
    Catch<JobStepResult> _result;

    void Establish() => _pageSubscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
        .Returns(Task.FromException<ObserverSubscriberResult>(new InvalidOperationException("page failed")));

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_not_resend_any_run() => _pageSubscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_leave_the_checkpoint_unchanged() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_report_any_handled_events() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.ReportHandledEvents) || call.GetMethodInfo().Name == nameof(IObserver.ReportHandledEventsBatch)).ShouldEqual(0);
    [Fact] void should_not_report_a_partition_failure_yet() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.PartitionFailed)).ShouldEqual(0);
    [Fact] void should_end_with_a_transient_failure()
    {
        _result.TryGetResult(out var stepResult).ShouldBeTrue();
        TransientTimeouts.TryGetTransientFailure(stepResult, new System.Text.Json.JsonSerializerOptions(), out var failure).ShouldBeTrue();
        failure.Partition.ShouldEqual((Key)"module");
        failure.FailedAt.ShouldEqual((EventSequenceNumber)1UL);
    }
}
