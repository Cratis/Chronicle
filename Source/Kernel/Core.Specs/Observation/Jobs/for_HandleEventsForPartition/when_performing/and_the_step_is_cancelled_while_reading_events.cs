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
/// Stopping a step cancels what it is reading with a plain <see cref="OperationCanceledException"/>. That ends the step
/// as cancelled; it is not a failure of the partition, so no failed partition is recorded and nothing is retried.
/// </summary>
public class and_the_step_is_cancelled_while_reading_events : given.a_performing_job_step
{
    readonly CancellationTokenSource _cancellation = new();
    Catch<JobStepResult> _result;

    void Establish()
    {
        _cancellation.Cancel();
        _eventTypesStorage
            .GetFor(Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromException<IEnumerable<EventTypeSchema>>(new OperationCanceledException(_cancellation.Token)));
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState, _cancellation.Token);

    [Fact] void should_end_the_step_as_cancelled() => _result.Match(result => result.TryGetError(out var error) && error.Cancelled, _ => false).ShouldBeTrue();

    [Fact]
    void should_not_report_the_partition_as_failed() =>
        _observer.DidNotReceive().PartitionFailed(
            Arg.Any<Key>(),
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<string>(),
            Arg.Any<FailureKind>());
}
