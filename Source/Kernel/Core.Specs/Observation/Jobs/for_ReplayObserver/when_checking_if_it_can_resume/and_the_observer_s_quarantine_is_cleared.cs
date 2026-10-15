// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_checking_if_it_can_resume;

/// <summary>
/// A resume refused while the observer is quarantined leaves the job stopped. Once the quarantine is cleared the observer
/// enters replay and resumes the job itself, which must then go through without waiting on the observer (Cratis/Chronicle#4514).
/// </summary>
public class and_the_observer_s_quarantine_is_cleared : for_ReplayObserver.given.a_replay_observer_job
{
    readonly TaskCompletionSource<JobId> _observerReplay = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ObserverRunningState _runningState = ObserverRunningState.Quarantined;
    bool _couldResumeWhileQuarantined;
    bool _canResumeOnceCleared;
    bool _resumedWithoutWaiting;

    void Establish()
    {
        _stateStorage.State.Request = _request;
        _observer.IsSubscribed().Returns(true);
        _observer.GetState().Returns(_ => ObserverState.Empty with { RunningState = _runningState });
        _observer.Replay().Returns(_observerReplay.Task);
    }

    async Task Because()
    {
        _couldResumeWhileQuarantined = await _job.CanResumeForTesting();

        // The observer marks itself replaying before its Replay entry resumes the stopped job.
        _runningState = ObserverRunningState.Replaying;
        _canResumeOnceCleared = await _job.CanResumeForTesting();
        var resuming = _job.ResumeForTesting();
        _resumedWithoutWaiting = resuming.IsCompleted;
        await resuming;
    }

    [Fact] void should_not_resume_while_quarantined() => _couldResumeWhileQuarantined.ShouldBeFalse();
    [Fact] void should_resume_once_the_observer_is_replaying() => _canResumeOnceCleared.ShouldBeTrue();
    [Fact] void should_resume_without_waiting_for_the_observer() => _resumedWithoutWaiting.ShouldBeTrue();
    [Fact] async Task should_switch_the_sinks_into_replay() => await _replayServiceClient.Received(1).ResumeReplayFor(Arg.Any<ObserverDetails>());
    [Fact] async Task should_not_call_into_the_observer_s_turn() => await _observer.DidNotReceive().Replay();
}
