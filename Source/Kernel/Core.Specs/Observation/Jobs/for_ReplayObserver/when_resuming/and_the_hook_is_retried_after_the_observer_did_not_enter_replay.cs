// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_resuming;

/// <summary>
/// Cratis.Orleans 1.31.0 holds step dispatch when the resume hook fails and re-runs the hook from the job watchdog, so the
/// hook has to leave the sinks alone when it fails and complete when the observer has entered replay (Cratis/Chronicle#5401).
/// </summary>
public class and_the_hook_is_retried_after_the_observer_did_not_enter_replay : given.a_replay_observer_job
{
    Exception? _firstError;
    Exception? _secondError;
    bool _replayResumedAfterFirstAttempt;

    void Establish()
    {
        _stateStorage.State.Request = _request;
        _observer.GetState().Returns(
            ObserverState.Empty with { RunningState = ObserverRunningState.Active },
            ObserverState.Empty with { RunningState = ObserverRunningState.Replaying });
        _observer.Replay().Returns(JobId.NotSet);
    }

    async Task Because()
    {
        _firstError = await Catch.Exception(_job.ResumeForTesting);
        _replayResumedAfterFirstAttempt = _replayServiceClient.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == nameof(IObserverServiceClient.ResumeReplayFor));
        _secondError = await Catch.Exception(_job.ResumeForTesting);
    }

    [Fact] void should_fail_the_first_attempt() => _firstError.ShouldBeOfExactType<ObserverDidNotEnterReplay>();
    [Fact] void should_not_switch_the_sinks_into_replay_on_the_first_attempt() => _replayResumedAfterFirstAttempt.ShouldBeFalse();
    [Fact] void should_complete_the_retry() => _secondError.ShouldBeNull();
    [Fact] async Task should_switch_the_sinks_into_replay_once_the_retry_completes() => await _replayServiceClient.Received(1).ResumeReplayFor(Arg.Any<ObserverDetails>());
}
