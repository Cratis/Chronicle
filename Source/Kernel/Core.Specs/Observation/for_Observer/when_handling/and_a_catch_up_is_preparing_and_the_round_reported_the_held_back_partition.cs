// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// Events held back while a catch-up round is preparing belong to that round, not to an unrelated partition completing; the round reported the partition, so nothing is suspected at handover either.
/// </summary>
public class and_a_catch_up_is_preparing_and_the_round_reported_the_held_back_partition : given.an_observer_with_skip_detection
{
    readonly TaskCompletionSource<Result<JobId, StartJobError>> _heldStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task _catchUp;
    int _measurementsBeforeHandover;
    int _warningsBeforeHandover;

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
        // The catch-up is still preparing in the background: Q@102 is held back, owned by the round.
        _catchUp = _observer.CatchUp();
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 102UL)]);

        // An unrelated partition completing moves the position past Q@102 and evaluates.
        await _observer.FailedPartitionRecovered("P", 103UL);
        _measurementsBeforeHandover = _metrics.For(EventsSuspectedSkipped).Count();
        _warningsBeforeHandover = _recordedLogs.SuspectedSkipWarnings.Count();
        await _observer.ReportHandledEvents("Q", new Dictionary<EventTypeId, EventCount> { [event_type.Id] = 1 });
        await _observer.CaughtUp(JobId.NotSet, 12UL, 12UL);
        _heldStart.SetResult(Result<JobId, StartJobError>.Success(JobId.New()));
        await _catchUp;
    }

    [Fact] void should_not_measure_before_the_round_hands_over() => _measurementsBeforeHandover.ShouldEqual(0);
    [Fact] void should_not_warn_before_the_round_hands_over() => _warningsBeforeHandover.ShouldEqual(0);
    [Fact] void should_not_measure_at_handover() => _suspectedSkips.ShouldEqual(0);
    [Fact] void should_not_warn_at_handover() => _recordedLogs.SuspectedSkipWarnings.ShouldBeEmpty();
}
