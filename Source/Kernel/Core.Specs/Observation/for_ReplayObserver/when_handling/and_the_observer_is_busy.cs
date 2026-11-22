// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_ReplayObserver.when_handling;

/// <summary>
/// The observer grain also serves client subscriptions, so a call to it can queue behind them for longer than a call may
/// take (30 seconds in a real silo). The command does not call it: the replay is accepted as a job, at once, and the job
/// gives it to the observer when the observer is free.
/// </summary>
public class and_the_observer_is_busy : given.a_replay_command
{
    readonly TaskCompletionSource<JobId> _grainCall = new();

    void Establish() => _observer.Replay().Returns(_grainCall.Task);

    async Task Because()
    {
        // Fails with a timeout, not by hanging, when the command waits for the observer.
        _result = await _scenario.Execute(new ReplayObserver("some-event-store", "some-namespace", "some-observer", string.Empty))
            .WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy() => _grainCall.TrySetResult(JobId.New());

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_accept_the_replay_as_a_job() => _jobsManager.Received(1).Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
        Arg.Is<ObserverOperatorCommandRequest>(_ => _.Command == ObserverOperatorCommand.Replay));
    [Fact] void should_not_call_the_observer() => _observer.DidNotReceive().Replay();
}
