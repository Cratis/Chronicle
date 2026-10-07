// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_replaying_while_catch_up_hands_over;

/// <summary>
/// CaughtUp interleaves, and the routing it starts schedules over any transition requested meanwhile. A replay requested
/// while that routing was in progress was replaced by routing's own choice of Observing and never ran, and the caller got
/// no job id back (Cratis/Chronicle#4514).
/// </summary>
public class and_routing_is_still_in_progress : given.an_observer_with_subscription
{
    static readonly JobId _concludedCatchUpJob = JobId.New();
    static readonly JobId _replayJob = JobId.New();

    readonly TaskCompletionSource<EventSequenceNumber> _tail = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _routingReadsTail = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _replayCompletedWhileRouting;
    bool _replayStartedWhileRouting;
    JobId _result = JobId.NotSet;

    void Establish()
    {
        _eventSequence.GetTailSequenceNumber().Returns(_ =>
        {
            _routingReadsTail.TrySetResult();
            return _tail.Task;
        });
        _jobsManager
            .Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_replayJob)));
    }

    async Task Because()
    {
        var caughtUp = _observer.CaughtUp(_concludedCatchUpJob, 1UL);
        await _routingReadsTail.Task;

        var replay = _observer.Replay();
        _replayCompletedWhileRouting = replay.IsCompleted;
        _replayStartedWhileRouting = _jobsManager.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == nameof(IJobsManager.Start));

        _tail.SetResult(EventSequenceNumber.Unavailable);
        await caughtUp;
        _result = await replay;
    }

    [Fact] void should_wait_for_routing_to_finish() => _replayCompletedWhileRouting.ShouldBeFalse();
    [Fact] void should_not_start_the_replay_while_routing() => _replayStartedWhileRouting.ShouldBeFalse();
    [Fact] void should_return_the_started_replay_job() => _result.ShouldEqual(_replayJob);
    [Fact] async Task should_be_in_the_replay_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Replay>();
    [Fact] void should_be_replaying() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);

    [Fact]
    async Task should_start_the_replay_once() =>
        await _jobsManager.Received(1).Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
