// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// Events held back while a catch-up round is preparing belong to that round until it hands over. When the round concludes without reaching the event, the handover reports exactly one suspected skip.
/// </summary>
public class and_a_catch_up_is_preparing_and_the_round_did_not_reach_the_held_back_event : given.an_observer_with_skip_detection
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

        await _observer.CaughtUp(JobId.NotSet, 101UL, 101UL);
        _heldStart.SetResult(Result<JobId, StartJobError>.Success(JobId.New()));
        await _catchUp;
    }

    [Fact] void should_not_measure_before_the_round_hands_over() => _measurementsBeforeHandover.ShouldEqual(0);
    [Fact] void should_not_warn_before_the_round_hands_over() => _warningsBeforeHandover.ShouldEqual(0);
    [Fact] void should_measure_exactly_one_suspected_skip_at_handover() => _metrics.For(EventsSuspectedSkipped).Select(_ => _.Value).SequenceEqual([1]).ShouldBeTrue();
    [Fact] void should_warn_once_at_handover() => _recordedLogs.SuspectedSkipWarnings.Count().ShouldEqual(1);
}
