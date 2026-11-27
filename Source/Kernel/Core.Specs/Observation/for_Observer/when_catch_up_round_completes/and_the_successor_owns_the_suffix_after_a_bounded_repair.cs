// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_the_successor_owns_the_suffix_after_a_bounded_repair : given.an_observer_behind_a_repair_barrier
{
    EventSequenceNumber _cursorWhileRepairing;
    bool _successorStartedBeforeAcknowledgement;

    void Establish()
    {
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(async _ =>
            {
                await DeliverSuccessor();
                return Result<JobId, StartJobError>.Success(JobId.New());
            });
    }

    async Task Because()
    {
        await _observer.CatchUp();
        await _silo.RunCatchUpContinuation();
        await _observer.Handle("P", [Event(14UL), Event(15UL), Event(16UL)]);
        await _observer.CatchUp();
        await _observer.RunWatchdogAsync();
        await _silo.RunCatchUpContinuation();
        _cursorWhileRepairing = _stateStorage.State.NextEventSequenceNumber;
        _successorStartedBeforeAcknowledgement = _jobsManager.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Start" && _.GetMethodInfo().GetGenericArguments().Contains(typeof(ICatchUpObserver)));
        await _observer.HandoverRepairCaughtUp("P", 11UL, 14UL, 13UL, succeeded: true);
        await _silo.RunCatchUpContinuation();
    }

    [Fact] void should_freeze_the_cursor_at_the_successor_start() => _cursorWhileRepairing.ShouldEqual((EventSequenceNumber)14UL);
    [Fact] void should_not_start_a_successor_before_durable_completion() => _successorStartedBeforeAcknowledgement.ShouldBeFalse();
    [Fact] void should_bound_the_repair_below_the_successor() => _jobsManager.Received(1).Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.FromSequenceNumber == (EventSequenceNumber)11UL && _.ToSequenceNumber == (EventSequenceNumber)13UL));
    [Fact] void should_start_the_successor_at_the_frozen_cursor() => _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Is<CatchUpObserverRequest>(_ => _.FromEventSequenceNumber == (EventSequenceNumber)14UL));
    [Fact] void should_deliver_the_whole_suffix_exactly_once() => _delivered.ShouldEqual([(EventSequenceNumber)14UL, (EventSequenceNumber)15UL, (EventSequenceNumber)16UL]);
    [Fact] void should_not_report_a_spurious_skip() => _suspectedSkips.ShouldEqual(0);
}
