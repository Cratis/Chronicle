// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_replaying_while_catch_up_hands_over;

/// <summary>
/// The second handover only schedules its routing into the first one's transition and returns at once. The routing it
/// asked for runs later, inside the first handover's chain, so the replay must keep waiting until that chain is done.
/// </summary>
public class and_another_handover_arrives_while_the_first_is_routing : given.an_observer_with_subscription
{
    static readonly JobId _replayJob = JobId.New();

    readonly TaskCompletionSource<EventSequenceNumber> _routingTail = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<EventSequenceNumber> _observingTail = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _routingReadsTail = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _observingReadsTail = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _replayCompletedAfterSecondHandover;
    JobId _result = JobId.NotSet;

    void Establish()
    {
        _eventSequence.GetTailSequenceNumber().Returns(
            _ =>
            {
                _routingReadsTail.TrySetResult();
                return _routingTail.Task;
            },
            _ =>
            {
                _observingReadsTail.TrySetResult();
                return _observingTail.Task;
            },
            _ => Task.FromResult(EventSequenceNumber.Unavailable));
        _jobsManager
            .Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_replayJob)));
    }

    async Task Because()
    {
        var firstHandover = _observer.CaughtUp(JobId.New(), 1UL);
        await _routingReadsTail.Task;

        var replay = _observer.Replay();

        // Routing moves on to Observing, which holds on its missed-events check while the second handover arrives.
        _routingTail.SetResult(EventSequenceNumber.Unavailable);
        await _observingReadsTail.Task;
        await _observer.CaughtUp(JobId.New(), 1UL);
        _replayCompletedAfterSecondHandover = replay.IsCompleted;

        _observingTail.SetResult(EventSequenceNumber.Unavailable);
        await firstHandover;
        _result = await replay;
    }

    [Fact] void should_still_wait_after_the_second_handover_returned() => _replayCompletedAfterSecondHandover.ShouldBeFalse();
    [Fact] void should_return_the_started_replay_job() => _result.ShouldEqual(_replayJob);
    [Fact] void should_be_replaying() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);

    [Fact]
    async Task should_start_the_replay_once() =>
        await _jobsManager.Received(1).Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
