// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_replaying_while_catch_up_hands_over;

public class and_routing_starts_a_catch_up_job : given.an_observer_with_subscription
{
    static readonly JobId _concludedCatchUpJob = JobId.New();
    static readonly JobId _successorCatchUpJob = JobId.New();
    static readonly JobId _replayJob = JobId.New();

    readonly TaskCompletionSource<EventSequenceNumber> _tail = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _routingReadsTail = new(TaskCreationOptions.RunContinuationsAsynchronously);
    JobId _result = JobId.NotSet;

    void Establish()
    {
        // The observer is behind once the concluded job has handled event 1, so routing starts a successor catch-up.
        _eventSequence.GetTailSequenceNumber().Returns(_ =>
        {
            _routingReadsTail.TrySetResult();
            return _tail.Task;
        });
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)2UL);
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_successorCatchUpJob)));
        _jobsManager
            .Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_replayJob)));
    }

    async Task Because()
    {
        var caughtUp = _observer.CaughtUp(_concludedCatchUpJob, 1UL);
        await _routingReadsTail.Task;

        var replay = _observer.Replay();

        _tail.SetResult(2UL);
        await caughtUp;
        _result = await replay;
    }

    [Fact] void should_return_the_started_replay_job() => _result.ShouldEqual(_replayJob);
    [Fact] void should_be_replaying() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);

    [Fact]
    void should_start_the_replay_after_routing_started_the_catch_up() => Received.InOrder(() =>
    {
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
        _jobsManager.Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    });
}
