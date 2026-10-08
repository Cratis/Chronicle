// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_resuming;

/// <summary>
/// The observer resumes a stopped replay job from inside its own Replay transition, so the observer's turn is waiting on
/// this job. Waiting on the observer from here would deadlock both until the call timed out (Cratis/Chronicle#4514).
/// </summary>
public class and_the_observer_is_entering_replay : given.a_replay_observer_job
{
    readonly TaskCompletionSource<JobId> _observerReplay = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _resumedWhileObserverWasBusy;
    bool _replayResumedWhileObserverWasBusy;

    void Establish()
    {
        _stateStorage.State.Request = _request;
        _observer.GetState().Returns(ObserverState.Empty with { RunningState = ObserverRunningState.Replaying });
        _observer.Replay().Returns(_observerReplay.Task);
    }

    async Task Because()
    {
        var resuming = _job.ResumeForTesting();
        _resumedWhileObserverWasBusy = resuming.IsCompleted;
        _replayResumedWhileObserverWasBusy = _replayServiceClient.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == nameof(IObserverServiceClient.ResumeReplayFor));

        _observerReplay.SetResult(JobId.New());
        await resuming;
    }

    [Fact] void should_not_wait_for_the_observer() => _resumedWhileObserverWasBusy.ShouldBeTrue();
    [Fact] void should_resume_the_replay_without_waiting_for_the_observer() => _replayResumedWhileObserverWasBusy.ShouldBeTrue();
    [Fact] async Task should_not_call_into_the_observer_s_turn() => await _observer.DidNotReceive().Replay();
}
