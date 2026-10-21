// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

/// <summary>
/// A step that fails for a reason other than the subscriber's answer still records a failed partition, so the
/// partition is retried rather than the failure only living on the job step (Cratis/Chronicle#4954).
/// </summary>
public class and_an_unexpected_exception_is_thrown : given.a_performing_job_step
{
    void Establish() =>
        _eventTypesStorage
            .GetFor(Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromException<IEnumerable<EventTypeSchema>>(new InvalidOperationException("storage is down")));

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact]
    void should_report_the_partition_as_failed_where_the_step_started() =>
        _observer.Received(1).PartitionFailed(
            Arg.Any<Key>(),
            _performState.StartEventSequenceNumber,
            Arg.Is<IEnumerable<string>>(messages => messages.Contains("storage is down")),
            Arg.Any<string>(),
            Arg.Any<FailureKind>());
}
