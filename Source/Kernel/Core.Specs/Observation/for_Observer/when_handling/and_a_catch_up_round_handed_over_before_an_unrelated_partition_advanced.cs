// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// Handover retains the held-back event as pending work before an unrelated partition can advance past it.
/// </summary>
public class and_a_catch_up_round_handed_over_before_an_unrelated_partition_advanced : given.an_observer_with_skip_detection
{
    readonly TaskCompletionSource<Result<JobId, StartJobError>> _heldStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task _catchUp;
    int _reportsBeforeCrossing;

    async Task Establish()
    {
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(
                _ => _heldStart.Task,
                _ => Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)103UL);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 102UL };
        await _observer.PartitionFailed("P", 100UL, ["Failed handler"], string.Empty);
        _subscriber.ClearReceivedCalls();
    }

    async Task Because()
    {
        _catchUp = _observer.CatchUp();
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 102UL)]);
        await _observer.CaughtUp(JobId.NotSet, 101UL, 101UL);
        _reportsBeforeCrossing = _suspectedSkips;
        await _observer.FailedPartitionRecovered("P", 103UL);
        _heldStart.SetResult(Result<JobId, StartJobError>.Success(JobId.New()));
        await _catchUp;
    }

    [Fact] void should_not_report_before_the_position_crosses_the_event() => _reportsBeforeCrossing.ShouldEqual(0);
    [Fact] void should_not_report_a_skip_that_was_prevented() => _metrics.For(EventsSuspectedSkipped).ShouldBeEmpty();
    [Fact] void should_keep_the_held_back_event_pending() => _stateStorage.State.CatchingUpPartitions.ShouldContain((Key)"Q");
}
