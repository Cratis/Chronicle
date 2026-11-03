// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

/// <summary>
/// Progress from slices the subscriber acknowledged is kept. Only the slice that timed out is lost, and that slice
/// is the one a retry delivers again; it may be applied twice if the abandoned call still completes.
/// </summary>
public class and_a_later_slice_of_a_cursor_batch_does_not_answer_within_the_timeout : given.a_performing_job_step
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _neverAnswers = new();

    void Establish()
    {
        _observersConfig = new Observers { SubscriberTimeout = 1 };
        _jobStep.TransientRetryDelay = TimeSpan.Zero;
        _jobStep.MaxEventsPerBatch = 2;
        _eventCursor.Current.Returns(Enumerable.Range(1, 4).Select(_ => AppendedEvent.EmptyWithEventSequenceNumber((ulong)_)).ToArray());
        _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(
                _ => Task.FromResult(ObserverSubscriberResult.Ok(2UL)),
                _ => _neverAnswers.Task);
    }

    Catch<JobStepResult> _result;

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    void Destroy() => _neverAnswers.TrySetCanceled();

    [Fact]
    void should_not_report_the_partition_as_failed_yet() =>
        _observer.DidNotReceive().PartitionFailed(
            Arg.Any<Key>(),
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<string>(),
            Arg.Any<FailureKind>());

    [Fact]
    void should_keep_the_progress_of_the_acknowledged_slice()
    {
        _observer.Received(1).ReportHandledEvents(
            Arg.Any<Key>(),
            Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>(),
            Arg.Any<JobStepId>(),
            (EventSequenceNumber)2UL);
    }

    [Fact]
    void should_retry_only_the_slice_that_timed_out() =>
        string.Join('|', _observerSubscriber.ReceivedCalls()
            .Where(_ => _.GetMethodInfo().Name == "OnNext")
            .Select(_ => string.Join(',', ((IEnumerable<AppendedEvent>)_.GetArguments()[1]!).Select(e => e.Context.SequenceNumber.Value))))
            .ShouldEqual("1,2|3,4|3,4");

    [Fact]
    void should_fail_at_the_first_event_of_the_slice_that_timed_out()
    {
        _result.TryGetResult(out var stepResult).ShouldBeTrue();
        TransientTimeouts.TryGetTransientFailure(stepResult, new System.Text.Json.JsonSerializerOptions(), out var failure).ShouldBeTrue();
        failure.FailedAt.ShouldEqual((EventSequenceNumber)3UL);
    }
}
