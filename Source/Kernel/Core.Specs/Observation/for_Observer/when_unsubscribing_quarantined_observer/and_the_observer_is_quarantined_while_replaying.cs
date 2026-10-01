// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_unsubscribing_quarantined_observer;

public class and_the_observer_is_quarantined_while_replaying : given.a_reactivated_quarantined_observer
{
    JobId _replayJob;

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { IsReplaying = true };
        _replayJob = JobId.New();
        _jobsManager.GetJobsOfType<IReplayObserver, ReplayObserverRequest>()
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(new JobState
            {
                Id = _replayJob,
                Status = JobStatus.Stopped,
                Request = new ReplayObserverRequest(_observerKey, ObserverType.Projection, [])
            })));
        _jobsManager.ClearReceivedCalls();
        _eventSequence.ClearReceivedCalls();
    }

    async Task Because() => await _observer.Unsubscribe();

    [Fact] async Task should_stay_quarantined() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_quarantined_running_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_replay_flag() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] async Task should_not_read_the_tail_for_a_routing_pass() => await _eventSequence.DidNotReceive().GetTailSequenceNumber();
    [Fact] async Task should_not_start_a_replay_job() => await _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] async Task should_not_resume_the_paused_replay_job() => await _jobsManager.DidNotReceive().Resume(_replayJob);
}
