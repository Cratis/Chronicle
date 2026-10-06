// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// CaughtUp is AlwaysInterleave and routes the observer, so an explicit Replay can arrive while the state machine is
/// still entering Routing. The state machine defers a transition requested during another one and returns at once,
/// so Replay reads the Replay state's LastStartedJobId before Replay.OnEnter has run and answers JobId.NotSet for a
/// replayable observer (Cratis/Chronicle#4514). Routing then replaces the scheduled replay with its own next state,
/// so the replay must still start once the observer settles - without Replay waiting for it, and over the same job.
/// </summary>
public class when_replaying_while_catch_up_is_routing_the_observer : given.an_observer_with_subscription
{
    static readonly JobId _replayJob = JobId.New();

    TaskCompletionSource<EventSequenceNumber> _tail;
    IImmutableList<JobState> _unfinishedJobs = ImmutableList<JobState>.Empty;
    bool _returnedBeforeRoutingCompleted;
    JobId _result;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(_ => Task.FromResult(_unfinishedJobs));
        _jobsManager
            .Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>())
            .Returns(callInfo =>
            {
                _unfinishedJobs = ImmutableList.Create(new JobState
                {
                    Id = _replayJob,
                    Status = JobStatus.PreparingJob,
                    Request = callInfo.Arg<ReplayObserverRequest>()
                });
                return Task.FromResult(Result<JobId, StartJobError>.Success(_replayJob));
            });

        // Holds Routing.OnEnter across an await, the way a storage round trip does in a running kernel.
        _tail = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _eventSequence.GetTailSequenceNumber().Returns(_ => _tail.Task);
    }

    async Task Because()
    {
        var caughtUp = _observer.CaughtUp(JobId.New(), EventSequenceNumber.First);
        var replay = _observer.Replay();
        _returnedBeforeRoutingCompleted = replay.IsCompleted;
        _result = await replay;
        _tail.SetResult(EventSequenceNumber.Unavailable);
        await caughtUp;
    }

    [Fact] void should_return_without_waiting_for_routing_to_complete() => _returnedBeforeRoutingCompleted.ShouldBeTrue();
    [Fact] void should_return_the_replay_job_it_started() => _result.ShouldEqual(_replayJob);
    [Fact] void should_start_the_replay_job_once() => _jobsManager.Received(1).Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] void should_be_replaying() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);
}
