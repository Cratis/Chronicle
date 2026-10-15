// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_resuming;

/// <summary>
/// An operator resumes a stopped replay job beside an observer that is still active. Its Replay request queues behind the
/// live batch it is handling; the shared sinks pick their container from replay mode, so switching them before the
/// observer has left live handling lets a live write land in the replay container (Cratis/Chronicle#4514).
/// </summary>
public class and_the_observer_is_handling_live_events : given.a_replay_observer_job
{
    readonly TaskCompletionSource<JobId> _liveHandlingTurn = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _resumedWhileObserverWasHandling;
    bool _sinksSwitchedWhileObserverWasHandling;
    Task _resuming;

    void Establish()
    {
        _stateStorage.State.Request = _request;
        _observer.GetState().Returns(ObserverState.Empty with { RunningState = ObserverRunningState.Active });
        _observer.Replay().Returns(_liveHandlingTurn.Task);
    }

    async Task Because()
    {
        _resuming = _job.ResumeForTesting();
        _resumedWhileObserverWasHandling = _resuming.IsCompleted;
        _sinksSwitchedWhileObserverWasHandling = _replayServiceClient.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == nameof(IObserverServiceClient.ResumeReplayFor));

        _liveHandlingTurn.SetResult(_jobId);
        await _resuming;
    }

    [Fact] void should_wait_for_the_observer_to_enter_replay() => _resumedWhileObserverWasHandling.ShouldBeFalse();
    [Fact] void should_not_switch_the_sinks_into_replay_while_the_observer_is_handling() => _sinksSwitchedWhileObserverWasHandling.ShouldBeFalse();
    [Fact] async Task should_switch_the_sinks_into_replay_once_the_observer_has_entered_replay() => await _replayServiceClient.Received(1).ResumeReplayFor(Arg.Any<ObserverDetails>());
    [Fact] void should_resume() => _resuming.IsCompletedSuccessfully.ShouldBeTrue();
}
