// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

/// <summary>
/// When the failed partition cannot be recorded either, the failure is handed to the job as transient, so the job runs
/// the step again and reports the partition as failed when the attempts run out (Cratis/Chronicle#4954).
/// </summary>
public class and_an_unexpected_exception_is_thrown_and_the_observer_cannot_be_told : given.a_performing_job_step
{
    Catch<JobStepResult> _result;

    void Establish()
    {
        _eventTypesStorage
            .GetFor(Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromException<IEnumerable<EventTypeSchema>>(new InvalidOperationException("storage is down")));
        _observer
            .PartitionFailed(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>())
            .Returns(Task.FromException(new InvalidOperationException("observer is unavailable")));
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact]
    void should_end_the_step_with_a_failure_the_job_reports_when_the_attempts_run_out()
    {
        _result.TryGetResult(out var stepResult).ShouldBeTrue();
        TransientTimeouts.TryGetTransientFailure(stepResult, new System.Text.Json.JsonSerializerOptions(), out var failure).ShouldBeTrue();
        failure.FailedAt.ShouldEqual(_performState.StartEventSequenceNumber);
    }
}
