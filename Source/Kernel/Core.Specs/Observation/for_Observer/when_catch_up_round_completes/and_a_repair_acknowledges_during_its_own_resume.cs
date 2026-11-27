// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_a_repair_acknowledges_during_its_own_resume : given.an_observer_behind_a_repair_barrier
{
    readonly JobId _stoppedJob = JobId.New();
    bool _successorStartedInsideResumeTurn;

    void Establish()
    {
        var stopped = new JobState
        {
            Id = _stoppedJob,
            Status = JobStatus.Stopped,
            StatusChanges = [new() { Status = JobStatus.StartingSteps, Occurred = DateTimeOffset.UtcNow }],
            Request = new CatchUpObserverPartitionRequest(_observerKey, ObserverType.Reactor, "P", 11UL, []) { ToSequenceNumber = 13UL, IsHandoverRepair = true }
        };
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(stopped)));
        _jobsManager.Resume(_stoppedJob).Returns(async _ =>
        {
            await _observer.HandoverRepairCaughtUp("P", 11UL, 14UL, 13UL, succeeded: true);
            return true;
        });
    }

    async Task Because()
    {
        await _observer.CatchUp();
        await _silo.RunCatchUpContinuation().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _successorStartedInsideResumeTurn = _jobsManager.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Start");
        await _silo.RunCatchUpContinuation();
    }

    [Fact] void should_resume_the_existing_bounded_owner() => _jobsManager.Received(1).Resume(_stoppedJob);
    [Fact] void should_not_acquire_a_successor_in_the_acknowledgement_turn() => _successorStartedInsideResumeTurn.ShouldBeFalse();
    [Fact] void should_release_the_barrier_durably() => _stateStorage.State.PendingCatchUpRepairs!.Count.ShouldEqual(0);
    [Fact] void should_start_the_successor_on_a_later_turn() => _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
