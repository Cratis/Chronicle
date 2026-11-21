// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// A repair that cannot start must retain a durable failure and a resume position at the missed event.
/// </summary>
public class and_starting_a_missed_event_repair_fails : given.an_observer_with_skip_detection
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 102UL };
        await _observer.PartitionFailed("P", 100UL, ["Failed handler"], string.Empty);
        await _observer.FailedPartitionRecovered("P", 103UL);
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Task.FromException<Result<JobId, StartJobError>>(new InvalidOperationException("Synthetic job start failure")));
    }

    async Task Because() => await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 102UL)]);

    [Fact] void should_keep_the_resume_position_at_the_missed_event() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)102UL);
    [Fact] void should_keep_a_failed_partition_for_the_missed_event() => _failedPartitionsStorage.State.IsFailed((Key)"Q").ShouldBeTrue();
    [Fact] void should_not_leave_a_running_catch_up_marker() => _stateStorage.State.CatchingUpPartitions.ShouldNotContain((Key)"Q");
}
