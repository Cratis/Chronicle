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
/// Proves the step actually consults the observer's configured subscriber timeout - the knob existed for a long time
/// without anything ever reading it. The bounding itself is specified against
/// <see cref="ObserverSubscriberExtensions.OnNextWithin"/>; this is about the wiring.
/// </summary>
public class and_the_subscriber_does_not_answer_within_the_timeout : given.a_performing_job_step
{
    /// <summary>
    /// Slow on purpose - infinitely so - because the step giving up on it is the behavior under test.
    /// </summary>
    readonly TaskCompletionSource<ObserverSubscriberResult> _neverAnswers = new();

    void Establish()
    {
        _observersConfig = new Observers { SubscriberTimeout = 1 };
        _observerSubscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(_neverAnswers.Task);
    }

    Catch<JobStepResult> _result;

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    void Destroy() => _neverAnswers.TrySetCanceled();

    [Fact] void should_not_report_the_partition_as_failed_yet() =>
        _observer.DidNotReceive().PartitionFailed(
            Arg.Any<Key>(),
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<string>(),
            Arg.Any<FailureKind>());

    [Fact] void should_end_the_step_with_the_timeout_for_the_job_to_retry()
    {
        _result.TryGetResult(out var stepResult).ShouldBeTrue();
        TransientTimeouts.TryGetTransientFailure(stepResult, new System.Text.Json.JsonSerializerOptions(), out var failure).ShouldBeTrue();
        failure.Partition.ShouldEqual(_performState.Partition);
    }
}
