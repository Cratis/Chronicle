// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_replaying_while_catch_up_hands_over;

public class and_the_handover_fails : given.an_observer_with_subscription
{
    static readonly JobId _concludedCatchUpJob = JobId.New();
    static readonly JobId _replayJob = JobId.New();

    readonly TaskCompletionSource<EventSequenceNumber> _tail = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _routingReadsTail = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception? _handoverError;
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

        _tail.SetException(new InvalidOperationException("Reading the tail failed"));
        _handoverError = await Cratis.Specifications.Catch.Exception(() => caughtUp);
        _result = await replay;
    }

    [Fact] void should_fail_the_handover() => _handoverError.ShouldNotBeNull();
    [Fact] void should_return_the_started_replay_job() => _result.ShouldEqual(_replayJob);
    [Fact] void should_be_replaying() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);

    [Fact]
    async Task should_start_the_replay_once() =>
        await _jobsManager.Received(1).Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
