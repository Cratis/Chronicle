// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

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
        await _observer.CaughtUp(JobId.New(), 1UL).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var routing = _silo.RunCatchUpContinuation();
        Task<JobId>? replay = null;
        try
        {
            await _routingReadsTail.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            replay = _observer.Replay();

            // Observing holds its missed-events check while the second acknowledgement arrives.
            _routingTail.TrySetResult(EventSequenceNumber.Unavailable);
            await _observingReadsTail.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await _observer.CaughtUp(JobId.New(), 1UL).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _replayCompletedAfterSecondHandover = replay.IsCompleted;
        }
        finally
        {
            _routingTail.TrySetResult(EventSequenceNumber.Unavailable);
            _observingTail.TrySetResult(EventSequenceNumber.Unavailable);
            await routing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            if (replay is not null) _result = await replay.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
    }

    [Fact] void should_still_wait_after_the_second_handover_returned() => _replayCompletedAfterSecondHandover.ShouldBeFalse();
    [Fact] void should_return_the_started_replay_job() => _result.ShouldEqual(_replayJob);
    [Fact] void should_be_replaying() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);

    [Fact]
    async Task should_start_the_replay_once() =>
        await _jobsManager.Received(1).Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
